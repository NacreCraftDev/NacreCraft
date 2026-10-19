using NacreCraft.Network.Protocol.Cookie;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Logging;
using NacreCraft.Network;
using NacreCraft.Game.Server.Progress;
using NacreCraft.Storage;

namespace NacreCraft.Network.Protocol.Configuration;

//ServerConfigurationPacketListenerImpl, the server configuration listener implementation
//The core is HandleConfigurationFinished switching to the Play phase and triggering PlayerList.PlaceNewPlayer
//Maps to the vanilla task queue SynchronizeRegistries -> PrepareSpawn -> JoinWorld
//finish_configuration is sent only after the spawn preload completes, maps to JoinWorldTask being released after PrepareSpawnTask finishes
//The other common/cookie sub-protocol packets are left unimplemented because ConfigurationProtocols does not register them and they are never decoded
public sealed class ServerConfigurationPacketListenerImpl : ServerConfigurationPacketListener, TickablePacketListener
{
    private readonly Connection _connection;
    private readonly ServerConfigurationContext _context;
    private readonly GameProfile _profile;
    //Spawn preload submitted for this connection, maps to chunkLoadFuture and ChunkLoadCounter in vanilla PrepareSpawnTask
    private SpawnChunkPreload? _spawnPreload;

    public ServerConfigurationPacketListenerImpl(Connection connection, GameProfile profile, ServerConfigurationContext context)
    {
        _connection = connection;
        _profile = profile;
        _context = context;
    }

    //HandleConfigurationFinished switches to the Play phase after the client finishes configuration and attaches ServerGamePacketListenerImpl
    public void HandleConfigurationFinished(ServerboundFinishConfigurationPacket packet)
    {
        Log.Debug($"HandleConfigurationFinished entry profile={_profile.Name}");
        _context.TransitionToGame(_connection, _profile);
        //Log.Debug("HandleConfigurationFinished exit");
    }

    //HandleSelectKnownPacks sends registry_data and starts the spawn preload after the client replies with the selected known packs
    //Aligns with the timing of vanilla SynchronizeRegistriesTask.handleResponse, empty contents relies on the client having initialized local resources by then
    //finish_configuration is moved to TickListener and sent once the spawn chunks are ready, avoiding players seeing unloaded void when entering the world
    public void HandleSelectKnownPacks(ServerboundSelectKnownPacks packet)
    {
        Log.Debug($"HandleSelectKnownPacks entry packs={packet.KnownPacks.Count}");
        try
        {
            _context.SendSynchronizedRegistries(_connection);
            _spawnPreload = _context.PrepareSpawnChunks();
            Log.Debug($"HandleSelectKnownPacks sent registry_data and submitted spawn preload for {_spawnPreload.Value.Tasks.Length} chunks");
        }
        catch (Exception e)
        {
            Log.Warning($"registry_data send failed {_profile.Name} {e.Message}");
        }
    }

    //TickListener polls the spawn preload tasks each tick and sends finish_configuration once all complete
    //Maps to the chunkLoadFuture wait in vanilla PrepareSpawnTask.tick and the release of JoinWorldTask
    //A failed load is also released, the play phase ChunkSender drops it by IsChunkFailed
    public void TickListener()
    {
        if (_spawnPreload is not { } preload) return;
        //Tickets time out after only 20 ticks, they must be renewed while loading is unfinished, maps to vanilla PrepareSpawnTask.Ready.keepAlive
        //Without renewal the chunks are reclaimed once the ticket expires and the player still sees holes when entering the world
        _context.KeepAliveSpawnTickets();
        _context.LoadListener.Update(LevelLoadListener.Stage.LoadPlayerChunks, preload.Counter.ReadyChunks, preload.Counter.TotalChunks);
        if (preload.Counter.PendingChunks() > 0) return;
        foreach (var task in preload.Tasks)
            if (!task.IsCompleted) return;
        _context.LoadListener.Finish(LevelLoadListener.Stage.LoadPlayerChunks);
        _connection.Send(ClientboundFinishConfigurationPacket.Instance);
        _spawnPreload = null;
        Log.Debug("Spawn preload finished, finish_configuration sent");
    }

    //HandleAcceptCodeOfConduct, code of conduct acceptance, left unimplemented
    public void HandleAcceptCodeOfConduct(ServerboundAcceptCodeOfConductPacket packet)
    {
        //Log.Debug("HandleAcceptCodeOfConduct entry");
    }

    //The following are methods inherited from ServerCommonPacketListener
    //ConfigurationProtocols does not register these packets, they are never decoded, so they are left unimplemented
    public void HandleClientInformation(ServerboundClientInformationPacket packet) { }
    public void HandleCustomPayload(ServerboundCustomPayloadPacket packet) { }
    public void HandleKeepAlive(ServerboundKeepAlivePacket packet) { }
    public void HandlePong(ServerboundPongPacket packet) { }
    public void HandleResourcePack(ServerboundResourcePackPacket packet) { }
    public void HandleCustomClickAction(ServerboundCustomClickActionPacket packet) { }

    //Inherited from ServerCookiePacketListener
    public void HandleCookieResponse(ServerboundCookieResponsePacket packet) { }

    public void OnDisconnect(string reason)
    {
        Log.Info($"configuration phase disconnect reason={reason} profile={_profile.Name}");
    }
}

//ServerConfigurationContext, configuration phase context, implemented by DedicatedServer and wrapping the switch to Game logic
public interface ServerConfigurationContext
{
    //TransitionToGame switches the connection to the Play phase, attaches ServerGamePacketListenerImpl and triggers PlayerList.PlaceNewPlayer
    void TransitionToGame(Connection connection, GameProfile profile);

    //SendSynchronizedRegistries sends registry_data for all SYNCHRONIZED_REGISTRIES
    void SendSynchronizedRegistries(Connection connection);

    //PrepareSpawnChunks submits chunk loading around the spawn point, maps to the PLAYER_SPAWN ticket in vanilla PrepareSpawnTask
    SpawnChunkPreload PrepareSpawnChunks();

    //KeepAliveSpawnTickets renews the spawn preload tickets, maps to vanilla PrepareSpawnTask.Ready.keepAlive
    void KeepAliveSpawnTickets();

    //LoadListener the level loading progress listener, maps to vanilla PrepareSpawnTask.loadListener
    LevelLoadListener LoadListener { get; }
}

//SpawnChunkPreload the result of one spawn preload, each configuration connection holds its own
public readonly record struct SpawnChunkPreload(Task[] Tasks, ChunkLoadCounter Counter);
