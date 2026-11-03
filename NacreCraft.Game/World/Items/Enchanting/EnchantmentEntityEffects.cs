using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentEffectContext what an entity effect may read while it runs
//Level, item and entity stay loosely typed here: the concrete types live in the storage and entity layers, and the
//effect table only needs to decode before those layers exist
public readonly record struct EnchantmentEffectContext(
    object Level, int EnchantmentLevel, object? Item, object Entity, object Position);

//EnchantmentEntityEffect base for effects that run against an entity, maps to vanilla EnchantmentEntityEffect
public abstract record EnchantmentEntityEffect : EnchantmentLocationBasedEffect
{
    //Apply runs the effect, maps to vanilla apply
    public abstract void Apply(in EnchantmentEffectContext context);

    //Vanilla entity effects treat a block change as a plain apply, maps to the default onChangedBlock
    public override void OnChangedBlock(in EnchantmentEffectContext context, bool becameActive) => Apply(context);
}

//AllOfEntityEffects runs a list of entity effects in order, maps to vanilla AllOf.EntityEffects
public sealed record AllOfEntityEffects(IReadOnlyList<EnchantmentEntityEffect> Effects) : EnchantmentEntityEffect
{
    public static readonly MapCodec<AllOfEntityEffects> Codec =
        RecordCodecBuilder.Of1<AllOfEntityEffects, IReadOnlyList<EnchantmentEntityEffect>>(
            EnchantmentEntityEffectCodec.Instance.ListOf().FieldOf("effects")
                .ForGetter<AllOfEntityEffects, IReadOnlyList<EnchantmentEntityEffect>>(value => value.Effects),
            effects => new AllOfEntityEffects(effects));

    public override void Apply(in EnchantmentEffectContext context)
    {
        foreach (var effect in Effects) effect.Apply(context);
    }
}

//ApplyMobEffect applies one of a set of mob effects with a level based duration and amplifier
public sealed record ApplyMobEffect(HolderSet<NacreCraft.Registry.MobEffect> ToApply,
    NacreCraft.Registry.LevelBasedValue MinDuration, NacreCraft.Registry.LevelBasedValue MaxDuration,
    NacreCraft.Registry.LevelBasedValue MinAmplifier, NacreCraft.Registry.LevelBasedValue MaxAmplifier)
    : EnchantmentEntityEffect
{
    public static readonly MapCodec<ApplyMobEffect> Codec =
        RecordCodecBuilder.Of5<ApplyMobEffect, HolderSet<NacreCraft.Registry.MobEffect>,
            NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue,
            NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue>(
            HolderSetCodecs.MobEffectSet.FieldOf("to_apply")
                .ForGetter<ApplyMobEffect, HolderSet<NacreCraft.Registry.MobEffect>>(value => value.ToApply),
            LevelBasedValueCodec.Instance.FieldOf("min_duration")
                .ForGetter<ApplyMobEffect, NacreCraft.Registry.LevelBasedValue>(value => value.MinDuration),
            LevelBasedValueCodec.Instance.FieldOf("max_duration")
                .ForGetter<ApplyMobEffect, NacreCraft.Registry.LevelBasedValue>(value => value.MaxDuration),
            LevelBasedValueCodec.Instance.FieldOf("min_amplifier")
                .ForGetter<ApplyMobEffect, NacreCraft.Registry.LevelBasedValue>(value => value.MinAmplifier),
            LevelBasedValueCodec.Instance.FieldOf("max_amplifier")
                .ForGetter<ApplyMobEffect, NacreCraft.Registry.LevelBasedValue>(value => value.MaxAmplifier),
            (toApply, minDuration, maxDuration, minAmplifier, maxAmplifier) =>
                new ApplyMobEffect(toApply, minDuration, maxDuration, minAmplifier, maxAmplifier));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the entity effect table, which arrives with the entity layer
    }
}

//ChangeItemDamage changes the durability of the item that carried the enchantment
public sealed record ChangeItemDamage(NacreCraft.Registry.LevelBasedValue Amount) : EnchantmentEntityEffect
{
    public static readonly MapCodec<ChangeItemDamage> Codec =
        RecordCodecBuilder.Of1<ChangeItemDamage, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("amount")
                .ForGetter<ChangeItemDamage, NacreCraft.Registry.LevelBasedValue>(value => value.Amount),
            amount => new ChangeItemDamage(amount));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs item damage handling, which arrives with the item layer
    }
}

