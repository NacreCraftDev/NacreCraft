using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//RemoteChatSession the server-side view of a client chat session
//maps to vanilla net.minecraft.network.chat.RemoteChatSession
//Vanilla holds a ProfilePublicKey; the profile key system is not ported, so the validator and the expiry check are held directly
public sealed record RemoteChatSession(Guid SessionId, SignatureValidator Validator, Func<bool> HasExpired)
{
    public SignedMessageValidator CreateMessageValidator(TimeSpan gracePeriod)
        => new KeyBasedSignedMessageValidator(Validator, HasExpired);

    public SignedMessageChain.Decoder CreateMessageDecoder(Guid profileId)
        => new SignedMessageChain(profileId, SessionId).CreateDecoder(Validator, HasExpired);

    public Data AsData() => new(SessionId);

    //Data the network form of a remote session, maps to vanilla RemoteChatSession.Data
    //Only the session id is carried; the public key payload belongs to the profile key system
    public sealed record Data(Guid SessionId)
    {
        public static Data Read(FriendlyByteBuf input) => new(input.ReadUuid());

        public static void Write(FriendlyByteBuf output, Data data) => output.WriteUuid(data.SessionId);
    }
}
