using System.Runtime.InteropServices;

namespace NacreCraft.Server.Network;

//EventLoopGroupHolder the I/O backend selected for the server socket, maps to vanilla net.minecraft.server.network.EventLoopGroupHolder
//NC has no Netty, so the holder describes the backend and thread naming instead of owning an EventLoopGroup;
//ConnectionAcceptor uses .NET sockets and the selected backend only affects diagnostics
public sealed class EventLoopGroupHolder
{
    public static readonly EventLoopGroupHolder Nio = new("NIO", isLocal: false);
    public static readonly EventLoopGroupHolder Epoll = new("Epoll", isLocal: false);
    public static readonly EventLoopGroupHolder KQueue = new("Kqueue", isLocal: false);
    public static readonly EventLoopGroupHolder Local = new("Local", isLocal: true);

    private EventLoopGroupHolder(string type, bool isLocal)
    {
        Type = type;
        IsLocal = isLocal;
    }

    public string Type { get; }

    public bool IsLocal { get; }

    //Remote selects the native transport when the platform supports it, maps to vanilla remote(boolean)
    public static EventLoopGroupHolder Remote(bool allowNativeTransport)
    {
        if (!allowNativeTransport) return Nio;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return KQueue;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return Epoll;
        return Nio;
    }

    public static EventLoopGroupHolder LocalHolder() => Local;

    //ThreadNamePrefix the thread name prefix used by the socket loops
    public string ThreadNamePrefix => IsLocal ? "Local" : Type;

    public override string ToString() => Type;
}
