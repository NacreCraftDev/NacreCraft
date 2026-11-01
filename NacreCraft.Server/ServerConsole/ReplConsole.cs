using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using NacreCraft.Game.Commands;
using NacreCraft.Game.Server;
using NacreCraft.Logging;

namespace NacreCraft.Server.ServerConsole;

//ReplConsole, the server console
//Interactively it reads key by key, supports inline editing, history, completion, and clears the prompt line before writing logs
//When input or output is redirected it degrades to a read loop, output is written straight through and no control sequence is emitted
//Submitted commands go through the server console command source, the same entry as the GUI console
public sealed class ReplConsole : IDisposable
{
    private const string Prompt = "> ";

    //HistoryCapacity history retention cap, aligned with the InputLine in-memory cap
    private const int HistoryCapacity = 1000;

    //Reverse search prompt, both the hit and miss forms, same as readline
    private const string SearchPrompt = "(reverse-i-search)`";
    private const string FailedSearchPrompt = "(failed reverse-i-search)`";
    //The segment between the query string and the matched text
    private const string SearchTail = "': ";
    //Gap between completion columns, the same three JLine puts between them
    private const int CandidateMargin = 3;

    private readonly object _lock = new();
    private readonly InputLine _input = new();
    private readonly DedicatedServer _server;
    private readonly TextWriter _output;
    private readonly bool _interactive;
    private readonly ICompletionSource? _completion;
    private readonly ICommandHighlighter? _highlighter;

    private Thread? _thread;
    private volatile bool _running;
    //_closing the console stopped reading, so no prompt is drawn again even while the shutdown keeps logging
    private volatile bool _closing;

    //_candidates the completion candidates currently kept above the prompt, null means none
    //The state is kept so it can be redrawn, when a log line arrives the candidates are drawn back with it, otherwise a fresh list is immediately wiped
    private IReadOnlyList<Completion>? _candidates;

    //_width the terminal width, the candidate list fills it with as many columns as fit
    //A host that cannot report one keeps the fallback, otherwise the grid would collapse to a single column and push
    //every later line off the screen
    private int _width = 80;

    //_display the drawn area, the same split JLine uses: this class decides what the area holds, the display owns the
    //terminal side and rewrites only that block
    private readonly TerminalDisplay _display;

    //_searching whether it is parked in Ctrl+R reverse search, keys use a different dispatch during search
    private bool _searching;
    private string _searchQuery = string.Empty;
    //_searchIndex the search cursor, moves backward from the end of history and points to the next position to compare
    private int _searchIndex;
    //_searchSavedText the input line from before entering search, restored on Esc
    private string _searchSavedText = string.Empty;
    //_searchMissed the last search missed, marked on the prompt, same as readline
    private bool _searchMissed;

    //_killBuffer the text the last kill command removed, restored by Ctrl+Y
    private string _killBuffer = string.Empty;

    private ReplConsole(DedicatedServer server, TextWriter output, bool interactive, ICompletionSource? completion,
        ICommandHighlighter? highlighter)
    {
        _server = server;
        _output = output;
        _interactive = interactive;
        _completion = completion;
        _highlighter = highlighter;
        _display = new TerminalDisplay();
    }

    //Start takes over the server console
    //In interactive mode the output is attached before the input, the reverse would let startup logs wipe the prompt
    //When both are redirected the output is not attached, piped content should stay as is
    public static ReplConsole Start(DedicatedServer server)
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        var repl = new ReplConsole(
            server,
            Console.Out,
            interactive,
            interactive ? new CommandCompletionSource(server) : null,
            interactive ? new CommandHighlighter(server) : null);

        if (interactive)
        {
            //Only interactive mode loads history and attaches the output, the redirected branch passes output through as is and has no use for history
            repl.LoadHistory();
            Console.SetOut(new ConsoleLineWriter(repl));
        }

