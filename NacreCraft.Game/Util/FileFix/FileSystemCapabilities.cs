namespace NacreCraft.Game.Util.FileFix;

//FileSystemCapabilities what the world's filesystem can do, maps to vanilla net.minecraft.util.filefix.FileSystemCapabilities
//The move options of the Java record collapse to plain bool flags here; C# has no CopyOption array
public sealed record FileSystemCapabilities(bool AtomicMove, bool HardLinks);
