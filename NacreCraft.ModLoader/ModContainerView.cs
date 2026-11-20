namespace NacreCraft.ModLoader;

//ModContainerView: the read-only mod-facing view over one loaded mod
//Metadata is built once from the manifest, Status reads the live internal state
internal sealed class ModContainerView : NacreCraft.ModLoader.Api.IModContainer
{
    private readonly InternalModInfo _mod;

    public ModContainerView(InternalModInfo mod)
    {
        _mod = mod;
        Metadata = new ModMetadataView(mod);
    }

    public NacreCraft.ModLoader.Api.IModMetadata Metadata { get; }

    public ModStatus Status => _mod.Status;
}

//ModMetadataView: the manifest facts as an immutable snapshot
internal sealed class ModMetadataView : NacreCraft.ModLoader.Api.IModMetadata
{
    private readonly ModManifest _manifest;

    public ModMetadataView(InternalModInfo mod)
    {
        _manifest = mod.Manifest ?? new ModManifest();
        Id = mod.Name;
        Version = _manifest.Version;
        Name = _manifest.EffectiveName;
        Description = _manifest.Description;
        Authors = _manifest.Authors;
        Contributors = _manifest.Contributors;
        License = _manifest.License;
        Contact = _manifest.Contact;
        Environment = _manifest.Environment;
        Depends = _manifest.Depends;
    }

    public string Id { get; }
    public string Version { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<string> Authors { get; }
    public IReadOnlyList<string> Contributors { get; }
    public string License { get; }
    public ModContact Contact { get; }
    public ModEnvironment Environment { get; }
    public IReadOnlyDictionary<string, string> Depends { get; }
}
