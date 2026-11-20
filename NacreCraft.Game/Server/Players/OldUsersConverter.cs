using System.Globalization;
using NacreCraft.Game.Server;
using NacreCraft.Logging;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Players;

//OldUsersConverter converts the pre-JSON list files to the current formats, maps to vanilla net.minecraft.server.players.OldUsersConverter
//The old files are line based: banned lists use name|created|source|expires|reason, ops/whitelist use one name per line.
//Successful conversions rename the old file to <name>.converted so the next startup skips it
public static class OldUsersConverter
{
    public const string OldIpBanList = "banned-ips.txt";
    public const string OldUserBanList = "banned-players.txt";
    public const string OldOpList = "ops.txt";
    public const string OldWhiteList = "white-list.txt";

    //ConvertUserBanlist converts banned-players.txt, maps to vanilla convertUserBanlist
    public static bool ConvertUserBanlist(string oldFile, string newFile, Func<string, NameAndId?> lookup)
    {
        if (!File.Exists(oldFile)) return true;
        try
        {
            var bans = new BanList(newFile);
            foreach (var line in File.ReadAllLines(oldFile))
            {
                var parts = line.Trim().Split('|');
                var name = parts[0].Trim();
                if (name.Length == 0 || name.StartsWith('#')) continue;
                var profile = lookup(name) ?? NameAndId.CreateOffline(name);
                var source = parts.Length > 2 ? parts[2] : "OldUsersConverter";
                var expires = parts.Length > 3 ? NormalizeDate(parts[3]) : BanList.Forever;
                var reason = parts.Length > 4 ? parts[4] : "";
                bans.Add(new GameProfile(profile.Uuid, profile.Name), source, reason, expires);
            }
            RenameOldFile(oldFile);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not convert old user ban list {oldFile}: {e.Message}");
            return false;
        }
    }

    //ConvertIpBanlist converts banned-ips.txt, maps to vanilla convertIpBanlist
    public static bool ConvertIpBanlist(string oldFile, string newFile)
    {
        if (!File.Exists(oldFile)) return true;
        try
        {
            var bans = new IpBanList(newFile);
            foreach (var line in File.ReadAllLines(oldFile))
            {
                var parts = line.Trim().Split('|');
                var address = parts[0].Trim();
                if (address.Length == 0 || address.StartsWith('#')) continue;
                var source = parts.Length > 2 ? parts[2] : "OldUsersConverter";
                var expires = parts.Length > 3 ? NormalizeDate(parts[3]) : BanList.Forever;
                var reason = parts.Length > 4 ? parts[4] : "";
                bans.Add(address, source, reason, expires);
            }
            RenameOldFile(oldFile);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not convert old IP ban list {oldFile}: {e.Message}");
            return false;
        }
    }

    //ConvertOpsList converts ops.txt, maps to vanilla convertOpsList
    public static bool ConvertOpsList(string oldFile, string newFile, Func<string, NameAndId?> lookup,
        int opPermissionLevel)
    {
        if (!File.Exists(oldFile)) return true;
        try
        {
            var ops = new OpList(newFile);
            foreach (var line in File.ReadAllLines(oldFile))
            {
                var name = line.Trim();
                if (name.Length == 0 || name.StartsWith('#')) continue;
                var profile = lookup(name) ?? NameAndId.CreateOffline(name);
                ops.Add(new GameProfile(profile.Uuid, profile.Name), opPermissionLevel, false);
            }
            RenameOldFile(oldFile);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not convert old op list {oldFile}: {e.Message}");
            return false;
        }
    }

    //ConvertWhitelist converts white-list.txt, maps to vanilla convertWhiteList
    public static bool ConvertWhitelist(string oldFile, string newFile, Func<string, NameAndId?> lookup)
    {
        if (!File.Exists(oldFile)) return true;
        try
        {
            var whitelist = new WhiteList(newFile);
            foreach (var line in File.ReadAllLines(oldFile))
            {
                var name = line.Trim();
                if (name.Length == 0 || name.StartsWith('#')) continue;
                var profile = lookup(name) ?? NameAndId.CreateOffline(name);
                whitelist.Add(new GameProfile(profile.Uuid, profile.Name));
            }
            RenameOldFile(oldFile);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not convert old whitelist {oldFile}: {e.Message}");
            return false;
        }
    }

    //ConvertMobOwnerIfNecessary resolves a legacy mob owner name or uuid, maps to vanilla convertMobOwnerIfNecessary
    //A 32-character or malformed input is treated as a raw uuid; offline mode falls back to the derived profile
    public static Guid? ConvertMobOwnerIfNecessary(string owner, UserNameToIdResolver nameToId,
        bool usesAuthentication)
    {
        if (string.IsNullOrEmpty(owner) || owner.Length > 16)
            return Guid.TryParse(owner, out var id) ? id : null;
        var cached = nameToId.Get(owner);
        if (cached.IsPresent) return cached.Get().Uuid;
        if (!usesAuthentication) return NameAndId.CreateOffline(owner).Uuid;
        return null;
    }

    //ConvertPlayers moves the legacy world/players directory to world/playerdata, maps to vanilla convertPlayers
    //The current PlayerDataStorage already reads playerdata, so only the directory move remains
    public static bool ConvertPlayers(string worldDirectory)
    {
        var oldDirectory = Path.Combine(worldDirectory, "players");
        var newDirectory = Path.Combine(worldDirectory, "playerdata");
        if (!Directory.Exists(oldDirectory)) return true;
        try
        {
            Directory.CreateDirectory(newDirectory);
            foreach (var file in Directory.EnumerateFiles(oldDirectory, "*.dat"))
            {
                var destination = Path.Combine(newDirectory, Path.GetFileName(file));
                if (!File.Exists(destination)) File.Move(file, destination);
            }
            Directory.Delete(oldDirectory, true);
            Log.Info($"Converted legacy player directory {oldDirectory} to {newDirectory}");
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Could not convert legacy player directory {oldDirectory}: {e.Message}");
            return false;
        }
    }

    //NormalizeDate converts the legacy date format to ISO-8601 so the JSON lists compare expiry correctly
    private static string NormalizeDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == BanList.Forever) return BanList.Forever;
        return BanListEntry<NameAndId>.ParseInstant(text) is { } instant
            ? instant.ToString("o", CultureInfo.InvariantCulture)
            : text;
    }

    private static void RenameOldFile(string oldFile)
    {
        try
        {
            var converted = oldFile + ".converted";
            if (File.Exists(converted)) File.Delete(converted);
            File.Move(oldFile, converted);
        }
        catch (Exception e)
        {
            Log.Warning($"Could not rename converted file {oldFile}: {e.Message}");
        }
    }
}
