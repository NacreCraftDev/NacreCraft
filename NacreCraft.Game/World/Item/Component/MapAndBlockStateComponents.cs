using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component;

//MapDecorations the decoration icons on a filled map, maps to vanilla net.minecraft.world.item.component.MapDecorations
public sealed record MapDecorations(IReadOnlyDictionary<string, MapDecorations.Decoration> Decorations)
{
    public static readonly MapDecorations Empty = new(new Dictionary<string, Decoration>());

    public static readonly Codec<MapDecorations> Codec =
        Codecs.UnboundedMap(Codecs.String, Decoration.Codec).Xmap(
            map => new MapDecorations(map),
            value => value.Decorations.ToDictionary(pair => pair.Key, pair => pair.Value));

    public MapDecorations With(string id, Decoration decoration)
    {
        var map = Decorations.ToDictionary(pair => pair.Key, pair => pair.Value);
        map[id] = decoration;
        return new MapDecorations(map);
    }

    //Decoration one decoration icon, maps to vanilla MapDecorations.Decoration
    public sealed record Decoration(Holder<MapDecorationType> Type, double X, double Z, float Rotation)
    {
        public static readonly Codec<Decoration> Codec = RecordCodecBuilder.Of4(
            HolderSetCodecs.MapDecorationTypeRef.FieldOf("type").ForGetter((Decoration value) => value.Type),
            Codecs.Double.FieldOf("x").ForGetter((Decoration value) => value.X),
            Codecs.Double.FieldOf("z").ForGetter((Decoration value) => value.Z),
            Codecs.Float.FieldOf("rotation").ForGetter((Decoration value) => value.Rotation),
            (type, x, z, rotation) => new Decoration(type, x, z, rotation));
    }
}

//BlockItemStateProperties block state values stored on a block item, maps to vanilla net.minecraft.world.item.component.BlockItemStateProperties
//Applying the values to a BlockState needs the block state definition lookup, so only storage/query is ported
public sealed record BlockItemStateProperties(IReadOnlyDictionary<string, string> Properties)
{
    public static readonly BlockItemStateProperties Empty = new(new Dictionary<string, string>());

    public static readonly Codec<BlockItemStateProperties> Codec =
        Codecs.UnboundedMap(Codecs.String, Codecs.String).Xmap(
            map => new BlockItemStateProperties(map),
            value => value.Properties.ToDictionary(pair => pair.Key, pair => pair.Value));

    public bool IsEmpty => Properties.Count == 0;

    //With stores a property value by its serialized name, maps to vanilla with(Property, T)
    public BlockItemStateProperties With<T>(NacreCraft.Registry.State.Property<T> property, T value)
        where T : IComparable
    {
        var map = Properties.ToDictionary(pair => pair.Key, pair => pair.Value);
        map[property.Name] = property.GetName(value);
        return new BlockItemStateProperties(map);
    }

    //With reads the value from a block state, maps to vanilla with(Property, BlockState)
    public BlockItemStateProperties With<T>(NacreCraft.Registry.State.Property<T> property,
        NacreCraft.Registry.State.BlockState state) where T : IComparable
        => With(property, state.GetValue(property));

    //Get reads a property value by name, maps to vanilla get
    public T? Get<T>(NacreCraft.Registry.State.Property<T> property) where T : IComparable
        => Properties.TryGetValue(property.Name, out var name) && property.TryGetValue(name, out var value)
            ? value
            : default;
}
