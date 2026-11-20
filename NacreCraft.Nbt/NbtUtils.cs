using System.Globalization;
using System.Text;
using NacreCraft.Codec;
using NacreCraft.Config;

namespace NacreCraft.Nbt;

//NBT utility class. Mirrors vanilla net.minecraft.nbt.NbtUtils.
//Provides pure NBT helpers: Tag comparison, pretty printing, data version read/write and BlockState string packing.
//Methods that depend on the Block/StateHolder system (readBlockState/writeBlockState/writeFluidState),
//methods that depend on Component (toPrettyComponent),
//and methods that depend on SNBT parsing (structureToSnbt/snbtToStructure/packStructureTemplate/unpackStructureTemplate)
//are not ported yet and will be completed once the matching subsystems are ready.
public static class NbtUtils
{
    public const string SnbtDataTag = "data";

    private const char PropertiesStart = '{';
    private const char PropertiesEnd = '}';
    private const char KeyValueSeparator = ':';
    private const int Indent = 2;
    private const string ElementSeparator = ",";
    private const string ColonSeparator = ":";

    // MIME line separator (vanilla net.minecraft.util.Crypt.MIME_LINE_SEPARATOR)
    private const string MimeLineSeparator = "\r\n";

    //Recursively compare two NBT values for equality.
    //Mirrors vanilla NbtUtils.compareNbt(Tag, Tag, boolean).
    //expected: the Tag to match against (used as the pattern).
    //actual: the Tag being checked.
    //partialListMatches: when true a ListTag may match as a subset (order does not matter, every expected element must be present).
    public static bool CompareNbt(Tag? expected, Tag? actual, bool partialListMatches)
    {
        if (ReferenceEquals(expected, actual) || expected is null)
            return true;
        if (actual is null || expected.GetType() != actual.GetType())
            return false;

        if (expected is CompoundTag expectedCompound)
        {
            var actualCompound = (CompoundTag)actual;
            if (actualCompound.Count < expectedCompound.Count)
                return false;
            foreach (var (key, tag) in expectedCompound)
            {
                if (!CompareNbt(tag, actualCompound[key], partialListMatches))
                    return false;
            }
            return true;
        }

        if (expected is ListTag expectedList)
        {
            if (partialListMatches)
            {
                var actualList = (ListTag)actual;
                if (expectedList.IsEmpty)
                    return actualList.IsEmpty;
                if (actualList.Count < expectedList.Count)
                    return false;
                foreach (var expectedTag in expectedList)
                {
                    var found = false;
                    foreach (var actualTag in actualList)
                    {
                        if (CompareNbt(expectedTag, actualTag, partialListMatches))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return false;
                }
                return true;
            }
        }

        return expected.Equals(actual);
    }

    //AreEqual is full deep equality, matching the recursive semantics of vanilla Tag.equals
    //Unlike CompareNbt it requires equal key and element counts and does no subset matching
    //The NBT path and /data rely on it to tell whether a write really changed anything
    public static bool AreEqual(Tag? a, Tag? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Id != b.Id) return false;

        switch (a)
        {
            case CompoundTag leftCompound:
                var rightCompound = (CompoundTag)b;
                if (leftCompound.Count != rightCompound.Count) return false;
                foreach (var (key, value) in leftCompound)
                {
                    if (!rightCompound.TryGetTag(key, out var other) || !AreEqual(value, other)) return false;
                }
                return true;

            case ListTag leftList:
                var rightList = (ListTag)b;
                if (leftList.Count != rightList.Count) return false;
                for (var i = 0; i < leftList.Count; i++)
                {
                    if (!AreEqual(leftList[i], rightList[i])) return false;
                }
                return true;

            case ByteArrayTag leftBytes:
                return leftBytes.Value.AsSpan().SequenceEqual(((ByteArrayTag)b).Value);

            case IntArrayTag leftInts:
                return leftInts.Value.AsSpan().SequenceEqual(((IntArrayTag)b).Value);

            case LongArrayTag leftLongs:
                return leftLongs.Value.AsSpan().SequenceEqual(((LongArrayTag)b).Value);

            default:
                return a.Equals(b);
        }
    }

    //Pretty-print NBT. Mirrors vanilla NbtUtils.prettyPrint(Tag, boolean).
    public static string PrettyPrint(Tag tag, bool withBinaryBlobs)
        => PrettyPrint(new StringBuilder(), tag, 0, withBinaryBlobs).ToString();

