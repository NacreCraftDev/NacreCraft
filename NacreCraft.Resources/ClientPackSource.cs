namespace NacreCraft.Resources;

//ClientPackSource the built-in client packs, maps to vanilla net.minecraft.client.resources.ClientPackSource
//The vanilla pack points at the extracted game root so assets/ and data/ resolve exactly as they did before packs existed
//Vanilla also lists the bundled programmer_art and high_contrast packs; NC does not ship those yet
public sealed class ClientPackSource : IRepositorySource
{
    //The vanilla pack is required and inserts at the bottom, so it sits under every user pack, maps to vanilla VANILLA_SELECTION_CONFIG
    private static readonly PackSelectionConfig VanillaSelectionConfig = new(true, PackPosition.Bottom, false);

    private readonly string _gameRoot;

    public ClientPackSource(string gameRoot) => _gameRoot = gameRoot;

    public void LoadPacks(Action<Pack> consumer)
    {
        //The built-in metadata covers a game root without a pack.mcmeta, declaring "this major, any minor" like vanilla does
        var metadata = new PackMetadataSection("The default data for Minecraft",
            PackCompatibilityRules.ClientPackFormat.MinorRange());
        var resources = new VanillaPackResources(_gameRoot, metadata);
        var pack = Pack.ReadMetaAndCreate("vanilla", "Minecraft", true, resources,
            PackSource.BuiltIn, PackType.ClientResources, VanillaSelectionConfig);
        if (pack is null)
        {
            resources.Dispose();
            return;
        }
        consumer(pack);
    }
}
