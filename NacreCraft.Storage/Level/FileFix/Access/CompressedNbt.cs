using NacreCraft.Codec;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//CompressedNbt base for one gzip-compressed NBT world file, maps to vanilla net.minecraft.util.filefix.access.CompressedNbt
//Paths are plain strings here, matching the storage layer; vanilla uses java.nio.Path
public abstract class CompressedNbt : IDisposable
{
    private readonly string _path;
    private readonly MissingSeverity _missingSeverity;

    protected CompressedNbt(string path, MissingSeverity missingSeverity)
    {
        _path = path;
        _missingSeverity = missingSeverity;
    }

    public abstract Optional<Dynamic<Tag>> Read();

    //ReadFile reads the file as a Dynamic compound, reporting a missing file at the configured severity
    protected Optional<Dynamic<Tag>> ReadFile()
    {
        try
        {
            return Optional<Dynamic<Tag>>.Of(
                new Dynamic<Tag>(NbtOps.Instance, NbtIo.ReadCompressed(_path, NbtAccounter.UnlimitedHeap())));
        }
        catch (FileNotFoundException)
        {
            _missingSeverity.LogMissing("Missing file:", _path);
            return Optional<Dynamic<Tag>>.Empty();
        }
    }

    public abstract void Write(Dynamic<Tag> data);

    //WriteFile writes a Dynamic compound back as compressed NBT
    protected void WriteFile(Dynamic<Tag> data)
    {
        var tag = (CompoundTag)data.Value;
        try
        {
            var parent = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(parent)) FileUtil.CreateDirectoriesSafe(parent);
            NbtIo.WriteCompressed(tag, _path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to write to {_path}: {e.Message}");
        }
    }

    public string Path() => _path;

    public virtual void Dispose()
    {
    }

    //MissingSeverity how loudly a missing file is reported, maps to vanilla MissingSeverity
    public enum MissingSeverity
    {
        IMPORTANT,
        NEUTRAL,
        MINOR
    }
}

//MissingSeverityLogging maps a missing-file severity to the matching log level, maps to the vanilla logFunction field
public static class MissingSeverityExtensions
{
    public static void LogMissing(this CompressedNbt.MissingSeverity severity, string message, string path)
    {
        switch (severity)
        {
            case CompressedNbt.MissingSeverity.IMPORTANT:
                Log.Error($"{message} {path}");
                break;
            case CompressedNbt.MissingSeverity.MINOR:
                Log.Debug($"{message} {path}");
                break;
            default:
                Log.Info($"{message} {path}");
                break;
        }
    }
}
