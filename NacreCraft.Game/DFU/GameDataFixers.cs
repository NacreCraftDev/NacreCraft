using NacreCraft.DataFixer;
using NacreCraft.Game.DFU.Fixes;
using NacreCraft.Game.DFU.Schemas;
using AddNewChoicesFix = NacreCraft.DataFixer.Fixes.AddNewChoices;
using FireResistantFix = NacreCraft.DataFixer.Fixes.FireResistantToDamageResistantComponentFix;
using References = NacreCraft.DataFixer.Fixes.References;

namespace NacreCraft.Game.DFU;

//GameDataFixers business-layer DFU registrar
//The DFU core only provides the framework and does not register schemas or fixes on its own
//This class registers 13 schemas and 18 concrete fix classes in V1_21 version-segment order to build a usable DataFixer
//Covers the save upgrade chain from 1.20.2 to 1.21.4
public static class GameDataFixers
{
    //The maximum version number in the V1_21 segment corresponds to 1.21.4
    public const int V1_21_VERSION = 4312;

    //BuildV1_21Fixer registers all schemas and fixes in version order and returns a DataFixer instance
    //Schemas are registered in increasing version order; each parent chain links automatically to the previous schema
    //Fixes are registered in increasing version order; each fix uses its corresponding output schema
    //The return type uses the fully qualified name to avoid a name clash with the NacreCraft.DataFixer namespace
    public static NacreCraft.DataFixer.DataFixer BuildV1_21Fixer()
    {
        var builder = new DataFixerBuilder(V1_21_VERSION);

        //Schema registration order: from the v99 foundation to v4312 at the end
        //V1Foundation registers recursive types such as ENTITY/BLOCK_ENTITY
        var v99 = builder.AddSchema(99, 0, (key, parent) => new V1Foundation(key, parent));
        var v2505 = builder.AddSchema(2505, 0, (key, parent) => new V2505(key, parent));
        var v3448 = builder.AddSchema(3448, 0, (key, parent) => new V3448(key, parent));
        var v3685 = builder.AddSchema(3685, 0, (key, parent) => new V3685(key, parent));
        //V3818_3 is vanilla 3818 with subVersion 3
        var v3818_3 = builder.AddSchema(3818, 3, (key, parent) => new V3818_3(key, parent));
        var v3825 = builder.AddSchema(3825, 0, (key, parent) => new V3825(key, parent));
        var v3938 = builder.AddSchema(3938, 0, (key, parent) => new V3938(key, parent));
        var v4054 = builder.AddSchema(4054, 0, Same);
        var v4057 = builder.AddSchema(4057, 0, Same);
        var v4059 = builder.AddSchema(4059, 0, (key, parent) => new V4059(key, parent));
        var v4064 = builder.AddSchema(4064, 0, Same);
        var v4067 = builder.AddSchema(4067, 0, (key, parent) => new V4067(key, parent));
        var v4068 = builder.AddSchema(4068, 0, Same);
        var v4070 = builder.AddSchema(4070, 0, (key, parent) => new NacreCraft.DataFixer.Schemas.V4070(key, parent));
        var v4071 = builder.AddSchema(4071, 0, (key, parent) => new NacreCraft.DataFixer.Schemas.V4071(key, parent));
        var v4081 = builder.AddSchema(4081, 0, Same);
        var v4300 = builder.AddSchema(4300, 0, (key, parent) => new V4300(key, parent));
        var v4306 = builder.AddSchema(4306, 0, (key, parent) => new V4306(key, parent));
        var v4307 = builder.AddSchema(4307, 0, (key, parent) => new V4307(key, parent));
        var v4312 = builder.AddSchema(4312, 0, (key, parent) => new V4312(key, parent));

        //Fix registration in increasing version order; each fix uses its corresponding output schema
        //The renames argument passes an identity function to make the flow work; the actual rename mapping is filled in later by the Game business layer
        builder.AddFixer(new MemoryExpiryDataFix(v2505, "minecraft:villager"));
        builder.AddFixer(new AttributesRenameLegacy(v3818_3, "Attributes rename (legacy)", id => id));
        builder.AddFixer(new DecoratedPotFieldRenameFix(v3448));
        builder.AddFixer(new FixProjectileStoredItem(v3685));
        builder.AddFixer(new RenameEnchantmentsFix(v3818_3, "Rename enchantments", new Dictionary<string, string>()));
        builder.AddFixer(new LodestoneCompassComponentFix(v3825));
        builder.AddFixer(new TrialSpawnerConfigFix(v3825));
        builder.AddFixer(new ProjectileStoredWeaponFix(v3938));
        builder.AddFixer(new AttributeModifierIdFix(v3938));
        builder.AddFixer(new JukeboxTicksSinceSongStartedFix(v3938));
        builder.AddFixer(new CarvingStepRemoveFix(v4057));
        builder.AddFixer(new AttributeIdPrefixFix(v4059));
        builder.AddFixer(new FoodToConsumableFix(v4059));
        builder.AddFixer(new FireResistantFix(v4064));
        builder.AddFixer(new BoatSplitFix(v4067));
        builder.AddFixer(new FeatureFlagRemoveFix(v4067, "Remove Bundle experimental feature flag",
            new HashSet<string> { "minecraft:bundle" }));
        builder.AddFixer(new LockComponentPredicateFix(v4068));
        builder.AddFixer(new ContainerBlockEntityLockPredicateFix(v4068));
        builder.AddFixer(new AddNewChoicesFix(v4070, "Added Pale Oak Boat and Pale Oak Chest Boat", References.Entity));
        builder.AddFixer(new AddNewChoicesFix(v4071, "Added Creaking", References.Entity));
        builder.AddFixer(new AddNewChoicesFix(v4071, "Added Creaking Heart", References.BlockEntity));
        builder.AddFixer(new EntitySalmonSizeFix(v4081));
        builder.AddFixer(new TrialSpawnerConfigInRegistryFix(v4067));
        builder.AddFixer(new EquippableAssetRenameFix(v4300));
        builder.AddFixer(new CustomModelDataExpandFix(v4300));
        builder.AddFixer(new DropChancesFormatFix(v4300));
        builder.AddFixer(new SaddleEquipmentSlotFix(v4300));
        builder.AddFixer(new TooltipDisplayComponentFix(v4307));

        return builder.Build().Fixer();
    }

    //Same schema factory for versions whose types did not change, maps to vanilla SAME/SAME_NAMESPACED
    private static NacreCraft.DataFixer.Schemas.Schema Same(int key, NacreCraft.DataFixer.Schemas.Schema? parent)
        => new(key, parent);
}
