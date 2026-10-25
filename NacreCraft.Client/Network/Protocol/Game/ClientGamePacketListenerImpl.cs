using NacreCraft.Commands;
using NacreCraft.Commands.Tree;
using NacreCraft.Client.Player.Inventory;
using NacreCraft.Client.Level;
using NacreCraft.Network.Protocol.Common;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Cookie;
using NacreCraft.Network.Protocol.Ping;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Network;
using NacreCraft.Network.Protocol;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Chunk;
using NacreCraft.Util;

namespace NacreCraft.Client.Network.Protocol.Game;

//ClientGamePacketListenerImpl client play listener implementation
//Maps to vanilla ClientPacketListener; this round implements the minimal subset for entering the world
//HandleLogin records entityId/gameType and triggers the onJoinWorld callback
//HandleMovePlayer syncs the local player position/orientation; HandleLevelChunkWithLight loads chunks into ClientLevel
//HandleSetHealth/SetExperience update the Player state; HandleForgetLevelChunk unloads chunks
//The remaining 120+ packets are empty implementations, expanded on demand
public sealed class ClientGamePacketListenerImpl : ClientGamePacketListener
{
    private readonly Connection _connection;
    private readonly ClientLevel? _level;
    private readonly World.Entity.Player _player;
    //OnJoinWorld fired once on the Login packet; MinecraftClient switches to GameScreen
    private readonly System.Action? _onJoinWorld;
    private bool _joinNotified;

    public ClientGamePacketListenerImpl(Connection connection, ClientLevel? level, World.Entity.Player player, System.Action? onJoinWorld = null)
    {
        _connection = connection;
        _level = level;
        _player = player;
        _onJoinWorld = onJoinWorld;
    }

    //Explicitly returns Play because the inheritance chain's default Protocol is Configuration
    ConnectionProtocol PacketListener.Protocol => ConnectionProtocol.Play;

    //Inventory client inventory cache; container packet results land here for the GUI and rendering to read
    public ClientInventory Inventory { get; } = new();

    //State client-side copy of the world/player data the play packets carry
    //The handler writes it here and presentation layers read it; no UI or audio is driven from the network thread
    public ClientPacketState State { get; } = new();

    //OnOpenScreen screen callback on open_screen, injected by MinecraftClient
    //The listener only does protocol parsing; the screen layer does not know network packets, and no screen is switched when not injected
    public System.Action<ClientboundOpenScreenPacket>? OnOpenScreen { get; set; }

    //OnContainerClose closes the container screen on container_close, injected by MinecraftClient
    public System.Action? OnContainerClose { get; set; }

    //CommandRoot the command tree root held by the client, sent by ClientboundCommandsPacket
    public CommandNode<CommandSourceStack>? CommandRoot { get; private set; }

    //HandleLogin on entering the world records entityId/gameType and triggers the join callback once
    public void HandleLogin(ClientboundLoginPacket packet)
    {
        _player.GameMode = packet.CommonPlayerSpawnInfo.GameType.Id;
        _player.IsHardcore = packet.Hardcore;
        if (_joinNotified) return;
        _joinNotified = true;
        Log.Info($"Entered world entityId={packet.PlayerId} gameType={packet.CommonPlayerSpawnInfo.GameType.Name}");
        _onJoinWorld?.Invoke();
    }

    //HandleMovePlayer server syncs the player position, maps to vanilla ClientPacketListener.handleMovePlayer
    //The packet components are added to the current value per relatives to restore the absolute position; without this step relative teleports are wrong
    //Immediately reply with the acknowledgment packet and own position; before the acknowledgment the server only accepts orientation, using its recorded target for position
    public void HandleMovePlayer(ClientboundPlayerPositionPacket packet)
    {
        var relatives = new HashSet<RelativeFlag>(RelativeFlags.Unpack(packet.Relatives));
        var x = relatives.Contains(RelativeFlag.X) ? _player.Pos.X + packet.X : packet.X;
        var y = relatives.Contains(RelativeFlag.Y) ? _player.Pos.Y + packet.Y : packet.Y;
        var z = relatives.Contains(RelativeFlag.Z) ? _player.Pos.Z + packet.Z : packet.Z;
        var yRot = relatives.Contains(RelativeFlag.YRot) ? _player.YRot + packet.YRot : packet.YRot;
        var xRot = relatives.Contains(RelativeFlag.XRot) ? _player.XRot + packet.XRot : packet.XRot;
        _player.Pos = new Vec3(x, y, z);
        _player.YRot = yRot;
        _player.XRot = xRot;
        _connection.Send(new ServerboundAcceptTeleportationPacket(packet.Id));
        _connection.Send(new ServerboundMovePlayerPacket(x, y, z, yRot, xRot, false, false, true, true));
    }

    //HandleLevelChunkWithLight loads chunk data into ClientLevel; the render layer subscribes to events and compiles meshes automatically
    public void HandleLevelChunkWithLight(ClientboundLevelChunkWithLightPacket packet)
    {
        if (packet.ChunkData is null) return;
        _level?.LoadChunk(packet.ChunkData);
        //Block entities carried by the chunk are filled into the client block entity table too; the chunk packet is sent only once and blocks are not resent individually
        if (_level is not null && packet.ChunkData is LevelChunk { BlockEntityTags: { Count: > 0 } tags })
            StoreChunkBlockEntities(tags);
        if (packet.LightData is not null && _level is not null)
            ApplyLightData(_level, packet.X, packet.Z, packet.LightData);
    }

    //StoreChunkBlockEntities stores block entities into the client block entity table by the coordinates in their NBT
    //The client builds no block entity objects; it only stores NBT by position for rendering and screens to read
    private void StoreChunkBlockEntities(List<CompoundTag> tags)
    {
        foreach (var tag in tags)
        {
            var pos = new BlockPos(tag.GetIntOr("x", 0), tag.GetIntOr("y", 0), tag.GetIntOr("z", 0));
            _level!.SetBlockEntityData(pos, tag);
        }
    }

