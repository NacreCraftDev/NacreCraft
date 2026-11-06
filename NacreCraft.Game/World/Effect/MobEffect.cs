using NacreCraft.Game.World.Damage;
using NacreCraft.Game.World.Entity;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.EntityAttribute;
using NacreCraft.Storage;
using RegEntity = NacreCraft.Registry.Entity;
using AttributeDef = NacreCraft.Registry.EntityAttribute.Attribute;

namespace NacreCraft.Game.World.Effect;

//MobEffect mob effect base class, maps to vanilla net.minecraft.world.effect.MobEffect
//Implements the Registry-layer placeholder interface NacreCraft.Registry.MobEffect so effects can enter the MOB_EFFECT registry
//The same namespace already has a MobEffect class, so registry element types must be referenced with the fully qualified name
public class MobEffect : NacreCraft.Registry.MobEffect
{
    //AmbientAlpha alpha applied to ambient effect particles, maps to vanilla AMBIENT_ALPHA
    protected const int AmbientAlpha = 38;

    //AttributeTemplate one attribute modifier an effect hands out, maps to vanilla MobEffect.AttributeTemplate
    //The amount is multiplied by amplifier+1 when the modifier is built, which is how stronger levels scale a fixed argument
    public sealed record AttributeTemplate(Identifier Id, double Amount, AttributeOperation Operation)
    {
        //Create builds the modifier for one effect level, maps to vanilla AttributeTemplate.create
        public AttributeModifier Create(int amplifier)
            => new(Id, Amount * (amplifier + 1), Operation);
    }

    //_attributeModifiers modifiers this effect applies while active, keyed by attribute
    //Several levels of the same effect only ever exist once, so one modifier per attribute is enough
    private readonly Dictionary<AttributeDef, AttributeTemplate> _attributeModifiers = new();

    public MobEffect(MobEffectCategory category, int color)
    {
        Category = category;
        Color = color;
    }

    //Category effect category; decides beneficial/harmful/neutral
    public MobEffectCategory Category { get; }

    //Color effect color; the client tints the icon and particles with it
    public int Color { get; }

    //BlendInDurationTicks ticks the effect fades in, maps to vanilla blendInDurationTicks
    public int BlendInDurationTicks { get; private set; }

    //BlendOutDurationTicks ticks the effect fades out, maps to vanilla blendOutDurationTicks
    public int BlendOutDurationTicks { get; private set; }

    //BlendOutAdvanceTicks how early the fade out starts, maps to vanilla blendOutAdvanceTicks
    public int BlendOutAdvanceTicks { get; private set; }

    //NeedsBlend whether the client should fade the effect; set by SetBlendDuration
    public bool NeedsBlend => BlendInDurationTicks > 0 || BlendOutDurationTicks > 0 || BlendOutAdvanceTicks > 0;

    //SoundOnAdded sound played once when the effect lands, maps to vanilla soundOnAdded
    public SoundEvent? SoundOnAdded { get; private set; }

    //AttributeModifiers the modifiers this effect hands out, exposed for tooltips and tests
    public IReadOnlyDictionary<AttributeDef, AttributeTemplate> AttributeModifiers => _attributeModifiers;

    //IsInstantaneous whether it is an instantaneous effect; instantaneous effects have no duration and do not tick
    public virtual bool IsInstantaneous => false;

    //IsBeneficial whether it is a positive effect
    public bool IsBeneficial => Category == MobEffectCategory.Beneficial;

    //IsHarmful whether it is a negative effect
    public bool IsHarmful => Category == MobEffectCategory.Harmful;

