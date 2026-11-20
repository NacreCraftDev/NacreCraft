using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.Server.Network;

//FilteredText a chat string and the mask of filtered characters, maps to vanilla net.minecraft.server.network.FilteredText
public sealed record FilteredText(string Raw, NacreCraft.Network.Chat.FilterMask Mask)
{
    public static readonly FilteredText Empty = PassThrough(string.Empty);

    public static FilteredText PassThrough(string message) => new(message, NacreCraft.Network.Chat.FilterMask.PassThrough);

    public static FilteredText FullyFiltered(string message) => new(message, NacreCraft.Network.Chat.FilterMask.FullyFiltered);

    public string? Filtered => Mask.Apply(Raw);

    public string FilteredOrEmpty() => Filtered ?? string.Empty;

    public bool IsFiltered => !Mask.IsEmpty;
}

//TextFilter an asynchronous chat filter, maps to vanilla net.minecraft.server.network.TextFilter
public interface TextFilter
{
    public static readonly TextFilter Dummy = new PassThroughTextFilter();

    Task<FilteredText> ProcessStreamMessage(string message);

    Task<IReadOnlyList<FilteredText>> ProcessMessageBundle(IReadOnlyList<string> messages);

    void Join()
    {
    }

    void Leave()
    {
    }

    private sealed class PassThroughTextFilter : TextFilter
    {
        public Task<FilteredText> ProcessStreamMessage(string message)
            => Task.FromResult(FilteredText.PassThrough(message));

        public Task<IReadOnlyList<FilteredText>> ProcessMessageBundle(IReadOnlyList<string> messages)
            => Task.FromResult<IReadOnlyList<FilteredText>>(messages.Select(FilteredText.PassThrough).ToList());
    }
}

//LegacyProtocolUtils legacy ping helpers, maps to vanilla net.minecraft.server.network.LegacyProtocolUtils
public static class LegacyProtocolUtils
{
    public const int CustomPayloadPacketId = 250;
    public const string CustomPayloadPacketPingChannel = "MC|PingHost";
    public const int GetInfoPacketId = 254;
    public const int GetInfoPacketVersion1 = 1;
    public const int DisconnectPacketId = 255;
    public const int FakeProtocolVersion = 127;

    //WriteLegacyString writes a UTF-16BE string with a short character-count prefix
    public static void WriteLegacyString(FriendlyByteBuf toSend, string value)
    {
        toSend.WriteShort((short)value.Length);
        foreach (var c in value) toSend.WriteShort((short)c);
    }

    //ReadLegacyString reads the UTF-16BE form written by WriteLegacyString
    public static string ReadLegacyString(FriendlyByteBuf message)
    {
        var charCount = message.ReadShort();
        var chars = new char[Math.Max(0, (int)charCount)];
        for (var i = 0; i < chars.Length; i++) chars[i] = (char)message.ReadShort();
        return new string(chars);
    }
}
