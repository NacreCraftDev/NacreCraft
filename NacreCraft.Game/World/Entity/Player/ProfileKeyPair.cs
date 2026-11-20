using System.Globalization;
using System.Security.Cryptography;
using NacreCraft.Codec;

namespace NacreCraft.Game.World.Entity;

//ProfileKeyPair a player's chat signing key pair, maps to vanilla net.minecraft.world.entity.player.ProfileKeyPair
//The private key is base64 PKCS#8 DER and only ever exists on the owning client; the public half travels in ProfilePublicKey
public sealed record ProfileKeyPair(RSA PrivateKey, ProfilePublicKey PublicKey, DateTimeOffset RefreshedAfter)
{
    //Codec the on-disk JSON form, maps to vanilla CODEC
    public static readonly Codec<ProfileKeyPair> Codec = RecordCodecBuilder.Of3(
        Codecs.String.ComapFlatMap(ParsePrivateKey, FormatPrivateKey).FieldOf("private_key")
            .ForGetter((ProfileKeyPair pair) => pair.PrivateKey),
        ProfilePublicKey.TrustedCodec.FieldOf("public_key")
            .ForGetter((ProfileKeyPair pair) => pair.PublicKey),
        Codecs.String.ComapFlatMap(ParseInstant, FormatInstant).FieldOf("refreshed_after")
            .ForGetter((ProfileKeyPair pair) => pair.RefreshedAfter),
        (privateKey, publicKey, refreshedAfter) => new ProfileKeyPair(privateKey, publicKey, refreshedAfter));

    //DueRefresh whether the key pair is past its refresh instant, maps to vanilla dueRefresh
    public bool DueRefresh() => RefreshedAfter < DateTimeOffset.UtcNow;

    private static DataResult<RSA> ParsePrivateKey(string value)
    {
        try
        {
            var key = RSA.Create();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(value), out _);
            return DataResult<RSA>.Success(key);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return DataResult<RSA>.Error(() => "Invalid private key");
        }
    }

    private static string FormatPrivateKey(RSA key) => Convert.ToBase64String(key.ExportPkcs8PrivateKey());

    private static DataResult<DateTimeOffset> ParseInstant(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var instant)
            ? DataResult<DateTimeOffset>.Success(instant)
            : DataResult<DateTimeOffset>.Error(() => $"Invalid instant '{value}'");

    private static string FormatInstant(DateTimeOffset instant) => instant.ToString("O", CultureInfo.InvariantCulture);
}
