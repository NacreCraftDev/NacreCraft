using System.Globalization;

namespace NacreCraft.Storage;

//FileNameDateFormatter maps to vanilla net.minecraft.world.level.storage.FileNameDateFormatter
//Vanilla exposes a java.time DateTimeFormatter; the .NET side keeps the same pattern and invariant culture
public sealed class FileNameDateFormatter
{
    //Pattern, the filename timestamp layout year-month-day_hour-minute-second, maps to vanilla FORMATTER
    public const string Pattern = "yyyy-MM-dd_HH-mm-ss";

    //Formatter, the pattern exposed the way vanilla exposes its DateTimeFormatter constant
    public static readonly string Formatter = Pattern;

    public static string Format(DateTime value) => value.ToString(Pattern, CultureInfo.InvariantCulture);

    public static string Format(DateTimeOffset value) => value.ToString(Pattern, CultureInfo.InvariantCulture);

    public static bool TryParse(string text, out DateTime value)
        => DateTime.TryParseExact(text, Pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
}
