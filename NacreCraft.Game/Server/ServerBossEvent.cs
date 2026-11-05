using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server;

//ServerBossEvent a boss bar shown to a set of players, maps to vanilla net.minecraft.server.level.ServerBossEvent
//The server keeps the authoritative name, progress, colour and overlay, and syncs them through boss event packets
public sealed class ServerBossEvent(Component name)
{
    private readonly HashSet<Guid> _players = new();
    private float _progress;

    public Component Name { get; set; } = name;

    //Progress 0..1 fill ratio, clamped like vanilla
    public float Progress
    {
        get => _progress;
        set => _progress = Math.Clamp(value, 0f, 1f);
    }

    public BossBarColor Color { get; set; } = BossBarColor.White;

    public BossBarOverlay Overlay { get; set; } = BossBarOverlay.Progress;

    //Visible whether the bar is rendered at all
    public bool Visible { get; set; } = true;

    public bool DarkenScreen { get; set; }

    public bool PlayBossMusic { get; set; }

    public bool CreateWorldFog { get; set; }

    public IReadOnlyCollection<Guid> Players => _players;

    public bool AddPlayer(Guid playerId) => _players.Add(playerId);

    public bool RemovePlayer(Guid playerId) => _players.Remove(playerId);

    public void RemoveAllPlayers() => _players.Clear();

    public bool ShouldDarkenScreen => DarkenScreen && Visible;

    public bool ShouldPlayBossMusic => PlayBossMusic && Visible;

    public bool ShouldCreateWorldFog => CreateWorldFog && Visible;

    public enum BossBarColor
    {
        Pink,
        Blue,
        Red,
        Green,
        Yellow,
        Purple,
        White,
    }

    public enum BossBarOverlay
    {
        Progress,
        Notched10,
        Notched12,
        Notched20,
    }
}
