using NacreCraft.Logging;

namespace NacreCraft.Game.Server;

//DedicatedServerSettings owns the server.properties file and its parsed properties, maps to vanilla net.minecraft.server.dedicated.DedicatedServerSettings
//Vanilla keeps the path and the immutable properties snapshot and rewrites the file on every update; NC keeps the mutable ServerSettings
//snapshot, so Update applies the mutation in place and stores, and ForceSave rewrites the current values
public sealed class DedicatedServerSettings
{
    private readonly string _source;
    private DedicatedServerProperties _properties;

    public DedicatedServerSettings(string source)
    {
        _source = source;
        _properties = DedicatedServerProperties.FromFile(source);
    }

    private DedicatedServerSettings(string source, DedicatedServerProperties properties)
    {
        _source = source;
        _properties = properties;
    }

    //Source the server.properties path this owner was created for, maps to vanilla getSource path field
    public string Source => _source;

    //GetProperties the current properties snapshot, maps to vanilla getProperties
    public DedicatedServerProperties GetProperties() => _properties;

    //ForceSave writes the current properties to disk, maps to vanilla forceSave
    public void ForceSave() => _properties.Store(_source);

    //Update mutates the properties, writes them and applies them to the running server, maps to vanilla update(UnaryOperator)
    //The NC version stores after the mutation and returns itself, so callers can chain a reload like the vanilla builder style
    public DedicatedServerSettings Update(Func<DedicatedServerProperties, DedicatedServerProperties> mutator)
    {
        _properties = mutator(_properties);
        _properties.Store(_source);
        return this;
    }

    //Update in-place overload; most NC settings setters mutate and save themselves, this one only writes the file
    public DedicatedServerSettings Update(Action<DedicatedServerProperties> mutator)
    {
        mutator(_properties);
        _properties.Store(_source);
        return this;
    }

    //Create loads server.properties, or generates a default file when it is missing, then fills in keys added by later versions
    //Mirrors ServerSettings.LoadOrGenerate while keeping the dedicated owner, and is the production entry point in ServerMain
    public static DedicatedServerSettings Create(string source)
    {
        var properties = new DedicatedServerProperties();
        if (File.Exists(source))
        {
            properties.Load(source);
            Log.Info(properties.EnsureDefaults(source)
                ? $"Loaded server settings {source} and filled in missing defaults"
                : $"Loaded server settings {source}");
        }
        else
        {
            Log.Info($"server.properties missing, generating default config at {source}");
            properties.SaveDefault(source);
        }
        return new DedicatedServerSettings(source, properties);
    }
}
