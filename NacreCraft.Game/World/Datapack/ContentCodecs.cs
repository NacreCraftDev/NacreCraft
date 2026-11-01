using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Datapack;

//ContentCodecs data pack codecs for the content tables that carry no cross-references
//A description is decoded into a component and stored back as json text: the registry layer holds these content types and
//cannot reference the network layer where Component lives
public static class ContentCodecs
{
    //Material trim material table codec
    public static readonly Codec<TrimMaterial> Material = new TrimMaterialCodec();

    //Pattern trim pattern table codec
    public static readonly Codec<TrimPattern> Pattern = new TrimPatternCodec();

    //Song jukebox song table codec
    public static readonly Codec<JukeboxSong> Song = new JukeboxSongCodec();

    //ReadDescription reads the description field into the raw json text the content types keep
    internal static DataResult<string> ReadDescription<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var tag = input.Get("description");
        if (!tag.IsPresent) return DataResult<string>.Error(() => "content is missing the description field");
        return ComponentSerialization.JsonCodec.Parse(ops, tag.Get())
            .Map(component => ComponentSerialization.ToJson(component));
    }

    //ReadString reads a required string field
    internal static DataResult<string> ReadString<U>(DynamicOps<U> ops, MapLike<U> input, string name)
    {
        var tag = input.Get(name);
        if (!tag.IsPresent) return DataResult<string>.Error(() => $"content is missing the {name} field");
        return ops.GetStringValue(tag.Get());
    }
}

//TrimMaterialCodec maps to vanilla TrimMaterial.CODEC
internal sealed class TrimMaterialCodec : AbstractMapCodec<TrimMaterial>
{
    public override DataResult<TrimMaterial> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var assetName = ContentCodecs.ReadString(ops, input, "asset_name");
        if (!assetName.Result().IsPresent) return DataResult<TrimMaterial>.Error(() => "asset_name must be a string");
        var description = ContentCodecs.ReadDescription(ops, input);
        if (!description.Result().IsPresent) return DataResult<TrimMaterial>.Error(() => "description failed to decode");

        var overrides = new Dictionary<string, string>();
        var overridesTag = input.Get("override_armor_assets");
        if (overridesTag.IsPresent)
        {
            var values = ops.GetMapValues(overridesTag.Get());
            if (!values.Result().IsPresent) return DataResult<TrimMaterial>.Error(() => "override_armor_assets must be a map");
            foreach (var pair in values.GetOrThrow())
            {
                var key = ops.GetStringValue(pair.First);
                var value = ops.GetStringValue(pair.Second);
                if (key.Result().IsPresent && value.Result().IsPresent)
                    overrides[key.GetOrThrow()] = value.GetOrThrow();
            }
        }

        return DataResult<TrimMaterial>.Success(
            new TrimMaterial(assetName.GetOrThrow(), description.GetOrThrow(), overrides));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, TrimMaterial value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding trim materials is not supported yet");
}

//TrimPatternCodec maps to vanilla TrimPattern.CODEC
internal sealed class TrimPatternCodec : AbstractMapCodec<TrimPattern>
{
    public override DataResult<TrimPattern> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var assetId = ContentCodecs.ReadString(ops, input, "asset_id");
        if (!assetId.Result().IsPresent) return DataResult<TrimPattern>.Error(() => "asset_id must be a string");
        var parsed = Identifier.TryParse(assetId.GetOrThrow());
        if (parsed is null) return DataResult<TrimPattern>.Error(() => $"invalid asset_id {assetId.GetOrThrow()}");
        var description = ContentCodecs.ReadDescription(ops, input);
        if (!description.Result().IsPresent) return DataResult<TrimPattern>.Error(() => "description failed to decode");

        var decal = false;
        var decalTag = input.Get("decal");
        if (decalTag.IsPresent)
        {
            var result = ops.GetBooleanValue(decalTag.Get());
            if (result.Result().IsPresent) decal = result.GetOrThrow();
        }

        return DataResult<TrimPattern>.Success(
            new TrimPattern(parsed.Value, decal, description.GetOrThrow()));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, TrimPattern value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding trim patterns is not supported yet");
}

//JukeboxSongCodec maps to vanilla JukeboxSong.CODEC
//The sound event is kept by id: the sound event table is filled by code and this table must decode independently of it
internal sealed class JukeboxSongCodec : AbstractMapCodec<JukeboxSong>
{
    public override DataResult<JukeboxSong> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var soundEvent = ContentCodecs.ReadString(ops, input, "sound_event");
        if (!soundEvent.Result().IsPresent) return DataResult<JukeboxSong>.Error(() => "sound_event must be a string");
        var soundId = Identifier.TryParse(soundEvent.GetOrThrow());
        if (soundId is null) return DataResult<JukeboxSong>.Error(() => $"invalid sound_event {soundEvent.GetOrThrow()}");
        var description = ContentCodecs.ReadDescription(ops, input);
        if (!description.Result().IsPresent) return DataResult<JukeboxSong>.Error(() => "description failed to decode");

        var length = 0f;
        var lengthTag = input.Get("length_in_seconds");
        if (lengthTag.IsPresent)
        {
            var result = ops.GetNumberValue(lengthTag.Get());
            if (result.Result().IsPresent) length = (float)result.GetOrThrow();
        }

        var comparator = 0;
        var comparatorTag = input.Get("comparator_output");
        if (comparatorTag.IsPresent)
        {
            var result = ops.GetNumberValue(comparatorTag.Get());
            if (result.Result().IsPresent) comparator = (int)result.GetOrThrow();
        }

        return DataResult<JukeboxSong>.Success(
            new JukeboxSong(soundId.Value, description.GetOrThrow(), length, comparator));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, JukeboxSong value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding jukebox songs is not supported yet");
}
