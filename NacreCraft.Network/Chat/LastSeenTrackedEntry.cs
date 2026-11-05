namespace NacreCraft.Network.Chat;

//LastSeenTrackedEntry one tracked last-seen signature, maps to vanilla net.minecraft.network.chat.LastSeenTrackedEntry
public sealed record LastSeenTrackedEntry(MessageSignature Signature, bool Pending)
{
    public LastSeenTrackedEntry Acknowledge() => Pending ? new LastSeenTrackedEntry(Signature, false) : this;
}
