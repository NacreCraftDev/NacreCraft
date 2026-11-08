namespace NacreCraft.Game.World.Food;

//Foods built-in food properties, maps to vanilla net.minecraft.world.food.Foods
//The table feeds the food component of the item registry; values match vanilla 26.2 exactly
public static class Foods
{
    public static readonly FoodProperties Apple = new FoodProperties.Builder().Nutrition(4).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties BakedPotato = new FoodProperties.Builder().Nutrition(5).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties Beef = new FoodProperties.Builder().Nutrition(3).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties Beetroot = new FoodProperties.Builder().Nutrition(1).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties BeetrootSoup = Stew(6).Build();
    public static readonly FoodProperties Bread = new FoodProperties.Builder().Nutrition(5).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties Carrot = new FoodProperties.Builder().Nutrition(3).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties Chicken = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties ChorusFruit = new FoodProperties.Builder().Nutrition(4).SaturationModifier(FoodConstants.FoodSaturationLow).AlwaysEdible().Build();
    public static readonly FoodProperties Cod = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties CookedBeef = new FoodProperties.Builder().Nutrition(8).SaturationModifier(FoodConstants.FoodSaturationGood).Build();
    public static readonly FoodProperties CookedChicken = new FoodProperties.Builder().Nutrition(6).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties CookedCod = new FoodProperties.Builder().Nutrition(5).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties CookedMutton = new FoodProperties.Builder().Nutrition(6).SaturationModifier(FoodConstants.FoodSaturationGood).Build();
    public static readonly FoodProperties CookedPorkchop = new FoodProperties.Builder().Nutrition(8).SaturationModifier(FoodConstants.FoodSaturationGood).Build();
    public static readonly FoodProperties CookedRabbit = new FoodProperties.Builder().Nutrition(5).SaturationModifier(FoodConstants.FoodSaturationNormal).Build();
    public static readonly FoodProperties CookedSalmon = new FoodProperties.Builder().Nutrition(6).SaturationModifier(FoodConstants.FoodSaturationGood).Build();
    public static readonly FoodProperties Cookie = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties DriedKelp = new FoodProperties.Builder().Nutrition(1).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties EnchantedGoldenApple = new FoodProperties.Builder().Nutrition(4).SaturationModifier(FoodConstants.FoodSaturationSupernatural).AlwaysEdible().Build();
    public static readonly FoodProperties GoldenApple = new FoodProperties.Builder().Nutrition(4).SaturationModifier(FoodConstants.FoodSaturationSupernatural).AlwaysEdible().Build();
    public static readonly FoodProperties GoldenCarrot = new FoodProperties.Builder().Nutrition(6).SaturationModifier(FoodConstants.FoodSaturationSupernatural).Build();
    public static readonly FoodProperties HoneyBottle = new FoodProperties.Builder().Nutrition(6).SaturationModifier(FoodConstants.FoodSaturationPoor).AlwaysEdible().Build();
    public static readonly FoodProperties MelonSlice = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties MushroomStew = Stew(6).Build();
    public static readonly FoodProperties Mutton = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties PoisonousPotato = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties Porkchop = new FoodProperties.Builder().Nutrition(3).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties Potato = new FoodProperties.Builder().Nutrition(1).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties Pufferfish = new FoodProperties.Builder().Nutrition(1).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties PumpkinPie = new FoodProperties.Builder().Nutrition(8).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties Rabbit = new FoodProperties.Builder().Nutrition(3).SaturationModifier(FoodConstants.FoodSaturationLow).Build();
    public static readonly FoodProperties RabbitStew = Stew(10).Build();
    public static readonly FoodProperties RottenFlesh = new FoodProperties.Builder().Nutrition(4).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties Salmon = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties SpiderEye = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationGood).Build();
    public static readonly FoodProperties SuspiciousStew = Stew(6).AlwaysEdible().Build();
    public static readonly FoodProperties SweetBerries = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties GlowBerries = new FoodProperties.Builder().Nutrition(2).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();
    public static readonly FoodProperties TropicalFish = new FoodProperties.Builder().Nutrition(1).SaturationModifier(FoodConstants.FoodSaturationPoor).Build();

    //Stew the base builder shared by the stews: normal saturation modifier, maps to vanilla stew helper
    private static FoodProperties.Builder Stew(int nutrition)
        => new FoodProperties.Builder().Nutrition(nutrition).SaturationModifier(FoodConstants.FoodSaturationNormal);
}