//DamageEntity hurts the target with a level based damage range
public sealed record DamageEntity(NacreCraft.Registry.LevelBasedValue MinDamage,
    NacreCraft.Registry.LevelBasedValue MaxDamage, string DamageType) : EnchantmentEntityEffect
{
    public static readonly MapCodec<DamageEntity> Codec =
        RecordCodecBuilder.Of3<DamageEntity, NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue,
            string>(
            LevelBasedValueCodec.Instance.FieldOf("min_damage")
                .ForGetter<DamageEntity, NacreCraft.Registry.LevelBasedValue>(value => value.MinDamage),
            LevelBasedValueCodec.Instance.FieldOf("max_damage")
                .ForGetter<DamageEntity, NacreCraft.Registry.LevelBasedValue>(value => value.MaxDamage),
            Codecs.String.FieldOf("damage_type").ForGetter<DamageEntity, string>(value => value.DamageType),
            (minDamage, maxDamage, damageType) => new DamageEntity(minDamage, maxDamage, damageType));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the entity damage path, which arrives with the entity layer
    }
}

//Ignite sets the target on fire for a level based duration
public sealed record Ignite(NacreCraft.Registry.LevelBasedValue Duration) : EnchantmentEntityEffect
{
    public static readonly MapCodec<Ignite> Codec =
        RecordCodecBuilder.Of1<Ignite, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("duration")
                .ForGetter<Ignite, NacreCraft.Registry.LevelBasedValue>(value => value.Duration),
            duration => new Ignite(duration));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs entity fire handling, which arrives with the entity layer
    }
}

//ApplyExhaustion adds exhaustion to a player
public sealed record ApplyExhaustion(NacreCraft.Registry.LevelBasedValue Amount) : EnchantmentEntityEffect
{
    public static readonly MapCodec<ApplyExhaustion> Codec =
        RecordCodecBuilder.Of1<ApplyExhaustion, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("amount")
                .ForGetter<ApplyExhaustion, NacreCraft.Registry.LevelBasedValue>(value => value.Amount),
            amount => new ApplyExhaustion(amount));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the player food path, which arrives with the player layer
    }
}

//PlaySound plays one of the listed sounds at the entity
public sealed record PlaySound(IReadOnlyList<Holder<SoundEvent>> SoundEvents,
    NacreCraft.Game.World.Level.LevelGen.FloatProvider Volume,
    NacreCraft.Game.World.Level.LevelGen.FloatProvider Pitch)
    : EnchantmentEntityEffect
{
    public static readonly MapCodec<PlaySound> Codec =
        RecordCodecBuilder.Of3<PlaySound, IReadOnlyList<Holder<SoundEvent>>,
            NacreCraft.Game.World.Level.LevelGen.FloatProvider,
            NacreCraft.Game.World.Level.LevelGen.FloatProvider>(
            SoundEventListCodec.Instance.FieldOf("sound")
                .ForGetter<PlaySound, IReadOnlyList<Holder<SoundEvent>>>(value => value.SoundEvents),
            NacreCraft.Game.World.Level.LevelGen.FloatProviders.Codec.FieldOf("volume")
                .ForGetter<PlaySound, NacreCraft.Game.World.Level.LevelGen.FloatProvider>(value => value.Volume),
            NacreCraft.Game.World.Level.LevelGen.FloatProviders.Codec.FieldOf("pitch")
                .ForGetter<PlaySound, NacreCraft.Game.World.Level.LevelGen.FloatProvider>(value => value.Pitch),
            (soundEvents, volume, pitch) => new PlaySound(soundEvents, volume, pitch));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the level sound path, which arrives with the level layer
    }
}

//RunFunction runs a datapack function
public sealed record RunFunction(string Function) : EnchantmentEntityEffect
{
    public static readonly MapCodec<RunFunction> Codec =
        RecordCodecBuilder.Of1<RunFunction, string>(
            Codecs.String.FieldOf("function").ForGetter<RunFunction, string>(value => value.Function),
            function => new RunFunction(function));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the function manager, which arrives with the command layer
    }
}

