using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server;

//DemoMode demo play session, maps to vanilla net.minecraft.server.level.DemoMode
//Counts play time and reminds the player that the demo ends; the demo event packets are not sent here
public sealed class DemoMode(ServerPlayer player) : ServerPlayerGameMode(player)
{
    public const int DemoDays = 5;
    public const int TotalPlayTicks = 120500;

    private bool _displayedIntro;
    private bool _demoHasEnded;
    private int _demoEndedReminder;
    private int _gameModeTicks;

    public bool DisplayedIntro => _displayedIntro;

    public bool DemoHasEnded => _demoHasEnded;

    public int GameModeTicks => _gameModeTicks;

    public override void Tick()
    {
        base.Tick();
        _gameModeTicks++;
        var time = Level.GameTime;
        if (!_displayedIntro && _gameModeTicks > 20) _displayedIntro = true;
        _demoHasEnded = time > TotalPlayTicks;
        if (!_demoHasEnded) return;
        _demoEndedReminder++;
        if (_demoEndedReminder > 100)
        {
            Player.SendSystemMessage(Component.Translatable("demo.reminder"));
            _demoEndedReminder = 0;
        }
    }
}
