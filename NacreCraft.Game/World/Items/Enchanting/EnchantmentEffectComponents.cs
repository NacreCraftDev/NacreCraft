using NacreCraft.Codec;
using NacreCraft.DataFixer.Util;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//ChargingSounds the three crossbow loading sounds, maps to vanilla CrossbowItem.ChargingSounds
public sealed record ChargingSounds(Optional<Holder<SoundEvent>> Start, Optional<Holder<SoundEvent>> Mid,
    Optional<Holder<SoundEvent>> End)
{
    public static readonly Codec<ChargingSounds> Codec =
        RecordCodecBuilder.Of3<ChargingSounds, Optional<Holder<SoundEvent>>, Optional<Holder<SoundEvent>>,
            Optional<Holder<SoundEvent>>>(
            HolderSetCodecs.SoundEventRef.OptionalFieldOf("start")
                .ForGetter<ChargingSounds, Optional<Holder<SoundEvent>>>(value => value.Start),
            HolderSetCodecs.SoundEventRef.OptionalFieldOf("mid")
                .ForGetter<ChargingSounds, Optional<Holder<SoundEvent>>>(value => value.Mid),
            HolderSetCodecs.SoundEventRef.OptionalFieldOf("end")
                .ForGetter<ChargingSounds, Optional<Holder<SoundEvent>>>(value => value.End),
            (start, mid, end) => new ChargingSounds(start, mid, end));
}

//EnchantmentEffectComponents the data driven effect table every enchantment carries, maps to vanilla
//EnchantmentEffectComponents
//Components are registered as boxed types like DataComponents, the registry only needs the non-generic form
//None of them sync to the client, so the stream codec stays unset
public static class EnchantmentEffectComponents
{
    //ValueEffectCodec the value effect dispatch codec, shared by every value effect component
    private static readonly Codec<NacreCraft.Registry.EnchantmentValueEffect> ValueEffectCodec =
        EnchantmentValueEffectCodec.Instance;

    //EntityEffectCodec the entity effect dispatch codec
    private static readonly Codec<EnchantmentEntityEffect> EntityEffectCodec = EnchantmentEntityEffectCodec.Instance;

    //LocationEffectCodec the location based effect dispatch codec
    private static readonly Codec<EnchantmentLocationBasedEffect> LocationEffectCodec =
        EnchantmentLocationBasedEffectCodec.Instance;