    //HandleLightUpdatePacket loads incremental light updates into ClientLevel
    public void HandleLightUpdatePacket(ClientboundLightUpdatePacket packet)
    {
        if (_level is null) return;
        ApplyLightData(_level, packet.X, packet.Z, packet.LightData);
    }

    //ApplyLightData restores the sent light per layer into section storage, maps to vanilla applyLightData
    //Sections with a set mask take the sequential updates data; sections with an empty mask are cleared to all 0; sections with neither keep their old value
    private static void ApplyLightData(ClientLevel level, int chunkX, int chunkZ,
        ClientboundLightUpdatePacketData data)
    {
        ApplyLightLayer(level, chunkX, chunkZ, data.SkyYMask, data.EmptySkyYMask, data.SkyUpdates,
            skyLayer: true);
        ApplyLightLayer(level, chunkX, chunkZ, data.BlockYMask, data.EmptyBlockYMask, data.BlockUpdates,
            skyLayer: false);
    }

    //ApplyLightLayer single-layer restore; section range is the light sections derived from the Overworld default -64..320
    private static void ApplyLightLayer(ClientLevel level, int chunkX, int chunkZ,
        byte[] mask, byte[] emptyMask, byte[][] updates, bool skyLayer)
    {
        var updateIndex = 0;
        for (var sectionIndex = 0; sectionIndex < LightSectionCount; sectionIndex++)
        {
            if (ClientboundLightUpdatePacketData.IsSet(mask, sectionIndex))
            {
                if (updateIndex >= updates.Length) break;
                var layer = new DataLayer(updates[updateIndex++]);
                level.SetLightLayer(new SectionPos(chunkX, LightMinSectionY + sectionIndex, chunkZ),
                    skyLayer, layer);
            }
            else if (ClientboundLightUpdatePacketData.IsSet(emptyMask, sectionIndex))
            {
                level.SetLightLayer(new SectionPos(chunkX, LightMinSectionY + sectionIndex, chunkZ),
                    skyLayer, new DataLayer());
            }
        }
    }

    //LightMinSectionY/LightSectionCount Overworld light section range, aligning with -4/24 used by chunk decoding
    private const int LightMinSectionY = -5;
    private const int LightSectionCount = 26;

    //HandleForgetLevelChunk unloads the chunk; the render layer subscribes to ChunkUnloaded and drops its mesh
    public void HandleForgetLevelChunk(ClientboundForgetLevelChunkPacket packet) => _level?.UnloadChunk(packet.Pos);
    //HandleOpenSignEditor the server opens the sign editor; the position is stored for the screen
    public void HandleOpenSignEditor(ClientboundOpenSignEditorPacket packet) => State.OpenSignPos = packet.Pos;

    //HandleSetHealth updates health and hunger
    public void HandleSetHealth(ClientboundSetHealthPacket packet)
    {
        _player.SetHealth(packet.Health);
        _player.FoodLevel = packet.Food;
    }

    //HandleSetExperience updates experience
    public void HandleSetExperience(ClientboundSetExperiencePacket packet)
    {
        _player.XpP = packet.ExperienceProgress;
        _player.XpTotal = packet.TotalExperience;
        _player.XpLevel = packet.ExperienceLevel;
    }

    //HandleChunkBatchStart chunk batch start; no flow control currently, logs only
    public void HandleChunkBatchStart(ClientboundChunkBatchStartPacket packet)
        => Log.Debug("HandleChunkBatchStart");

    //HandleChunkBatchFinished chunk batch finished; no flow control currently, logs only
    public void HandleChunkBatchFinished(ClientboundChunkBatchFinishedPacket packet)
        => Log.Debug($"HandleChunkBatchFinished batchSize={packet.BatchSize}");

    //HandleSetChunkCacheRadius view distance sync; currently uses the local config, callback kept for extension
    public void HandleSetChunkCacheRadius(ClientboundSetChunkCacheRadiusPacket packet)
        => Log.Debug($"HandleSetChunkCacheRadius radius={packet.Radius}");

    //HandleSetChunkCacheCenter view distance center sync; the current player cannot move, ignored
    public void HandleSetChunkCacheCenter(ClientboundSetChunkCacheCenterPacket packet)
        => Log.Debug($"HandleSetChunkCacheCenter ({packet.X},{packet.Z})");

    //The remaining ClientGamePacketListener packets below are empty implementations, expanded on demand
    //HandleAddEntity loads an entity added by the server; orientation bytes are decompressed into angles as in vanilla
    public void HandleAddEntity(ClientboundAddEntityPacket packet)
    {
        _level?.AddEntity(packet.Id, packet.Kind,
            new Vec3(packet.X, packet.Y, packet.Z),
            Mth.UnpackDegrees(packet.YRot), Mth.UnpackDegrees(packet.XRot),
            packet.Movement, false);
        Log.Debug($"HandleAddEntity id={packet.Id} type={packet.Kind.Id}");
    }

    //HandleAddObjective add/change/remove scoreboard objective, maps to vanilla handleAddObjective
    //Method: 0 create, 1 remove, 2 update
    public void HandleAddObjective(ClientboundSetObjectivePacket packet)
    {
        switch (packet.Method)
        {
            case 1:
                State.Objectives.Remove(packet.ObjectiveName);
                foreach (var slot in State.DisplaySlots.Where(e => e.Value == packet.ObjectiveName).Select(e => e.Key).ToArray())
                    State.DisplaySlots.Remove(slot);
                break;
            default:
                State.Objectives[packet.ObjectiveName] = new ClientPacketState.ScoreboardObjective(
                    packet.ObjectiveName, packet.DisplayName, packet.RenderType, packet.NumberFormat, false);
                break;
        }
    }

    //HandleAnimate entity swing/arm animation; recorded as an entity event for the render layer
    public void HandleAnimate(ClientboundAnimatePacket packet)
        => RecordEntityEvent(packet.Id, (byte)packet.Action);

