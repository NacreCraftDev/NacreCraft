using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Equipment;

//EquipmentAssets built-in equipment asset keys, maps to vanilla net.minecraft.world.item.equipment.EquipmentAssets
//An asset id only names a client model/texture group; the registry is data driven, these keys are the vanilla defaults
public static class EquipmentAssets
{
    //RootId the equipment asset registry key, maps to vanilla ROOT_ID
    public static readonly ResourceKey<Registry<EquipmentAsset>> RootId = Registries.EQUIPMENT_ASSET;

    public static readonly ResourceKey<EquipmentAsset> Leather = CreateId("leather");
    public static readonly ResourceKey<EquipmentAsset> Copper = CreateId("copper");
    public static readonly ResourceKey<EquipmentAsset> Chainmail = CreateId("chainmail");
    public static readonly ResourceKey<EquipmentAsset> Iron = CreateId("iron");
    public static readonly ResourceKey<EquipmentAsset> Gold = CreateId("gold");
    public static readonly ResourceKey<EquipmentAsset> Diamond = CreateId("diamond");
    public static readonly ResourceKey<EquipmentAsset> TurtleScute = CreateId("turtle_scute");
    public static readonly ResourceKey<EquipmentAsset> Netherite = CreateId("netherite");
    public static readonly ResourceKey<EquipmentAsset> ArmadilloScute = CreateId("armadillo_scute");
    public static readonly ResourceKey<EquipmentAsset> Elytra = CreateId("elytra");
    public static readonly ResourceKey<EquipmentAsset> Saddle = CreateId("saddle");
    public static readonly ResourceKey<EquipmentAsset> TraderLlama = CreateId("trader_llama");
    public static readonly ResourceKey<EquipmentAsset> TraderLlamaBaby = CreateId("trader_llama_baby");

    //CreateId builds a default-namespace equipment asset key
    public static ResourceKey<EquipmentAsset> CreateId(string name)
        => ResourceKey<EquipmentAsset>.Create(RootId, Identifier.WithDefaultNamespace(name));
}
