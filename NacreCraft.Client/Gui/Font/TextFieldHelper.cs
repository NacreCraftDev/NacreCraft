using NacreCraft.Client.Gui;
using NacreCraft.Client.Input;
using NacreCraft.Network.Chat;
using NacreCraft.Util;

namespace NacreCraft.Client.Gui.Font;

//TextFieldHelper caret/selection/clipboard editing for a text field, maps to vanilla net.minecraft.client.gui.font.TextFieldHelper
public sealed class TextFieldHelper
{
    private readonly Func<string> _getMessageFn;
    private readonly Action<string> _setMessageFn;
    private readonly Func<string> _getClipboardFn;
    private readonly Action<string> _setClipboardFn;
    private readonly Func<string, bool> _stringValidator;
    private int _cursorPos;
    private int _selectionPos;

    public TextFieldHelper(Func<string> getMessageFn, Action<string> setMessageFn, Func<string> getClipboardFn,
        Action<string> setClipboardFn, Func<string, bool> stringValidator)
    {
        _getMessageFn = getMessageFn;
        _setMessageFn = setMessageFn;
        _getClipboardFn = getClipboardFn;
        _setClipboardFn = setClipboardFn;
        _stringValidator = stringValidator;
        SetCursorToEnd();
    }

    public static Func<string> CreateClipboardGetter(MinecraftClient minecraft) => () => GetClipboardContents(minecraft);

    public static string GetClipboardContents(MinecraftClient minecraft)
        => ChatFormatting.StripFormatting(GuiTextBox.ClipboardText.Replace("\r", "")) ?? string.Empty;

    public static Action<string> CreateClipboardSetter(MinecraftClient minecraft) => text => SetClipboardContents(minecraft, text);

    public static void SetClipboardContents(MinecraftClient minecraft, string text) => GuiTextBox.ClipboardText = text;

    public bool CharTyped(CharacterEvent @event)
    {
        if (@event.IsAllowedChatCharacter()) InsertText(_getMessageFn(), @event.CodepointAsString());
        return true;
    }

    public bool KeyPressed(KeyEvent @event)
    {
        var input = (InputWithModifiers)@event;
        if (input.IsSelectAll())
        {
            SelectAll();
            return true;
        }

        if (input.IsCopy())
        {
            Copy();
            return true;
        }

        if (input.IsPaste())
        {
            Paste();
            return true;
        }

        if (input.IsCut())
        {
            Cut();
            return true;
        }

        var cursorStep = input.HasControlDownWithQuirk() ? CursorStep.Word : CursorStep.Character;
        if (@event.Key == 259)
        {
            RemoveFromCursor(-1, cursorStep);
            return true;
        }

        if (@event.Key == 261)
        {
            RemoveFromCursor(1, cursorStep);
            return true;
        }

        if (input.IsLeft())
        {
            MoveBy(-1, input.HasShiftDown(), cursorStep);
            return true;
        }

        if (input.IsRight())
        {
            MoveBy(1, input.HasShiftDown(), cursorStep);
            return true;
        }

        if (@event.Key == 268)
        {
            SetCursorToStart(input.HasShiftDown());
            return true;
        }

        if (@event.Key == 269)
        {
            SetCursorToEnd(input.HasShiftDown());
            return true;
        }

        return false;
    }

    private int ClampToMsgLength(int value) => Mth.Clamp(value, 0, _getMessageFn().Length);

    private void InsertText(string message, string text)
    {
        if (_selectionPos != _cursorPos) message = DeleteSelection(message);
        _cursorPos = Mth.Clamp(_cursorPos, 0, message.Length);
        var newText = message.Insert(_cursorPos, text);
        if (_stringValidator(newText))
        {
            _setMessageFn(newText);
            _selectionPos = _cursorPos = Math.Min(newText.Length, _cursorPos + text.Length);
        }
    }

    public void InsertText(string text) => InsertText(_getMessageFn(), text);

    private void ResetSelectionIfNeeded(bool selecting)
    {
        if (!selecting) _selectionPos = _cursorPos;
    }

    public void MoveBy(int count, bool selecting, CursorStep scope)
    {
        if (scope == CursorStep.Character) MoveByChars(count, selecting);
        else MoveByWords(count, selecting);
    }

