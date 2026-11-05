using NacreCraft.Util;

namespace NacreCraft.Game.Data;

//CachedOutput the sink data providers write files through, maps to vanilla net.minecraft.data.CachedOutput
public interface CachedOutput
{
    Task WriteIfNeeded(string path, byte[] input, string hash);
}

public static class CachedOutputs
{
    //NoCache writes every file unconditionally, maps to vanilla CachedOutput.NO_CACHE
    public static readonly CachedOutput NoCache = new NoCacheOutput();

    private sealed class NoCacheOutput : CachedOutput
    {
        public Task WriteIfNeeded(string path, byte[] input, string hash)
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) FileUtil.CreateDirectoriesSafe(parent);
            File.WriteAllBytes(path, input);
            return Task.CompletedTask;
        }
    }
}