    //Pretty-print NBT into a StringBuilder. Mirrors vanilla NbtUtils.prettyPrint(StringBuilder, Tag, int, boolean).
    public static StringBuilder PrettyPrint(StringBuilder builder, Tag input, int indent, bool withBinaryBlobs)
    {
        ArgumentNullException.ThrowIfNull(input);
        switch (input)
        {
            case NumericTag numeric:
                return builder.Append(numeric);
            case EndTag:
                return builder;
            case ByteArrayTag byteArray:
                return PrettyPrintByteArray(builder, byteArray, indent, withBinaryBlobs);
            case ListTag listTag:
                return PrettyPrintList(builder, listTag, indent, withBinaryBlobs);
            case IntArrayTag intArray:
                return PrettyPrintIntArray(builder, intArray, indent, withBinaryBlobs);
            case CompoundTag compound:
                return PrettyPrintCompound(builder, compound, indent, withBinaryBlobs);
            case LongArrayTag longArray:
                return PrettyPrintLongArray(builder, longArray, indent, withBinaryBlobs);
            default:
                return builder.Append(input);
        }
    }

    //SnbtMaxDepth the nesting depth past which SNBT abbreviates, maps to vanilla TextComponentTagVisitor.MAX_DEPTH
    private const int SnbtMaxDepth = 64;

    //SnbtMaxElements how many entries of a collection are printed before abbreviating, maps to vanilla MAX_LENGTH
    private const int SnbtMaxElements = 128;

    //ToSnbt renders a tag as single-line SNBT, the text form vanilla prints for /data get replies
    //Command output uses this rather than PrettyPrint: a player expects one compact line, whereas the indented
    //quoted-key layout is a debug dump shape that no vanilla command ever produces
    public static string ToSnbt(Tag tag) => ToSnbt(new StringBuilder(), tag, 0).ToString();

    private static StringBuilder ToSnbt(StringBuilder builder, Tag input, int depth)
    {
        switch (input)
        {
            case NumericTag numeric:
                return builder.Append(numeric);
            case StringTag stringTag:
                return builder.Append(QuoteAndEscape(stringTag.Value));
            case ByteArrayTag byteArray:
                return ToSnbtArray(builder, byteArray.Value, depth, "B", "b");
            case IntArrayTag intArray:
                return ToSnbtArray(builder, intArray.Value, depth, "I", string.Empty);
            case LongArrayTag longArray:
                return ToSnbtArray(builder, longArray.Value, depth, "L", "L");
            case ListTag list:
                return ToSnbtList(builder, list, depth);
            case CompoundTag compound:
                return ToSnbtCompound(builder, compound, depth);
            default:
                return builder;
        }
    }

    //ToSnbtArray writes a typed array, maps to vanilla TextComponentTagVisitor.visitByteArray and its siblings
    //Every element is preceded by a space, which is what makes an int array print as [I; 1, 2, 3]
    private static StringBuilder ToSnbtArray<T>(StringBuilder builder, T[] values, int depth, string prefix, string suffix)
    {
        builder.Append('[').Append(prefix).Append(';');
        var count = Math.Min(values.Length, SnbtMaxElements);
        for (var i = 0; i < count; i++)
        {
            if (i != 0) builder.Append(',');
            builder.Append(' ').Append(values[i]).Append(suffix);
        }
        if (values.Length > SnbtMaxElements) builder.Append(" <...>");
        return builder.Append(']');
    }

    //ToSnbtList writes a list, maps to vanilla TextComponentTagVisitor.visitList with an empty indentation
    private static StringBuilder ToSnbtList(StringBuilder builder, ListTag list, int depth)
    {
        if (list.Count == 0) return builder.Append("[]");
        if (depth >= SnbtMaxDepth) return builder.Append("[<...>]");
        builder.Append('[');
        var count = Math.Min(list.Count, SnbtMaxElements);
        for (var i = 0; i < count; i++)
        {
            if (i != 0) builder.Append(", ");
            ToSnbt(builder, list[i], depth + 1);
        }
        if (list.Count > SnbtMaxElements) builder.Append(" <...>");
        return builder.Append(']');
    }

