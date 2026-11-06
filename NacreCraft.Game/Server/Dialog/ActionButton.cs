using NacreCraft.Codec;

namespace NacreCraft.Game.Server.Dialog;

//ActionButton a button with an optional click action, maps to vanilla net.minecraft.server.dialog.ActionButton
public sealed record ActionButton(CommonButtonData Button, Optional<Action> ClickAction)
{
    public static readonly Codec<ActionButton> CODEC = RecordCodecBuilder.Of2(
        CommonButtonData.MAP_CODEC.ForGetter((ActionButton button) => button.Button),
        Action.CODEC.OptionalFieldOf("action").ForGetter((ActionButton button) => button.ClickAction),
        (data, action) => new ActionButton(data, action));
}

