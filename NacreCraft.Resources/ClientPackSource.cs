namespace NacreCraft.Resources;

//ClientPackSource the built-in client packs, maps to vanilla net.minecraft.client.resources.ClientPackSource
//The vanilla pack points at the extracted game root so assets/ and data/ resolve exactly as they did before packs existed
//Vanilla also lists the bundled programmer_art and high_contrast packs; NC does not ship those yet
public sealed class ClientPackSource : BuiltInPackSource
{
    //The vanilla pack is required and inserts at the bottom, so it sits under every user pack, maps to vanilla VANILLA_SELECTION_CONFIG
    private static readonly PackSelectionConfig VanillaSelectionConfig = new(true, PackPosition.Bottom, false);

    public ClientPackSource(string gameRoot)
        : base(PackType.ClientResources, CreateVanillaResources(gameRoot), "resourcepacks") { }

    private static VanillaPackResources CreateVanillaResources(string gameRoot)
        //The built-in metadata covers a game root without a pack.mcmeta, declaring "this major, any minor" like vanilla does
        => new(gameRoot, new PackMetadataSection("The default data for Minecraft",
            PackCompatibilityRules.ClientPackFormat.MinorRange()));

    protected override Pack? CreateVanillaPack(PackResources resources)
        => Pack.ReadMetaAndCreate(VanillaId, "Minecraft", true, resources, PackSource.BuiltIn, ResourceType, VanillaSelectionConfig);

    protected override string GetPackTitle(string id) => id;
}
