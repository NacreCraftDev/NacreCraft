namespace NacreCraft.Client.Input;

//InputWithModifiers a key or mouse input carrying modifier bits, maps to vanilla net.minecraft.client.input.InputWithModifiers
public interface InputWithModifiers
{
    public const int NotDigit = -1;

    int Input { get; }

    int Modifiers { get; }

    bool IsSelection() => Input is 257 or 32 or 335;

    bool IsConfirmation() => Input is 257 or 335;

    bool IsEscape() => Input == 256;

    bool IsLeft() => Input == 263;

    bool IsRight() => Input == 262;

    bool IsUp() => Input == 265;

    bool IsDown() => Input == 264;

    bool IsCycleFocus() => Input == 258;

    int GetDigit()
    {
        var value = Input - 48;
        return value is >= 0 and <= 9 ? value : NotDigit;
    }

    bool HasAltDown() => (Modifiers & 4) != 0;

    bool HasShiftDown() => (Modifiers & 1) != 0;

    bool HasControlDown() => (Modifiers & 2) != 0;

    bool HasControlDownWithQuirk() => (Modifiers & InputQuirks.EditShortcutKeyModifier) != 0;

    bool IsSelectAll() => Input == 65 && HasControlDownWithQuirk() && !HasShiftDown() && !HasAltDown();

    bool IsCopy() => Input == 67 && HasControlDownWithQuirk() && !HasShiftDown() && !HasAltDown();

    bool IsPaste() => Input == 86 && HasControlDownWithQuirk() && !HasShiftDown() && !HasAltDown();

    bool IsCut() => Input == 88 && HasControlDownWithQuirk() && !HasShiftDown() && !HasAltDown();
}
