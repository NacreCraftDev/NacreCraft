using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items.ConsumeEffects;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component;

//Consumables built-in consumable presets, maps to vanilla net.minecraft.world.item.component.Consumables
//Food/drink presets use the registry sound values; the ominous bottle dispose sound is not a separate post-consume slot here
public static class Consumables
{
    public static readonly Consumable DefaultFood = DefaultFoodBuilder().Build();
    public static readonly Consumable DefaultDrink = DefaultDrinkBuilder().Build();

    public static readonly Consumable HoneyBottle = DefaultDrinkBuilder()
        .ConsumeSeconds(2.0f)
        .Sound(SoundEvents.HoneyDrink)
        .OnConsume(new RemoveStatusEffectsConsumeEffect(
            new DirectHolderSet<NacreCraft.Registry.MobEffect>(new[] { MobEffects.POISON })))
        .Build();

    public static readonly Consumable OminousBottle = DefaultDrinkBuilder().Build();
    public static readonly Consumable DriedKelp = DefaultFoodBuilder().ConsumeSeconds(0.8f).Build();

    public static readonly Consumable Chicken = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance(MobEffects.HUNGER, 600, 0, false, true), 0.3f))
        .Build();

    public static readonly Consumable EnchantedGoldenApple = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance[]
        {
            new(MobEffects.REGENERATION, 600, 1, false, true),
            new(MobEffects.RESISTANCE, 6000, 0, false, true),
            new(MobEffects.FIRE_RESISTANCE, 6000, 0, false, true),
            new(MobEffects.ABSORPTION, 2400, 3, false, true),
        }))
        .Build();

    public static readonly Consumable GoldenApple = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance[]
        {
            new(MobEffects.REGENERATION, 100, 1, false, true),
            new(MobEffects.ABSORPTION, 2400, 0, false, true),
        }))
        .Build();

    public static readonly Consumable PoisonousPotato = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance(MobEffects.POISON, 100, 0, false, true), 0.6f))
        .Build();

    public static readonly Consumable Pufferfish = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance[]
        {
            new(MobEffects.POISON, 1200, 1, false, true),
            new(MobEffects.HUNGER, 300, 2, false, true),
            new(MobEffects.NAUSEA, 300, 0, false, true),
        }))
        .Build();

    public static readonly Consumable RottenFlesh = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance(MobEffects.HUNGER, 600, 0, false, true), 0.8f))
        .Build();

    public static readonly Consumable SpiderEye = DefaultFoodBuilder()
        .OnConsume(new ApplyStatusEffectsConsumeEffect(new MobEffectInstance(MobEffects.POISON, 100, 0, false, true)))
        .Build();

    public static readonly Consumable MilkBucket = DefaultDrinkBuilder()
        .OnConsume(ClearAllStatusEffectsConsumeEffect.Instance)
        .Build();

    public static readonly Consumable ChorusFruit = DefaultFoodBuilder()
        .OnConsume(new TeleportRandomlyConsumeEffect())
        .Build();

    public static Consumable.Builder DefaultFoodBuilder() => Consumable.CreateBuilder()
        .ConsumeSeconds(1.6f).Animation(ItemUseAnimation.Eat).Sound(SoundEvents.GenericEat).HasConsumeParticles(true);

    public static Consumable.Builder DefaultDrinkBuilder() => Consumable.CreateBuilder()
        .ConsumeSeconds(1.6f).Animation(ItemUseAnimation.Drink).Sound(SoundEvents.GenericDrink).HasConsumeParticles(false);
}