//SummonEntity spawns one of the listed entity types at the target
public sealed record SummonEntity(HolderSet<EntityType<object>> EntityTypes, bool JoinTeam)
    : EnchantmentEntityEffect
{
    public static readonly MapCodec<SummonEntity> Codec =
        RecordCodecBuilder.Of2<SummonEntity, HolderSet<EntityType<object>>, bool>(
            HolderSetCodecs.EntityTypeSet.FieldOf("entity")
                .ForGetter<SummonEntity, HolderSet<EntityType<object>>>(value => value.EntityTypes),
            Codecs.Bool.OptionalFieldOf("join_team", false).ForGetter<SummonEntity, bool>(value => value.JoinTeam),
            (entityTypes, joinTeam) => new SummonEntity(entityTypes, joinTeam));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs entity construction, which arrives with the entity layer
    }
}

//SpawnParticles spawns particles around the target
//The four position and velocity sources are not read yet: their types live in the particle and level layers, and the
//enchantment data only uses the default values so decoding them would add no behaviour
public sealed record SpawnParticles(NacreCraft.Game.World.Particle.ParticleOptions Particle,
    NacreCraft.Game.World.Level.LevelGen.FloatProvider Speed) : EnchantmentEntityEffect
{
    public static readonly MapCodec<SpawnParticles> Codec =
        RecordCodecBuilder.Of2<SpawnParticles, NacreCraft.Game.World.Particle.ParticleOptions,
            NacreCraft.Game.World.Level.LevelGen.FloatProvider>(
            ParticleOptionsCodec.Instance.FieldOf("particle")
                .ForGetter<SpawnParticles, NacreCraft.Game.World.Particle.ParticleOptions>(value => value.Particle),
            NacreCraft.Game.World.Level.LevelGen.FloatProviders.Codec.FieldOf("speed")
                .ForGetter<SpawnParticles, NacreCraft.Game.World.Level.LevelGen.FloatProvider>(value => value.Speed),
            (particle, speed) => new SpawnParticles(particle, speed));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the level particle path, which arrives with the level layer
    }
}

//ParticleOptionsCodec data pack form of a particle, maps to vanilla ParticleTypes.CODEC
//Only parameterless particles are read; the enchantment data never names a particle that carries parameters
public sealed class ParticleOptionsCodec : AbstractMapCodec<NacreCraft.Game.World.Particle.ParticleOptions>
{
    public static readonly ParticleOptionsCodec Instance = new();

    public override DataResult<NacreCraft.Game.World.Particle.ParticleOptions> Decode<U>(DynamicOps<U> ops,
        MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent)
            return DataResult<NacreCraft.Game.World.Particle.ParticleOptions>.Error(
                () => "particle is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<NacreCraft.Game.World.Particle.ParticleOptions>.Error(
                () => "particle type is not a string");
        var id = Identifier.TryParse(typeText.GetOrThrow());
        if (id is null)
            return DataResult<NacreCraft.Game.World.Particle.ParticleOptions>.Error(
                () => $"invalid particle type {typeText.GetOrThrow()}");
        if (BuiltInRegistries.PARTICLE_TYPE.GetValue(id.Value) is
            NacreCraft.Game.World.Particle.SimpleParticleType simple)
            return DataResult<NacreCraft.Game.World.Particle.ParticleOptions>.Success(simple.Options);
        return DataResult<NacreCraft.Game.World.Particle.ParticleOptions>.Error(
            () => $"particle {typeText.GetOrThrow()} carries parameters that are not supported yet");
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops,
        NacreCraft.Game.World.Particle.ParticleOptions value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding particle options is not supported yet");
}

//Vec3Codec double vector codec, json form is three numbers [x, y, z]
internal sealed class Vec3Codec : ScalarCodec<Vec3>
{
    public static readonly Vec3Codec Instance = new();

