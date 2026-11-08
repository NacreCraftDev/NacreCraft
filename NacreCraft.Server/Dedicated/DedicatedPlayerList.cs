using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Game.Server;

//DedicatedPlayerList the player list installed by the dedicated server, maps to vanilla net.minecraft.server.dedicated.DedicatedPlayerList
//Besides the base player list it applies view/simulation distance from server.properties and adds the operator rules:
//operators bypass the whitelist and, with bypassesPlayerLimit, a full server
public sealed class DedicatedPlayerList : PlayerList
{
    public DedicatedPlayerList(DedicatedServer server, int maxPlayers) : base(server, maxPlayers)
    {
        //The list is loaded by its own constructor, reload keeps the vanilla "load then save" startup order observable in the log
        ReloadWhiteList();
        //Seed the distances from the settings file before the first player joins, maps to setViewDistance/setSimulationDistance in the vanilla constructor
        SetViewDistance(server.Settings.ViewDistance);
        SetSimulationDistance(server.Settings.SimulationDistance);
    }

    //DedicatedServer the owning server, maps to the covariant getServer of vanilla DedicatedPlayerList
    public DedicatedServer DedicatedServer => (DedicatedServer)Server;

    //IsWhiteListed non-members are rejected while the whitelist is on, operators and listed players are allowed
    //Maps to vanilla DedicatedPlayerList.isWhiteListed (!isUsingWhitelist || isOp || whiteList.isWhiteListed)
    public override bool IsWhiteListed(GameProfile profile)
        => !Server.IsWhiteListEnabled || Server.OpList.IsOp(profile) || Server.WhiteList.IsAllowed(profile);

    //CanBypassPlayerLimit only operators flagged with bypassesPlayerLimit may enter a full server, maps to vanilla canBypassPlayerLimit
    public override bool CanBypassPlayerLimit(GameProfile profile)
        => Server.OpList.CanBypassPlayerLimit(profile);

    //ReloadWhiteList re-reads whitelist.json, maps to vanilla reloadWhiteList and backs /whitelist reload
    public void ReloadWhiteList() => Server.WhiteList.Reload();
}
