using System.Globalization;
using System.IO;
using System.Text;

namespace NacreCraft.Server.ServerConsole;

//TerminalDisplay owns the few lines the console keeps on screen, the role org.jline.utils.Display plays for the Paper
//console: it remembers what is currently drawn and rewrites only that area, so a log line never clears more than it has
//to. Everything it emits is built into one buffer and handed over in a single write, because a terminal that gets to
//show the intermediate frames is exactly what the flicker looked like
internal sealed class TerminalDisplay
{
    //DECSET 2026: the terminal holds every change until the closing sequence, so a frame reaches the screen whole
    //Terminals that do not know it ignore the pair, which costs nothing
    private const string SyncStart = "\u001B[?2026h";
    private const string SyncEnd = "\u001B[?2026l";

    //The bare standard output stream rather than Console.Out
    //Console.Out is a StreamWriter with a buffer of about a thousand characters and AutoFlush on, so a frame larger than
    //that is handed to the terminal in pieces: the erased area arrives first, the candidates next, the prompt last, and
    //each of those was a frame on screen. Writing the bytes here keeps one frame to one system call
    private readonly Stream _stdout;
    private readonly Encoding _encoding;
    private readonly List<string> _drawn = new();
    private int _drawnRow;
    private int _drawnColumn;

    public TerminalDisplay()
    {
        _stdout = Console.OpenStandardOutput();
        _encoding = Console.OutputEncoding;
    }

    //Update replaces the drawn area with the given lines, the cursor ends at cursorRow and cursorColumn
    //Nothing at all is written when the new area matches the one already on screen, which is what keeps an idle prompt
    //from touching the terminal
    public void Update(IReadOnlyList<string> lines, int cursorRow, int cursorColumn)
    {
        if (Matches(lines, cursorRow, cursorColumn)) return;

        var buffer = new StringBuilder();
        AppendErase(buffer);
        AppendLines(buffer, lines, cursorRow, cursorColumn);

        Write(buffer);

        Remember(lines, cursorRow, cursorColumn);
    }

    //PrintAbove writes log lines above the drawn area and draws that area again, all in one write
    //The area is taken down before the log lines so they never land between the candidates and the prompt, and putting
    //it back inside the same write is what keeps the prompt from visibly vanishing and coming back; a flush in between
    //lets the terminal render that gap, which is the flash this avoids
    public void PrintAbove(IReadOnlyList<string> lines, IReadOnlyList<string> areaLines, int cursorRow,
        int cursorColumn)
    {
        if (lines.Count == 0) return;

        var buffer = new StringBuilder();
        AppendErase(buffer);

        foreach (var line in lines)
        {
            buffer.Append(line);
            buffer.Append('\n');
        }

        //An empty area means the console stopped reading, so only the log lines go out and nothing is drawn back
        if (areaLines.Count > 0) AppendLines(buffer, areaLines, cursorRow, cursorColumn);

        Write(buffer);

        Remember(areaLines, cursorRow, cursorColumn);
    }

    //EraseArea takes the drawn area off the screen and leaves nothing drawn
    //Used once the console stops reading: a prompt that stays up invites typing into a server that is already on its way out
    public void EraseArea()
    {
        if (_drawn.Count == 0) return;

        var buffer = new StringBuilder();
        AppendErase(buffer);
        Write(buffer);
        Clear();
    }

    //ClearScreen wipes the terminal and forgets the drawn area, used by Ctrl+L
    public void ClearScreen()
    {
        var buffer = new StringBuilder("\u001B[2J\u001B[H");
        Write(buffer);
        Clear();
    }

    //AppendErase writes the sequence that takes the drawn area off the screen, the cursor lands on the first drawn line
    //The cursor always sits inside the area, so moving up by its row lands there
    private void AppendErase(StringBuilder buffer)
    {
        if (_drawn.Count == 0) return;
        if (_drawnRow > 0) buffer.Append(CultureInfo.InvariantCulture, $"\u001B[{_drawnRow}A");
        buffer.Append("\r\u001B[J");
    }

    //Write hands one frame to the terminal, wrapped so it is rendered in one go
    private void Write(StringBuilder buffer)
    {
        if (buffer.Length == 0) return;

        buffer.Insert(0, SyncStart);
        buffer.Append(SyncEnd);

        var bytes = _encoding.GetBytes(buffer.ToString());
        _stdout.Write(bytes, 0, bytes.Length);
        _stdout.Flush();
    }

    //AppendLines writes the drawn area and leaves the cursor at the requested position inside it
    private static void AppendLines(StringBuilder buffer, IReadOnlyList<string> lines, int cursorRow, int cursorColumn)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            buffer.Append(lines[i]);
            if (i + 1 < lines.Count) buffer.Append('\n');
        }

        //Writing left the cursor at the end of the last line, so it is walked back up to the requested row
        var up = lines.Count - 1 - cursorRow;
        if (up > 0) buffer.Append(CultureInfo.InvariantCulture, $"\u001B[{up}A");
        buffer.Append('\r');
        if (cursorColumn > 0) buffer.Append(CultureInfo.InvariantCulture, $"\u001B[{cursorColumn}C");
    }

    //Clear forgets what is drawn, used after the screen itself was wiped
    public void Clear()
    {
        _drawn.Clear();
        _drawnRow = 0;
        _drawnColumn = 0;
    }

    private bool Matches(IReadOnlyList<string> lines, int cursorRow, int cursorColumn)
    {
        if (cursorRow != _drawnRow || cursorColumn != _drawnColumn) return false;
        if (lines.Count != _drawn.Count) return false;
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i], _drawn[i], StringComparison.Ordinal)) return false;
        }

        return true;
    }

    private void Remember(IReadOnlyList<string> lines, int cursorRow, int cursorColumn)
    {
        _drawn.Clear();
        _drawn.AddRange(lines);
        _drawnRow = cursorRow;
        _drawnColumn = cursorColumn;
    }
}