    public override DataResult<Vec3> Parse<U>(DynamicOps<U> ops, U input)
    {
        var streamResult = ops.GetStream(input);
        if (!streamResult.Result().IsPresent)
            return DataResult<Vec3>.Error(() => "vector must be an [x, y, z] array");
        var components = new List<double>();
        foreach (var element in streamResult.GetOrThrow())
        {
            var numberResult = ops.GetNumberValue(element);
            if (!numberResult.Result().IsPresent)
                return DataResult<Vec3>.Error(() => "vector components must be numbers");
            components.Add(numberResult.GetOrThrow());
        }
        if (components.Count != 3)
            return DataResult<Vec3>.Error(() => $"vector needs 3 components, got {components.Count}");
        return DataResult<Vec3>.Success(new Vec3(components[0], components[1], components[2]));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, Vec3 value)
        => throw new NotSupportedException("encoding vectors is not supported yet");
}

//ApplyImpulse pushes the target along a direction with a level based magnitude
public sealed record ApplyImpulse(Vec3 Direction, Vec3 CoordinateScale, NacreCraft.Registry.LevelBasedValue Magnitude)
    : EnchantmentEntityEffect
{
    public static readonly MapCodec<ApplyImpulse> Codec =
        RecordCodecBuilder.Of3<ApplyImpulse, Vec3, Vec3, NacreCraft.Registry.LevelBasedValue>(
            Vec3Codec.Instance.FieldOf("direction").ForGetter<ApplyImpulse, Vec3>(value => value.Direction),
            Vec3Codec.Instance.FieldOf("coordinate_scale").ForGetter<ApplyImpulse, Vec3>(value => value.CoordinateScale),
            LevelBasedValueCodec.Instance.FieldOf("magnitude")
                .ForGetter<ApplyImpulse, NacreCraft.Registry.LevelBasedValue>(value => value.Magnitude),
            (direction, scale, magnitude) => new ApplyImpulse(direction, scale, magnitude));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs entity motion handling, which arrives with the entity layer
    }
}

//Explode creates an explosion at the target
//Damage type, knockback multiplier, immune blocks, block particles and sound are not read: their codecs need the
//damage, block tag and weighted list layers, and the enchantment data never sets them
public sealed record Explode(bool AttributeToUser, Vec3 Offset, NacreCraft.Registry.LevelBasedValue Radius,
    bool CreateFire, string BlockInteraction, NacreCraft.Game.World.Particle.ParticleOptions SmallParticle,
    NacreCraft.Game.World.Particle.ParticleOptions LargeParticle) : EnchantmentEntityEffect
{
    //BlockInteractionCodec keeps the five vanilla explosion interaction names
    private static readonly Codec<string> BlockInteractionCodec = Codecs.String.ComapFlatMap(
        value => value is "none" or "block" or "mob" or "tnt" or "trigger"
            ? DataResult<string>.Success(value)
            : DataResult<string>.Error(() => $"unknown block interaction {value}"),
        value => value);

    public static readonly MapCodec<Explode> Codec =
        RecordCodecBuilder.Of7<Explode, bool, Vec3, NacreCraft.Registry.LevelBasedValue, bool, string,
            NacreCraft.Game.World.Particle.ParticleOptions, NacreCraft.Game.World.Particle.ParticleOptions>(
            Codecs.Bool.OptionalFieldOf("attribute_to_user", false)
                .ForGetter<Explode, bool>(value => value.AttributeToUser),
            Vec3Codec.Instance.OptionalFieldOf("offset", Vec3.Zero).ForGetter<Explode, Vec3>(value => value.Offset),
            LevelBasedValueCodec.Instance.FieldOf("radius")
                .ForGetter<Explode, NacreCraft.Registry.LevelBasedValue>(value => value.Radius),
            Codecs.Bool.OptionalFieldOf("create_fire", false).ForGetter<Explode, bool>(value => value.CreateFire),
            BlockInteractionCodec.FieldOf("block_interaction")
                .ForGetter<Explode, string>(value => value.BlockInteraction),
            ParticleOptionsCodec.Instance.FieldOf("small_particle")
                .ForGetter<Explode, NacreCraft.Game.World.Particle.ParticleOptions>(value => value.SmallParticle),
            ParticleOptionsCodec.Instance.FieldOf("large_particle")
                .ForGetter<Explode, NacreCraft.Game.World.Particle.ParticleOptions>(value => value.LargeParticle),
            (attributeToUser, offset, radius, createFire, blockInteraction, smallParticle, largeParticle) =>
                new Explode(attributeToUser, offset, radius, createFire, blockInteraction, smallParticle,
                    largeParticle));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs the level explosion path, which arrives with the level layer
    }
}

