using NacreCraft.Registry;

namespace NacreCraft.Game.Data.WorldGen;

//BootstrapContext registration view for one datapack registry, maps to vanilla net.minecraft.data.worldgen.BootstrapContext
//Every built-in data class (carvers, biomes, features, placements, structures) registers through this so the same code can drive a
//datapack registry or the built-in registries
public interface BootstrapContext<T> where T : class
{
    //Register adds one element and returns its holder, maps to vanilla register
    Reference<T> Register(ResourceKey<T> key, T value, Lifecycle lifecycle);

    //Lookup cross-registry lookup used to resolve holders a data element depends on, maps to vanilla lookup
    //Vanilla answers a HolderGetter; a NC Registry is one and also offers GetOrCreate for tags
    Registry<S>? Lookup<S>(ResourceKey<Registry<S>> registryKey) where S : class;

    //Register with the stable lifecycle, maps to the vanilla default method
    Reference<T> Register(ResourceKey<T> key, T value) => Register(key, value, Lifecycle.Stable);

    //LookupOrThrow throws when the registry is missing, maps to vanilla lookupOrThrow
    Registry<S> LookupOrThrow<S>(ResourceKey<Registry<S>> registryKey) where S : class
        => Lookup(registryKey) ?? throw new InvalidOperationException($"Missing registry: {registryKey}");
}

//BuiltInBootstrapContext bootstrap context backed by one of the built-in registries, maps to the vanilla context used by Bootstrap
//An element that a data pack already loaded is left in place so data-driven values keep winning over the built-in fallback
public sealed class BuiltInBootstrapContext<T> : BootstrapContext<T> where T : class
{    private readonly WritableRegistry<T> _registry;
    private readonly RegistryAccess _access;

    public BuiltInBootstrapContext(Registry<T> registry)
    {
        _registry = registry as WritableRegistry<T>
                    ?? throw new ArgumentException($"Registry is not writable: {registry.Key}", nameof(registry));
        //The registry access is a reflection snapshot of every built-in registry, which is what cross-registry lookups read
        _access = BuiltInRegistries.CreateRegistryAccess();
    }

    public Reference<T> Register(ResourceKey<T> key, T value, Lifecycle lifecycle)
    {
        var existing = _registry.Get(key.Identifier);
        if (existing is not null) return existing;
        return _registry.Register(key, value, new RegistrationInfo(null, lifecycle));
    }

    public Registry<S>? Lookup<S>(ResourceKey<Registry<S>> registryKey) where S : class
        => _access.Lookup<S>(registryKey);
}

//BootstrapLookups holder lookup helpers shared by the data classes, maps to the getOrThrow calls in vanilla bootstrap code
public static class BootstrapLookups
{
    //GetOrThrow looks up a registered element, creating an unbound placeholder when it is not registered yet
    //Forward references are normal during bootstrap: vegetation features point at placed trees that register later, and the later
    //Register call binds the value into this same holder
    public static Holder<T> GetOrThrow<T>(this HolderLookup<T> lookup, ResourceKey<T> key) where T : class
        => lookup is Registry<T> registry
            ? registry.GetOrCreateHolder(key.Identifier)
            : lookup.Get(key) ?? throw new InvalidOperationException($"Missing element: {key.Identifier}");
}