    //HandleHurtAnimation entity hurt tilt; stored so the render layer can play it
    public void HandleHurtAnimation(ClientboundHurtAnimationPacket packet)
        => State.HurtYaw[packet.Id] = packet.Yaw;

    //HandleAwardStats statistic update; stored verbatim until a statistics screen exists
    public void HandleAwardStats(ClientboundAwardStatsPacket packet) => State.Stats = packet.Stats;

    //HandleRecipeBookAdd server adds recipes to the book; stored until the recipe book UI exists
    public void HandleRecipeBookAdd(ClientboundRecipeBookAddPacket packet) => State.Recipes = packet.Entries;

    //HandleRecipeBookRemove server removes recipes from the book
    public void HandleRecipeBookRemove(ClientboundRecipeBookRemovePacket packet) => State.Recipes = packet.Recipes;

    //HandleRecipeBookSettings recipe book visibility settings
    public void HandleRecipeBookSettings(ClientboundRecipeBookSettingsPacket packet) => State.Recipes = packet.BookSettings;
    //HandleBlockDestruction block breaking crack stage; the stock client has no rendering, it only logs for debugging
    public void HandleBlockDestruction(ClientboundBlockDestructionPacket packet)
        => Log.Debug($"HandleBlockDestruction id={packet.Id} pos={packet.Pos} progress={packet.Progress}");
    //HandleBlockEntityData loads block entity state into the client world; empty NBT means the block entity there was removed
    public void HandleBlockEntityData(ClientboundBlockEntityDataPacket packet)
    {
        _level?.SetBlockEntityData(packet.Pos, packet.Tag);
        Log.Debug($"HandleBlockEntityData pos={packet.Pos} type={packet.BlockEntityTypeId}");
    }

    //HandleBlockEvent block event (chest open/close, piston extend/retract, etc.); no visual representation yet, logs only
    public void HandleBlockEvent(ClientboundBlockEventPacket packet)
        => Log.Debug($"HandleBlockEvent pos={packet.Pos} b0={packet.B0} b1={packet.B1} block={packet.BlockId}");
    public void HandleBlockUpdate(ClientboundBlockUpdatePacket packet)
    {
        //Client-side block placement must leave a trace, otherwise "the server did not send" and "the client did not apply" cannot be told apart
        Log.Debug($"Redstone block update received {packet.Pos} state={packet.BlockState} block={BlockStateRegistry.GetState(packet.BlockState).Owner.Id}");
        _level?.SetBlockState(packet.Pos, BlockStateRegistry.GetState(packet.BlockState));
    }
    //HandleSystemChat system message; an overlay message goes to the action bar, the rest to the chat log
    public void HandleSystemChat(ClientboundSystemChatPacket packet)
    {
        if (packet.Overlay)
        {
            State.ActionBar = packet.Content;
            return;
        }
        State.ChatLog.Add(new ClientPacketState.ChatEntry(packet.Content, false, Environment.TickCount64));
    }

    //HandlePlayerChat signed player chat; the unsigned content carries what can be displayed without signature checks
    public void HandlePlayerChat(ClientboundPlayerChatPacket packet)
    {
        if (packet.UnsignedContent is not null)
            State.ChatLog.Add(new ClientPacketState.ChatEntry(packet.UnsignedContent, false, Environment.TickCount64));
    }

    //HandleDisguisedChat chat message without a player signature, used for command output and server messages
    public void HandleDisguisedChat(ClientboundDisguisedChatPacket packet)
        => State.ChatLog.Add(new ClientPacketState.ChatEntry(packet.Message, false, Environment.TickCount64));

    //HandleDeleteChat removes a chat line by signature; the signature type is not decoded yet, so the line is kept
    public void HandleDeleteChat(ClientboundDeleteChatPacket packet)
        => Log.Debug("HandleDeleteChat signature removal ignored");
    //HandleChunkBlocksUpdate batched block update within a section; each entry is (stateId << 12) | a 12-bit in-section offset
    //Maps to runUpdates in vanilla ClientPacketListener.handleChunkBlocksUpdate
    public void HandleChunkBlocksUpdate(ClientboundSectionBlocksUpdatePacket packet)
    {
        if (_level is null) return;
        var baseX = SectionPos.SectionToBlockCoord(packet.SectionPos.X);
        var baseY = SectionPos.SectionToBlockCoord(packet.SectionPos.Y);
        var baseZ = SectionPos.SectionToBlockCoord(packet.SectionPos.Z);
        foreach (var packed in packet.PackedChanges)
        {
            var pos = new BlockPos(
                baseX + SectionPos.RelativeX(packed),
                baseY + SectionPos.RelativeY(packed),
                baseZ + SectionPos.RelativeZ(packed));
            _level.SetBlockState(pos, BlockStateRegistry.GetState((int)(packed >> 12)));
        }
    }
    //HandleMapItemData map color patch and decorations; stored until the map screen exists
    public void HandleMapItemData(ClientboundMapItemDataPacket packet) => State.MapData.Add(packet);
    //HandleContainerClose the server proactively closes the container screen; received when the player walks away or the block is broken
    public void HandleContainerClose(ClientboundContainerClosePacket packet)
    {
        State.OpenContainerId = -1;
        State.OpenContainerTitle = null;
        OnContainerClose?.Invoke();
    }
    //HandleContainerContent full container contents; builds menu slots and records the cursor
    public void HandleContainerContent(ClientboundContainerSetContentPacket packet)
    {
        Inventory.SetContent(packet.ContainerId, packet.StateId, packet.Items, packet.CarriedItem);
        State.CursorItem = packet.CarriedItem;
    }

    //HandleMountScreenOpen opens the mount inventory; the entity id is kept for the screen to read
    public void HandleMountScreenOpen(ClientboundMountScreenOpenPacket packet)
        => State.OpenContainerId = packet.ContainerId;

