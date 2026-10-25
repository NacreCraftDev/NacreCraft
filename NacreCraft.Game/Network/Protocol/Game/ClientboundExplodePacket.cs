using NacreCraft.Game.World.Particle;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundExplodePacket explosion packet, maps to vanilla ClientboundExplodePacket
//Fields: center Vec3 split into 3 doubles, radius float, blockCount int, playerKnockback Optional Vec3 split into hasKnockback + 3 doubles
//ExplosionParticle is a ParticleOptions, ExplosionSound is the inline SoundEvent, BlockParticles is a weighted list of ExplosionParticleInfo
public sealed record ClientboundExplodePacket(double CenterX, double CenterY, double CenterZ, float Radius, int BlockCount, bool HasKnockback, double KnockbackX, double KnockbackY, double KnockbackZ, object ExplosionParticle, object ExplosionSound, object BlockParticles) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundExplodePacket> StreamCodec { get; } = new ExplodeCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundExplode;

    public void Handle(ClientGamePacketListener handler) => handler.HandleExplosion(this);

    private sealed class ExplodeCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundExplodePacket>
    {
        public ClientboundExplodePacket Decode(RegistryFriendlyByteBuf buf)
        {
            var x = buf.ReadDouble();
            var y = buf.ReadDouble();
            var z = buf.ReadDouble();
            var radius = buf.ReadFloat();
            var blockCount = buf.ReadInt();
            var hasKnockback = buf.ReadBoolean();
            var knockbackX = hasKnockback ? buf.ReadDouble() : 0.0;
            var knockbackY = hasKnockback ? buf.ReadDouble() : 0.0;
            var knockbackZ = hasKnockback ? buf.ReadDouble() : 0.0;
            var particle = ParticleTypes.StreamCodec.Decode(buf);
            var sound = ReadSound(buf);
            var weightedCount = buf.ReadVarInt();
            var blockParticles = new List<ExplosionParticleInfo>(weightedCount);
            for (var i = 0; i < weightedCount; i++)
            {
                var infoParticle = ParticleTypes.StreamCodec.Decode(buf);
                var scaling = buf.ReadFloat();
                var speed = buf.ReadFloat();
                var weight = buf.ReadVarInt();
                blockParticles.Add(new ExplosionParticleInfo(infoParticle, scaling, speed, weight));
            }
            return new ClientboundExplodePacket(x, y, z, radius, blockCount, hasKnockback, knockbackX, knockbackY, knockbackZ, particle, sound, blockParticles);
        }

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundExplodePacket value)
        {
            buf.WriteDouble(value.CenterX);
            buf.WriteDouble(value.CenterY);
            buf.WriteDouble(value.CenterZ);
            buf.WriteFloat(value.Radius);
            buf.WriteInt(value.BlockCount);
            buf.WriteBoolean(value.HasKnockback);
            if (value.HasKnockback)
            {
                buf.WriteDouble(value.KnockbackX);
                buf.WriteDouble(value.KnockbackY);
                buf.WriteDouble(value.KnockbackZ);
            }
            if (value.ExplosionParticle is not ParticleOptions explosionParticle)
                throw new InvalidOperationException("Explosion particle payload is missing");
            ParticleTypes.StreamCodec.Encode(buf, explosionParticle);
            if (value.ExplosionSound is not SoundEvent explosionSound)
                throw new InvalidOperationException("Explosion sound payload is missing");
            WriteSound(buf, explosionSound);
            var blockParticles = value.BlockParticles as IReadOnlyList<ExplosionParticleInfo> ?? Array.Empty<ExplosionParticleInfo>();
            buf.WriteVarInt(blockParticles.Count);
            foreach (var info in blockParticles)
            {
                ParticleTypes.StreamCodec.Encode(buf, info.Particle);
                buf.WriteFloat(info.Scaling);
                buf.WriteFloat(info.Speed);
                buf.WriteVarInt(info.Weight);
            }
        }

        //ReadSound reads the sound holder; this project's sound_event registry is empty, so only the inline form can be recognized
        private static SoundEvent ReadSound(FriendlyByteBuf buf)
        {
            if (buf.ReadVarInt() != 0)
                throw new NotSupportedException("sound_event registry entry references are not supported yet");
            var location = buf.ReadIdentifier();
            float? fixedRange = buf.ReadBoolean() ? buf.ReadFloat() : null;
            return new SoundEvent(location, fixedRange);
        }

        //WriteSound writes the sound holder, always in the inline form
        private static void WriteSound(FriendlyByteBuf buf, SoundEvent sound)
        {
            buf.WriteVarInt(0);
            buf.WriteIdentifier(sound.Location);
            buf.WriteBoolean(sound.FixedRange.HasValue);
            if (sound.FixedRange.HasValue) buf.WriteFloat(sound.FixedRange.Value);
        }
    }
}

//ExplosionParticleInfo one weighted block particle entry; weight is carried on the wire but normally 1
public sealed record ExplosionParticleInfo(ParticleOptions Particle, float Scaling, float Speed, int Weight);
