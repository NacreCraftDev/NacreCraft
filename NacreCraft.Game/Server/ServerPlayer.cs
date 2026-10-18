using NacreCraft.Network.Protocol.Common;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level;
using NacreCraft.Logging;
using NacreCraft.Network;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Primitives;
//Attribute-related types carry their own namespace; only the needed names are taken here
using AttributeMap = NacreCraft.Registry.EntityAttribute.AttributeMap;
using AttributeSupplier = NacreCraft.Registry.EntityAttribute.AttributeSupplier;
using AttributeDef = NacreCraft.Registry.EntityAttribute.Attribute;
using EntityAttributes = NacreCraft.Registry.EntityAttribute.Attributes;
//The effect class has the same name as the registry placeholder interface, so the effect implementation class gets a short alias
using GameMobEffect = NacreCraft.Game.World.Effect.MobEffect;

namespace NacreCraft.Game.Server;

//ServerPlayer server-side player object, maps to vanilla ServerPlayer
//Minimal implementation holding Connection/GameProfile/position/game type for PlayerList to manage
//Does not extend NacreCraft.Registry.Entity to avoid confusion with the existing entity system; this round only does the network-layer player
//Implements ITrackedEntity so players and entities share one tracking sync path
//Implements ISyncedEntity so metadata sync shares a mechanism with entities such as drops
public sealed class ServerPlayer : Player
{
    public GameProfile Profile { get; }
    public Connection Connection { get; }

    //Level the dimension this player is in, viewed as the concrete server level
    //The base holds it as a plain object because the registry layer cannot name the level types; this is the same storage
    //behind a typed view, not a second field
    public new ServerLevel Level
    {
        get => (ServerLevel)base.Level!;
        internal set => base.Level = value;
    }

    //GameType the player's game type, initialized from the server default in PlaceNewPlayer
    //On switch it syncs the inventory's infinite materials flag: creative swallows items it cannot fit without counting it as failure
    //Only creative sets instabuild, spectator does not, consistent with vanilla updatePlayerAbilities
    public GameType GameType
    {
        get => _gameType;
        set
        {
            _gameType = value;
            Inventory.InfiniteMaterials = value == GameType.Creative;
            //Abilities are recomputed with the mode; the creative branch does not touch the flight state, so the player's toggled flight survives a mode switch, maps to vanilla updatePlayerAbilities
            Abilities.ApplyGameType(value);
        }
    }

    private GameType _gameType = GameType.Survival;

    //Abilities player ability state; the join ability packet and the player save both read it, maps to vanilla Player.abilities
    public Abilities Abilities { get; } = new();

    //LastAttacker the player who landed the killing blow, recorded by PlayerList.Hurt so the death announcement can name
    //them; the death event itself carries no damage source, so this is where the name has to come from
    public ServerPlayer? LastAttacker { get; set; }

    //PermissionLevel permission level 0-4, set by PlayerList from ops.json on join
    //After op/deop changes PlayerList updates it and syncs the client, maps to the vanilla ServerPlayer permission level
    public int PermissionLevel { get; set; }

    //HasPermissions whether it reaches the given permission level, maps to vanilla ServerPlayer.hasPermissions
    public bool HasPermissions(int level) => PermissionLevel >= level;

    //Position/Yaw/Pitch typed views onto the entity's own position and rotation
    //The server code that predates the entity base keeps using these names; the storage is Pos/YRot/XRot
    public Vec3 Position { get => Pos; set => Pos = value; }
    public float Yaw { get => YRot; set => YRot = value; }
    public float Pitch { get => XRot; set => XRot = value; }

    //_lastActivePosition the previous tick's position; differing from the current one means the player is active
    private Vec3 _lastActivePosition;

    //LastActiveMillis the last active moment, the baseline for the idle kick timer
    public long LastActiveMillis { get; private set; } = Environment.TickCount64;

    //RespawnPos personal respawn point, maps to the player respawn position set by the vanilla spawnpoint command; empty falls back to the world spawn
    public Vec3? RespawnPos { get; set; }

    //RespawnAngle personal respawn facing
    public float RespawnAngle { get; set; }

    //--- spectator camera, maps to vanilla ServerPlayer.camera ---

