namespace NacreCraft.Client.Input;

//KeyEvent a keyboard input, maps to vanilla net.minecraft.client.input.KeyEvent
public sealed record KeyEvent(int Key, int Scancode, int Modifiers) : InputWithModifiers
{
    public int Input => Key;
}