    //ToSnbtCompound writes a compound, maps to vanilla TextComponentTagVisitor.visitCompound with an empty indentation
    //Keys keep their insertion order: vanilla only sorts when the logger is at debug, and that is off by default
    private static StringBuilder ToSnbtCompound(StringBuilder builder, CompoundTag compound, int depth)
    {
        if (compound.Count == 0) return builder.Append("{}");
        if (depth >= SnbtMaxDepth) return builder.Append("{<...>}");
        builder.Append('{');
        var first = true;
        foreach (var key in compound.Keys)
        {
            if (!first) builder.Append(", ");
            first = false;
            builder.Append(IsSimpleKey(key) ? key : QuoteAndEscape(key)).Append(": ");
            if (compound[key] is { } value) ToSnbt(builder, value, depth + 1);
        }
        return builder.Append('}');
    }

    //IsSimpleKey whether a key needs no quotes, maps to vanilla TextComponentTagVisitor.SIMPLE_VALUE
    private static bool IsSimpleKey(string key)
    {
        if (key.Length == 0) return false;
        foreach (var c in key)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '.' && c != '_' && c != '+' && c != '-') return false;
        }
        return true;
    }

    //QuoteAndEscape writes a string as a quoted SNBT string, maps to vanilla StringTag.quoteAndEscape
    private static string QuoteAndEscape(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            if (c == '"' || c == '\\') builder.Append('\\');
            builder.Append(c);
        }
        return builder.Append('"').ToString();
    }

    private static StringBuilder PrettyPrintByteArray(StringBuilder builder, ByteArrayTag tag, int indent, bool withBinaryBlobs)
    {
        var array = tag.Value;
        IndentTo(builder, indent).Append("byte[").Append(array.Length).Append("] {\n");
        if (withBinaryBlobs)
        {
            IndentTo(builder, indent + 1);
            for (var i = 0; i < array.Length; i++)
            {
                if (i != 0) builder.Append(',');
                if (i % 16 == 0 && i / 16 > 0)
                {
                    builder.Append('\n');
                    if (i < array.Length) IndentTo(builder, indent + 1);
                }
                else if (i != 0)
                {
                    builder.Append(' ');
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", array[i] & 0xFF));
            }
        }
        else
        {
            IndentTo(builder, indent + 1).Append(" // Skipped, supply withBinaryBlobs true");
        }
        builder.Append('\n');
        return IndentTo(builder, indent).Append('}');
    }

    private static StringBuilder PrettyPrintList(StringBuilder builder, ListTag tag, int indent, bool withBinaryBlobs)
    {
        var size = tag.Count;
        IndentTo(builder, indent).Append("list[").Append(size).Append("] [");
        if (size != 0) builder.Append('\n');
        for (var i = 0; i < size; i++)
        {
            if (i != 0) builder.Append(",\n");
            IndentTo(builder, indent + 1);
            PrettyPrint(builder, tag[i], indent + 1, withBinaryBlobs);
        }
        if (size != 0) builder.Append('\n');
        return IndentTo(builder, indent).Append(']');
    }

    private static StringBuilder PrettyPrintIntArray(StringBuilder builder, IntArrayTag tag, int indent, bool withBinaryBlobs)
    {
        var array = tag.Value;
        var hexWidth = 0;
        foreach (var v in array)
            hexWidth = Math.Max(hexWidth, string.Format(CultureInfo.InvariantCulture, "{0:X}", v).Length);
        IndentTo(builder, indent).Append("int[").Append(array.Length).Append("] {\n");
        if (withBinaryBlobs)
        {
            IndentTo(builder, indent + 1);
            for (var i = 0; i < array.Length; i++)
            {
                if (i != 0) builder.Append(',');
                if (i % 16 == 0 && i / 16 > 0)
                {
                    builder.Append('\n');
                    if (i < array.Length) IndentTo(builder, indent + 1);
                }
                else if (i != 0)
                {
                    builder.Append(' ');
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "0x{0:D" + hexWidth + "}", array[i]));
            }
        }
        else
        {
            IndentTo(builder, indent + 1).Append(" // Skipped, supply withBinaryBlobs true");
        }
        builder.Append('\n');
        return IndentTo(builder, indent).Append('}');
    }

    private static StringBuilder PrettyPrintCompound(StringBuilder builder, CompoundTag tag, int indent, bool withBinaryBlobs)
    {
        var keys = tag.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        IndentTo(builder, indent).Append('{');
        if (builder.Length - LastIndexOf(builder, MimeLineSeparator) > 2 * (indent + 1))
        {
            builder.Append('\n');
            IndentTo(builder, indent + 1);
        }
        var paddingLength = keys.Count == 0 ? 0 : keys.Max(k => k.Length);
        var padding = new string(' ', paddingLength);
        for (var i = 0; i < keys.Count; i++)
        {
            if (i != 0) builder.Append(",\n");
            var key = keys[i];
            IndentTo(builder, indent + 1).Append('"').Append(key).Append('"')
                .Append(padding, 0, padding.Length - key.Length)
                .Append(": ");
            PrettyPrint(builder, tag[key]!, indent + 1, withBinaryBlobs);
        }
        if (keys.Count != 0) builder.Append('\n');
        return IndentTo(builder, indent).Append('}');
    }

    private static StringBuilder PrettyPrintLongArray(StringBuilder builder, LongArrayTag tag, int indent, bool withBinaryBlobs)
    {
        var array = tag.Value;
        var hexWidth = 0L;
        foreach (var v in array)
            hexWidth = Math.Max(hexWidth, string.Format(CultureInfo.InvariantCulture, "{0:X}", v).Length);
        IndentTo(builder, indent).Append("long[").Append(array.Length).Append("] {\n");
        if (withBinaryBlobs)
        {
            IndentTo(builder, indent + 1);
            for (var i = 0; i < array.Length; i++)
            {
                if (i != 0) builder.Append(',');
                if (i % 16 == 0 && i / 16 > 0)
                {
                    builder.Append('\n');
                    if (i < array.Length) IndentTo(builder, indent + 1);
                }
                else if (i != 0)
                {
                    builder.Append(' ');
                }
                builder.Append(string.Format(CultureInfo.InvariantCulture, "0x{0:D" + hexWidth + "}", array[i]));
            }
        }
        else
        {
            IndentTo(builder, indent + 1).Append(" // Skipped, supply withBinaryBlobs true");
        }
        builder.Append('\n');
        return IndentTo(builder, indent).Append('}');
    }

    //Pad the current line with spaces up to column (2 * indent). Mirrors vanilla NbtUtils.indent(int, StringBuilder).
    private static StringBuilder IndentTo(StringBuilder builder, int indent)
    {
        var index = LastIndexOf(builder, MimeLineSeparator) + 1;
        var len = builder.Length - index;
        for (var i = 0; i < (2 * indent) - len; i++)
            builder.Append(' ');
        return builder;
    }

    //StringBuilder has no LastIndexOf, so this wraps a string lookup.
    private static int LastIndexOf(StringBuilder builder, string value)
    {
        // Simplification: convert to a string and search. StringBuilder is usually small, so the cost is acceptable.
        return builder.ToString().LastIndexOf(value, StringComparison.Ordinal);
    }

    // ============ structure template SNBT packing ============

    //StructureToSnbt packs a structure template into the compact SNBT form, maps to vanilla structureToSnbt
    //The palette becomes a list of state strings, block state indexes become the strings and blocks moves under data
    public static string StructureToSnbt(CompoundTag structure)
        => ToSnbt(PackStructureTemplate(structure));

    //SnbtToStructure parses the compact SNBT form back into a full structure template, maps to vanilla snbtToStructure
    public static CompoundTag SnbtToStructure(string snbt)
        => UnpackStructureTemplate(TagParser<Tag>.ParseCompoundFully(snbt));

    //PackStructureTemplate converts a full template to the compact form, maps to vanilla packStructureTemplate
    //A palette list present takes precedence over the single palette; entities and blocks are sorted by Y/X/Z with empty
    //position lists last, so the output is stable across runs
    public static CompoundTag PackStructureTemplate(CompoundTag snbt)
    {
        var palettes = snbt.GetList(PaletteListTag);
        var palette = palettes is not null ? palettes.GetList(0) ?? new ListTag() : snbt.GetListOrEmpty(PaletteTag);
        var deflatedPalette = new ListTag();
        foreach (var element in palette)
        {
            if (element is not CompoundTag compound) continue;
            deflatedPalette.Add(new StringTag(PackBlockState(compound)));
        }
        snbt.Put(PaletteTag, deflatedPalette);

        if (palettes is not null)
        {
            var newPalettes = new ListTag();
            foreach (var oldPaletteTag in palettes)
            {
                if (oldPaletteTag is not ListTag oldPalette) continue;
                var newPalette = new CompoundTag();
                for (var i = 0; i < oldPalette.Count; i++)
                {
                    var key = deflatedPalette.GetString(i)?.Value;
                    if (key is null || oldPalette.GetCompound(i) is not { } oldState) continue;
                    newPalette.PutString(key, PackBlockState(oldState));
                }
                newPalettes.Add(newPalette);
            }
            snbt.Put(PaletteListTag, newPalettes);
        }

        var oldEntities = snbt.GetList(EntitiesTag);
        if (oldEntities is not null)
        {
            var entities = new List<CompoundTag>();
            foreach (var element in oldEntities)
                if (element is CompoundTag entity) entities.Add(entity);
            entities.Sort((a, b) => ComparePositionLists(a.GetList(PositionTag), b.GetList(PositionTag), doubles: true));
            var newEntities = new ListTag();
            foreach (var entity in entities) newEntities.Add(entity);
            snbt.Put(EntitiesTag, newEntities);
        }

        var blocks = new List<CompoundTag>();
        foreach (var element in snbt.GetListOrEmpty(BlocksTag))
            if (element is CompoundTag block) blocks.Add(block);
        blocks.Sort((a, b) => ComparePositionLists(a.GetList(PositionTag), b.GetList(PositionTag), doubles: false));
        foreach (var block in blocks)
        {
            var index = block.GetIntValue(StructureBlockStateTag);
            var state = index >= 0 && index < deflatedPalette.Count ? deflatedPalette.GetString(index)?.Value : null;
            if (state is null)
                throw new InvalidOperationException($"Block state index {index} is missing from the palette");
            block.PutString(StructureBlockStateTag, state);
        }
        var data = new ListTag();
        foreach (var block in blocks) data.Add(block);
        snbt.Put(StructureDataTag, data);
        snbt.Remove(BlocksTag);
        return snbt;
    }

    //UnpackStructureTemplate converts the compact form back to a full template, maps to vanilla unpackStructureTemplate
    //The palette strings expand back into block state compounds and the data indexes move back under blocks as integers
    public static CompoundTag UnpackStructureTemplate(CompoundTag template)
    {
        var packedPalette = template.GetListOrEmpty(PaletteTag);
        var paletteKeys = new List<string>();
        foreach (var element in packedPalette)
        {
            var key = element.AsString();
            if (key is not null) paletteKeys.Add(key);
        }
        var palette = new ListTag();
        foreach (var key in paletteKeys) palette.Add(UnpackBlockState(key));

        var oldPalettes = template.GetList(PaletteListTag);
        if (oldPalettes is not null)
        {
            var newPalettes = new ListTag();
            foreach (var oldPaletteTag in oldPalettes)
            {
                if (oldPaletteTag is not CompoundTag oldPalette) continue;
                var newPalette = new ListTag();
                foreach (var key in paletteKeys)
                {
                    var packedState = oldPalette.GetString(key)?.Value
                        ?? throw new InvalidOperationException($"Entry {key} is missing from the palette");
                    newPalette.Add(UnpackBlockState(packedState));
                }
                newPalettes.Add(newPalette);
            }
            template.Put(PaletteListTag, newPalettes);
            template.Remove(PaletteTag);
        }
        else
        {
            template.Put(PaletteTag, palette);
        }

        var maybeBlocks = template.GetList(StructureDataTag);
        if (maybeBlocks is not null)
        {
            var stateIds = new Dictionary<string, int>();
            for (var i = 0; i < paletteKeys.Count; i++) stateIds[paletteKeys[i]] = i;
            for (var i = 0; i < maybeBlocks.Count; i++)
            {
                if (maybeBlocks.GetCompound(i) is not { } block) continue;
                var stateName = block.GetStringValue(StructureBlockStateTag);
                if (!stateIds.TryGetValue(stateName, out var stateId))
                    throw new InvalidOperationException($"Entry {stateName} is missing from the palette");
                block.PutInt(StructureBlockStateTag, stateId);
            }
            template.Put(BlocksTag, maybeBlocks);
            template.Remove(StructureDataTag);
        }
        return template;
    }

    //ComparePositionLists sorts position lists by Y, then X, then Z, putting entries without a position last
    //Maps to the vanilla YXZ comparators wrapped in Comparators.emptiesLast
    private static int ComparePositionLists(ListTag? a, ListTag? b, bool doubles)
    {
        if (a is null || b is null) return a is null ? (b is null ? 0 : 1) : -1;
        var compare = ComparePositionComponent(a, b, 1, doubles);
        if (compare != 0) return compare;
        compare = ComparePositionComponent(a, b, 0, doubles);
        if (compare != 0) return compare;
        return ComparePositionComponent(a, b, 2, doubles);
    }

    private static int ComparePositionComponent(ListTag a, ListTag b, int index, bool doubles)
        => doubles
            ? (a.GetDouble(index)?.Value ?? 0d).CompareTo(b.GetDouble(index)?.Value ?? 0d)
            : (a.GetInt(index)?.Value ?? 0).CompareTo(b.GetInt(index)?.Value ?? 0);

    //Structure template tag names, matching the constants of StructureTemplate without taking a Game-layer dependency
    private const string PaletteTag = "palette";
    private const string PaletteListTag = "palettes";
    private const string EntitiesTag = "entities";
    private const string BlocksTag = "blocks";
    private const string StructureDataTag = "data";
    private const string StructureBlockStateTag = "state";
    private const string PositionTag = "pos";

    // ============ data version read/write ============

    //Add the current data version to a CompoundTag. Mirrors vanilla addCurrentDataVersion(CompoundTag).
    public static CompoundTag AddCurrentDataVersion(CompoundTag tag)
        => AddDataVersion(tag, SharedConstants.WorldDataVersion);

    //Add the given data version to a CompoundTag. Mirrors vanilla addDataVersion(CompoundTag, int).
    public static CompoundTag AddDataVersion(CompoundTag tag, int version)
    {
        tag.PutInt(SharedConstants.DataVersionTag, version);
        return tag;
    }

    //Read the data version from a CompoundTag, defaulting to -1. Mirrors vanilla getDataVersion(CompoundTag).
    public static int GetDataVersion(CompoundTag tag) => GetDataVersion(tag, -1);

    //Read the data version from a CompoundTag, returning the default when absent. Mirrors vanilla getDataVersion(CompoundTag, int).
    public static int GetDataVersion(CompoundTag tag, int @default)
    {
        if (!tag.Contains(SharedConstants.DataVersionTag))
            return @default;
        return tag.GetIntValue(SharedConstants.DataVersionTag);
    }

    //Read the data version from a Dynamic tag, defaulting to -1. Mirrors vanilla getDataVersion(Dynamic).
    public static int GetDataVersion(Dynamic<Tag> tag) => GetDataVersion(tag, -1);

    //Read the data version from a Dynamic tag, returning the default when absent. Mirrors vanilla getDataVersion(Dynamic, int).
    public static int GetDataVersion(Dynamic<Tag> tag, int @default)
        => tag.Get(SharedConstants.DataVersionTag).AsInt(@default);

    //Write the given data version into a Dynamic tag. Mirrors vanilla addDataVersion(Dynamic, int).
    public static Dynamic<T> AddDataVersion<T>(Dynamic<T> tag, int version)
        => tag.Set(SharedConstants.DataVersionTag, tag.CreateInt(version));

    // ============ BlockState string packing/unpacking (pure string handling, no Block system)============

    //Pack a BlockState CompoundTag into a string. Mirrors vanilla NbtUtils.packBlockState(CompoundTag).
    //Format: name{key:value,key:value}
    public static string PackBlockState(CompoundTag compound)
    {
        var builder = new StringBuilder(compound.GetStringValue("Name"));
        if (compound.GetCompound("Properties") is { } properties)
        {
            var keyValues = string.Join(ElementSeparator,
                properties
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => kv.Key + KeyValueSeparator.ToString() + kv.Value!.AsString()));
            builder.Append(PropertiesStart).Append(keyValues).Append(PropertiesEnd);
        }
        return builder.ToString();
    }

    //Unpack a string into a BlockState CompoundTag. Mirrors vanilla NbtUtils.unpackBlockState(String).
    //Format: name{key:value,key:value}
    public static CompoundTag UnpackBlockState(string compound)
    {
        var tag = new CompoundTag();
        string name;
        var openIndex = compound.IndexOf(PropertiesStart);
        if (openIndex >= 0)
        {
            name = compound.Substring(0, openIndex);
            var properties = new CompoundTag();
            if (openIndex + 2 <= compound.Length)
            {
                var closeIndex = compound.IndexOf(PropertiesEnd, openIndex);
                var values = compound.Substring(openIndex + 1, closeIndex - openIndex - 1);
                foreach (var keyValue in values.Split(ElementSeparator))
                {
                    var parts = keyValue.Split(ColonSeparator.ToCharArray(), 2);
                    if (parts.Length == 2)
                    {
                        properties.PutString(parts[0], parts[1]);
                    }
                }
            }
            tag.Put("Properties", properties);
        }
        else
        {
            name = compound;
        }
        tag.PutString("Name", name);
        return tag;
    }
}