    //IsSpectator whether in spectator mode, maps to vanilla isSpectator
    //Velocity and OnGround come from the entity base: movement is reported by the client and written straight onto them
    public override bool IsSpectator() => GameType == GameType.Spectator;

    //_camera the current spectator camera entity; null means the view is on the player, maps to the vanilla camera field
    private ITrackedEntity? _camera;

    //GetCamera the current camera entity; returns the player itself when not spectating, maps to vanilla getCamera
    public ITrackedEntity GetCamera() => _camera ?? this;

    //SetCamera switches the spectator camera, maps to vanilla ServerPlayer.setCamera
    //null means returning to the player's own view; switching syncs the position to the camera and sends the set-camera packet
    //Cross-dimension spectating is not implemented: ServerPlayer.Level is fixed after construction, so a camera in another dimension only follows position without changing dimension
    public void SetCamera(ITrackedEntity? newCamera)
    {
        var oldCamera = GetCamera();
        _camera = newCamera ?? this;
        if (ReferenceEquals(oldCamera, _camera)) return;
        //The position lands at the camera; chunk loading and entity tracking follow the camera, maps to teleportTo in vanilla setCamera
        var pos = _camera.Pos;
        if (Listener is { } listener)
            listener.Teleport(pos, Yaw, Pitch, pos.X, pos.Y, pos.Z, Yaw, Pitch, 0);
        else
            Position = pos;
        Connection.Send(new ClientboundSetCameraPacket(_camera.EntityId));
    }

    //TickCamera each tick a spectator follows the camera position, maps to the camera branch of vanilla ServerPlayer.tick
    //When the camera entity is removed it returns to the player's own view; the position lands directly in server state and the client view is already taken over by the camera packet
    private void TickCamera()
    {
        if (_camera is not { } camera || ReferenceEquals(camera, this)) return;
        if (camera is NacreCraft.Registry.Entity { IsRemoved: true })
        {
            SetCamera(null);
            return;
        }
        Position = camera.Pos;
        Yaw = camera.YRot;
        Pitch = camera.XRot;
    }

    //--- pose and shared flags, maps to vanilla Entity.DATA_SHARED_FLAGS_ID and DATA_POSE ---

    //PoseStanding/PoseCrouching pose ids, aligned with the vanilla Pose enum
    public const int PoseStanding = 0;
    public const int PoseCrouching = 5;

    //IsSneaking sneak state, toggled by the player_command packet
    //The metadata container and the sprint flag come from the entity base
    public bool IsSneaking { get; private set; }

    //A player is ticked by the server's player loop, not by the level entity manager
    public override bool TicksByEntityManager => false;

    //SharedFlags shared flags: bit 1 sneak, bit 3 sprint, the rest unused here
    public byte SharedFlags => (byte)((IsSneaking ? 1 << 1 : 0) | (IsSprinting ? 1 << 3 : 0));

    //PoseId current pose; the model crouches when sneaking
    public int PoseId => IsSneaking ? PoseCrouching : PoseStanding;

    //SetSprinting toggles sprint, maps to vanilla setSprinting
    public void SetSprinting(bool value)
    {
        IsSprinting = value;
        SyncSharedState();
    }

    //SetSneaking toggles sneak, maps to vanilla setShiftKeyDown
    public void SetSneaking(bool value)
    {
        IsSneaking = value;
        SyncSharedState();
    }

    //SyncSharedState writes the shared flags and pose into metadata; Set only bumps the version when the value changed
    private void SyncSharedState()
    {
        SyncedData.Set(NacreCraft.Registry.Entity.SharedFlagsIndex, SharedFlags);
        SyncedData.Set(NacreCraft.Registry.Entity.PoseIndex, PoseId);
    }

    //ChunkSender progressive chunk sender, injected by PlayerList in PlaceNewPlayer and driven by tick
    public ChunkSender? ChunkSender { get; set; }

    //ViewDistanceChunks the player's view distance in chunks, taken from server config in PlaceNewPlayer
    //Entity tracking decides visibility by the smaller of it and the entity tracking distance
    public int ViewDistanceChunks { get; set; }

