namespace NacreCraft.Network.Chat;

//MessageSignatureCache a fixed-size cache of recently seen signatures, maps to vanilla net.minecraft.network.chat.MessageSignatureCache
public sealed class MessageSignatureCache
{
    public const int NotFound = -1;

    private const int DefaultCapacity = 128;

    private readonly MessageSignature?[] _entries;

    public MessageSignatureCache(int capacity) => _entries = new MessageSignature[capacity];

    public static MessageSignatureCache CreateDefault() => new(DefaultCapacity);

    //Pack the cache index of a signature, or -1 when absent
    public int Pack(MessageSignature signature)
    {
        for (var i = 0; i < _entries.Length; i++)
            if (signature.Equals(_entries[i])) return i;
        return NotFound;
    }

    public MessageSignature? Unpack(int id) => id >= 0 && id < _entries.Length ? _entries[id] : null;

    //Push adds a body's last-seen entries plus the newest signature, maps to vanilla push
    public void Push(SignedMessageBody body, MessageSignature? signature)
    {
        var queue = new LinkedList<MessageSignature>(body.LastSeen.Entries);
        if (signature is not null) queue.AddLast(signature);
        Push(queue);
    }

    private void Push(LinkedList<MessageSignature> queue)
    {
        var incoming = new HashSet<MessageSignature>(queue);
        for (var i = 0; i < _entries.Length && queue.Count > 0; i++)
        {
            var entry = _entries[i];
            _entries[i] = queue.Last!.Value;
            queue.RemoveLast();
            if (entry is null || incoming.Contains(entry)) continue;
            queue.AddFirst(entry);
        }
    }
}