//ReplaceBlock replaces one block near the target
//trigger_game_event is not read, the game event table is filled but the holder codec needs the event layer
public sealed record ReplaceBlock(Vec3i Offset,
    NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate? Predicate,
    NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider BlockState) : EnchantmentEntityEffect
{
    public static readonly MapCodec<ReplaceBlock> Codec =
        RecordCodecBuilder.Of3<ReplaceBlock, Vec3i,
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?,
            NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider>(
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.Vec3iCodec.Unbounded
                .OptionalFieldOf("offset", Vec3i.Zero).ForGetter<ReplaceBlock, Vec3i>(value => value.Offset),
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate.Codec
                .OptionalFieldOf("predicate", (NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?)null)
                .ForGetter<ReplaceBlock, NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?>(
                    value => value.Predicate),
            NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider.Codec.FieldOf("block_state")
                .ForGetter<ReplaceBlock, NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider>(
                    value => value.BlockState),
            (offset, predicate, blockState) => new ReplaceBlock(offset, predicate, blockState));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs level block writes, which arrive with the level layer
    }
}

//ReplaceDisk replaces a flat disc of blocks around the target
public sealed record ReplaceDisk(NacreCraft.Registry.LevelBasedValue Radius,
    NacreCraft.Registry.LevelBasedValue Height, Vec3i Offset,
    NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate? Predicate,
    NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider BlockState) : EnchantmentEntityEffect
{
    public static readonly MapCodec<ReplaceDisk> Codec =
        RecordCodecBuilder.Of5<ReplaceDisk, NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue,
            Vec3i, NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?,
            NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider>(
            LevelBasedValueCodec.Instance.FieldOf("radius")
                .ForGetter<ReplaceDisk, NacreCraft.Registry.LevelBasedValue>(value => value.Radius),
            LevelBasedValueCodec.Instance.FieldOf("height")
                .ForGetter<ReplaceDisk, NacreCraft.Registry.LevelBasedValue>(value => value.Height),
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.Vec3iCodec.Unbounded
                .OptionalFieldOf("offset", Vec3i.Zero).ForGetter<ReplaceDisk, Vec3i>(value => value.Offset),
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate.Codec
                .OptionalFieldOf("predicate", (NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?)null)
                .ForGetter<ReplaceDisk, NacreCraft.Game.World.Level.LevelGen.BlockPredicates.BlockPredicate?>(
                    value => value.Predicate),
            NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider.Codec.FieldOf("block_state")
                .ForGetter<ReplaceDisk, NacreCraft.Game.World.Level.LevelGen.Features.Impl.BlockStateProvider>(
                    value => value.BlockState),
            (radius, height, offset, predicate, blockState) =>
                new ReplaceDisk(radius, height, offset, predicate, blockState));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs level block writes, which arrive with the level layer
    }
}

//SetBlockProperties rewrites block state properties near the target
//trigger_game_event is not read, the game event table is filled but the holder codec needs the event layer
public sealed record SetBlockProperties(Dictionary<string, string> Properties, Vec3i Offset)
    : EnchantmentEntityEffect
{
    public static readonly MapCodec<SetBlockProperties> Codec =
        RecordCodecBuilder.Of2<SetBlockProperties, Dictionary<string, string>, Vec3i>(
            Codecs.UnboundedMap(Codecs.String, Codecs.String).FieldOf("properties")
                .ForGetter<SetBlockProperties, Dictionary<string, string>>(value => value.Properties),
            NacreCraft.Game.World.Level.LevelGen.BlockPredicates.Vec3iCodec.Unbounded
                .OptionalFieldOf("offset", Vec3i.Zero).ForGetter<SetBlockProperties, Vec3i>(value => value.Offset),
            (properties, offset) => new SetBlockProperties(properties, offset));

    public override void Apply(in EnchantmentEffectContext context)
    {
        //Needs level block writes, which arrive with the level layer
    }
}

