//DebugEntityNameGenerator maps to vanilla net.minecraft.network.protocol.game.DebugEntityNameGenerator
using System.Buffers.Binary;
using NacreCraft.Game.Server;
using NacreCraft.Registry;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.Network.Protocol.Game;

//Generates the short human-readable names used by the debug renderers, maps to vanilla DebugEntityNameGenerator
//Names are deterministic for a UUID: same hash seed, same two random picks
public static class DebugEntityNameGenerator
{
    //First name parts, copied verbatim from vanilla
    private static readonly string[] NamesFirstPart =
    {
        "Slim", "Far", "River", "Silly", "Fat", "Thin", "Fish", "Bat", "Dark", "Oak", "Sly", "Bush", "Zen", "Bark",
        "Cry", "Slack", "Soup", "Grim", "Hook", "Dirt", "Mud", "Sad", "Hard", "Crook", "Sneak", "Stink", "Weird",
        "Fire", "Soot", "Soft", "Rough", "Cling", "Scar"
    };

    //Second name parts, copied verbatim from vanilla
    private static readonly string[] NamesSecondPart =
    {
        "Fox", "Tail", "Jaw", "Whisper", "Twig", "Root", "Finder", "Nose", "Brow", "Blade", "Fry", "Seek", "Wart",
        "Tooth", "Foot", "Leaf", "Stone", "Fall", "Face", "Tongue", "Voice", "Lip", "Mouth", "Snail", "Toe", "Ear",
        "Hair", "Beard", "Shirt", "Fist"
    };

    //GetEntityName picks a stable debug name for an entity, maps to vanilla getEntityName(Entity)
    public static string GetEntityName(Entity entity)
    {
        //Vanilla returns the plain text name for players; the server player keeps it in its game profile
        if (entity is ServerPlayer player) return player.Profile.Name;
        //Entity has no custom name field in NacreCraft yet, so the vanilla custom-name branch is omitted
        return GetEntityName(entity.Uuid);
    }

    //GetEntityName derives the name from the UUID, maps to vanilla getEntityName(UUID)
    //The UUID hash is Java's UUID.hashCode, not Guid.GetHashCode, so names match vanilla for the same UUID
    public static string GetEntityName(Guid uuid)
    {
        var random = new ThreadSafeLegacyRandomSource(JavaUuidHash(uuid) >> 2);
        return GetRandomString(random, NamesFirstPart) + GetRandomString(random, NamesSecondPart);
    }

    //GetRandomString picks one part from a name table, maps to vanilla getRandomString
    private static string GetRandomString(RandomSource random, string[] names)
        => RandomCollections.GetRandom(names, random);

    //JavaUuidHash reproduces java.util.UUID.hashCode from the two 64-bit halves
    private static int JavaUuidHash(Guid uuid)
    {
        var bytes = uuid.ToByteArray();
        Span<byte> bigEndian = stackalloc byte[16];
        bigEndian[0] = bytes[3];
        bigEndian[1] = bytes[2];
        bigEndian[2] = bytes[1];
        bigEndian[3] = bytes[0];
        bigEndian[4] = bytes[5];
        bigEndian[5] = bytes[4];
        bigEndian[6] = bytes[7];
        bigEndian[7] = bytes[6];
        for (var i = 8; i < 16; i++) bigEndian[i] = bytes[i];

        var msb = BinaryPrimitives.ReadInt64BigEndian(bigEndian);
        var lsb = BinaryPrimitives.ReadInt64BigEndian(bigEndian[8..]);
        var hilo = msb ^ lsb;
        return (int)(hilo >> 32) ^ (int)hilo;
    }
}
