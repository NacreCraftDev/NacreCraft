using NacreCraft.DataFixer;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Util.WorldUpdate;

//UpgradeStatusTranslator turns the upgrade progress into a display line, maps to vanilla net.minecraft.util.worldupdate.UpgradeStatusTranslator
public sealed class UpgradeStatusTranslator
{
    private static readonly Component Failed = Component.Translatable("optimizeWorld.stage.failed");
    private static readonly Component Counting = Component.Translatable("optimizeWorld.stage.counting");
    private static readonly Component Upgrading = Component.Translatable("optimizeWorld.stage.upgrading");

    private readonly Dictionary<DataFixTypes, Messages> _messages = new()
    {
        [DataFixTypes.Chunk] = Messages.Create("chunks"),
        [DataFixTypes.EntityChunk] = Messages.Create("entities"),
        [DataFixTypes.PoiChunk] = Messages.Create("poi")
    };

    public Component Translate(UpgradeProgress upgradeProgress)
    {
        var status = upgradeProgress.GetStatus();
        if (status == UpgradeProgress.Status.FAILED) return Failed;
        if (status == UpgradeProgress.Status.COUNTING) return Counting;
        var dataFixType = upgradeProgress.GetDataFixType();
        if (dataFixType is null) return Counting;
        return _messages.TryGetValue(dataFixType, out var typeMessages)
            ? typeMessages.ForStatus(status)
            : Upgrading;
    }

    public sealed record Messages(Component UpgradingText, Component Finished)
    {
        public static Messages Create(string type) => new(
            Component.Translatable($"optimizeWorld.stage.upgrading.{type}"),
            Component.Translatable($"optimizeWorld.stage.finished.{type}"));

        public Component ForStatus(UpgradeProgress.Status status) => status switch
        {
            UpgradeProgress.Status.UPGRADING => UpgradingText,
            UpgradeProgress.Status.FINISHED => Finished,
            _ => throw new InvalidOperationException($"Invalid Status received: {status}")
        };
    }
}