        repl._running = true;
        repl._thread = new Thread(interactive ? repl.RunInteractive : repl.RunRedirected)
        {
            Name = "NacreCraft-Console",
            //Background thread, no need to wait for it once the server main loop exits
            IsBackground = true,
        };
        repl._thread.Start();
        return repl;
    }

    //HistoryPath where history is written, in the program root, maps to Paper's console console_history
    private static string HistoryPath => Path.Combine(AppPaths.BaseDirectory, "console_history");

    //LoadHistory loads the persisted session history and wires up subsequent writes
    //An unreadable history file must not block the console, any exception is swallowed and treated as no history
    private void LoadHistory()
    {
        _input.HistoryAppended = SaveHistoryLine;
        try
        {
            if (!File.Exists(HistoryPath)) return;

            var lines = File.ReadAllLines(HistoryPath);
            _input.LoadHistory(lines);
            //If the file is longer than the in-memory cap, trim once to keep it from growing without bound
            if (lines.Length > HistoryCapacity) File.WriteAllLines(HistoryPath, _input.History);
        }
        catch (Exception e)
        {
            Log.Warning($"Console history load failed: {e.Message}");
        }
    }

    //SaveHistoryLine appends to disk on every commit so it can be recalled on the next startup
    //A failed write only logs a warning, a command already dispatched should not be derailed by it
    private void SaveHistoryLine(string line)
    {
        try
        {
            File.AppendAllText(HistoryPath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Log.Warning($"Console history save failed: {e.Message}");
        }
    }

    //WriteLine the output point for a single kernel log line
    public void WriteLine(string text) => WriteLines([text]);

    //WriteLines writes a whole batch under one repaint
    //The log layer hands several lines over at once, and repainting the prompt once per line makes it flicker while a burst
    //arrives; erasing around the batch keeps it at one repaint per burst. Everything also lands in a single write, so the
    //terminal never shows a frame with the prompt already erased
    public void WriteLines(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0) return;

        if (!_interactive)
        {
            foreach (var line in lines)
            {
                _output.Write(line);
                _output.Write('\n');
            }
            _output.Flush();
            return;
        }

        lock (_lock)
        {
            //The area comes down, the log lines take its place and the area is drawn back, all inside one write
            //Once the console stopped reading there is no area to draw, so the shutdown log gets the whole screen
            var frame = _closing ? new Frame([], 0, 0) : BuildFrame();
            _display.PrintAbove(lines, frame.Lines, frame.CursorRow, frame.CursorColumn);
        }
    }

    public void Dispose()
    {
        //The key-reading thread may be blocked in ReadKey, it is a background thread so its exit is not awaited
        _running = false;
    }

    //RefreshWidth picks up a resize, the candidate grid is built from the terminal width
    private void RefreshWidth()
    {
        try
        {
            var width = Console.WindowWidth;
            if (width > 0) _width = width;
        }
        catch (IOException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    //RunInteractive the main path when input is a terminal
    private void RunInteractive()
    {
        lock (_lock)
        {
            RefreshWidth();
            Render();
        }

        //The console stops as soon as a shutdown starts, the same condition SimpleTerminalConsole uses
        //Waiting for Dispose would keep the prompt on screen through the whole shutdown, inviting input into a server
        //that is already on its way out
        while (_running && !_server.Stopping)
        {
            if (!Console.KeyAvailable)
            {
                //15ms per step, key latency is imperceptible and it does not spin and burn a core
                Thread.Sleep(15);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            lock (_lock) HandleKey(key);
        }

        lock (_lock)
        {
            _closing = true;
            _candidates = null;
            _display.EraseArea();
        }
    }

    //RunRedirected the degraded path when input is not a terminal
    //One command per line, reading null means the pipe end closed and the thread finishes on its own
    private void RunRedirected()
    {
        while (_running)
        {
            string? line;
            try
            {
                line = Console.ReadLine();
            }
            catch (IOException)
            {
                return;
            }

            if (line is null) return;
            Execute(line);
        }
    }

    //HandleKey key dispatch
    private void HandleKey(ConsoleKeyInfo key)
    {
        //Search mode has its own key map and switches from literal input to the search string, intercepted entirely here
        if (_searching)
        {
            HandleSearchKey(key);
            return;
        }

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                Submit();
                return;
            case ConsoleKey.Tab:
                Complete();
                return;
            case ConsoleKey.Backspace:
                _input.Backspace();
                break;
            case ConsoleKey.Delete:
                _input.Delete();
                break;
            case ConsoleKey.LeftArrow:
                _input.MoveLeft();
                break;
            case ConsoleKey.RightArrow:
                _input.MoveRight();
                break;
            case ConsoleKey.Home:
                _input.MoveHome();
                break;
            case ConsoleKey.End:
                _input.MoveEnd();
                break;
            case ConsoleKey.UpArrow:
                _input.HistoryUp();
                break;
            case ConsoleKey.DownArrow:
                _input.HistoryDown();
                break;
            case ConsoleKey.Escape:
                //Same as readline, discard the current line
                _input.Clear();
                break;
            //Emacs bindings, the same set a shell provides; without them a long command can only be edited one
            //character at a time
            case ConsoleKey.A when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.MoveHome();
                break;
            case ConsoleKey.E when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.MoveEnd();
                break;
            case ConsoleKey.B when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.MoveLeft();
                break;
            case ConsoleKey.F when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.MoveRight();
                break;
            case ConsoleKey.B when key.Modifiers.HasFlag(ConsoleModifiers.Alt):
                _input.MoveWordLeft();
                break;
            case ConsoleKey.F when key.Modifiers.HasFlag(ConsoleModifiers.Alt):
                _input.MoveWordRight();
                break;
            case ConsoleKey.P when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.HistoryUp();
                break;
            case ConsoleKey.N when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.HistoryDown();
                break;
            case ConsoleKey.K when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _killBuffer = _input.KillToEnd();
                break;
            case ConsoleKey.U when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _killBuffer = _input.KillToStart();
                break;
            case ConsoleKey.W when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _killBuffer = _input.DeleteWordBackward();
                break;
            case ConsoleKey.Y when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.Insert(_killBuffer);
                break;
            case ConsoleKey.D when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _input.Delete();
                break;
            case ConsoleKey.L when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                //Redraw the input and prompt after clearing the screen, the candidate rows are gone with the clear so the state is cleared too
                _candidates = null;
                _display.ClearScreen();
                Render();
                return;
            case ConsoleKey.R when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                //Ctrl+R enters reverse search, same route as readline
                BeginSearch();
                Render();
                return;
            default:
                //Control characters and function keys do not enter the input
                if (key.KeyChar < ' ' || key.KeyChar == '\u007F') return;
                _input.Insert(key.KeyChar);
                break;
        }

        //The input changed, the previous completion result is stale
        _candidates = null;
        Render();
    }

    //BeginSearch enters Ctrl+R reverse search, saves the current line to restore on Esc
    private void BeginSearch()
    {
        _searching = true;
        _searchQuery = string.Empty;
        //The cursor sits past the end of history so one Ctrl+R starts from the newest entry
        _searchIndex = _input.History.Count;
        _searchSavedText = _input.Text;
        _searchMissed = false;
        //Candidates left from the previous round are unrelated to search, let Render clear them
        _candidates = null;
    }

    //HandleSearchKey search-mode keys, the query string goes through _searchQuery and the input line only carries the match
    private void HandleSearchKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            //Another Ctrl+R continues searching backward from the current match
            SearchBackward();
            Render();
            return;
        }

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                //Accept the match and submit directly, same as readline's accept-line
                _searching = false;
                ResetSearch();
                Submit();
                return;
            case ConsoleKey.Escape:
                //Abandon the search, restore the line from before entering search
                _searching = false;
                _input.SetText(_searchSavedText);
                ResetSearch();
                Render();
                return;
            case ConsoleKey.Backspace:
                if (_searchQuery.Length > 0) _searchQuery = _searchQuery[..^1];
                //The query string changed, search again from the end
                SearchBackward(reset: true);
                Render();
                return;
            default:
                if (key.KeyChar < ' ' || key.KeyChar == '\u007F')
                {
                    //Other control keys end the search in place, the match stays on the input for further editing
                    _searching = false;
                    ResetSearch();
                    Render();
                    return;
                }
                _searchQuery += key.KeyChar;
                SearchBackward(reset: true);
                Render();
                return;
        }
    }

    //SearchBackward finds the first entry in history containing the query string
    //When reset is true it searches from the newest entry, otherwise it steps one further back from the current cursor, matching another Ctrl+R
    private void SearchBackward(bool reset = false)
    {
        var history = _input.History;
        //An empty query string stays put and does not count as a miss
        if (_searchQuery.Length == 0 || history.Count == 0)
        {
            _searchMissed = false;
            return;
        }

        var index = reset ? history.Count - 1 : _searchIndex - 1;
        while (index >= 0)
        {
            if (history[index].Contains(_searchQuery, StringComparison.Ordinal))
            {
                _searchIndex = index;
                _input.SetText(history[index]);
                _searchMissed = false;
                return;
            }
            index--;
        }

        //Reached the start without a match, the last match stays on the input and only the prompt is marked as missed
        _searchMissed = true;
    }

    //ResetSearch clears all intermediate search state
    private void ResetSearch()
    {
        _searchQuery = string.Empty;
        _searchIndex = 0;
        _searchSavedText = string.Empty;
        _searchMissed = false;
    }

    //Submit submits the current line
    private void Submit()
    {
        var line = _input.Commit();
        //Clear the candidate area on submit, the reply must follow this line directly and must not have the previous candidates in between
        _candidates = null;

        //An empty line is just an Enter press and should not leave an extra line on screen, drawing the prompt back as is suffices
        if (line is null)
        {
            Render();
            return;
        }

        //The submitted line goes out exactly like a log line, so it stays on screen above the reply, in one write with
        //the fresh prompt below it
        var frame = BuildFrame();
        _display.PrintAbove([$"{Prompt}{line}"], frame.Lines, frame.CursorRow, frame.CursorColumn);

        Execute(line);
    }

    //Complete inline completion, same scheme as readline
    //A single candidate is filled in directly, multiple candidates fill to the common prefix first and only list when no further fill is possible
    private void Complete()
    {
        if (_completion is null) return;

        var text = _input.Text;
        IReadOnlyList<Completion> candidates;
        try
        {
            candidates = _completion.GetCompletions(text, _input.Caret);
        }
        catch (Exception ex)
        {
            //The parser used for completion parses whatever it sees, it must not take down the console thread
            WriteLine($"Completion failed: {ex.GetType().Name} {ex.Message}");
            return;
        }

        if (candidates.Count == 0) return;

        if (candidates.Count == 1)
        {
            _input.SetText(candidates[0].Text);
            _candidates = null;
            Render();
            return;
        }

        var prefix = CommonPrefix(candidates);
        if (prefix.Length > text.Length)
        {
            _input.SetText(prefix);
            _candidates = null;
            Render();
            return;
        }

        //Multiple candidates that cannot fill to the common prefix stay above the prompt until the next edit
        _candidates = candidates;
        Render();
    }

    //Execute enqueues one command line for the main loop to pick up
    //A leading slash is accepted too, the vanilla terminal takes both spellings
    //Does not run directly on the console thread, crossing world state with the main loop causes problems, exception fallback also belongs to the main loop
    private void Execute(string line)
    {
        var text = line.Trim();
        if (text.StartsWith('/')) text = text[1..];
        if (text.Length == 0) return;
        _server.EnqueueConsoleCommand(ServerCommandSource.Console(_server), text);
    }

    //Render draws the area the console keeps on screen: the candidate grid when a list is shown, then the prompt line
    //The display compares this against what it last drew and stays silent when nothing moved, which is what an idle
    //prompt and a repeated Enter press rely on
    private void Render()
    {
        if (_closing) return;

        var frame = BuildFrame();
        _display.Update(frame.Lines, frame.CursorRow, frame.CursorColumn);
    }

    //Frame the lines the console occupies on screen together with where the cursor belongs inside them
    private readonly record struct Frame(List<string> Lines, int CursorRow, int CursorColumn);

    //BuildFrame works out what the console area currently holds
    private Frame BuildFrame()
    {
        RefreshWidth();

        var lines = new List<string>(_candidates?.Count ?? 1);
        if (_candidates is { Count: > 0 } candidates) AppendCandidateLines(lines, candidates);

        var promptLine = BuildPromptLine(out var cursorColumn);
        lines.Add(promptLine);

        return new Frame(lines, lines.Count - 1, cursorColumn);
    }

    //AppendCandidateLines lays the candidates out as a grid that fills the terminal width, the same layout JLine uses
    //Columns are as wide as the longest candidate plus a margin, and the count is walked back until adding a column would
    //overflow, then recomputed from the row count so nine candidates split five and four rather than six and three
    private void AppendCandidateLines(List<string> lines, IReadOnlyList<Completion> candidates)
    {
        var maxWidth = 0;
        foreach (var candidate in candidates)
        {
            var candidateWidth = DisplayWidth(candidate.Text);
            if (candidateWidth > maxWidth) maxWidth = candidateWidth;
        }

        //One column stays unused: a row that reaches the last column makes the terminal wrap it on its own, and the
        //cursor then sits one line lower than the display believes, which throws off every later move
        var width = Math.Max(_width - 1, 1);
        maxWidth = Math.Clamp(maxWidth, 1, width);

        var columns = width / maxWidth;
        while (columns > 1 && columns * maxWidth + (columns - 1) * CandidateMargin >= width) columns--;
        var rowCount = (candidates.Count + columns - 1) / columns;
        columns = (candidates.Count + rowCount - 1) / rowCount;

        for (var row = 0; row < rowCount; row++)
        {
            var builder = new StringBuilder();
            for (var column = 0; column < columns; column++)
            {
                //Columns fill top to bottom first, the order a shell completion list uses
                var index = column * rowCount + row;
                if (index >= candidates.Count) break;

                var candidate = candidates[index];
                builder.Append(LevelColor(candidate.Level));
                builder.Append(candidate.Text);
                //The padding goes after the reset, so the gap between columns is not painted in the command's colour
                builder.Append("\u001B[0m");
                if (column + 1 < columns)
                    builder.Append(' ', maxWidth - DisplayWidth(candidate.Text) + CandidateMargin);
            }

            lines.Add(builder.ToString());
        }
    }

    //BuildPromptLine returns the line the input sits on and reports the column the cursor belongs at
    private string BuildPromptLine(out int cursorColumn)
    {
        if (_searching)
        {
            //In search mode the prompt becomes the readline line, the cursor sits at the end of the query string rather than the matched line
            var head = (_searchMissed ? FailedSearchPrompt : SearchPrompt) + _searchQuery;
            cursorColumn = DisplayWidth(head);
            return head + SearchTail + _input.Text;
        }

        cursorColumn = DisplayWidth(Prompt) + DisplayWidth(_input.Text[.._input.Caret]);
        //The parts the command tree recognizes are colored by node, highlighting only inserts escapes between characters and the visible length is unchanged
        return Prompt + (_highlighter?.Highlight(_input.Text) ?? _input.Text);
    }

    //LevelColor picks the colour a candidate is drawn in, walking from green through amber to red as the permission the
    //command needs goes up, so a command that can do damage stands out before it is run
    //The bright variants are used because a console usually runs on a dark background, where the dim ones read as grey
    private static string LevelColor(int level) => level switch
    {
        <= 0 => "\u001B[92m",
        1 => "\u001B[96m",
        2 => "\u001B[93m",
        3 => "\u001B[38;5;208m",
        _ => "\u001B[91m",
    };

    //DisplayWidth counts the columns a string occupies
    //A wide character takes two columns and a combining mark none, so counting chars would leave the cursor short of
    //the input position whenever the line carries anything outside ASCII
    private static int DisplayWidth(string text)
    {
        var width = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark) continue;
            width += IsWide(rune.Value) ? 2 : 1;
        }
        return width;
    }

    //IsWide reports whether a code point occupies two columns, following the East Asian Wide and Fullwidth ranges
    private static bool IsWide(int codePoint) => codePoint switch
    {
        >= 0x1100 and <= 0x115F => true,
        >= 0x2E80 and <= 0x303E => true,
        >= 0x3041 and <= 0x33FF => true,
        >= 0x3400 and <= 0x4DBF => true,
        >= 0x4E00 and <= 0x9FFF => true,
        >= 0xA000 and <= 0xA4CF => true,
        >= 0xAC00 and <= 0xD7A3 => true,
        >= 0xF900 and <= 0xFAFF => true,
        >= 0xFE30 and <= 0xFE6F => true,
        >= 0xFF00 and <= 0xFF60 => true,
        >= 0xFFE0 and <= 0xFFE6 => true,
        >= 0x1F300 and <= 0x1F64F => true,
        >= 0x1F900 and <= 0x1F9FF => true,
        >= 0x20000 and <= 0x3FFFD => true,
        _ => false,
    };

    //CommonPrefix finds the common prefix of all candidates, for multiple candidates it fills to the divergence point
    private static string CommonPrefix(IReadOnlyList<Completion> values)
    {
        var prefix = values[0].Text;
        for (var i = 1; i < values.Count; i++)
        {
            var other = values[i].Text;
            var length = 0;
            while (length < prefix.Length && length < other.Length &&
                   char.ToLowerInvariant(prefix[length]) == char.ToLowerInvariant(other[length]))
            {
                length++;
            }

            prefix = prefix[..length];
            if (prefix.Length == 0) break;
        }

        return prefix;
    }
}
