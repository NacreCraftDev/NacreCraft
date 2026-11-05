using NacreCraft.Network.Protocol.Common;

namespace NacreCraft.Game.Server;

//ClientInformation the settings a client reported, maps to vanilla net.minecraft.server.level.ClientInformation
//Built from the client information packet and read by chat, skin and particle handling
public sealed record ClientInformation(
    string Language,
    int ViewDistance,
    ChatVisibility ChatVisibility,
    bool ChatColors,
    int ModelCustomisation,
    HumanoidArm MainHand,
    bool TextFiltering,
    bool AllowListing,
    ParticleStatus ParticleStatus)
{
    public const int MaxLanguageLength = ServerboundClientInformationPacket.MaxLanguageLength;
    public const int MaxViewDistance = ServerboundClientInformationPacket.MaxViewDistance;

    //Default what an unknown client is assumed to use, maps to vanilla DEFAULT
    public static readonly ClientInformation Default = new(
        "en_us", MaxViewDistance, ChatVisibility.Full, true, 0, HumanoidArm.Right, false, false,
        ParticleStatus.All);
}
