using NacreCraft.Codec;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.World.Entity;

//ChatVisiblity chat visibility option, maps to vanilla net.minecraft.world.entity.player.ChatVisiblity (vanilla spelling)
//FULL shows everything, SYSTEM hides player chat but keeps server messages, HIDDEN hides chat entirely
public enum ChatVisiblity
{
    FULL = 0,
    SYSTEM = 1,
    HIDDEN = 2
}

//ChatVisiblityExtensions helpers, maps to the vanilla caption and LEGACY_CODEC
public static class ChatVisiblityExtensions
{
    private const int Count = 3;

    //LegacyCodec encodes the numeric id, out of range ids wrap like vanilla ByIdMap.continuous
    public static readonly Codec<ChatVisiblity> LegacyCodec =
        Codecs.Int.Xmap(ById, value => (int)value);

    //Caption the option caption, maps to vanilla caption
    public static Component Caption(this ChatVisiblity visibility) => visibility switch
    {
        ChatVisiblity.FULL => Component.Translatable("options.chat.visibility.full"),
        ChatVisiblity.SYSTEM => Component.Translatable("options.chat.visibility.system"),
        ChatVisiblity.HIDDEN => Component.Translatable("options.chat.visibility.hidden"),
        _ => throw new ArgumentOutOfRangeException(nameof(visibility))
    };

    //ById looks up by numeric id, wrapping like the vanilla WRAP strategy
    public static ChatVisiblity ById(int id) => (ChatVisiblity)(((id % Count) + Count) % Count);
}
