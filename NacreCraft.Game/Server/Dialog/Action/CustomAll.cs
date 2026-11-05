using NacreCraft.Codec;
using NacreCraft.Nbt;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using RegistryAction = NacreCraft.Registry.Action;

namespace NacreCraft.Game.Server.Dialog;

//CustomAll a custom click payload carrying every parameter, maps to vanilla net.minecraft.server.dialog.action.CustomAll
public sealed record CustomAll(Identifier Id, Optional<CompoundTag> Additions) : Action
{
    public static readonly MapCodec<CustomAll> MAP_CODEC = RecordCodecBuilder.Of2(
        IdentifierCodec.Instance.FieldOf("id").ForGetter((CustomAll value) => value.Id),
        CompoundTag.Codec.OptionalFieldOf("additions").ForGetter((CustomAll value) => value.Additions),
        (id, additions) => new CustomAll(id, additions));

    public static readonly MapCodec<RegistryAction> ActionCodec =
        new XmapMapCodec<CustomAll, RegistryAction>(MAP_CODEC, value => value, action => (CustomAll)action);

    public MapCodec<RegistryAction> Codec() => ActionCodec;

    public Optional<ClickEvent> CreateAction(IReadOnlyDictionary<string, Action.ValueGetter> parameters)
    {
        var tag = Additions.IsPresent ? (CompoundTag)Additions.Get().Copy() : new CompoundTag();
        foreach (var (key, value) in parameters) tag.Put(key, value.AsTag());
        return Optional<ClickEvent>.Of(new ClickEvent.Custom(Id, Optional<CompoundTag>.Of(tag)));
    }
}

