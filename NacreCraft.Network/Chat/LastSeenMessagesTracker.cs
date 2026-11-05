namespace NacreCraft.Network.Chat;

//LastSeenMessagesTracker tracks pending last-seen entries and builds the acknowledgement update
//maps to vanilla net.minecraft.network.chat.LastSeenMessagesTracker
public sealed class LastSeenMessagesTracker
{
    private readonly LastSeenTrackedEntry?[] _trackedMessages;
    private int _tail;
    private int _offset;
    private MessageSignature? _lastTrackedMessage;

    public LastSeenMessagesTracker(int lastSeenCount) => _trackedMessages = new LastSeenTrackedEntry[lastSeenCount];

    public bool AddPending(MessageSignature message, bool wasShown)
    {
        if (Equals(message, _lastTrackedMessage)) return false;
        _lastTrackedMessage = message;
        AddEntry(wasShown ? new LastSeenTrackedEntry(message, true) : null);
        return true;
    }

    private void AddEntry(LastSeenTrackedEntry? entry)
    {
        var index = _tail;
        _tail = (index + 1) % _trackedMessages.Length;
        _offset++;
        _trackedMessages[index] = entry;
    }

    public void IgnorePending(MessageSignature pendingMessage)
    {
        for (var i = 0; i < _trackedMessages.Length; i++)
        {
            var entry = _trackedMessages[i];
            if (entry is null || !entry.Pending || !pendingMessage.Equals(entry.Signature)) continue;
            _trackedMessages[i] = null;
            break;
        }
    }

    public int GetAndClearOffset()
    {
        var original = _offset;
        _offset = 0;
        return original;
    }

    public int Offset => _offset;

    //GenerateAndApplyUpdate builds the update and acknowledges every entry, maps to vanilla generateAndApplyUpdate
    public UpdateResult GenerateAndApplyUpdate()
    {
        var offset = GetAndClearOffset();
        var acknowledged = new bool[_trackedMessages.Length];
        var lastSeenEntries = new List<MessageSignature>(_trackedMessages.Length);
        for (var i = 0; i < _trackedMessages.Length; i++)
        {
            var index = (_tail + i) % _trackedMessages.Length;
            var message = _trackedMessages[index];
            if (message is null) continue;
            acknowledged[i] = true;
            lastSeenEntries.Add(message.Signature);
            _trackedMessages[index] = message.Acknowledge();
        }
        var lastSeen = new LastSeenMessages(lastSeenEntries);
        var update = new LastSeenMessages.Update(offset, acknowledged, lastSeen.ComputeChecksum());
        return new UpdateResult(lastSeen, update);
    }

    //UpdateResult the validated last-seen window plus the acknowledgement update
    public sealed record UpdateResult(LastSeenMessages LastSeen, LastSeenMessages.Update Update);
}
