using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;
using BossBarColor = NacreCraft.Game.Server.ServerBossEvent.BossBarColor;
using BossBarOverlay = NacreCraft.Game.Server.ServerBossEvent.BossBarOverlay;
using DirtyCallback = System.Action;

namespace NacreCraft.Game.Server.BossEvents;

//CustomBossEvent a command-created boss bar, maps to vanilla net.minecraft.server.bossevents.CustomBossEvent
//NC's ServerBossEvent keys players by UUID and has no packet-level id, so the UUID argument of the vanilla constructor is dropped
public sealed class CustomBossEvent : ServerBossEvent
{
    private const int DefaultMax = 100;

    private readonly Identifier _customId;
    private readonly HashSet<Guid> _players = new();
    private readonly DirtyCallback _dirtyCallback;
    private int _value;
    private int _max = DefaultMax;

    public CustomBossEvent(Identifier customId, Component name, DirtyCallback dirtyCallback) : base(name)
    {
        _dirtyCallback = dirtyCallback;
        _customId = customId;
        Progress = 0.0f;
    }

    public Identifier CustomId => _customId;

    public override bool AddPlayer(Guid playerId)
    {
        var added = base.AddPlayer(playerId);
        if (_players.Add(playerId)) SetDirty();
        return added;
    }

    public override bool RemovePlayer(Guid playerId)
    {
        var removed = base.RemovePlayer(playerId);
        if (_players.Remove(playerId)) SetDirty();
        return removed;
    }

    public override void RemoveAllPlayers()
    {
        base.RemoveAllPlayers();
        if (_players.Count == 0) return;
        _players.Clear();
        SetDirty();
    }

    public int Value => _value;

    public int Max => _max;

    public void SetValue(int value)
    {
        _value = value;
        Progress = Mth.Clamp((float)value / _max, 0.0f, 1.0f);
        SetDirty();
    }

    public void SetMax(int max)
    {
        _max = max;
        Progress = Mth.Clamp((float)_value / max, 0.0f, 1.0f);
        SetDirty();
    }

    //GetDisplayName the bracketed name used by /bossbar list, maps to vanilla getDisplayName
    public Component GetDisplayName()
        => ComponentUtils.WrapInSquareBrackets(Name).WithStyle(style => style
            .WithColor(FormattingFor(Color))
            .WithHoverEvent(new HoverEvent.ShowText(Component.Literal(_customId.ToString())))
            .WithInsertion(_customId.ToString()));

    //SetPlayers replaces the tracked audience and returns whether anything changed, maps to vanilla setPlayers
    public bool SetPlayers(IReadOnlyCollection<ServerPlayer> players)
    {
        var wanted = players.Select(player => player.Uuid).ToHashSet();
        var toRemove = _players.Where(uuid => !wanted.Contains(uuid)).ToList();
        var toAdd = players.Where(player => !_players.Contains(player.Uuid)).ToList();
        foreach (var uuid in toRemove)
        {
            var player = players.FirstOrDefault(candidate => candidate.Uuid == uuid);
            if (player is not null) RemovePlayer(uuid);
            _players.Remove(uuid);
        }
        foreach (var player in toAdd) AddPlayer(player.Uuid);
        var changed = toRemove.Count > 0 || toAdd.Count > 0;
        if (changed) SetDirty();
        return changed;
    }

    public static CustomBossEvent Load(Identifier customId, Packed packed, DirtyCallback setDirty)
    {
        var bossEvent = new CustomBossEvent(customId, packed.Name, setDirty)
        {
            Visible = packed.Visible,
            Color = packed.Color,
            Overlay = packed.Overlay,
            DarkenScreen = packed.DarkenScreen,
            PlayBossMusic = packed.PlayBossMusic,
            CreateWorldFog = packed.CreateWorldFog
        };
        bossEvent.SetValue(packed.Value);
        bossEvent.SetMax(packed.Max);
        foreach (var uuid in packed.Players) bossEvent._players.Add(uuid);
        return bossEvent;
    }

