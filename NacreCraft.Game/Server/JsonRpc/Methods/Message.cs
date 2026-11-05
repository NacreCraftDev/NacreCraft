using NacreCraft.Codec;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server.JsonRpc;

//Message a text payload in RPC requests, maps to vanilla net.minecraft.server.jsonrpc.methods.Message
public sealed record Message(Optional<string> Literal, Optional<string> Translatable,
    Optional<IReadOnlyList<string>> TranslatableParams)
{
    public static readonly Codec<Message> CODEC = RecordCodecBuilder.Of3(
        Codecs.String.OptionalFieldOf("literal").ForGetter((Message message) => message.Literal),
        Codecs.String.OptionalFieldOf("translatable").ForGetter((Message message) => message.Translatable),
        Codecs.String.ListOf().LenientOptionalFieldOf("translatableParams")
            .ForGetter((Message message) => message.TranslatableParams),
        (literal, translatable, translatableParams) => new Message(literal, translatable, translatableParams));

    //AsComponent turns the payload into a chat component, maps to vanilla asComponent
    public Optional<Component> AsComponent()
    {
        if (Translatable.IsPresent)
        {
            var key = Translatable.Get();
            return Optional<Component>.Of(TranslatableParams.IsPresent
                ? Component.Translatable(key, TranslatableParams.Get().Cast<object>().ToArray())
                : Component.Translatable(key));
        }
        return Literal.IsPresent ? Optional<Component>.Of(Component.Literal(Literal.Get())) : Optional<Component>.Empty();
    }
}
