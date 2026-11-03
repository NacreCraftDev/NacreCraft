namespace NacreCraft.Client.Input;

//MouseButtonEvent a mouse click with its position, maps to vanilla net.minecraft.client.input.MouseButtonEvent
public sealed record MouseButtonEvent(double X, double Y, MouseButtonInfo ButtonInfo) : InputWithModifiers
{
    public int Input => ButtonInfo.Button;

    public int Modifiers => ButtonInfo.Modifiers;
}