    //KeepAliveIntervalMillis heartbeat interval aligned with vanilla 15 seconds; it keeps the client's 30-second netty read timeout from firing
    public const long KeepAliveIntervalMillis = 15000;

    //Inventory player inventory, created with the player object; menu slots ultimately land here
    public PlayerInventory Inventory { get; } = new();

    //OwnerList the owning player list; sounds broadcast to the whole server, injected by PlayerList.PlaceNewPlayer
    public PlayerList? OwnerList { get; set; }

    //AddItem core give entry, maps to vanilla Player.addItem
    //All paths handing items to the player converge here; returns whether at least one was placed
    //What cannot be placed stays in the passed stack; the caller decides whether to drop it or leave it in place
    public bool AddItem(ItemStack stack) => Inventory.Add(stack);

    //GiveItem gives an item, dropping what cannot fit at the feet and playing the pickup sound when something was placed; returns the count actually placed
    //maps to the inventory.add plus player.drop combination in the vanilla give command; command giving and picking up share this path
    public int GiveItem(ItemStack stack)
    {
        var total = stack.GetCount();
        Inventory.Add(stack);
        var placed = total - stack.GetCount();
        if (!stack.IsEmpty()) Drop(stack, randomly: false, thrownFromHand: false);
        if (placed > 0) PlayPickupSound();
        ContainerMenu?.SendAllDataToRemote();
        return placed;
    }