    //HandleContainerSetData furnace progress, enchantment seed and similar menu properties
    public void HandleContainerSetData(ClientboundContainerSetDataPacket packet)
    {
        if (!State.ContainerData.TryGetValue(packet.ContainerId, out var values))
            State.ContainerData[packet.ContainerId] = values = new Dictionary<int, int>();
        values[packet.Id] = packet.Value;
    }

    //HandleContainerSetSlot single-slot change; packets for a non-current menu are discarded
    public void HandleContainerSetSlot(ClientboundContainerSetSlotPacket packet)
    {
        if (packet.ContainerId != Inventory.ContainerId) return;
        Inventory.SetSlot(packet.Slot, packet.Stack);
    }
    //HandleEntityEvent entity status event (swing, hurt, death, ...); recorded for the render layer
    public void HandleEntityEvent(ClientboundEntityEventPacket packet)
        => RecordEntityEvent(packet.EntityId, packet.EventId);

    //HandleEntityLinkPacket leash attachment; destId 0 means the leash was removed
    public void HandleEntityLinkPacket(ClientboundSetEntityLinkPacket packet)
    {
        if (packet.DestId == 0) State.Leashes.Remove(packet.SourceId);
        else State.Leashes[packet.SourceId] = packet.DestId;
    }

    //HandleSetEntityPassengersPacket replaces the passenger list of the vehicle
    public void HandleSetEntityPassengersPacket(ClientboundSetPassengersPacket packet)
    {
        if (packet.Passengers.Length == 0) State.Passengers.Remove(packet.Vehicle);
        else State.Passengers[packet.Vehicle] = packet.Passengers;
    }

    //HandleExplosion explosion particle, sound and knockback; recorded until the particle and audio layers exist
    public void HandleExplosion(ClientboundExplodePacket packet)
        => Log.Debug($"HandleExplosion at {packet.CenterX},{packet.CenterY},{packet.CenterZ} radius={packet.Radius}");

    //HandleGameEvent world game events; the ones that change persistent client state are applied, the rest are logged
    public void HandleGameEvent(ClientboundGameEventPacket packet)
    {
        if (packet.Event == GameEventType.ChangeGameMode)
        {
            _player.GameMode = (int)packet.Param;
            return;
        }
        if (packet.Event == GameEventType.RainLevelChange)
        {
            State.RainLevel = packet.Param;
            return;
        }
        if (packet.Event == GameEventType.ThunderLevelChange)
        {
            State.ThunderLevel = packet.Param;
            return;
        }
        Log.Debug($"HandleGameEvent {packet.Event.Name} param={packet.Param}");
    }

    //HandleChunksBiomes biome data for already loaded chunks; stored until the chunk store exposes a biome update
    public void HandleChunksBiomes(ClientboundChunksBiomesPacket packet)
        => Log.Debug($"HandleChunksBiomes chunks={packet.Chunks.Count}");

    //HandleLevelEvent world event (sound, particle, break effect); recorded until the effect layer exists
    public void HandleLevelEvent(ClientboundLevelEventPacket packet)
        => Log.Debug($"HandleLevelEvent kind={packet.Kind} pos={packet.Pos} data={packet.Data}");

    //RecordEntityEvent keeps the latest status event per entity for the render layer to consume
    private void RecordEntityEvent(int entityId, byte eventId)
        => State.LastEntityEvent[entityId] = eventId;
    //HandleMoveEntity relative displacement and rotation packet; displacement is restored at 1/4096 block units
    public void HandleMoveEntity(ClientboundMoveEntityPacket packet)
    {
        _level?.MoveEntity(packet.EntityId,
            packet.Xa / 4096.0, packet.Ya / 4096.0, packet.Za / 4096.0,
            packet.HasRot ? Mth.UnpackDegrees(packet.YRot) : null,
            packet.HasRot ? Mth.UnpackDegrees(packet.XRot) : null,
            packet.OnGround);
    }
    //HandleMinecartAlongTrack minecart interpolation steps; the payload is not decoded yet
    public void HandleMinecartAlongTrack(ClientboundMoveMinecartPacket packet)
        => Log.Debug($"HandleMinecartAlongTrack entity={packet.EntityId}");
    //HandleRotatePlayer server rotation nudge; relatives are added to the current angles
    public void HandleRotatePlayer(ClientboundPlayerRotationPacket packet)
    {
        _player.YRot = packet.RelativeY ? _player.YRot + packet.YRot : packet.YRot;
        _player.XRot = packet.RelativeX ? _player.XRot + packet.XRot : packet.XRot;
    }
    //HandlePlayerAbilities ability flags and speeds, maps to vanilla LocalPlayer.abilities
    public void HandlePlayerAbilities(ClientboundPlayerAbilitiesPacket packet)
    {
        State.Invulnerable = packet.Invulnerable;
        State.Flying = packet.IsFlying;
        State.CanFly = packet.CanFly;
        State.Instabuild = packet.Instabuild;
        State.FlyingSpeed = packet.FlyingSpeed;
        State.WalkingSpeed = packet.WalkingSpeed;
    }

    //HandleGameRuleValues full game rule snapshot; stored until the client consults rules
    public void HandleGameRuleValues(ClientboundGameRuleValuesPacket packet) => State.GameRules = packet.Values;

    //HandlePlayerInfoRemove players leaving the tab list
    public void HandlePlayerInfoRemove(ClientboundPlayerInfoRemovePacket packet)
    {
        foreach (var id in packet.ProfileIds) State.PlayerInfo.Remove(id);
    }

    //HandlePlayerInfoUpdate merges tab list entries keyed by profile id, maps to vanilla handlePlayerInfoUpdate
    public void HandlePlayerInfoUpdate(ClientboundPlayerInfoUpdatePacket packet)
    {
        foreach (var entry in packet.Entries) State.PlayerInfo[entry.ProfileId] = entry;
    }

    //HandleRemoveEntities removes entities from the client level and drops their side data
    public void HandleRemoveEntities(ClientboundRemoveEntitiesPacket packet)
    {
        _level?.RemoveEntities(packet.EntityIds);
        foreach (var id in packet.EntityIds)
        {
            State.Equipment.Remove(id);
            State.Passengers.Remove(id);
            State.Leashes.Remove(id);
            State.HeadYaw.Remove(id);
            State.HurtYaw.Remove(id);
            State.LastEntityEvent.Remove(id);
            State.MobEffects.Remove(id);
        }
    }

