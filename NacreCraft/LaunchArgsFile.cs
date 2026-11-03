using System.IO;

namespace NacreCraft;

//LaunchArgsFile merges a plain-text argument file with the command line before the kernel parses anything
//The usual setup is a baseline in Server.Args/Client.Args and per-run overrides on the command line: a key that
//appears on the command line removes every occurrence of that key from the file, then the command line is appended
public static class LaunchArgsFile
{
    //Merge merges the argument file at filePath into the command line
    //A missing or unreadable file leaves the command line untouched; command-line tokens always win over file tokens
    public static string[] Merge(string[] commandLine, string filePath)
    {
        string[] fileTokens;
        try
        {
            if (!File.Exists(filePath)) return commandLine;
            fileTokens = Tokenize(File.ReadAllLines(filePath));
        }
        catch (Exception ex)
        {
            //Runs before the log directory is configured, so this goes straight to stderr
            Console.Error.WriteLine($"Failed to read argument file {filePath}: {ex.Message}");
            return commandLine;
        }

        if (fileTokens.Length == 0) return commandLine;

        var overridden = CollectNames(commandLine);
        var merged = new List<string>(fileTokens.Length + commandLine.Length);
        for (var i = 0; i < fileTokens.Length; i++)
        {
            var token = fileTokens[i];
            var name = ArgumentName(token);
            if (name is null || !overridden.Contains(name))
            {
                merged.Add(token);
                continue;
            }

            //An option keeps its value in a following token; the value is dropped together with the option
            if (!token.Contains('=') && i + 1 < fileTokens.Length && !LooksLikeArgument(fileTokens[i + 1]))
                i++;
        }

        merged.AddRange(commandLine);
        return [.. merged];
    }

    //CollectNames collects every key the command line already defines, normalizing --opt=value and --opt value
    private static HashSet<string> CollectNames(string[] args)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in args)
        {
            var name = ArgumentName(token);
            if (name is not null) names.Add(name);
        }
        return names;
    }

    //ArgumentName extracts the key from one argument token, matching what GameOptions sees
    //Returns null for bare values and for the lone "--" separator
    private static string? ArgumentName(string token)
    {
        if (token.Length < 2 || token[0] != '-') return null;

        if (token[1] != '-')
            return token[1..];

        if (token.Length == 2) return null;

        var body = token.AsSpan(2);
        var eq = body.IndexOf('=');
        return (eq >= 0 ? body[..eq] : body).ToString();
    }

    //LooksLikeArgument reports whether a token can be the start of another argument rather than an option value
    private static bool LooksLikeArgument(string token)
        => token.Length > 1 && token[0] == '-';

    //Tokenize splits the file into argument tokens
    //Blank lines and # comments are skipped, quotes group a token containing spaces, one token per whitespace run
    private static string[] Tokenize(string[] lines)
    {
        var tokens = new List<string>();
        foreach (var raw in lines)
        {
            var line = raw.AsSpan().Trim();
            if (line.IsEmpty || line[0] == '#') continue;

            var i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
                if (i >= line.Length || line[i] == '#') break;

                if (line[i] is '"' or '\'')
                {
                    var quote = line[i++];
                    var start = i;
                    while (i < line.Length && line[i] != quote) i++;
                    tokens.Add(line[start..i].ToString());
                    i++;
                    continue;
                }

                var tokenStart = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i])) i++;
                tokens.Add(line[tokenStart..i].ToString());
            }
        }
        return [.. tokens];
    }
}
