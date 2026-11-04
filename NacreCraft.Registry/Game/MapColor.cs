using NacreCraft.Registry.Environment;

namespace NacreCraft.Registry;

//MapColor the color a block shows on a map, maps to vanilla net.minecraft.world.level.material.MapColor
//Ids 0..63 are indexes into a shared table; the packed form carries the id in the high six bits and the brightness in the low two
public sealed class MapColor
{
    private static readonly MapColor?[] MaterialColors = new MapColor?[64];

    public static readonly MapColor None = new(0, 0);
    public static readonly MapColor Grass = new(1, 8368696);
    public static readonly MapColor Sand = new(2, 16247203);
    public static readonly MapColor Wool = new(3, 0xC7C7C7);
    public static readonly MapColor Fire = new(4, 0xFF0000);
    public static readonly MapColor Ice = new(5, 0xA0A0FF);
    public static readonly MapColor Metal = new(6, 0xA7A7A7);
    public static readonly MapColor Plant = new(7, 31744);
    public static readonly MapColor Snow = new(8, 0xFFFFFF);
    public static readonly MapColor Clay = new(9, 10791096);
    public static readonly MapColor Dirt = new(10, 9923917);
    public static readonly MapColor Stone = new(11, 0x707070);
    public static readonly MapColor Water = new(12, 0x4040FF);
    public static readonly MapColor Wood = new(13, 9402184);
    public static readonly MapColor Quartz = new(14, 0xFFFCF5);
    public static readonly MapColor ColorOrange = new(15, 14188339);
    public static readonly MapColor ColorMagenta = new(16, 11685080);
    public static readonly MapColor ColorLightBlue = new(17, 6724056);
    public static readonly MapColor ColorYellow = new(18, 0xE5E533);
    public static readonly MapColor ColorLightGreen = new(19, 8375321);
    public static readonly MapColor ColorPink = new(20, 15892389);
    public static readonly MapColor ColorGray = new(21, 0x4C4C4C);
    public static readonly MapColor ColorLightGray = new(22, 0x999999);
    public static readonly MapColor ColorCyan = new(23, 5013401);
    public static readonly MapColor ColorPurple = new(24, 8339378);
    public static readonly MapColor ColorBlue = new(25, 3361970);
    public static readonly MapColor ColorBrown = new(26, 6704179);
    public static readonly MapColor ColorGreen = new(27, 6717235);
    public static readonly MapColor ColorRed = new(28, 0x993333);
    public static readonly MapColor ColorBlack = new(29, 0x191919);
    public static readonly MapColor Gold = new(30, 16445005);
    public static readonly MapColor Diamond = new(31, 6085589);
    public static readonly MapColor Lapis = new(32, 4882687);
    public static readonly MapColor Emerald = new(33, 55610);
    public static readonly MapColor Podzol = new(34, 8476209);
    public static readonly MapColor Nether = new(35, 0x700200);
    public static readonly MapColor TerracottaWhite = new(36, 13742497);
    public static readonly MapColor TerracottaOrange = new(37, 10441252);
    public static readonly MapColor TerracottaMagenta = new(38, 9787244);
    public static readonly MapColor TerracottaLightBlue = new(39, 7367818);
    public static readonly MapColor TerracottaYellow = new(40, 12223780);
    public static readonly MapColor TerracottaLightGreen = new(41, 6780213);
    public static readonly MapColor TerracottaPink = new(42, 10505550);
    public static readonly MapColor TerracottaGray = new(43, 0x392923);
    public static readonly MapColor TerracottaLightGray = new(44, 8874850);
    public static readonly MapColor TerracottaCyan = new(45, 0x575C5C);
    public static readonly MapColor TerracottaPurple = new(46, 8014168);
    public static readonly MapColor TerracottaBlue = new(47, 4996700);
    public static readonly MapColor TerracottaBrown = new(48, 4993571);
    public static readonly MapColor TerracottaGreen = new(49, 5001770);
    public static readonly MapColor TerracottaRed = new(50, 9321518);
    public static readonly MapColor TerracottaBlack = new(51, 2430480);
    public static readonly MapColor CrimsonNylium = new(52, 12398641);
    public static readonly MapColor CrimsonStem = new(53, 9715553);
    public static readonly MapColor CrimsonHyphae = new(54, 6035741);
    public static readonly MapColor WarpedNylium = new(55, 1474182);
    public static readonly MapColor WarpedStem = new(56, 3837580);
    public static readonly MapColor WarpedHyphae = new(57, 5647422);
    public static readonly MapColor WarpedWartBlock = new(58, 1356933);
    public static readonly MapColor Deepslate = new(59, 0x646464);
    public static readonly MapColor RawIron = new(60, 14200723);
    public static readonly MapColor GlowLichen = new(61, 8365974);

    public int Col { get; }

    public int Id { get; }

    private MapColor(int id, int col)
    {
        if (id < 0 || id > 63)
            throw new ArgumentOutOfRangeException(nameof(id), "Map colour ID must be between 0 and 63 (inclusive)");
        Id = id;
        Col = col;
        MaterialColors[id] = this;
    }

    //CalculateArgbColor the opaque map color dimmed by the brightness, maps to vanilla calculateARGBColor
    public int CalculateArgbColor(Brightness brightness)
        => ReferenceEquals(this, None) ? 0 : Argb.ScaleRgb(Argb.Opaque(Col), ModifierOf(brightness));

    //ById looks a color up by id, maps to vanilla byId
    public static MapColor ById(int id)
    {
        if (id < 0 || id >= MaterialColors.Length)
            throw new ArgumentOutOfRangeException(nameof(id), "material id");
        return ByIdUnsafe(id);
    }

    private static MapColor ByIdUnsafe(int id) => MaterialColors[id] ?? None;

    //GetColorFromPackedId unpacks a packed id and returns its argb color, maps to vanilla getColorFromPackedId
    public static int GetColorFromPackedId(int packedId)
    {
        var value = packedId & 0xFF;
        return ByIdUnsafe(value >> 2).CalculateArgbColor((Brightness)(value & 3));
    }

    //GetPackedId packs the id and brightness into one byte, maps to vanilla getPackedId
    public byte GetPackedId(Brightness brightness) => (byte)((Id << 2) | ((int)brightness & 3));

    //ModifierOf the brightness multiplier, maps to the modifier field of vanilla MapColor.Brightness
    public static int ModifierOf(Brightness brightness) => brightness switch
    {
        Brightness.Low => 180,
        Brightness.Normal => 220,
        Brightness.High => 255,
        _ => 135
    };

    //Brightness the four brightness steps of a map color, maps to vanilla MapColor.Brightness
    //The numeric values are the ids used by the packed form
    public enum Brightness
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Lowest = 3
    }
}
