namespace NacreCraft.Client.Input;

//InputQuirks platform input differences, maps to vanilla net.minecraft.client.input.InputQuirks
public static class InputQuirks
{
    private static readonly bool OnMacOs = OperatingSystem.IsMacOS();

    public static readonly bool ReplaceCtrlKeyWithCmdKey = OnMacOs;

    public static readonly int EditShortcutKeyModifier = ReplaceCtrlKeyWithCmdKey ? 8 : 2;

    public static readonly bool SimulateRightClickWithLongLeftClick = OnMacOs;

    public static readonly bool RestoreKeyStateAfterMouseGrab = !OnMacOs;
}
