namespace NacreCraft.Network.Chat;

//LastSeenMessagesValidator validates the client's last-seen acknowledgement window
//maps to vanilla net.minecraft.network.chat.LastSeenMessagesValidator
public sealed class LastSeenMessagesValidator
{
    private readonly int _lastSeenCount;
    private readonly List<LastSeenTrackedEntry?> _trackedMessages = new();
    private MessageSignature? _lastPendingMessage;

    public LastSeenMessagesValidator(int lastSeenCount)
    {
        _lastSeenCount = lastSeenCount;
        for (var i = 0; i < lastSeenCount; i++) _trackedMessages.Add(null);
    }

    public void AddPending(MessageSignature message)
    {
        if (message.Equals(_lastPendingMessage)) return;
        _trackedMessages.Add(new LastSeenTrackedEntry(message, true));
        _lastPendingMessage = message;
    }

    public int TrackedMessagesCount() => _trackedMessages.Count;

    public void ApplyOffset(int offset)
    {
        var maxOffset = _trackedMessages.Count - _lastSeenCount;
        if (offset < 0 || offset > maxOffset)
            throw new ValidationException($"Advanced last seen window by {offset} messages, but expected at most {maxOffset}");
        _trackedMessages.RemoveRange(0, offset);
    }

    //ApplyUpdate validates the acknowledgement bits and returns the resulting window, maps to vanilla applyUpdate
    public LastSeenMessages ApplyUpdate(LastSeenMessages.Update update)
    {
        ApplyOffset(update.Offset);
        var lastSeenEntries = new List<MessageSignature>(update.Acknowledged.Count(flag => flag));
        if (update.Acknowledged.Count > _lastSeenCount)
            throw new ValidationException(
                $"Last seen update contained {update.Acknowledged.Count} messages, but maximum window size is {_lastSeenCount}");
        for (var i = 0; i < _lastSeenCount; i++)
        {
            var acknowledged = i < update.Acknowledged.Count && update.Acknowledged[i];
            var message = _trackedMessages[i];
            if (acknowledged)
            {
                if (message is null)
                    throw new ValidationException(
                        $"Last seen update acknowledged unknown or previously ignored message at index {i}");
                _trackedMessages[i] = message.Acknowledge();
                lastSeenEntries.Add(message.Signature);
                continue;
            }
            if (message is not null && !message.Pending)
                throw new ValidationException(
                    $"Last seen update ignored previously acknowledged message at index {i} and signature {message.Signature}");
            _trackedMessages[i] = null;
        }
        var lastSeen = new LastSeenMessages(lastSeenEntries);
        if (!update.VerifyChecksum(lastSeen))
            throw new ValidationException("Checksum mismatch on last seen update: the client and server must have desynced");
        return lastSeen;
    }

    //ValidationException a malformed last-seen update
    public sealed class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
