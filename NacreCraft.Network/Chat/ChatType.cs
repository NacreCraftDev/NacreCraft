using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Network.Chat;

//ChatType chat type, maps to vanilla net.minecraft.network.chat.ChatType
//Two ChatTypeDecoration fields: chat + narration
//CHAT_TYPE registry uses weak-typed object to avoid cross-layer circular dependencies; the StreamCodec is hand-written, looking up the registry via Lookup and casting
//Bound nested class binds Holder + name + targetName, used by the business layer to decorate
public sealed class ChatType
{
    public ChatTypeDecoration Chat { get; }
    public ChatTypeDecoration Narration { get; }

    //DIRECT_STREAM_CODEC directly codes the two ChatTypeDecoration fields, maps to vanilla DIRECT_STREAM_CODEC
    public static StreamCodec<RegistryFriendlyByteBuf, ChatType> DirectStreamCodec { get; }
        = new ChatTypeDirectCodec();

    //STREAM_CODEC encodes Holder through the CHAT_TYPE registry id, maps to vanilla STREAM_CODEC
    //DirectHolderStreamCodec supports both Reference id+1 and Direct id==0 forms
    //CHAT_TYPE is a Registry<object>, so decode casts object to ChatType
    public static StreamCodec<RegistryFriendlyByteBuf, Holder<ChatType>> StreamCodec { get; }
        = new ChatTypeHolderCodec();

    //DEFAULT_CHAT_DECORATION default decoration, aligns with vanilla DEFAULT_CHAT_DECORATION
    public static readonly ChatTypeDecoration DefaultChatDecoration
        = ChatTypeDecoration.WithSender("chat.type.text");

    //7 ResourceKeys such as CHAT/SAY_COMMAND, aligning with the vanilla predefined keys
    //CHAT_TYPE is a Registry<object>, so its ResourceKey also uses weak-typed object
    public static readonly ResourceKey<object> CHAT = Create("chat");
    public static readonly ResourceKey<object> SAY_COMMAND = Create("say_command");
    public static readonly ResourceKey<object> MSG_COMMAND_INCOMING = Create("msg_command_incoming");
    public static readonly ResourceKey<object> MSG_COMMAND_OUTGOING = Create("msg_command_outgoing");
    public static readonly ResourceKey<object> TEAM_MSG_COMMAND_INCOMING = Create("team_msg_command_incoming");
    public static readonly ResourceKey<object> TEAM_MSG_COMMAND_OUTGOING = Create("team_msg_command_outgoing");
    public static readonly ResourceKey<object> EMOTE_COMMAND = Create("emote_command");

    public ChatType(ChatTypeDecoration chat, ChatTypeDecoration narration)
    {
        Chat = chat;
        Narration = narration;
    }

    //Codec the data pack form of a chat type, maps to vanilla ChatType.CODEC
    public static readonly Codec<ChatType> Codec = new ChatTypeCodec();

    //Create builds a ChatType ResourceKey, aligns with vanilla create
    private static ResourceKey<object> Create(string name)
        => ResourceKey<object>.Create(Registries.CHAT_TYPE, Identifier.WithDefaultNamespace(name));

    //Bound binds Holder+name+targetName for business-layer decorate, maps to vanilla ChatType.Bound
    //STREAM_CODEC encodes chatType(Holder) + name(Component) + targetName(Optional<Component>)
    public sealed class Bound
    {
        public Holder<ChatType> ChatType { get; }
        public Component Name { get; }
        public Component? TargetName { get; }

        public static StreamCodec<RegistryFriendlyByteBuf, Bound> StreamCodec { get; }
            = new ChatTypeBoundCodec();

        public Bound(Holder<ChatType> chatType, Component name, Component? targetName = null)
        {
            ChatType = chatType;
            Name = name;
            TargetName = targetName;
        }

        //WithTargetName sets the target name and returns a new Bound, aligns with vanilla withTargetName
        public Bound WithTargetName(Component targetName)
            => new(ChatType, Name, targetName);
    }
}

//ChatTypeDirectCodec directly codes the two ChatTypeDecoration fields of ChatType
internal sealed class ChatTypeDirectCodec : StreamCodec<RegistryFriendlyByteBuf, ChatType>
{
    public ChatType Decode(RegistryFriendlyByteBuf buf)
    {
        var chat = ChatTypeDecoration.StreamCodec.Decode(buf);
        var narration = ChatTypeDecoration.StreamCodec.Decode(buf);
        return new(chat, narration);
    }

    public void Encode(RegistryFriendlyByteBuf buf, ChatType value)
    {
        ChatTypeDecoration.StreamCodec.Encode(buf, value.Chat);
        ChatTypeDecoration.StreamCodec.Encode(buf, value.Narration);
    }
}