    public void MoveByChars(int count) => MoveByChars(count, false);

    public void MoveByChars(int count, bool selecting)
    {
        _cursorPos = NacreCraft.Util.Util.OffsetByCodepoints(_getMessageFn(), _cursorPos, count);
        ResetSelectionIfNeeded(selecting);
    }

    public void MoveByWords(int count) => MoveByWords(count, false);

    public void MoveByWords(int count, bool selecting)
    {
        _cursorPos = StringSplitter.GetWordPosition(_getMessageFn(), count, _cursorPos, true);
        ResetSelectionIfNeeded(selecting);
    }

    public void RemoveFromCursor(int count, CursorStep scope)
    {
        if (scope == CursorStep.Character) RemoveCharsFromCursor(count);
        else RemoveWordsFromCursor(count);
    }

    public void RemoveWordsFromCursor(int count)
    {
        var wordPosition = StringSplitter.GetWordPosition(_getMessageFn(), count, _cursorPos, true);
        RemoveCharsFromCursor(wordPosition - _cursorPos);
    }

    public void RemoveCharsFromCursor(int count)
    {
        var message = _getMessageFn();
        if (message.Length == 0) return;
        string newMessage;
        if (_selectionPos != _cursorPos)
        {
            newMessage = DeleteSelection(message);
        }
        else
        {
            var otherPos = NacreCraft.Util.Util.OffsetByCodepoints(message, _cursorPos, count);
            var start = Math.Min(otherPos, _cursorPos);
            var end = Math.Max(otherPos, _cursorPos);
            newMessage = message.Remove(start, end - start);
            if (count < 0) _selectionPos = _cursorPos = start;
        }

        _setMessageFn(newMessage);
    }

    public void Cut()
    {
        var message = _getMessageFn();
        _setClipboardFn(GetSelected(message));
        _setMessageFn(DeleteSelection(message));
    }

    public void Paste()
    {
        InsertText(_getMessageFn(), _getClipboardFn());
        _selectionPos = _cursorPos;
    }

    public void Copy() => _setClipboardFn(GetSelected(_getMessageFn()));

    public void SelectAll()
    {
        _selectionPos = 0;
        _cursorPos = _getMessageFn().Length;
    }

    private string GetSelected(string text)
    {
        var startIndex = Math.Min(_cursorPos, _selectionPos);
        var endIndex = Math.Max(_cursorPos, _selectionPos);
        return text[startIndex..endIndex];
    }

    private string DeleteSelection(string message)
    {
        if (_selectionPos == _cursorPos) return message;
        var startIndex = Math.Min(_cursorPos, _selectionPos);
        var endIndex = Math.Max(_cursorPos, _selectionPos);
        var updatedText = message[..startIndex] + message[endIndex..];
        _selectionPos = _cursorPos = startIndex;
        return updatedText;
    }

    public void SetCursorToStart() => SetCursorToStart(false);

    public void SetCursorToStart(bool selecting)
    {
        _cursorPos = 0;
        ResetSelectionIfNeeded(selecting);
    }

    public void SetCursorToEnd() => SetCursorToEnd(false);

    public void SetCursorToEnd(bool selecting)
    {
        _cursorPos = _getMessageFn().Length;
        ResetSelectionIfNeeded(selecting);
    }

    public int GetCursorPos() => _cursorPos;

    public void SetCursorPos(int value) => SetCursorPos(value, true);

    public void SetCursorPos(int value, bool selecting)
    {
        _cursorPos = ClampToMsgLength(value);
        ResetSelectionIfNeeded(selecting);
    }

    public int GetSelectionPos() => _selectionPos;

    public void SetSelectionPos(int value) => _selectionPos = ClampToMsgLength(value);

    public void SetSelectionRange(int start, int end)
    {
        var maxSize = _getMessageFn().Length;
        _cursorPos = Mth.Clamp(start, 0, maxSize);
        _selectionPos = Mth.Clamp(end, 0, maxSize);
    }

    public bool IsSelecting() => _cursorPos != _selectionPos;

    public enum CursorStep
    {
        Character,
        Word
    }
}



