namespace NacreCraft.Resources;

//ServerPacksSource the built-in server content, maps to vanilla net.minecraft.server.packs.repository.ServerPacksSource
//The server half of the built-in content lives under data/ in the extracted game root
public sealed class ServerPacksSource : IRepositorySource
{
    //The server pack is optional and inserts at the bottom, maps to vanilla VANILLA_SELECTION_CONFIG
    private static readonly PackSelectionConfig VanillaSelectionConfig = new(false, PackPosition.Bottom, false);

    private readonly string _gameRoot;

    public ServerPacksSource(string gameRoot) => _gameRoot = gameRoot;

    public void LoadPacks(Action<Pack> consumer)
    {
        var metadata = new PackMetadataSection("The default data for Minecraft",
            PackCompatibilityRules.ServerPackFormat.MinorRange());
        var resources = new VanillaPackResources(_gameRoot, metadata);
        var pack = Pack.ReadMetaAndCreate("vanilla", "Minecraft", true, resources,
            PackSource.BuiltIn, PackType.ServerData, VanillaSelectionConfig);
        if (pack is null)
        {
            resources.Dispose();
            return;
        }
        consumer(pack);
    }
}
