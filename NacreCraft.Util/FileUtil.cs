using System.IO;

namespace NacreCraft.Util;

//File utilities, maps to vanilla FileUtil
public static class FileUtil
{
    //Idempotent directory creation, maps to vanilla createDirectoriesSafe
    //Directory.CreateDirectory is already idempotent, swallows race-induced IOException
    public static void CreateDirectoriesSafe(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (IOException)
        {
            if (!Directory.Exists(path)) throw;
        }
    }

    //FindAvailableName returns a name in the directory that is not taken, maps to vanilla findAvailableName
    //The first try is name+suffix, then name (1)+suffix, name (2)+suffix and so on
    public static string FindAvailableName(string directory, string name, string suffix)
    {
        var candidate = name + suffix;
        for (var index = 1; Path.Exists(Path.Combine(directory, candidate)); index++)
            candidate = $"{name} ({index}){suffix}";
        return candidate;
    }
}
