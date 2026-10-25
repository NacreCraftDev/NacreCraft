using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network.Chat;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Client.Network.Protocol.Game;

//ClientPacketState client-side copy of everything the play phase packets describe
//Maps to the state vanilla keeps on ClientPacketListener, ClientLevel and ClientChunkCache
//This layer only stores data; chat UI, scoreboard rendering, sounds and particles read it later
public sealed class ClientPacketState
{
    //World clock and time, maps to ClientLevel.ClientLevelData
    public long GameTime { get; set; }
    public readonly Dictionary<int, ClientboundSetTimePacket.ClockUpdate> Clocks = new();
    public BlockPos? SpawnPos { get; set; }
    public float SpawnAngle { get; set; }
    public object? Difficulty { get; set; }
    public bool DifficultyLocked { get; set; }
    public int SimulationDistance { get; set; }
    public int ChunkCacheRadius { get; set; }
    public int ChunkCacheCenterX { get; set; }
    public int ChunkCacheCenterZ { get; set; }
    public int LastBlockAckSequence { get; set; }
    public float RainLevel { get; set; }
    public float ThunderLevel { get; set; }
    public CommonPlayerSpawnInfo? SpawnInfo { get; set; }
    public object? GameRules { get; set; }

    //Player abilities, maps to vanilla LocalPlayer.abilities
    public bool Invulnerable { get; set; }
    public bool Flying { get; set; }
    public bool CanFly { get; set; }
    public bool Instabuild { get; set; }
    public float FlyingSpeed { get; set; } = 0.05f;
    public float WalkingSpeed { get; set; } = 0.1f;
    public int SelectedSlot { get; set; }

    //Entity side data keyed by entity id
    public readonly Dictionary<int, Dictionary<EquipmentSlot, ItemStack>> Equipment = new();
    public readonly Dictionary<int, int[]> Passengers = new();
    public readonly Dictionary<int, int> Leashes = new();
    public readonly Dictionary<int, float> HeadYaw = new();
    public readonly Dictionary<int, float> HurtYaw = new();
    public readonly Dictionary<int, byte> LastEntityEvent = new();
    public readonly Dictionary<int, List<MobEffectState>> MobEffects = new();

    //Container side data; the slot contents themselves live in ClientInventory
    public readonly Dictionary<int, Dictionary<int, int>> ContainerData = new();
    public ItemStack? CursorItem { get; set; }
    public object? MerchantOffers { get; set; }
    public int OpenContainerId { get; set; } = -1;
    public Component? OpenContainerTitle { get; set; }

    //Chat, action bar and titles
    public readonly List<ChatEntry> ChatLog = new();
    public Component? ActionBar { get; set; }
    public Component? Title { get; set; }
    public Component? Subtitle { get; set; }
    public int TitleFadeIn { get; set; } = 10;
    public int TitleStay { get; set; } = 70;
    public int TitleFadeOut { get; set; } = 20;
    public Component? TabHeader { get; set; }
    public Component? TabFooter { get; set; }
    public Component? ServerMotd { get; set; }
    public readonly List<string> CustomChatCompletions = new();

    //Scoreboard
    public readonly Dictionary<string, ScoreboardObjective> Objectives = new();
    public readonly Dictionary<string, string> DisplaySlots = new();
    public readonly Dictionary<(string Owner, string Objective), int> Scores = new();
    public readonly Dictionary<string, ScoreboardTeam> Teams = new();

    //Misc mirrors
    public readonly Dictionary<Guid, object?> BossBars = new();
    public readonly Dictionary<Guid, PlayerInfoEntry> PlayerInfo = new();
    public readonly List<(object? Group, int Duration)> Cooldowns = new();
    public object? CommandSuggestions { get; set; }
    public object? TagQuery { get; set; }
    public Component? DeathMessage { get; set; }
    public int CameraEntityId { get; set; }
    public object? LastDamageEvent { get; set; }
    public object? OpenSignPos { get; set; }
    public long LastPongTime { get; set; }
    public object? Dialog { get; set; }
    public object? ServerLinks { get; set; }
    public object? CustomReportDetails { get; set; }
    public readonly Dictionary<NacreCraft.Registry.Identifier, byte[]> Cookies = new();
    public (string Host, int Port)? TransferTarget { get; set; }
    public readonly Dictionary<int, double> ProjectilePower = new();
    public readonly Dictionary<string, object?> Waypoints = new();
    public int DebugValues { get; set; }
    public object? GameTestHighlight { get; set; }
    public readonly List<ClientboundMapItemDataPacket> MapData = new();
    public readonly List<SoundEventRecord> Sounds = new();
    public readonly List<ParticleEventRecord> Particles = new();
    public WorldBorderState Border { get; } = new();
    public object? Tags { get; set; }
    public object? Recipes { get; set; }
    public object? Advancements { get; set; }
    public object? Stats { get; set; }

    //ChatEntry one received chat line, maps to vanilla GuiMessage
    public readonly record struct ChatEntry(Component Content, bool Overlay, long Time);

    //MobEffectState one active effect on an entity
    public readonly record struct MobEffectState(Holder<MobEffect> Effect, int Amplifier, int DurationTicks, byte Flags);

    //ScoreboardObjective objective definition plus its display name
    public sealed record ScoreboardObjective(string Name, Component DisplayName, object? RenderType, object? NumberFormat, bool Removed);

    //ScoreboardTeam current team definition, null fields mean "not part of this update"
    public sealed class ScoreboardTeam
    {
        public string Name { get; init; } = string.Empty;
        public Component? DisplayName { get; set; }
        public string? PlayerPrefix { get; set; }
        public string? PlayerSuffix { get; set; }
        public bool? FriendlyFire { get; set; }
        public bool? SeeFriendlyInvisibles { get; set; }
        public bool? NameTagVisibility { get; set; }
        public bool? CollisionRule { get; set; }
        public int? Color { get; set; }
        public HashSet<string> Players { get; } = new();
    }

    //WorldBorderState client copy of the world border
    public sealed class WorldBorderState
    {
        public double CenterX { get; set; }
        public double CenterZ { get; set; }
        public double Size { get; set; } = 29999984.0;
        public double LerpTarget { get; set; } = 29999984.0;
        public long LerpTime { get; set; }
        public int WarningBlocks { get; set; } = 5;
        public int WarningTime { get; set; } = 15;
        public int AbsoluteMaxSize { get; set; } = 29999984;
    }

    //SoundEventRecord one received sound, kept for the future audio subsystem
    public readonly record struct SoundEventRecord(SoundEvent Sound, SoundSource Source,
        double X, double Y, double Z, float Volume, float Pitch, long Seed, int EntityId);

    //ParticleEventRecord one received particle burst, kept for the future particle subsystem
    public readonly record struct ParticleEventRecord(double X, double Y, double Z, float XDist, float YDist,
        float ZDist, float MaxSpeed, int Count, bool OverrideLimiter, bool AlwaysShow, object? Particle);
}
