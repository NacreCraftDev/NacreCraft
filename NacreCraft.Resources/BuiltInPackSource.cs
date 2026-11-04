namespace NacreCraft.Resources;

//BuiltInPackSource the content shipped with the game, maps to vanilla net.minecraft.server.packs.repository.BuiltInPackSource
//Loads the vanilla pack first and then every bundled pack; the vanilla pack always sits at the bottom of the stack
public abstract class BuiltInPackSource : IRepositorySource
{
    public const string VanillaId = "vanilla";
    public const string TestsId = "tests";

    private readonly PackType _packType;

    protected BuiltInPackSource(PackType packType, VanillaPackResources vanillaPack, string packDir)
    {
        _packType = packType;
        VanillaPack = vanillaPack;
        PackDir = packDir;
    }

    //ResourceType the resource type this source provides
    public PackType ResourceType => _packType;

    //VanillaPack the built-in content accessor
    public VanillaPackResources VanillaPack { get; }

    //PackDir the directory inside the built-in pack that holds bundled packs, maps to vanilla packDir
    public string PackDir { get; }

    public void LoadPacks(Action<Pack> consumer)
    {
        var vanilla = CreateVanillaPack(VanillaPack);
        if (vanilla is not null) consumer(vanilla);
        ListBundledPacks(consumer);
    }

    protected abstract Pack? CreateVanillaPack(PackResources resources);

    protected abstract string GetPackTitle(string id);

    //ListBundledPacks adds the packs shipped next to the vanilla one, maps to vanilla listBundledPacks
    private void ListBundledPacks(Action<Pack> consumer)
    {
        var discovered = new Dictionary<string, Func<string, Pack?>>();
        PopulatePackList((id, factory) => discovered[id] = factory);
        foreach (var (id, factory) in discovered)
        {
            var pack = factory(id);
            if (pack is not null) consumer(pack);
        }
    }

    //PopulatePackList registers the bundled packs; vanilla lists raw paths inside the built-in pack
    //NC's VanillaPackResources reads an extracted game root and has no raw path listing yet, so nothing is bundled
    protected virtual void PopulatePackList(Action<string, Func<string, Pack?>> discoveredPacks) { }
}