    //DAMAGE_PROTECTION value effects applied to incoming damage, maps to vanilla DAMAGE_PROTECTION
    public static readonly DataComponentType<object> DAMAGE_PROTECTION =
        Register("damage_protection", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //DAMAGE_IMMUNITY marks immunity to damage, maps to vanilla DAMAGE_IMMUNITY
    public static readonly DataComponentType<object> DAMAGE_IMMUNITY =
        Register("damage_immunity", ConditionalEffect<DamageImmunity>.Create(DamageImmunity.Codec).ListOf());

    //DAMAGE value effects applied to outgoing damage, maps to vanilla DAMAGE
    public static readonly DataComponentType<object> DAMAGE =
        Register("damage", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //SMASH_DAMAGE_PER_FALLEN_BLOCK extra damage per fallen block, maps to vanilla SMASH_DAMAGE_PER_FALLEN_BLOCK
    public static readonly DataComponentType<object> SMASH_DAMAGE_PER_FALLEN_BLOCK =
        Register("smash_damage_per_fallen_block", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //KNOCKBACK value effects applied to knockback, maps to vanilla KNOCKBACK
    public static readonly DataComponentType<object> KNOCKBACK =
        Register("knockback", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //ARMOR_EFFECTIVENESS value effects applied to armor effectiveness, maps to vanilla ARMOR_EFFECTIVENESS
    public static readonly DataComponentType<object> ARMOR_EFFECTIVENESS =
        Register("armor_effectiveness", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //POST_ATTACK entity effects run after an attack lands, maps to vanilla POST_ATTACK
    public static readonly DataComponentType<object> POST_ATTACK =
        Register("post_attack", TargetedConditionalEffect<EnchantmentEntityEffect>
            .Create(EntityEffectCodec).ListOf());

    //POST_PIERCING_ATTACK entity effects run after a piercing projectile hits, maps to vanilla POST_PIERCING_ATTACK
    public static readonly DataComponentType<object> POST_PIERCING_ATTACK =
        Register("post_piercing_attack", ConditionalEffect<EnchantmentEntityEffect>.Create(EntityEffectCodec).ListOf());

    //HIT_BLOCK entity effects run when a block is hit, maps to vanilla HIT_BLOCK
    public static readonly DataComponentType<object> HIT_BLOCK =
        Register("hit_block", ConditionalEffect<EnchantmentEntityEffect>.Create(EntityEffectCodec).ListOf());

    //ITEM_DAMAGE value effects applied to the durability taken, maps to vanilla ITEM_DAMAGE
    public static readonly DataComponentType<object> ITEM_DAMAGE =
        Register("item_damage", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //EQUIPMENT_DROPS value effects applied to the drop chance of the enchanted equipment, maps to vanilla EQUIPMENT_DROPS
    public static readonly DataComponentType<object> EQUIPMENT_DROPS =
        Register("equipment_drops", TargetedConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .EquipmentDropsCodec(ValueEffectCodec).ListOf());

    //LOCATION_CHANGED location based effects run when the enchanted entity moves, maps to vanilla LOCATION_CHANGED
    public static readonly DataComponentType<object> LOCATION_CHANGED =
        Register("location_changed", ConditionalEffect<EnchantmentLocationBasedEffect>
            .Create(LocationEffectCodec).ListOf());

    //TICK entity effects run on the periodic enchantment tick, maps to vanilla TICK
    public static readonly DataComponentType<object> TICK =
        Register("tick", ConditionalEffect<EnchantmentEntityEffect>.Create(EntityEffectCodec).ListOf());

    //AMMO_USE value effects applied to the ammo count used, maps to vanilla AMMO_USE
    public static readonly DataComponentType<object> AMMO_USE =
        Register("ammo_use", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //PROJECTILE_PIERCING value effects applied to projectile piercing, maps to vanilla PROJECTILE_PIERCING
    public static readonly DataComponentType<object> PROJECTILE_PIERCING =
        Register("projectile_piercing", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //PROJECTILE_SPAWNED entity effects run when a projectile is spawned, maps to vanilla PROJECTILE_SPAWNED
    public static readonly DataComponentType<object> PROJECTILE_SPAWNED =
        Register("projectile_spawned", ConditionalEffect<EnchantmentEntityEffect>.Create(EntityEffectCodec).ListOf());

    //PROJECTILE_SPREAD value effects applied to projectile spread, maps to vanilla PROJECTILE_SPREAD
    public static readonly DataComponentType<object> PROJECTILE_SPREAD =
        Register("projectile_spread", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //PROJECTILE_COUNT value effects applied to the projectile count, maps to vanilla PROJECTILE_COUNT
    public static readonly DataComponentType<object> PROJECTILE_COUNT =
        Register("projectile_count", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //TRIDENT_RETURN_ACCELERATION value effects applied to the trident return acceleration, maps to
    //vanilla TRIDENT_RETURN_ACCELERATION
    public static readonly DataComponentType<object> TRIDENT_RETURN_ACCELERATION =
        Register("trident_return_acceleration", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //FISHING_TIME_REDUCTION value effects applied to the fishing wait time, maps to vanilla FISHING_TIME_REDUCTION
    public static readonly DataComponentType<object> FISHING_TIME_REDUCTION =
        Register("fishing_time_reduction", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //FISHING_LUCK_BONUS value effects applied to the fishing luck, maps to vanilla FISHING_LUCK_BONUS
    public static readonly DataComponentType<object> FISHING_LUCK_BONUS =
        Register("fishing_luck_bonus", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //BLOCK_EXPERIENCE value effects applied to the experience dropped by a block, maps to vanilla BLOCK_EXPERIENCE
    public static readonly DataComponentType<object> BLOCK_EXPERIENCE =
        Register("block_experience", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //MOB_EXPERIENCE value effects applied to the experience dropped by a mob, maps to vanilla MOB_EXPERIENCE
    public static readonly DataComponentType<object> MOB_EXPERIENCE =
        Register("mob_experience", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //REPAIR_WITH_XP value effects applied to how much an item repairs per experience point, maps to vanilla REPAIR_WITH_XP
    public static readonly DataComponentType<object> REPAIR_WITH_XP =
        Register("repair_with_xp", ConditionalEffect<NacreCraft.Registry.EnchantmentValueEffect>
            .Create(ValueEffectCodec).ListOf());

    //ATTRIBUTES attribute effects always kept while the item is worn, maps to vanilla ATTRIBUTES
    //This one is a plain list without a condition, unlike the location based attribute effect
    public static readonly DataComponentType<object> ATTRIBUTES =
        Register("attributes", Codecs.AsCodec(EnchantmentAttributeEffect.Codec).ListOf());

    //CROSSBOW_CHARGE_TIME value effect applied to the crossbow charge time, maps to vanilla CROSSBOW_CHARGE_TIME
    public static readonly DataComponentType<object> CROSSBOW_CHARGE_TIME =
        Register("crossbow_charge_time", ValueEffectCodec);

    //CROSSBOW_CHARGING_SOUNDS the loading sounds of the crossbow, maps to vanilla CROSSBOW_CHARGING_SOUNDS
    public static readonly DataComponentType<object> CROSSBOW_CHARGING_SOUNDS =
        Register("crossbow_charging_sounds", ChargingSounds.Codec.ListOf());

    //TRIDENT_SOUND the sound played by the trident, maps to vanilla TRIDENT_SOUND
    public static readonly DataComponentType<object> TRIDENT_SOUND =
        Register("trident_sound", HolderSetCodecs.SoundEventRef.ListOf());

    //PREVENT_EQUIPMENT_DROP keeps the equipment from dropping on death, maps to vanilla PREVENT_EQUIPMENT_DROP
    public static readonly DataComponentType<object> PREVENT_EQUIPMENT_DROP = RegisterUnit("prevent_equipment_drop");

    //PREVENT_ARMOR_CHANGE keeps the armor from being swapped, maps to vanilla PREVENT_ARMOR_CHANGE
    public static readonly DataComponentType<object> PREVENT_ARMOR_CHANGE = RegisterUnit("prevent_armor_change");

    //TRIDENT_SPIN_ATTACK_STRENGTH value effect applied to the trident spin attack strength, maps to
    //vanilla TRIDENT_SPIN_ATTACK_STRENGTH
    public static readonly DataComponentType<object> TRIDENT_SPIN_ATTACK_STRENGTH =
        Register("trident_spin_attack_strength", ValueEffectCodec);

    //COMPONENT_CODEC resolves an effect component type from this table, maps to vanilla COMPONENT_CODEC
    public static readonly Codec<DataComponentType<object>> COMPONENT_CODEC = new EffectComponentTypeCodec();

    //CODEC the whole effects map one enchantment carries, maps to vanilla CODEC
    public static readonly Codec<DataComponentMap> CODEC = new EffectComponentMapCodec();

    //Bootstrap forces the static initializers that register every component type
    public static void Bootstrap() => _ = DAMAGE_PROTECTION;

    //Register boxes a typed codec and registers the component type, the shape DataComponents uses
    private static DataComponentType<object> Register<T>(string name, Codec<T> codec) where T : class
    {
        var type = new SimpleDataComponentType<object>(new ObjectCodec<T>(codec), null!, false);
        Registry<object>.Register(BuiltInRegistries.ENCHANTMENT_EFFECT_COMPONENT_TYPE, name, type);
        return type;
    }

    //RegisterUnit registers a component that only carries presence, maps to vanilla Unit.CODEC
    private static DataComponentType<object> RegisterUnit(string name)
    {
        var type = new SimpleDataComponentType<object>(UnitObjectNbtCodec.Instance, null!, false);
        Registry<object>.Register(BuiltInRegistries.ENCHANTMENT_EFFECT_COMPONENT_TYPE, name, type);
        return type;
    }
}

//EffectComponentTypeCodec resolves an effect component type by registry name, maps to vanilla
//BuiltInRegistries.ENCHANTMENT_EFFECT_COMPONENT_TYPE.byNameCodec
internal sealed class EffectComponentTypeCodec : ScalarCodec<DataComponentType<object>>
{
    public override DataResult<DataComponentType<object>> Parse<U>(DynamicOps<U> ops, U input)
    {
        var text = ops.GetStringValue(input);
        if (!text.Result().IsPresent)
            return DataResult<DataComponentType<object>>.Error(() => "Effect component type must be a string");
        var id = Identifier.TryParse(text.GetOrThrow());
        if (id is null)
            return DataResult<DataComponentType<object>>.Error(
                () => $"Invalid effect component type name {text.GetOrThrow()}");
        return BuiltInRegistries.ENCHANTMENT_EFFECT_COMPONENT_TYPE.GetValue(id.Value) is DataComponentType<object> type
            ? DataResult<DataComponentType<object>>.Success(type)
            : DataResult<DataComponentType<object>>.Error(() => $"Unknown effect component type {id}");
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, DataComponentType<object> value)
    {
        var key = BuiltInRegistries.ENCHANTMENT_EFFECT_COMPONENT_TYPE.GetKey(value);
        return key is null
            ? DataResult<U>.Error(() => "Effect component type not registered, cannot encode")
            : DataResult<U>.Success(ops.CreateString(key.Value.ToString()));
    }
}

//EffectComponentMapCodec reads an effects object into a component map, maps to vanilla DataComponentMap.makeCodec
//Only the decode direction is needed, data packs never write these maps back
internal sealed class EffectComponentMapCodec : ScalarCodec<DataComponentMap>
{
    public override DataResult<DataComponentMap> Parse<U>(DynamicOps<U> ops, U input)
        => ops.GetMapValues(input).FlatMap(entries =>
        {
            var builder = DataComponentMap.Builder();
            foreach (var entry in entries)
            {
                var typeResult = EnchantmentEffectComponents.COMPONENT_CODEC.Parse(ops, entry.First);
                if (!typeResult.Result().IsPresent)
                    return DataResult<DataComponentMap>.Error(
                        () => typeResult.ErrorMessage ?? "Effect component type failed to parse");
                var type = typeResult.GetOrThrow();
                var valueResult = type.CodecOrThrow().Parse(ops, entry.Second);
                if (!valueResult.Result().IsPresent)
                    return DataResult<DataComponentMap>.Error(
                        () => valueResult.ErrorMessage ?? "Effect component value failed to parse");
                builder.SetUnchecked(type, valueResult.GetOrThrow());
            }
            return DataResult<DataComponentMap>.Success(builder.Build());
        });

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, DataComponentMap value)
        => DataResult<U>.Error(() => "Effect component maps are not encodable");
}
