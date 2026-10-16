using System.Text;
//The table type is named Language and this file lives in the NacreCraft.Client.Language namespace, so the simple
//name would bind to the namespace; the alias keeps the type reachable
using ChatLanguage = NacreCraft.Network.Chat.Language;

namespace NacreCraft.Client.Language;

//I18n translation lookup, maps to vanilla net.minecraft.client.resources.language.I18n
//Reads the global Language.Instance that the reload chain installs; a missing key falls back to the key itself
public static class I18n
{
    //Get looks up the key and applies args to its placeholders; without args the raw value is returned
    public static string Get(string key, params object[] args)
    {
        var value = ChatLanguage.Instance.GetOrDefault(key);
        return args.Length == 0 ? value : Format(value, args);
    }

    //Has reports whether the key has a translation, maps to vanilla Language.has
    public static bool Has(string key) => ChatLanguage.Instance.Has(key);

    //Format expands the Java style placeholders of the value, maps to the String.format call in vanilla I18n
    //Language.LoadFromJson rewrites %d and %f into %s while loading, so only %s and the positional %N$s form remain
    //A missing argument or a malformed placeholder is kept as written, so a bad translation still shows something
    private static string Format(string value, object[] args)
    {
        var builder = new StringBuilder(value.Length + 16);
        var next = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '%' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }
            if (value[i + 1] == '%')
            {
                builder.Append('%');
                i++;
                continue;
            }
            //An explicit index like %2$s picks the argument by position, otherwise they are consumed in order
            var cursor = i + 1;
            while (cursor < value.Length && char.IsAsciiDigit(value[cursor])) cursor++;
            var positional = cursor > i + 1 && cursor < value.Length && value[cursor] == '$';
            if (positional) cursor++;
            if (cursor >= value.Length || value[cursor] != 's')
            {
                builder.Append(value[i]);
                continue;
            }
            object? argument = null;
            if (positional)
            {
                var index = int.TryParse(value.AsSpan(i + 1, cursor - i - 2), out var parsed) ? parsed - 1 : -1;
                if (index >= 0 && index < args.Length) argument = args[index];
            }
            else if (next < args.Length)
            {
                argument = args[next++];
            }
            builder.Append(argument?.ToString() ?? string.Empty);
            i = cursor;
        }
        return builder.ToString();
    }
}
