namespace NacreCraft.Resources;

//ServerPacksSource the built-in server content, maps to vanilla net.minecraft.server.packs.repository.ServerPacksSource
//The server half of the built-in content lives under data/ in the extracted game root
public sealed class ServerPacksSource : BuiltInPackSource
{
    //The server pack is optional and inserts at the bottom, maps to vanilla VANILLA_SELECTION_CONFIG
    private static readonly PackSelectionConfig VanillaSelectionConfig = new(false, PackPosition.Bottom, false);

    public ServerPacksSource(string gameRoot)
        : base(PackType.ServerData, CreateVanillaResources(gameRoot), "datapacks") { }

    private static VanillaPackResources CreateVanillaResources(string gameRoot)
        => new(gameRoot, new PackMetadataSection("The default data for Minecraft",
            PackCompatibilityRules.ServerPackFormat.MinorRange()));

    protected override Pack? CreateVanillaPack(PackResources resources)
        => Pack.ReadMetaAndCreate(VanillaId, "Minecraft", true, resources, PackSource.BuiltIn, ResourceType, VanillaSelectionConfig);

    protected override string GetPackTitle(string id) => id;
}