    public Packed Pack() => new(Name, Visible, _value, _max, Color, Overlay, DarkenScreen, PlayBossMusic,
        CreateWorldFog, _players.ToList());

    public void OnPlayerConnect(ServerPlayer player)
    {
        if (_players.Contains(player.Uuid)) AddPlayer(player.Uuid);
    }

    public void OnPlayerDisconnect(ServerPlayer player) => base.RemovePlayer(player.Uuid);

    public void SetDirty() => _dirtyCallback();

    private static ChatFormatting FormattingFor(BossBarColor color) => color switch
    {
        BossBarColor.Pink => ChatFormatting.LightPurple,
        BossBarColor.Blue => ChatFormatting.Blue,
        BossBarColor.Red => ChatFormatting.Red,
        BossBarColor.Green => ChatFormatting.Green,
        BossBarColor.Yellow => ChatFormatting.Yellow,
        BossBarColor.Purple => ChatFormatting.DarkPurple,
        _ => ChatFormatting.White
    };

    //Packed the persisted form of one boss bar, maps to vanilla CustomBossEvent.Packed
    public sealed record Packed(Component Name, bool Visible, int Value, int Max, BossBarColor Color,
        BossBarOverlay Overlay, bool DarkenScreen, bool PlayBossMusic, bool CreateWorldFog,
        IReadOnlyList<Guid> Players)
    {
        private static readonly Codec<Guid> UuidCodec = Codecs.String.ComapFlatMap(
            text => Guid.TryParse(text, out var id)
                ? DataResult<Guid>.Success(id)
                : DataResult<Guid>.Error(() => $"Invalid UUID: {text}"),
            id => id.ToString());

        private static readonly Codec<BossBarColor> ColorCodec = Codecs.String.ComapFlatMap(
            name => Enum.TryParse<BossBarColor>(name, true, out var color)
                ? DataResult<BossBarColor>.Success(color)
                : DataResult<BossBarColor>.Error(() => $"Unknown boss bar color: {name}"),
            color => color.ToString());

        private static readonly Codec<BossBarOverlay> OverlayCodec = Codecs.String.ComapFlatMap(
            name => Enum.TryParse<BossBarOverlay>(name, true, out var overlay)
                ? DataResult<BossBarOverlay>.Success(overlay)
                : DataResult<BossBarOverlay>.Error(() => $"Unknown boss bar overlay: {name}"),
            overlay => overlay.ToString());

        public static readonly Codec<Packed> CODEC = RecordCodecBuilder.Of10(
            ComponentSerialization.JsonCodec.FieldOf("Name").ForGetter((Packed packed) => packed.Name),
            Codecs.Bool.OptionalFieldOf("Visible", false).ForGetter((Packed packed) => packed.Visible),
            Codecs.Int.OptionalFieldOf("Value", 0).ForGetter((Packed packed) => packed.Value),
            Codecs.Int.OptionalFieldOf("Max", DefaultMax).ForGetter((Packed packed) => packed.Max),
            ColorCodec.OptionalFieldOf("Color", BossBarColor.White).ForGetter((Packed packed) => packed.Color),
            OverlayCodec.OptionalFieldOf("Overlay", BossBarOverlay.Progress).ForGetter((Packed packed) => packed.Overlay),
            Codecs.Bool.OptionalFieldOf("DarkenScreen", false).ForGetter((Packed packed) => packed.DarkenScreen),
            Codecs.Bool.OptionalFieldOf("PlayBossMusic", false).ForGetter((Packed packed) => packed.PlayBossMusic),
            Codecs.Bool.OptionalFieldOf("CreateWorldFog", false).ForGetter((Packed packed) => packed.CreateWorldFog),
            UuidCodec.ListOf().OptionalFieldOf("Players", Array.Empty<Guid>())
                .ForGetter((Packed packed) => packed.Players),
            (name, visible, value, max, color, overlay, darkenScreen, playBossMusic, createWorldFog, players) =>
                new Packed(name, visible, value, max, color, overlay, darkenScreen, playBossMusic, createWorldFog,
                    players));
    }
}