    //HandleRemoveMobEffect removes one active effect from an entity
    public void HandleRemoveMobEffect(ClientboundRemoveMobEffectPacket packet)
    {
        if (!State.MobEffects.TryGetValue(packet.EntityId, out var effects)) return;
        effects.RemoveAll(e => Equals(e.Effect, packet.Effect));
        if (effects.Count == 0) State.MobEffects.Remove(packet.EntityId);
    }

    //HandleRespawn respawn or dimension change; keeps the new spawn info and resets per-dimension entity state
    public void HandleRespawn(ClientboundRespawnPacket packet)
    {
        State.SpawnInfo = packet.CommonPlayerSpawnInfo;
        _player.GameMode = packet.CommonPlayerSpawnInfo.GameType.Id;
        //Entity ids are per dimension, so the old table would alias new entities with stale ones
        State.Equipment.Clear();
        State.Passengers.Clear();
        State.Leashes.Clear();
        State.HeadYaw.Clear();
        Log.Info($"Respawn dimension={packet.CommonPlayerSpawnInfo.Dimension}");
    }

    //HandleRotateMob head yaw update; the byte is unpacked to degrees like other entity angles
    public void HandleRotateMob(ClientboundRotateHeadPacket packet)
        => State.HeadYaw[packet.EntityId] = Mth.UnpackDegrees(packet.YHeadRot);

    //HandleSetHeldSlot server-selected hotbar slot
    public void HandleSetHeldSlot(ClientboundSetHeldSlotPacket packet) => State.SelectedSlot = packet.Slot;

