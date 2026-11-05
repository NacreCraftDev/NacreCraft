using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//LocalChatSession the client-side chat session that signs outgoing messages
//maps to vanilla net.minecraft.network.chat.LocalChatSession
//Vanilla holds a ProfileKeyPair; the profile key system is not ported, so the signer is passed in directly
public sealed record LocalChatSession(Guid SessionId, Signer Signer)
{
    public static LocalChatSession Create(Signer signer) => new(Guid.NewGuid(), signer);

    public SignedMessageChain.Encoder CreateMessageEncoder(Guid profileId)
        => new SignedMessageChain(profileId, SessionId).CreateEncoder(Signer);

    //AsRemote the remote view of this session, which keeps the public side only
    public RemoteChatSession AsRemote(SignatureValidator validator, Func<bool> hasExpired)
        => new(SessionId, validator, hasExpired);
}
