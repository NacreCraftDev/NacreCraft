using NacreCraft.Codec;

namespace NacreCraft.Network.Chat;

//FilterMask marks which characters of a chat message were filtered out
//maps to vanilla net.minecraft.network.chat.FilterMask
//NC also has a component FilterMask under Game.World.Items; this is the chat one, kept in the network layer
public sealed class FilterMask
{
    public static readonly Style FilteredStyle = Style.Empty
        .WithColor(ChatFormatting.DarkGray)
        .WithHoverEvent(new HoverEvent.ShowText(Component.Translatable("chat.filtered")));

    public static readonly FilterMask FullyFiltered = new(new List<bool>(), MaskType.FullyFiltered);
    public static readonly FilterMask PassThrough = new(new List<bool>(), MaskType.PassThrough);

    //CODEC pass_through / fully_filtered by name, partially filtered as a list of filtered indexes
    public static readonly Codec<FilterMask> CODEC = new FilterMaskCodec();

    private readonly List<bool> _mask;
    private readonly MaskType _type;

    private FilterMask(List<bool> mask, MaskType type)
    {
        _mask = mask;
        _type = type;
    }

    //FilterMask a partially filtered mask with the given length, maps to the vanilla int constructor
    public FilterMask(int length)
    {
        _mask = new List<bool>(length);
        for (var i = 0; i < length; i++) _mask.Add(false);
        _type = MaskType.PartiallyFiltered;
    }

    public bool IsEmpty => _type == MaskType.PassThrough;

    public bool IsFullyFiltered => _type == MaskType.FullyFiltered;

    //SetFiltered marks one character as filtered out
    public void SetFiltered(int index)
    {
        while (_mask.Count <= index) _mask.Add(false);
        _mask[index] = true;
    }

    private bool IsFiltered(int index) => index < _mask.Count && _mask[index];

    //Apply returns the text with filtered characters replaced by #, null when fully filtered
    public string? Apply(string text)
    {
        switch (_type)
        {
            case MaskType.FullyFiltered:
                return null;
            case MaskType.PassThrough:
                return text;
        }
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length && i < _mask.Count; i++)
            if (_mask[i]) chars[i] = '#';
        return new string(chars);
    }

    //ApplyWithFormatting replaces filtered runs with a styled # run, maps to vanilla applyWithFormatting
    public Component? ApplyWithFormatting(string text)
    {
        switch (_type)
        {
            case MaskType.FullyFiltered:
                return null;
            case MaskType.PassThrough:
                return Component.Literal(text);
        }
        var result = Component.Empty();
        var previousIndex = 0;
        var filtered = IsFiltered(0);
        while (previousIndex < text.Length)
        {
            var nextIndex = previousIndex;
            while (nextIndex < text.Length && IsFiltered(nextIndex) == filtered) nextIndex++;
            if (filtered)
                result.Append(Component.Literal(new string('#', nextIndex - previousIndex)).WithStyle(FilteredStyle));
            else
                result.Append(text[previousIndex..nextIndex]);
            filtered = !filtered;
            previousIndex = nextIndex;
        }
        return result;
    }

    public static FilterMask Read(FriendlyByteBuf input)
    {
        var type = input.ReadVarInt();
        return type switch
        {
            0 => PassThrough,
            1 => FullyFiltered,
            _ => new FilterMask(ReadMask(input), MaskType.PartiallyFiltered)
        };
    }

    public static void Write(FriendlyByteBuf output, FilterMask mask)
    {
        output.WriteVarInt((int)mask._type);
        if (mask._type == MaskType.PartiallyFiltered)
            WriteMask(output, mask._mask);
    }

    //ReadMask reads the filtered flags as a long array
    private static List<bool> ReadMask(FriendlyByteBuf input)
    {
        var longs = input.ReadLongArray();
        var mask = new List<bool>(longs.Length * 64);
        foreach (var value in longs)
            for (var bit = 0; bit < 64; bit++)
                mask.Add((value & (1L << bit)) != 0);
        return mask;
    }

    //WriteMask writes the filtered flags as a long array
    private static void WriteMask(FriendlyByteBuf output, List<bool> mask)
    {
        var count = (mask.Count + 63) / 64;
        var longs = new long[count];
        for (var i = 0; i < mask.Count; i++)
            if (mask[i]) longs[i / 64] |= 1L << (i % 64);
        output.WriteLongArray(longs);
    }

    public override bool Equals(object? obj)
        => obj is FilterMask that && _type == that._type && _mask.SequenceEqual(that._mask);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_type);
        foreach (var flag in _mask) hash.Add(flag);
        return hash.ToHashCode();
    }

    private enum MaskType
    {
        PassThrough = 0,
        FullyFiltered = 1,
        PartiallyFiltered = 2
    }

    //FilterMaskCodec textual form of a filter mask
    private sealed class FilterMaskCodec : ScalarCodec<FilterMask>
    {
        public override DataResult<FilterMask> Parse<U>(DynamicOps<U> ops, U input)
        {
            var name = ops.GetStringValue(input);
            if (name.Result().IsPresent)
            {
                return name.GetOrThrow() switch
                {
                    "pass_through" => DataResult<FilterMask>.Success(PassThrough),
                    "fully_filtered" => DataResult<FilterMask>.Success(FullyFiltered),
                    _ => DataResult<FilterMask>.Error(() => $"Unknown filter mask type: {name.GetOrThrow()}")
                };
            }
            var stream = ops.GetStream(input);
            if (!stream.Result().IsPresent)
                return DataResult<FilterMask>.Error(() => "filter mask must be a name or a list of filtered indexes");
            var mask = new List<bool>();
            foreach (var element in stream.GetOrThrow())
            {
                var index = ops.GetNumberValue(element);
                if (!index.Result().IsPresent)
                    return DataResult<FilterMask>.Error(() => "filter mask indexes must be numbers");
                var position = (int)index.GetOrThrow();
                while (mask.Count <= position) mask.Add(false);
                mask[position] = true;
            }
            return DataResult<FilterMask>.Success(new FilterMask(mask, MaskType.PartiallyFiltered));
        }

        public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, FilterMask value)
        {
            if (value._type == MaskType.PassThrough)
                return DataResult<U>.Success(ops.CreateString("pass_through"));
            if (value._type == MaskType.FullyFiltered)
                return DataResult<U>.Success(ops.CreateString("fully_filtered"));
            var indexes = new List<U>();
            for (var i = 0; i < value._mask.Count; i++)
                if (value._mask[i]) indexes.Add(ops.CreateInt(i));
            return DataResult<U>.Success(ops.CreateList(indexes));
        }
    }
}