    //HandleSetDisplayObjective binds an objective to a sidebar/list/below-name slot; an empty name clears it
    public void HandleSetDisplayObjective(ClientboundSetDisplayObjectivePacket packet)
    {
        var slot = packet.Slot?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(packet.ObjectiveName)) State.DisplaySlots.Remove(slot);
        else State.DisplaySlots[slot] = packet.ObjectiveName;
    }

    //HandleSetEntityData records entity metadata; the dropped item's item stack is synced through it
    public void HandleSetEntityData(ClientboundSetEntityDataPacket packet)
        => _level?.SetEntityData(packet.Id, packet.PackedItems);
    //HandleSetEntityMotion records entity velocity for interpolation
    public void HandleSetEntityMotion(ClientboundSetEntityMotionPacket packet)
        => _level?.SetEntityMotion(packet.Id, packet.Movement);
    //HandleSetEquipment replaces the listed equipment slots of an entity
    public void HandleSetEquipment(ClientboundSetEquipmentPacket packet)
    {
        if (!State.Equipment.TryGetValue(packet.Entity, out var slots))
            State.Equipment[packet.Entity] = slots = new Dictionary<EquipmentSlot, ItemStack>();
        foreach (var pair in packet.Slots) slots[pair.Key] = pair.Value;
    }

    //HandleSetPlayerTeamPacket team create/remove/update and player membership changes
    //Method: 0 create, 1 remove, 2 update, 3 add players, 4 remove players; the parameter payload is not decoded yet
    public void HandleSetPlayerTeamPacket(ClientboundSetPlayerTeamPacket packet)
    {
        switch (packet.Method)
        {
            case 1:
                State.Teams.Remove(packet.Name);
                return;
            default:
                if (!State.Teams.TryGetValue(packet.Name, out var team))
                    State.Teams[packet.Name] = team = new ClientPacketState.ScoreboardTeam { Name = packet.Name };
                if (packet.Method == 3 && packet.Players is IEnumerable<string> added)
                    foreach (var player in added) team.Players.Add(player);
                if (packet.Method == 4 && packet.Players is IEnumerable<string> removed)
                    foreach (var player in removed) team.Players.Remove(player);
                return;
        }
    }

    //HandleSetScore sets or updates one score entry
    public void HandleSetScore(ClientboundSetScorePacket packet)
        => State.Scores[(packet.Owner, packet.ObjectiveName)] = packet.Score;

    //HandleResetScore removes one score entry, or every score of the owner when the objective name is empty
    public void HandleResetScore(ClientboundResetScorePacket packet)
    {
        if (string.IsNullOrEmpty(packet.ObjectiveName))
        {
            foreach (var key in State.Scores.Keys.Where(k => k.Owner == packet.Owner).ToArray())
                State.Scores.Remove(key);
            return;
        }
        State.Scores.Remove((packet.Owner, packet.ObjectiveName));
    }

    //HandleSetSpawn records the world spawn point shown on compasses and used for respawn
    public void HandleSetSpawn(ClientboundSetDefaultSpawnPositionPacket packet)
    {
        State.SpawnPos = packet.Pos;
        State.SpawnAngle = packet.Angle;
    }

    //HandleSetTime stores the game time and the per-clock totals used by day/night rendering
    public void HandleSetTime(ClientboundSetTimePacket packet)
    {
        State.GameTime = packet.GameTime;
        foreach (var clock in packet.ClockUpdates) State.Clocks[clock.ClockId] = clock;
    }

    //HandleTakeItemEntity an entity picked up an item; recorded for the pickup animation
    public void HandleTakeItemEntity(ClientboundTakeItemEntityPacket packet)
        => Log.Debug($"HandleTakeItemEntity item={packet.ItemId} collector={packet.PlayerId} amount={packet.Amount}");
    //HandleSoundEvent positional sound; recorded for the audio layer, which does not exist yet
    public void HandleSoundEvent(ClientboundSoundPacket packet)
        => State.Sounds.Add(new ClientPacketState.SoundEventRecord(packet.Sound, packet.Source,
            packet.X, packet.Y, packet.Z, packet.Volume, packet.Pitch, packet.Seed, 0));
    //HandleSoundEntityEvent sound bound to an entity, following it while it moves
    public void HandleSoundEntityEvent(ClientboundSoundEntityPacket packet)
        => State.Sounds.Add(new ClientPacketState.SoundEventRecord(packet.Sound, packet.Source,
            0, 0, 0, packet.Volume, packet.Pitch, packet.Seed, packet.Id));
    //HandleParticleEvent particle burst; recorded for the particle layer, which does not exist yet
    public void HandleParticleEvent(ClientboundLevelParticlesPacket packet)
        => State.Particles.Add(new ClientPacketState.ParticleEventRecord(packet.X, packet.Y, packet.Z,
            packet.XDist, packet.YDist, packet.ZDist, packet.MaxSpeed, packet.Count, packet.OverrideLimiter,
            packet.AlwaysShow, packet.Particle));
    //HandleEntityPositionSync server-authoritative position sync; overwrites the entity position and orientation entirely
    //The entity position baseline is reset with it, so later incremental displacements are relative to the new position, maps to vanilla handleEntityPositionSync
    public void HandleEntityPositionSync(ClientboundEntityPositionSyncPacket packet)
        => _level?.SetEntityPosition(packet.Id, packet.Position, packet.YRot, packet.XRot, packet.OnGround);
    //HandleTeleportEntity teleport packet overwrites the entity position and orientation entirely
    public void HandleTeleportEntity(ClientboundTeleportEntityPacket packet)
        => _level?.SetEntityPosition(packet.Id, packet.Position, packet.YRot, packet.XRot, packet.OnGround);
    //HandleTickingState tick rate and frozen state; the local world advances at the server tick rate, and while frozen entity animation stops
    public void HandleTickingState(ClientboundTickingStatePacket packet)
        => _level?.SetTickingState(packet.TickRate, packet.IsFrozen);
    //HandleTickingStep step ticks while frozen; after these ticks the local world returns to frozen
    public void HandleTickingStep(ClientboundTickingStepPacket packet)
        => _level?.SetTickingStep(packet.TickSteps);
    //HandlePongResponse the server echoes the timestamp the client sent; the round trip is computed from it
    public void HandlePongResponse(ClientboundPongResponsePacket packet)
        => State.LastPongTime = packet.Time;
    //HandleUpdateAttributes records entity attributes; base values and modifiers are applied to the client entity
    public void HandleUpdateAttributes(ClientboundUpdateAttributesPacket packet)
        => _level?.SetEntityAttributes(packet.EntityId, packet.Attributes);
    //HandleUpdateMobEffect adds or refreshes one effect on an entity, maps to vanilla handleUpdateMobEffect
    public void HandleUpdateMobEffect(ClientboundUpdateMobEffectPacket packet)
    {
        if (!State.MobEffects.TryGetValue(packet.EntityId, out var effects))
            State.MobEffects[packet.EntityId] = effects = new List<ClientPacketState.MobEffectState>();
        effects.RemoveAll(e => Equals(e.Effect, packet.Effect));
        effects.Add(new ClientPacketState.MobEffectState(packet.Effect, packet.EffectAmplifier,
            packet.EffectDurationTicks, packet.Flags));
    }

    //HandlePlayerCombatEnd combat tracking ended; only logged, the client shows no combat indicator
    public void HandlePlayerCombatEnd(ClientboundPlayerCombatEndPacket packet)
        => Log.Debug($"HandlePlayerCombatEnd duration={packet.Duration}");

    //HandlePlayerCombatEnter combat tracking started
    public void HandlePlayerCombatEnter(ClientboundPlayerCombatEnterPacket packet)
        => Log.Debug("HandlePlayerCombatEnter");

    //HandlePlayerCombatKill the player died; the death screen reads the message from here
    public void HandlePlayerCombatKill(ClientboundPlayerCombatKillPacket packet)
    {
        State.DeathMessage = packet.Message;
        Log.Info($"Player died: {packet.Message}");
    }

    //HandleChangeDifficulty difficulty and its locked flag
    public void HandleChangeDifficulty(ClientboundChangeDifficultyPacket packet)
    {
        State.Difficulty = packet.Difficulty;
        State.DifficultyLocked = packet.Locked;
    }

    //HandleSetCamera redirects the camera to another entity; 0 restores the player camera
    public void HandleSetCamera(ClientboundSetCameraPacket packet) => State.CameraEntityId = packet.CameraId;

    //HandleInitializeBorder full world border reset, maps to vanilla handleInitializeBorder
    public void HandleInitializeBorder(ClientboundInitializeBorderPacket packet)
    {
        var border = State.Border;
        border.CenterX = packet.NewCenterX;
        border.CenterZ = packet.NewCenterZ;
        border.Size = packet.OldSize;
        border.LerpTarget = packet.NewSize;
        border.LerpTime = packet.LerpTime;
        border.AbsoluteMaxSize = packet.NewAbsoluteMaxSize;
        border.WarningBlocks = packet.WarningBlocks;
        border.WarningTime = packet.WarningTime;
    }

    //HandleSetBorderLerpSize the border grows or shrinks over time
    public void HandleSetBorderLerpSize(ClientboundSetBorderLerpSizePacket packet)
    {
        State.Border.Size = packet.OldSize;
        State.Border.LerpTarget = packet.NewSize;
        State.Border.LerpTime = packet.LerpTime;
    }

    //HandleSetBorderSize the border jumps to a new size
    public void HandleSetBorderSize(ClientboundSetBorderSizePacket packet)
    {
        State.Border.Size = packet.Size;
        State.Border.LerpTarget = packet.Size;
    }

    //HandleSetBorderWarningDelay seconds before the border warning appears
    public void HandleSetBorderWarningDelay(ClientboundSetBorderWarningDelayPacket packet)
        => State.Border.WarningTime = packet.WarningDelay;

    //HandleSetBorderWarningDistance blocks from the border where the warning appears
    public void HandleSetBorderWarningDistance(ClientboundSetBorderWarningDistancePacket packet)
        => State.Border.WarningBlocks = packet.WarningBlocks;

    //HandleSetBorderCenter border center
    public void HandleSetBorderCenter(ClientboundSetBorderCenterPacket packet)
    {
        State.Border.CenterX = packet.NewCenterX;
        State.Border.CenterZ = packet.NewCenterZ;
    }

    //HandleTabListCustomisation tab list header and footer text
    public void HandleTabListCustomisation(ClientboundTabListPacket packet)
    {
        State.TabHeader = packet.Header;
        State.TabFooter = packet.Footer;
    }

    //HandleBossUpdate boss bar create/update/remove; the operation payload is stored verbatim
    public void HandleBossUpdate(ClientboundBossEventPacket packet) => State.BossBars[packet.Id] = packet.Operation;

    //HandleItemCooldown item cooldown started, recorded for the HUD overlay
    public void HandleItemCooldown(ClientboundCooldownPacket packet)
        => State.Cooldowns.Add((packet.CooldownGroup, packet.Duration));

    //HandleMoveVehicle server-authoritative vehicle position; the local player follows its vehicle
    public void HandleMoveVehicle(ClientboundMoveVehiclePacket packet)
        => Log.Debug($"HandleMoveVehicle position={packet.Position} yRot={packet.YRot} xRot={packet.XRot}");

    //HandleUpdateAdvancementsPacket advancement definitions and progress, stored until the advancements screen exists
    public void HandleUpdateAdvancementsPacket(ClientboundUpdateAdvancementsPacket packet)
    {
        if (packet.Reset) State.Advancements = null;
        State.Advancements = packet;
    }

    //HandleSelectAdvancementsTab selects the visible advancement tab
    public void HandleSelectAdvancementsTab(ClientboundSelectAdvancementsTabPacket packet)
        => Log.Debug("HandleSelectAdvancementsTab");

    //HandlePlaceRecipe ghost recipe placement for the crafting grid
    public void HandlePlaceRecipe(ClientboundPlaceGhostRecipePacket packet)
        => Log.Debug($"HandlePlaceRecipe container={packet.ContainerId}");

    //HandleCommandSuggestions tab-completion results for the command input
    public void HandleCommandSuggestions(ClientboundCommandSuggestionsPacket packet)
        => State.CommandSuggestions = packet;

    //HandleUpdateRecipes full recipe and stonecutter update
    public void HandleUpdateRecipes(ClientboundUpdateRecipesPacket packet) => State.Recipes = packet;

    //HandleLookAt points the player camera at a position or entity
    public void HandleLookAt(ClientboundPlayerLookAtPacket packet)
        => Log.Debug($"HandleLookAt x={packet.X} y={packet.Y} z={packet.Z} atEntity={packet.AtEntity}");

    //HandleTagQueryPacket book/sign tag query reply
    public void HandleTagQueryPacket(ClientboundTagQueryPacket packet) => State.TagQuery = packet;

    //HandleOpenBook requests opening the written book screen
    public void HandleOpenBook(ClientboundOpenBookPacket packet) => Log.Debug($"HandleOpenBook hand={packet.Hand}");
    public void HandleCommands(ClientboundCommandsPacket packet) => CommandRoot = packet.Root;
    public void HandleStopSoundEvent(ClientboundStopSoundPacket packet) => Log.Debug($"Stop sound received name={packet.Name} source={packet.Source}");
    //HandleOpenScreen the server requests opening a menu screen; container contents are filled in afterwards by container_set_content
    public void HandleOpenScreen(ClientboundOpenScreenPacket packet)
    {
        State.OpenContainerId = packet.ContainerId;
        State.OpenContainerTitle = packet.Title;
        OnOpenScreen?.Invoke(packet);
    }
    //HandleMerchantOffers villager trade list; stored until the merchant screen exists
    public void HandleMerchantOffers(ClientboundMerchantOffersPacket packet)
    {
        State.MerchantOffers = packet.Offers;
        State.OpenContainerId = packet.ContainerId;
    }

    //HandleSetSimulationDistance server simulation distance
    public void HandleSetSimulationDistance(ClientboundSetSimulationDistancePacket packet)
        => State.SimulationDistance = packet.SimulationDistance;

    //HandleBlockChangedAck acknowledges a client block prediction sequence, maps to vanilla handleBlockChangedAck
    public void HandleBlockChangedAck(ClientboundBlockChangedAckPacket packet)
        => State.LastBlockAckSequence = packet.Sequence;

    //SetActionBarText action bar message
    public void SetActionBarText(ClientboundSetActionBarTextPacket packet) => State.ActionBar = packet.Text;

    //SetSubtitleText title screen subtitle
    public void SetSubtitleText(ClientboundSetSubtitleTextPacket packet) => State.Subtitle = packet.Text;

    //SetTitleText title screen main text
    public void SetTitleText(ClientboundSetTitleTextPacket packet) => State.Title = packet.Text;

    //SetTitlesAnimation title fade in/stay/fade out durations
    public void SetTitlesAnimation(ClientboundSetTitlesAnimationPacket packet)
    {
        State.TitleFadeIn = packet.FadeIn;
        State.TitleStay = packet.Stay;
        State.TitleFadeOut = packet.FadeOut;
    }

    //HandleTitlesClear clears the current title and subtitle
    public void HandleTitlesClear(ClientboundClearTitlesPacket packet)
    {
        State.Title = null;
        State.Subtitle = null;
    }

    //HandleServerData server motd announced after login
    public void HandleServerData(ClientboundServerDataPacket packet) => State.ServerMotd = packet.Motd;

    //HandleCustomChatCompletions adds or removes tab-completion entries for chat
    public void HandleCustomChatCompletions(ClientboundCustomChatCompletionsPacket packet)
    {
        if (packet.Entries is IEnumerable<string> entries)
            State.CustomChatCompletions.AddRange(entries);
    }

    //HandleBundlePacket a batch of play packets; each child is handled in order, maps to vanilla handleBundlePacket
    public void HandleBundlePacket(ClientboundBundlePacket packet)
    {
        foreach (var child in packet.SubPackets()) child.Handle(this);
    }

    //HandleDamageEvent damage source data used by the damage tilt and death screen
    public void HandleDamageEvent(ClientboundDamageEventPacket packet)
        => State.LastDamageEvent = packet;

    //HandleConfigurationStart the server switches the connection back to the configuration phase
    public void HandleConfigurationStart(ClientboundStartConfigurationPacket packet)
        => Log.Info("Server requested configuration phase");

    //HandleDebugSample debug profiler sample push; not consumed by the client
    public void HandleDebugSample(ClientboundDebugSamplePacket packet)
        => Log.Debug("HandleDebugSample");

    //HandleProjectilePowerPacket projectile acceleration power, used to compute arrow trajectories
    public void HandleProjectilePowerPacket(ClientboundProjectilePowerPacket packet)
        => State.ProjectilePower[packet.Id] = packet.AccelerationPower;
    //HandleSetCursorItem cursor item change; slot -1 is used
    public void HandleSetCursorItem(ClientboundSetCursorItemPacket packet)
        => Inventory.SetSlot(AbstractContainerMenu.CarriedSlotIndex, packet.Contents);

    //HandleSetPlayerInventory single player inventory slot change; the slot number is the inventory index
    public void HandleSetPlayerInventory(ClientboundSetPlayerInventoryPacket packet)
        => Inventory.SetPlayerSlot(packet.Slot, packet.Contents);
    //HandleTestInstanceBlockStatus game test block status; not used by the normal client
    public void HandleTestInstanceBlockStatus(ClientboundTestInstanceBlockStatus packet)
        => Log.Debug("HandleTestInstanceBlockStatus");

    //HandleWaypoint tracked waypoint create/update/remove; stored until a waypoint renderer exists
    public void HandleWaypoint(ClientboundTrackedWaypointPacket packet) => State.Waypoints[packet.Operation?.ToString() ?? string.Empty] = packet.Waypoint;

    //Debug value packets come from the server's debug subscriptions, which the client does not enable
    public void HandleDebugChunkValue(ClientboundDebugChunkValuePacket packet) => State.DebugValues++;

    public void HandleDebugBlockValue(ClientboundDebugBlockValuePacket packet) => State.DebugValues++;

    public void HandleDebugEntityValue(ClientboundDebugEntityValuePacket packet) => State.DebugValues++;

    public void HandleDebugEvent(ClientboundDebugEventPacket packet) => State.DebugValues++;

    //HandleGameTestHighlightPos game test position highlight; recorded for the debug renderer
    public void HandleGameTestHighlightPos(ClientboundGameTestHighlightPosPacket packet)
        => State.GameTestHighlight = packet;

    //HandleLowDiskSpaceWarning the server reports low disk space; shown as a chat warning
    public void HandleLowDiskSpaceWarning(ClientboundLowDiskSpaceWarningPacket packet)
        => Log.Warning("Server reports low disk space");

    //The following are inherited from ClientCommonPacketListener
    //KeepAlive must be answered or the server drops the connection after its timeout, maps to vanilla handleKeepAlive
    public void HandleKeepAlive(ClientboundKeepAlivePacket packet) => _connection.Send(new ServerboundKeepAlivePacket(packet.Id));
    //Ping must be echoed to let the server measure latency, maps to vanilla handlePing
    public void HandlePing(ClientboundPingPacket packet) => _connection.Send(new ServerboundPongPacket(packet.Id));
    public void HandleCustomPayload(ClientboundCustomPayloadPacket packet)
        => Log.Debug($"Custom payload {packet.Id} bytes={packet.Payload.Length}");
    public void HandleDisconnect(ClientboundDisconnectPacket packet)
        => Log.Warning($"play phase disconnected {packet.Reason}");
    //HandleResourcePackPush the server offers a resource pack; downloading is not implemented, so it is declined
    //The reply is mandatory: without it the server keeps the pack pending and can drop the connection on a required pack
    public void HandleResourcePackPush(ClientboundResourcePackPushPacket packet)
    {
        Log.Warning($"Resource pack offered url={packet.Url} required={packet.Required}, declining");
        _connection.Send(new ServerboundResourcePackPacket(packet.Id, ResourcePackAction.Declined));
    }
    public void HandleResourcePackPop(ClientboundResourcePackPopPacket packet)
        => Log.Debug("Resource pack popped");
    //UpdateTags stores the registry tag map; the client needs it to resolve holder tags received later
    public void HandleUpdateTags(ClientboundUpdateTagsPacket packet) => State.Tags = packet.Tags;
    //StoreCookie saves a server cookie for later requests
    public void HandleStoreCookie(ClientboundStoreCookiePacket packet) => State.Cookies[packet.Key] = packet.Payload;
    //Transfer the server redirects the client to another host; recorded, the reconnect is not automated
    public void HandleTransfer(ClientboundTransferPacket packet)
    {
        State.TransferTarget = (packet.Host, packet.Port);
        Log.Info($"Server requested transfer to {packet.Host}:{packet.Port}");
    }
    public void HandleCustomReportDetails(ClientboundCustomReportDetailsPacket packet) => State.CustomReportDetails = packet;
    public void HandleServerLinks(ClientboundServerLinksPacket packet) => State.ServerLinks = packet;
    public void HandleClearDialog(ClientboundClearDialogPacket packet) => State.Dialog = null;
    public void HandleShowDialog(ClientboundShowDialogPacket packet) => State.Dialog = packet;

    //Inherited from ClientCookiePacketListener
    //HandleCookieRequest the server asks for a cookie; the client keeps no cookie jar, so an empty payload is returned
    public void HandleCookieRequest(ClientboundCookieRequestPacket packet)
        => _connection.Send(new ServerboundCookieResponsePacket(packet.Key, Array.Empty<byte>()));

    public void OnDisconnect(string reason)
        => Log.Info($"play phase disconnect reason={reason}");
}
