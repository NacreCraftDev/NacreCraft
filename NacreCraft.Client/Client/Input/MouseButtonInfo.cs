namespace NacreCraft.Client.Input;

//MouseButtonInfo a mouse button plus modifier bits, maps to vanilla net.minecraft.client.input.MouseButtonInfo
public sealed record MouseButtonInfo(int Button, int Modifiers) : InputWithModifiers
{
    public int Input => Button;
}