    //PlayPickupSound plays the item pickup sound, maps to vanilla ServerPlayer.onItemPickup
    //Volume 0.2, pitch jittering around double by the vanilla formula; this is the only sound exit for items entering the inventory
    public void PlayPickupSound()
    {
        if (OwnerList is null) return;
        var pitch = (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.7f + 1.0f;
        ServerSounds.PlaySound(OwnerList, SoundEvents.ItemPickup, SoundSource.Players,
            Position.X, Position.Y, Position.Z, 0.2f, pitch * 2.0f);
    }

    //DropPickupDelay pickup delay ticks after dropping, maps to vanilla 40 ticks
    private const int DropPickupDelay = 40;

    //Drop drops the item in the selected slot, maps to vanilla ServerPlayer.drop(boolean)
    //all true drops the whole slot, false drops one; the inventory change is synced to the client
    public ItemEntity? Drop(bool all)
    {
        var removed = Inventory.RemoveFromSelected(all);
        ContainerMenu?.SendAllDataToRemote();
        return Drop(removed, randomly: false, thrownFromHand: true);
    }

    //Drop spawns the drop entity and adds it to the level, maps to vanilla LivingEntity.drop and createItemStackToDrop
    //The position is 0.3 blocks below the eyes; after dropping it has a 40-tick pickup delay so it is not picked back immediately
    public ItemEntity? Drop(ItemStack stack, bool randomly, bool thrownFromHand)
    {
        if (stack.IsEmpty()) return null;
        if (Level is not PersistentServerLevel level) return null;
        var drop = new ItemEntity(EntityTypes.ITEM)
        {
            Pos = new Vec3(Position.X, Position.Y + EyeHeight - 0.3, Position.Z),
            Item = stack,
            Thrower = thrownFromHand ? Profile.Id : null,
        };
        drop.SetPickUpDelay(DropPickupDelay);
        drop.Velocity = DropVelocity(randomly);
        level.AddEntity(drop);
        return drop;
    }

    //DropVelocity drop velocity, maps to the two branches of vanilla createItemStackToDrop
    //randomly is used for random scatter such as death drops; a hand drop is thrown along the look direction with a little random jitter
    private Vec3 DropVelocity(bool randomly)
    {
        if (randomly)
        {
            var power = Random.Shared.NextSingle() * 0.5f;
            var angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            return new Vec3(-MathF.Sin(angle) * power, 0.2, MathF.Cos(angle) * power);
        }
        const float toRadians = MathF.PI / 180f;
        var sinX = MathF.Sin(Pitch * toRadians);
        var cosX = MathF.Cos(Pitch * toRadians);
        var sinY = MathF.Sin(Yaw * toRadians);
        var cosY = MathF.Cos(Yaw * toRadians);
        var spread = Random.Shared.NextSingle() * MathF.PI * 2f;
        var jitter = 0.02f * Random.Shared.NextSingle();
        return new Vec3(
            -sinY * cosX * 0.3f + MathF.Cos(spread) * jitter,
            -sinX * 0.3f + 0.1f + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.1f,
            cosY * cosX * 0.3f + MathF.Sin(spread) * jitter);
    }

    //GetNearestLookingDirection the nearest of the six directions to the player's look; needed when placing blocks such as observers that include up/down, maps to the vanilla same-named method
    //The look vector is computed by vanilla getViewVector and includes pitch, so UP and DOWN can come out
    public Direction GetNearestLookingDirection()
    {
        const float toRadians = MathF.PI / 180f;
        var pitch = Pitch * toRadians;
        var yaw = -Yaw * toRadians;
        var cosPitch = MathF.Cos(pitch);
        return Direction.FromViewVector(
            MathF.Sin(yaw) * cosPitch, -MathF.Sin(pitch), MathF.Cos(yaw) * cosPitch);
    }

    //AttackDamage bare-hand attack damage from the attack_damage attribute, maps to the vanilla player's ATTACK_DAMAGE
    //Weapon bonus/cooldown/crit are not implemented, added in phase 2
    public float AttackDamage => (float)GetAttributeValue(EntityAttributes.AttackDamage);

    //XpProgress view onto the base's XpP, kept because the command and save layers predate the rename
    public float XpProgress { get => XpP; set => XpP = value; }

    //ContainerMenu the menu the player currently has open; PlaceNewPlayer creates the inventory menu and sends the initial content
    public AbstractContainerMenu? ContainerMenu { get; set; }

    //BackpackMenu the player inventory menu; switching back to it after closing a container menu, maps to vanilla inventoryMenu
    public InventoryMenu BackpackMenu { get; private set; } = null!;

    //_containerCounter container menu id allocator; vanilla starts at 1, with 0 reserved for the inventory menu
    private int _containerCounter;

    //SetUpInventoryMenu builds the inventory menu and makes it current, called once when the player enters the world
    public void SetUpInventoryMenu()
    {
        BackpackMenu = new InventoryMenu(Inventory) { Synchronizer = new ServerContainerSynchronizer(this) };
        ContainerMenu = BackpackMenu;
    }

    //OpenMenu opens a menu: closes the previous container menu, allocates an id, sends open_screen then syncs the initial content
    //maps to vanilla ServerPlayer.openMenu(MenuProvider)
    public void OpenMenu(MenuProvider provider)
    {
        if (ContainerMenu is { } current && !ReferenceEquals(current, BackpackMenu)) DoCloseContainer();
        var menu = provider.CreateMenu(++_containerCounter, Inventory, this);
        if (menu is null) return;
        menu.Synchronizer = new ServerContainerSynchronizer(this);
        ContainerMenu = menu;
        if (menu.Kind is { } kind)
            Connection.Send(new ClientboundOpenScreenPacket(menu.ContainerId, kind, provider.DisplayName));
        menu.SendAllDataToRemote();
    }

    //CloseContainer the server closes the container menu, telling the client first then switching back to the backpack
    //Used when a block is broken or the player walks away, maps to vanilla ServerPlayer.closeContainer
    public void CloseContainer()
    {
        if (ContainerMenu is not { } menu || ReferenceEquals(menu, BackpackMenu)) return;
        Connection.Send(new ClientboundContainerClosePacket(menu.ContainerId));
        DoCloseContainer();
    }

    //DoCloseContainer switches back to the inventory menu and syncs the inventory content, maps to vanilla doCloseContainer
    //A client-initiated menu close only goes here; the server does not send container_close back, consistent with vanilla
    public void DoCloseContainer()
    {
        if (BackpackMenu is null) return;
        ContainerMenu = BackpackMenu;
        BackpackMenu.SendAllDataToRemote();
    }

    //SendSystemMessage sends system chat through the chat bar by default, maps to vanilla sendSystemMessage(Component)
    public void SendSystemMessage(Component message) => SendSystemMessage(message, false);

    //SendOverlayMessage sends an overlay message rendered in the action bar, maps to vanilla sendOverlayMessage
    //This is the generic action bar entry; the title command's actionbar branch has a dedicated packet and both render the same on the client
    public void SendOverlayMessage(Component message) => SendSystemMessage(message, true);

    //SendSystemMessage with overlay true uses the action bar, maps to vanilla sendSystemMessage(Component,boolean)
    public void SendSystemMessage(Component message, bool overlay)
        => Connection.Send(new ClientboundSystemChatPacket(message, overlay));

    //SendBuildLimitMessage build height out-of-bounds message, maps to vanilla sendBuildLimitMessage
    //Above the upper bound gives build.tooHigh with 319, below the lower bound build.tooLow with -64, both red action bar text
    public void SendBuildLimitMessage(bool isTooHigh, int limit)
        => SendOverlayMessage(Component.Translatable(isTooHigh ? "build.tooHigh" : "build.tooLow", limit)
            .WithStyle(ChatFormatting.Red));

    //Listener the associated play-stage listener, injected by DedicatedServer.TransitionToGame
    //Teleports must go through its wait-for-client-ack flow; directly changing coordinates and sending packets would be pulled back by in-flight old position packets
    public ServerGamePacketListenerImpl? Listener { get; set; }

    private long _keepAliveSentAt;
    private long _keepAliveId;
    private bool _keepAlivePending;
    //_lastChunkX/_lastChunkZ the last view center chunk, for cross-chunk detection, initial invalid value
    private int _lastChunkX = int.MinValue;
    private int _lastChunkZ = int.MinValue;

    //ChangeLevel repoints the player at another dimension, maps to the level move in vanilla ServerPlayer.changeDimension
    //The chunk sender is bound to one dimension's chunk source, so it is rebuilt against the new one
    //The cached chunk position is invalidated as well: crossing dimensions is not a chunk crossing, and if the coordinate
    //happens to be unchanged UpdateChunkTracking would skip UpdateCenter and the client would keep the old level's chunks
    //The respawn packet is not sent here; only the caller knows the server-wide spawn info such as seed and dimension type
    public void ChangeLevel(PersistentServerLevel target)
    {
        Level = target;
        _lastChunkX = int.MinValue;
        _lastChunkZ = int.MinValue;
        ChunkSender = new ChunkSender(Connection, target.GetChunk, target.IsChunkFailed, target.ChunkSource,
            target.BlockEntityBridge);
    }

    //--- mob effects, maps to vanilla LivingEntity.activeEffects ---

    //_activeEffects the effects currently active on the player, indexed by effect value
    //Effects live in the entity base; these overrides add only the client sync a server player owes
    //They map to the vanilla ServerPlayer onEffectAdded/onEffectUpdated/onEffectRemoved, which send to this connection alone

    //AddEffect applies an effect and tells this connection about it
    public override bool AddEffect(MobEffectInstance instance)
    {
        if (!base.AddEffect(instance)) return false;
        Connection.Send(ClientboundUpdateMobEffectPacket.Create(EntityId, instance,
            instance.Effect.Value is GameMobEffect gameEffect && gameEffect.NeedsBlend));
        return true;
    }

    //RemoveEffect removes one effect and tells this connection
    public override bool RemoveEffect(Holder<NacreCraft.Registry.MobEffect> effect)
    {
        if (!base.RemoveEffect(effect)) return false;
        Connection.Send(new ClientboundRemoveMobEffectPacket(EntityId, effect));
        return true;
    }

    //RemoveAllEffects clears every effect, telling this connection once per effect
    public override bool RemoveAllEffects()
    {
        var effects = Effects.Map.Keys.ToList();
        if (!base.RemoveAllEffects()) return false;
        foreach (var effect in effects)
            Connection.Send(new ClientboundRemoveMobEffectPacket(EntityId, effect));
        return true;
    }

    //TickEffects ages the effects, reporting the ones that expired to this connection
    public override void TickEffects()
    {
        var before = Effects.Map.Keys.ToList();
        base.TickEffects();
        foreach (var effect in before)
        {
            if (Effects.Map.ContainsKey(effect)) continue;
            Connection.Send(new ClientboundRemoveMobEffectPacket(EntityId, effect));
        }
    }

    public ServerPlayer(GameProfile profile, Connection connection, ServerLevel level)
    {
        SetId(level.GetNextEntityId());
        Profile = profile;
        Connection = connection;
        Level = level;
        Uuid = profile.Id;
        //Default position just above the origin; PlayerList moves the player onto its spawn right after
        Pos = new Vec3(0, 64, 0);
            //The player attribute map takes the default by entity type, from the same source as the client's local one; falls back to an empty map when unregistered
        SetAttributes(new AttributeMap(DefaultAttributes.GetSupplier(EntityTypes.PLAYER) ?? AttributeSupplier.Empty));
            //A new player is at full health, maps to setHealth(getMaxHealth()) in the vanilla LivingEntity constructor
        SetHealth(MaxHealth);
        _keepAliveSentAt = Environment.TickCount64;
            //Player metadata baseline, maps to vanilla Player's defineSynchedData
        SyncedData.Define(NacreCraft.Registry.Entity.SharedFlagsIndex, EntityDataSerializers.Byte, (byte)0);
        SyncedData.Define(NacreCraft.Registry.Entity.PoseIndex, EntityDataSerializers.Pose, PoseStanding);
    }

        //Tick per-frame scheduling, maps to vanilla ServerPlayer.tick
        //On a chunk crossing it updates the chunk view, drives ChunkSender progressive sending, keeps the heartbeat and syncs the menu's dirty slots to the client
    public override void Tick()
    {
            //Only the bookkeeping half of the base tick runs: the physics half would fight the position the client reports
            //Keeping it is what advances the tick counter and the previous position, which tracking and animation read
        TickBase();
            //A spectator follows the camera position every tick, maps to the camera branch near the start of vanilla ServerPlayer.tick
        TickCamera();
            //Mob effects decrement duration every tick; expired ones are removed automatically and synced to the client
        TickEffects();
            //Invulnerability frames decrement every tick, maps to invulnerableTime-- in vanilla LivingEntity.tick
        if (InvulnerableTime > 0) InvulnerableTime--;
            //A position change counts as active; the idle kick timer resets from it
        if (Position != _lastActivePosition)
        {
            _lastActivePosition = Position;
            LastActiveMillis = Environment.TickCount64;
        }
            //Out-of-bounds damage; creative and spectator carry the invulnerable flag and vanilla blocks it via isInvulnerableTo
        if (GameType != GameType.Creative && GameType != GameType.Spectator) TickWorldBorderDamage();
        UpdateChunkTracking();
        ChunkSender?.Tick();
        TickItemPickup();
        ContainerMenu?.BroadcastChanges();
            //A container menu is closed automatically when it becomes invalid, a block broken or the player more than 8 blocks away, maps to the stillValid check in vanilla ServerPlayer.tick
        if (ContainerMenu is { } openMenu && BackpackMenu is not null
            && !ReferenceEquals(openMenu, BackpackMenu) && !openMenu.StillValid(this))
            CloseContainer();
        TickKeepAlive();
            //A player's position is reported by the client, so there is no move of ours to hang the inside-block check on
            //and it runs from the tick instead; now that a player is an entity, the block gets the real entity, not just a box
        Level?.CheckInsideBlocks(this);
    }

        //TickItemPickup checks drops underfoot and nearby and tries to pick them up
        //Vanilla triggers playerTouch on entity contact during movement; this project has no entity-entity collision and queries actively each tick by the pickup box
    private void TickItemPickup()
    {
        if (Level is not PersistentServerLevel level) return;
        var box = PickupBox();
        foreach (var entity in level.EntitiesInBox(box))
        {
            if (entity is not ItemEntity item) continue;
                //The spatial index is the bucketing result; filter once more by bounding box intersection
            if (!box.Intersects(item.BoundingBox)) continue;
            item.PlayerTouch(this);
        }
    }

        //PickupBox the pickup test box: the player hit box inflated by vanilla 1.0/0.5/1.0 blocks
    private AABB PickupBox() => BoundingBox.Inflate(1.0, 0.5, 1.0);

        //TickWorldBorderDamage continuous damage outside the world border, maps to the border damage section of vanilla LivingEntity.baseTick
        //No damage within the damage buffer; damage is the out-of-bounds blocks times damage per block with a minimum of 1
    private void TickWorldBorderDamage()
    {
        var border = Level.WorldBorder;
        var box = BoundingBox;
        if (border.IsWithinBounds(box)) return;
        var distance = border.GetDistanceToBorder(Position.X, Position.Z) + border.SafeZone;
        if (distance >= 0.0 || border.DamagePerBlock <= 0.0) return;
        Hurt(Math.Max(1f, MathF.Floor((float)(-distance * border.DamagePerBlock))));
    }

        //UpdateChunkTracking updates the view center when a player crosses a chunk, maps to vanilla ChunkMap.move
        //Each tick it compares the current chunk with the last recorded one and notifies ChunkSender only on change
    private void UpdateChunkTracking()
    {
        var chunkX = (int)Math.Floor(Position.X / 16);
        var chunkZ = (int)Math.Floor(Position.Z / 16);
        if (chunkX == _lastChunkX && chunkZ == _lastChunkZ) return;
        _lastChunkX = chunkX;
        _lastChunkZ = chunkZ;
        ChunkSender?.UpdateCenter(chunkX, chunkZ, ViewDistanceChunks);
            //Player tickets follow the view center: load tickets within view distance and a simulation ticket for the player's own chunk, maps to vanilla ChunkMap.move
        if (Level is PersistentServerLevel persistent)
            persistent.ChunkSource.UpdatePlayerTickets(this, chunkX, chunkZ, ViewDistanceChunks);
    }

        //TickKeepAlive sends a heartbeat every 15 seconds
        //If the client receives no packet for 30 seconds it declares a read timeout and disconnects; after chunks are sent the server has no other outbound packets
        //An unanswered last heartbeat means the link is dead and disconnects directly, aligned with vanilla ServerGamePacketListenerImpl.tick
    private void TickKeepAlive()
    {
        if (!Connection.IsConnected) return;
        var now = Environment.TickCount64;
        if (now - _keepAliveSentAt < KeepAliveIntervalMillis) return;
        if (_keepAlivePending)
        {
            Log.Warning($"Disconnecting due to keep-alive timeout {Profile.Name}");
            Disconnect("disconnect.timeout");
            return;
        }
        _keepAliveSentAt = now;
        _keepAliveId = now;
        _keepAlivePending = true;
        try
        {
            Connection.Send(new ClientboundKeepAlivePacket(_keepAliveId));
        }
        catch (Exception e)
        {
            Log.Warning($"Keep-alive send failed {Profile.Name} {e.Message}");
        }
    }

        //PendingKeepAliveId the heartbeat id awaiting a reply; null when nothing is pending
        //For a fake client to emulate a real client's reply behavior; a fake connection has no netty to auto-reply
    public long? PendingKeepAliveId => _keepAlivePending ? _keepAliveId : null;

        //HandleKeepAliveResponse clears the wait marker after the client replies to the heartbeat
    public void HandleKeepAliveResponse(long id)
    {
            //Prints the match result, used when a heartbeat times out to distinguish a missing reply from an id mismatch
        Log.Debug($"HandleKeepAliveResponse id={id} expected={_keepAliveId} pending={_keepAlivePending} match={id == _keepAliveId}");
        if (id == _keepAliveId) _keepAlivePending = false;
    }

        //Disconnect disconnects the player, aligned with vanilla ServerPlayer.disconnect
        //Disconnect actively disconnects the player, maps to vanilla ServerGamePacketListenerImpl.disconnect
        //The disconnect packet must be sent before closing the connection, or the client only sees the connection drop without the kick reason
        //Vanilla stop goes through multiPlayerList.removeAll sending multiplayer.disconnect.server_shutdown to every player
    public void Disconnect(string reason)
    {
        if (Connection.IsConnected)
            Connection.Send(new ClientboundDisconnectPacket(Component.Literal(reason)));
        Connection.Disconnect(reason);
    }

        //Disconnect with a component reason; bans/kicks use a translation key component and the client shows it in its local language
    public void Disconnect(Component reason)
    {
        if (Connection.IsConnected)
            Connection.Send(new ClientboundDisconnectPacket(reason));
        Connection.Disconnect("disconnected");
    }
}