//EnchantmentEntityEffectCodec dispatch codec, maps to vanilla EnchantmentEntityEffect.CODEC
//The table stores boxed map codecs, so the value is cast back to the effect codec before decoding
public sealed class EnchantmentEntityEffectCodec : AbstractMapCodec<EnchantmentEntityEffect>
{
    public static readonly EnchantmentEntityEffectCodec Instance = new();

    public override DataResult<EnchantmentEntityEffect> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent)
            return DataResult<EnchantmentEntityEffect>.Error(() => "entity effect is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<EnchantmentEntityEffect>.Error(() => "entity effect type is not a string");
        var id = Identifier.TryParse(typeText.GetOrThrow());
        if (id is null)
            return DataResult<EnchantmentEntityEffect>.Error(
                () => $"invalid entity effect type {typeText.GetOrThrow()}");
        var boxed = BuiltInRegistries.ENCHANTMENT_ENTITY_EFFECT_TYPE.GetValue(id.Value);
        if (boxed is not MapCodec<EnchantmentEntityEffect> codec)
            return DataResult<EnchantmentEntityEffect>.Error(
                () => $"unknown entity effect type {typeText.GetOrThrow()}");
        return codec.Decode(ops, input);
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, EnchantmentEntityEffect value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding entity effects is not supported yet");
}

//SoundEventListCodec accepts a single sound event or a list, maps to vanilla ExtraCodecs.compactListCodec
internal sealed class SoundEventListCodec : ScalarCodec<IReadOnlyList<Holder<SoundEvent>>>
{
    public static readonly SoundEventListCodec Instance = new();

    public override DataResult<IReadOnlyList<Holder<SoundEvent>>> Parse<U>(DynamicOps<U> ops, U input)
    {
        var single = HolderSetCodecs.SoundEventRef.Parse(ops, input);
        if (single.Result().IsPresent)
            return DataResult<IReadOnlyList<Holder<SoundEvent>>>.Success(new[] { single.GetOrThrow() });
        return HolderSetCodecs.SoundEventRef.ListOf().Parse(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, IReadOnlyList<Holder<SoundEvent>> value)
        => DataResult<U>.Error(() => "sound event lists are not encodable");
}

//EnchantmentEntityEffects registers every entity effect, maps to vanilla EnchantmentEntityEffect.bootstrap
public static class EnchantmentEntityEffects
{
    private static bool _registered;

    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        Register("all_of", Box(AllOfEntityEffects.Codec));
        Register("apply_mob_effect", Box(ApplyMobEffect.Codec));
        Register("change_item_damage", Box(ChangeItemDamage.Codec));
        Register("damage_entity", Box(DamageEntity.Codec));
        Register("explode", Box(Explode.Codec));
        Register("ignite", Box(Ignite.Codec));
        Register("apply_impulse", Box(ApplyImpulse.Codec));
        Register("apply_exhaustion", Box(ApplyExhaustion.Codec));
        Register("play_sound", Box(PlaySound.Codec));
        Register("replace_block", Box(ReplaceBlock.Codec));
        Register("replace_disk", Box(ReplaceDisk.Codec));
        Register("run_function", Box(RunFunction.Codec));
        Register("set_block_properties", Box(SetBlockProperties.Codec));
        Register("spawn_particles", Box(SpawnParticles.Codec));
        Register("summon_entity", Box(SummonEntity.Codec));
    }

    //Box widens a concrete map codec to the effect codec type the dispatch path casts back to
    private static MapCodec<EnchantmentEntityEffect> Box<T>(MapCodec<T> codec) where T : EnchantmentEntityEffect
        => codec.Xmap<T, EnchantmentEntityEffect>(value => value, value => (T)value);

    private static void Register(string id, MapCodec<EnchantmentEntityEffect> codec)
        => Registry<object>.Register(BuiltInRegistries.ENCHANTMENT_ENTITY_EFFECT_TYPE,
            ResourceKey<object>.Create(Registries.ENCHANTMENT_ENTITY_EFFECT_TYPE, Identifier.WithDefaultNamespace(id)),
            codec);
}
