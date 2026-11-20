using NacreCraft.Game.Server;
using NacreCraft.Network;
using NacreCraft.Network.Protocol.Common;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Game.Server.Network;

//CommonListenerCookie the state carried across a listener reconfiguration, maps to vanilla net.minecraft.server.network.CommonListenerCookie
public sealed record CommonListenerCookie(
    GameProfile GameProfile,
    int Latency,
    ClientInformation ClientInformation,
    bool Transferred)
{
    public static CommonListenerCookie CreateInitial(GameProfile profile, bool transferred)
        => new(profile, 0, ClientInformation.Default, transferred);
}

//ConfigurationTask one configuration-phase task, maps to vanilla net.minecraft.server.network.ConfigurationTask
//The connection sink carries IPacket because NC's configuration packets do not share one listener interface yet
public interface ConfigurationTask
{
    void Start(Action<IPacket> connection);

    ConfigurationTask.Type TaskType { get; }

    bool Tick() => false;

    //Type task identity compared by id, maps to vanilla ConfigurationTask.Type
    public sealed record Type(string Id)
    {
        public override string ToString() => Id;
    }
}

//ServerPlayerConnection a connection bound to a server player, maps to vanilla net.minecraft.server.network.ServerPlayerConnection
public interface ServerPlayerConnection
{
    ServerPlayer GetPlayer();

    void Send(IPacket packet);
}