//ChatTypeHolderCodec codes Holder<ChatType> through the CHAT_TYPE registry id
//CHAT_TYPE is a Registry<object>, so decode casts object to ChatType and encode casts ChatType to object
internal sealed class ChatTypeHolderCodec : StreamCodec<RegistryFriendlyByteBuf, Holder<ChatType>>
{
    private const int DirectHolderId = 0;

    public Holder<ChatType> Decode(RegistryFriendlyByteBuf buf)
    {
        int id = buf.ReadVarInt();
        if (id == DirectHolderId)
            return Holder<ChatType>.Direct(ChatType.DirectStreamCodec.Decode(buf));
        var registry = buf.Lookup(Registries.CHAT_TYPE);
        var holder = registry.Get(id - 1);
        if (holder is null)
            throw new InvalidOperationException($"unknown holder id {id} in CHAT_TYPE");
        var value = holder.Value as ChatType
            ?? throw new InvalidOperationException($"CHAT_TYPE registry value is not a ChatType: {holder.Value}");
        return Holder<ChatType>.Direct(value);
    }

    public void Encode(RegistryFriendlyByteBuf buf, Holder<ChatType> value)
    {
        if (value.HolderKind == Holder<ChatType>.Kind.Reference)
        {
            var registry = buf.Lookup(Registries.CHAT_TYPE);
            int id = registry.GetId((object)value.Value);
            if (id == IdMap<object>.Default)
                throw new InvalidOperationException($"holder value not registered: {value.Value}");
            buf.WriteVarInt(id + 1);
        }
        else
        {
            buf.WriteVarInt(DirectHolderId);
            ChatType.DirectStreamCodec.Encode(buf, value.Value);
        }
    }
}

//ChatTypeBoundCodec codes ChatType.Bound: chatType(Holder) + name(Component) + targetName(nullable Component)
internal sealed class ChatTypeBoundCodec : StreamCodec<RegistryFriendlyByteBuf, ChatType.Bound>
{
    public ChatType.Bound Decode(RegistryFriendlyByteBuf buf)
    {
        var chatType = ChatType.StreamCodec.Decode(buf);
        var name = ComponentSerialization.StreamCodec.Decode(buf);
        Component? targetName = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
        return new(chatType, name, targetName);
    }

    public void Encode(RegistryFriendlyByteBuf buf, ChatType.Bound value)
    {
        ChatType.StreamCodec.Encode(buf, value.ChatType);
        ComponentSerialization.StreamCodec.Encode(buf, value.Name);
        bool hasTarget = value.TargetName is not null;
        buf.WriteBoolean(hasTarget);
        if (hasTarget) ComponentSerialization.StreamCodec.Encode(buf, value.TargetName!);
    }
}

//ChatTypeCodec data pack codec for ChatType, maps to vanilla ChatType.CODEC
//The style field of a decoration is ignored on load: the translation key and the parameter list are what drive the
//decoration, and the data pack only uses the plain style
internal sealed class ChatTypeCodec : AbstractMapCodec<ChatType>
{
    private static readonly MapCodec<ChatTypeDecoration> Decoration =
        RecordCodecBuilder.Of2<ChatTypeDecoration, string, IReadOnlyList<string>>(
            Codecs.String.FieldOf("translation_key")
                .ForGetter<ChatTypeDecoration, string>(decoration => decoration.TranslationKey),
            Codecs.String.ListOf().FieldOf("parameters")
                .ForGetter<ChatTypeDecoration, IReadOnlyList<string>>(
                    decoration => decoration.Parameters.Select(parameter => parameter.GetName()).ToList()),
            (key, parameters) => new ChatTypeDecoration(key, parameters.Select(ParseParameter).ToList(), Style.Empty));

    public override DataResult<ChatType> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var chat = ReadDecoration<U>(ops, input, "chat");
        if (!chat.Result().IsPresent)
            return DataResult<ChatType>.Error(() => "chat type is missing the chat decoration");
        var narration = ReadDecoration<U>(ops, input, "narration");
        if (!narration.Result().IsPresent)
            return DataResult<ChatType>.Error(() => "chat type is missing the narration decoration");
        return DataResult<ChatType>.Success(new ChatType(chat.GetOrThrow(), narration.GetOrThrow()));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, ChatType value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding chat types is not supported yet");

    private static DataResult<ChatTypeDecoration> ReadDecoration<U>(DynamicOps<U> ops, MapLike<U> input, string name)
    {
        var tag = input.Get(name);
        if (!tag.IsPresent) return DataResult<ChatTypeDecoration>.Error(() => $"missing {name} decoration");
        return ops.GetMap(tag.Get()).FlatMap(map => Decoration.Decode(ops, map));
    }

    //ParseParameter maps the serialized parameter name back to the enum, unknown names fall back to sender like vanilla
    private static ChatTypeDecoration.Parameter ParseParameter(string name) => name switch
    {
        "target" => ChatTypeDecoration.Parameter.TARGET,
        "content" => ChatTypeDecoration.Parameter.CONTENT,
        _ => ChatTypeDecoration.Parameter.SENDER
    };
}
