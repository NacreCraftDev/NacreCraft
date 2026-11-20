using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Network.Chat;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Entity;

//ProfilePublicKey a player's chat signing public key with its expiry, maps to vanilla net.minecraft.world.entity.player.ProfilePublicKey
//The key is a base64 DER RSA SubjectPublicKeyInfo, signatures are SHA256withRSA; NC has the same Crypt layout, so validation works
public sealed record ProfilePublicKey(ProfilePublicKey.KeyData Data)
{
    public static readonly Component ExpiredProfilePublicKey =
        Component.Translatable("multiplayer.disconnect.expired_public_key");

    private static readonly Component InvalidSignature =
        Component.Translatable("multiplayer.disconnect.invalid_public_key_signature");

    public static readonly TimeSpan ExpiryGracePeriod = TimeSpan.FromHours(8);

    //TrustedCodec the on-disk JSON form, maps to vanilla TRUSTED_CODEC
    public static readonly Codec<ProfilePublicKey> TrustedCodec =
        KeyData.Codec.Xmap(data => new ProfilePublicKey(data), key => key.Data);

    //CreateValidated checks the key signature before trusting the key, maps to vanilla createValidated
    public static ProfilePublicKey CreateValidated(SignatureValidator validator, Guid profileId, KeyData data)
    {
        if (!data.ValidateSignature(validator, profileId)) throw new ValidationException(InvalidSignature);
        return new ProfilePublicKey(data);
    }

    //CreateSignatureValidator builds a validator for messages signed by this key, maps to vanilla createSignatureValidator
    public SignatureValidator CreateSignatureValidator() => new RsaSignatureValidator(Data.Key);

    //KeyData the key, its expiry and the signature over it, maps to vanilla ProfilePublicKey.Data
    public sealed record KeyData(DateTimeOffset ExpiresAt, RSA Key, byte[] KeySignature)
    {
        private const int MaxKeySignatureSize = 4096;

        //Codec JSON form: ISO-8601 instant, base64 SPKI key, base64 signature, maps to vanilla CODEC
        public static readonly Codec<KeyData> Codec = RecordCodecBuilder.Of3(
            Codecs.String.ComapFlatMap(ParseInstant, FormatInstant).FieldOf("expires_at")
                .ForGetter((KeyData d) => d.ExpiresAt),
            Codecs.String.ComapFlatMap(ParsePublicKey, FormatPublicKey).FieldOf("key")
                .ForGetter((KeyData d) => d.Key),
            Codecs.String.ComapFlatMap(ParseBase64, Convert.ToBase64String).FieldOf("signature_v2")
                .ForGetter((KeyData d) => d.KeySignature),
            (expiresAt, key, signature) => new KeyData(expiresAt, key, signature));

        //Read the network form: epoch millis, SPKI key, signature, maps to the vanilla FriendlyByteBuf constructor
        public KeyData(FriendlyByteBuf buf)
            : this(DateTimeOffset.FromUnixTimeMilliseconds(buf.ReadLong()), ReadPublicKey(buf), buf.ReadByteArray(MaxKeySignatureSize))
        {
        }

        //Write the network form, maps to vanilla write
        public void Write(FriendlyByteBuf buf)
        {
            buf.WriteLong(ExpiresAt.ToUnixTimeMilliseconds());
            buf.WriteByteArray(Key.ExportSubjectPublicKeyInfo());
            buf.WriteByteArray(KeySignature);
        }

        //ValidateSignature checks the key was signed by the account's session key, maps to vanilla validateSignature
        public bool ValidateSignature(SignatureValidator validator, Guid profileId)
            => validator.Validate(SignatureUpdater.Of(output => output.Update(SignedPayload(profileId))), KeySignature);

        //SignedPayload the byte string the session service signs: profile uuid, expiry millis, key bytes, maps to vanilla signedPayload
        private byte[] SignedPayload(Guid profileId)
        {
            var keyBytes = Key.ExportSubjectPublicKeyInfo();
            var payload = new byte[24 + keyBytes.Length];
            var span = payload.AsSpan();
            profileId.TryWriteBytes(span[..16], bigEndian: true, out _);
            BinaryPrimitives.WriteInt64BigEndian(span[16..24], ExpiresAt.ToUnixTimeMilliseconds());
            keyBytes.CopyTo(span[24..]);
            return payload;
        }

        public bool HasExpired() => ExpiresAt < DateTimeOffset.UtcNow;

        public bool HasExpired(TimeSpan gracePeriod) => ExpiresAt + gracePeriod < DateTimeOffset.UtcNow;

        private static DataResult<DateTimeOffset> ParseInstant(string value)
            => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var instant)
                ? DataResult<DateTimeOffset>.Success(instant)
                : DataResult<DateTimeOffset>.Error(() => $"Invalid instant '{value}'");

        private static string FormatInstant(DateTimeOffset instant) => instant.ToString("O", CultureInfo.InvariantCulture);

        private static DataResult<RSA> ParsePublicKey(string value)
        {
            try
            {
                var key = RSA.Create();
                key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(value), out _);
                return DataResult<RSA>.Success(key);
            }
            catch (Exception exception) when (exception is FormatException or CryptographicException)
            {
                return DataResult<RSA>.Error(() => "Invalid public key");
            }
        }

        private static string FormatPublicKey(RSA key) => Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

        private static RSA ReadPublicKey(FriendlyByteBuf buf)
        {
            var key = RSA.Create();
            key.ImportSubjectPublicKeyInfo(buf.ReadByteArray(512), out _);
            return key;
        }

        private static DataResult<byte[]> ParseBase64(string value)
        {
            try
            {
                return DataResult<byte[]>.Success(Convert.FromBase64String(value));
            }
            catch (FormatException)
            {
                return DataResult<byte[]>.Error(() => "Invalid base64 signature");
            }
        }
    }

    //ValidationException thrown when the key signature does not verify, maps to vanilla ValidationException
    public sealed class ValidationException(Component component) : ThrowingComponent(component);

    //RsaSignatureValidator SHA256withRSA verification over the bytes an updater produces, maps to vanilla SignatureValidator.from(key, "SHA256withRSA")
    private sealed class RsaSignatureValidator(RSA key) : SignatureValidator
    {
        public bool Validate(SignatureUpdater updater, byte[] signature)
        {
            var output = new BufferOutput();
            updater.Update(output);
            return key.VerifyData(output.ToArray(), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        private sealed class BufferOutput : SignatureUpdater.Output
        {
            private readonly MemoryStream _stream = new();

            public void Update(byte[] bytes) => _stream.Write(bytes, 0, bytes.Length);

            public byte[] ToArray() => _stream.ToArray();
        }
    }
}
