namespace NacreCraft.Registry;

//DataComponentInitializers collects per-element component initializers and bakes them onto registry holders
//maps to vanilla net.minecraft.core.component.DataComponentInitializers
//Vanilla walks every registry and fills component-less elements with EMPTY; NC elements already default to Empty at
//construction, so only keys that were initialized are baked here
public sealed class DataComponentInitializers
{
    private readonly List<Entry> _initializers = new();

    //Add registers an initializer for one registry element
    public void Add<T>(ResourceKey<T> key, Initializer<T> initializer) where T : class
        => _initializers.Add(new Entry<T>(key, initializer));

    //Build runs every initializer and returns the pending components per element, ready to be applied
    public IReadOnlyList<IPendingComponents> Build(HolderLookupProvider context)
    {
        var builders = new Dictionary<(Identifier Registry, Identifier Key), (Entry Entry, DataComponentMapBuilder Builder)>();
        foreach (var entry in _initializers)
        {
            var id = (entry.RegistryId, entry.KeyId);
            if (!builders.TryGetValue(id, out var slot))
                builders[id] = slot = (entry, new DataComponentMapBuilder());
            entry.Run(slot.Builder, context);
        }

        var result = new List<IPendingComponents>(builders.Count);
        foreach (var slot in builders.Values)
            result.Add(slot.Entry.Bake(context, slot.Builder));
        return result;
    }

    //Entry untyped initializer entry, so keys of different element types can share one list
    private abstract class Entry
    {
        public abstract Identifier RegistryId { get; }

        public abstract Identifier KeyId { get; }

        public abstract void Run(DataComponentMapBuilder components, HolderLookupProvider context);

        public abstract IPendingComponents Bake(HolderLookupProvider context, DataComponentMapBuilder components);
    }

    //Entry<T> typed initializer entry
    private sealed class Entry<T>(ResourceKey<T> key, Initializer<T> initializer) : Entry where T : class
    {
        public override Identifier RegistryId => key.Registry;

        public override Identifier KeyId => key.Identifier;

        public override void Run(DataComponentMapBuilder components, HolderLookupProvider context)
            => initializer.Run(components, context, key);

        public override IPendingComponents Bake(HolderLookupProvider context, DataComponentMapBuilder components)
        {
            var registry = context.LookupOrThrow(key.RegistryKey());
            var holder = registry.Get(key) as Reference<T>;
            return new PendingComponents<T>(key.RegistryKey(), holder is null
                ? Array.Empty<(Reference<T>, DataComponentMap)>()
                : new[] { (holder, components.Build()) });
        }
    }

    //Initializer one element's component initializer, maps to vanilla DataComponentInitializers.Initializer
    public interface Initializer<T> where T : class
    {
        void Run(DataComponentMapBuilder components, HolderLookupProvider context, ResourceKey<T> key);

        //AndThen runs the two initializers in order
        Initializer<T> AndThen(Initializer<T> other) => new CompositeInitializer<T>(this, other);

        //Add appends a component value
        Initializer<T> Add<C>(DataComponentType<C> type, C value) where C : class
            => AndThen(new SetComponentInitializer<T, C>(type, value));
    }

    private sealed class CompositeInitializer<T>(Initializer<T> first, Initializer<T> second) : Initializer<T>
        where T : class
    {
        public void Run(DataComponentMapBuilder components, HolderLookupProvider context, ResourceKey<T> key)
        {
            first.Run(components, context, key);
            second.Run(components, context, key);
        }
    }

    private sealed class SetComponentInitializer<T, C>(DataComponentType<C> type, C value) : Initializer<T>
        where T : class
        where C : class
    {
        public void Run(DataComponentMapBuilder components, HolderLookupProvider context, ResourceKey<T> key)
            => components.Set(type, value);
    }

    //SingleComponentInitializer an initializer that computes one component value from the registry context
    public interface SingleComponentInitializer<C> where C : class
    {
        C Create(HolderLookupProvider context);

        //AsInitializer adapts the single component value into an element initializer
        Initializer<T> AsInitializer<T>(DataComponentType<C> type) where T : class
            => new SingleComponentInitializerAdapter<T, C>(this, type);
    }

    private sealed class SingleComponentInitializerAdapter<T, C>(
        SingleComponentInitializer<C> initializer, DataComponentType<C> type) : Initializer<T>
        where T : class
        where C : class
    {
        public void Run(DataComponentMapBuilder components, HolderLookupProvider context, ResourceKey<T> key)
            => components.Set(type, initializer.Create(context));
    }

    //IPendingComponents baked components waiting to be bound onto holders
    public interface IPendingComponents
    {
        Identifier RegistryId { get; }

        void Apply();
    }

    //PendingComponents<T> baked components of one element, maps to vanilla DataComponentInitializers.PendingComponents
    public sealed class PendingComponents<T>(
        ResourceKey<Registry<T>> registryKey,
        IReadOnlyList<(Reference<T> Element, DataComponentMap Components)> entries)
        : IPendingComponents where T : class
    {
        public ResourceKey<Registry<T>> RegistryKey { get; } = registryKey;

        public Identifier RegistryId => RegistryKey.Identifier;

        //ForEach reports every element with its baked components
        public void ForEach(Action<Reference<T>, DataComponentMap> output)
        {
            foreach (var (element, components) in entries)
                output(element, components);
        }

        //Apply binds the components onto the registry holders
        public void Apply()
        {
            foreach (var (element, components) in entries)
                element.BindComponents(components);
        }
    }
}