    //ApplyEffectTick runs the per-tick effect logic, maps to vanilla applyEffectTick
    //Returning false removes the effect immediately, which is how bad omen and raid omen hand over to the raid flow
    public virtual bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification) => true;

    //ApplyInstantaneousEffect runs the one-shot effect logic, maps to vanilla applyInstantaneousEffect
    //The default falls back to the tick logic, which is what vanilla does
    public virtual void ApplyInstantaneousEffect(ServerLevel level, RegEntity? source, RegEntity? owner,
        LivingEntity mob, int amplification, double scale)
        => ApplyEffectTick(level, mob, amplification);

    //ShouldApplyEffectTickThisTick whether applyEffectTick runs this tick, maps to vanilla shouldApplyEffectTickThisTick
    //tickCount is the remaining duration, or the entity tick count for an infinite effect
    public virtual bool ShouldApplyEffectTickThisTick(int tickCount, int amplification) => false;

    //OnEffectStarted runs after the effect was added or refreshed, maps to vanilla onEffectStarted
    public virtual void OnEffectStarted(LivingEntity mob, int amplifier) { }

    //OnEffectAdded runs once when the effect first lands, maps to vanilla onEffectAdded
    //The sound is played to everyone around the entity, mapped to the level sound call
    public virtual void OnEffectAdded(LivingEntity mob, int amplifier)
    {
        if (SoundOnAdded is { } sound && mob.Level is ServerLevel level)
            level.PlaySound(sound, SoundSource.Neutral, mob.BlockPosition(), 1.0f, 1.0f);
    }

    //OnMobRemoved runs when the affected entity leaves the level, maps to vanilla onMobRemoved
    //Weaving, oozing, infested and wind charged hang their death payload here
    public virtual void OnMobRemoved(ServerLevel level, LivingEntity mob, int amplifier, RemovalReason reason) { }

    //OnMobHurt runs after the affected entity took damage, maps to vanilla onMobHurt
    public virtual void OnMobHurt(ServerLevel level, LivingEntity mob, int amplifier, DamageSource source, float damage) { }

    //AddAttributeModifier records a modifier handed out while the effect is active, maps to vanilla addAttributeModifier
    public MobEffect AddAttributeModifier(AttributeDef attribute, Identifier id, double amount, AttributeOperation operation)
    {
        _attributeModifiers[attribute] = new AttributeTemplate(id, amount, operation);
        return this;
    }

    //AddAttributeModifiers installs every modifier at the given level, maps to vanilla addAttributeModifiers
    //The modifier is replaced by id first so a refresh never trips the duplicate-id guard
    public void AddAttributeModifiers(AttributeMap attributes, int amplifier)
    {
        foreach (var (attribute, template) in _attributeModifiers)
        {
            var instance = attributes.GetInstance(attribute);
            if (instance is null) continue;
            instance.RemoveModifier(template.Id);
            instance.AddTransientModifier(template.Create(amplifier));
        }
    }

    //RemoveAttributeModifiers removes every modifier this effect owns, maps to vanilla removeAttributeModifiers
    public void RemoveAttributeModifiers(AttributeMap attributes)
    {
        foreach (var (attribute, template) in _attributeModifiers)
            attributes.GetInstance(attribute)?.RemoveModifier(template.Id);
    }

    //SetBlendDuration marks the effect as needing blend rendering with the same value for all three phases
    public MobEffect SetBlendDuration(int duration) => SetBlendDuration(duration, duration, duration);

    //SetBlendDuration sets the fade in/out timings, maps to the three argument vanilla setBlendDuration
    public MobEffect SetBlendDuration(int inTicks, int outTicks, int outAdvanceTicks)
    {
        BlendInDurationTicks = inTicks;
        BlendOutDurationTicks = outTicks;
        BlendOutAdvanceTicks = outAdvanceTicks;
        return this;
    }

    //WithSoundOnAdded sets the sound played when the effect lands, maps to vanilla withSoundOnAdded
    public MobEffect WithSoundOnAdded(SoundEvent soundEvent)
    {
        SoundOnAdded = soundEvent;
        return this;
    }
}

//InstantaneousMobEffect instantaneous effect base class, maps to vanilla InstantaneousMobEffect
public class InstantaneousMobEffect : MobEffect
{
    public InstantaneousMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool IsInstantaneous => true;

    //An instantaneous effect ticks as long as any duration remains, maps to vanilla shouldApplyEffectTickThisTick
    public override bool ShouldApplyEffectTickThisTick(int remainingDuration, int amplification)
        => remainingDuration >= 1;
}

//HealOrHarmMobEffect instant heal/harm effect, maps to vanilla HealOrHarmMobEffect
//The harm flag is flipped for undead: healing harms them and harming heals them
public sealed class HealOrHarmMobEffect : InstantaneousMobEffect
{
    //_isHarm whether this is the harming variant, maps to vanilla isHarm
    private readonly bool _isHarm;

    public HealOrHarmMobEffect(MobEffectCategory category, int color, bool isHarm) : base(category, color)
        => _isHarm = isHarm;

    //IsHarm whether this is the harming variant
    public bool IsHarm => _isHarm;

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        if (_isHarm == mob.IsInvertedHealAndHarm)
        {
            mob.Heal(Math.Max(4 << amplification, 0));
            return true;
        }
        mob.HurtServer(level, mob.DamageSources().Magic(), 6 << amplification);
        return true;
    }

    public override void ApplyInstantaneousEffect(ServerLevel level, RegEntity? source, RegEntity? owner,
        LivingEntity mob, int amplification, double scale)
    {
        if (_isHarm == mob.IsInvertedHealAndHarm)
        {
            mob.Heal((int)(scale * (4 << amplification) + 0.5));
            return;
        }
        var amount = (int)(scale * (6 << amplification) + 0.5);
        if (source is null)
            mob.HurtServer(level, mob.DamageSources().Magic(), amount);
        else
            mob.HurtServer(level, mob.DamageSources().IndirectMagic(source, owner), amount);
    }
}
