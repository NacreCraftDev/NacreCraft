using Avalonia;
using Avalonia.Fonts.Inter;
using NacreCraft.Game.Server;

namespace NacreCraft.Server.Gui;

//ServerGuiHost, the server GUI host
//Maps to the assembly part of vanilla net.minecraft.server.gui.MinecraftServerGui.showFrameFor
//The main thread runs the Avalonia message loop and returns only when the window closes, the caller puts the server main loop on a background thread
public static class ServerGuiHost
{
    //IsAvailable reports whether this machine can host a window
    //Platform detection throws rather than returning a failure when a Linux box has neither an X display nor a Wayland
    //socket, which is what a WSL session without WSLg looks like, and it throws while the message loop is starting
    public static bool IsAvailable()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) return true;
        if (!OperatingSystem.IsLinux()) return false;
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    //Run starts the GUI and blocks until the window closes
    //server is a fully constructed server instance, the window only reads its state and does not take over its lifecycle
    public static void Run(MinecraftServer server, string[] args)
    {
        //--potato easter egg toggle, PotatoIcon skips internally on non-Windows
        var potato = Array.IndexOf(args, "--potato") >= 0;
        AppBuilder.Configure(() => new ServerGuiApp(server, potato))
            .UsePlatformDetect()
            .WithInterFont()
            .StartWithClassicDesktopLifetime(args);
    }
}
