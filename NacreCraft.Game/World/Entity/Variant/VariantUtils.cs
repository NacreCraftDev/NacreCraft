using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity.Variant;

//VariantUtils shared helpers for variant registries, maps to vanilla net.minecraft.world.entity.variant.VariantUtils
public static class VariantUtils
{
    public const string TAG_VARIANT = "variant";

    //GetDefaultOrAny the holder for the key, falling back to any element of the registry, maps to vanilla getDefaultOrAny
    public static Holder<T> GetDefaultOrAny<T>(RegistryAccess registryAccess, ResourceKey<T> id) where T : class
    {
        var registry = registryAccess.LookupOrThrow(id.RegistryKey());
        if (registry.GetValue(id) is { } value) return registry.WrapAsHolder(value);
        return registry.GetAny() ?? throw new InvalidOperationException($"Registry {registry.Key} is empty");
    }

    //GetAny an arbitrary element holder, maps to vanilla getAny
    public static Holder<T> GetAny<T>(RegistryAccess registryAccess, ResourceKey<Registry<T>> registryId) where T : class
        => registryAccess.LookupOrThrow(registryId).GetAny()
            ?? throw new InvalidOperationException($"Registry {registryId} is empty");

    //WriteVariant writes the variant key, maps to vanilla writeVariant
    public static void WriteVariant<T>(ValueOutput output, Holder<T> holder) where T : class
    {
        if (holder.UnwrapKey() is { } key)
            output.Store(TAG_VARIANT, IdentifierCodec.Instance, key.Identifier);
    }

    //ReadVariant reads the variant holder back, maps to vanilla readVariant
    public static Optional<Holder<T>> ReadVariant<T>(ValueInput input, ResourceKey<Registry<T>> registryId) where T : class
    {
        var text = input.GetString(TAG_VARIANT);
        if (text is null || Identifier.TryParse(text) is not { } id) return Optional<Holder<T>>.Empty();
        var registry = input.Lookup().Lookup(registryId);
        return registry?.Get(id) is { } holder ? Optional<Holder<T>>.Of(holder) : Optional<Holder<T>>.Empty();
    }

    //SelectVariantToSpawn picks a variant whose selectors match the context, maps to vanilla selectVariantToSpawn
    public static Optional<Holder<T>> SelectVariantToSpawn<T>(SpawnContext context, ResourceKey<Registry<T>> variantRegistry)
        where T : class, PriorityProvider<SpawnContext, SpawnCondition>
    {
        var registry = context.Level.GetRegistryAccess().LookupOrThrow(variantRegistry);
        var entries = new List<Holder<T>>();
        foreach (var id in registry.KeySet)
            if (registry.Get(id) is { } holder) entries.Add(holder);
        return PriorityProviders.Pick(entries, holder => holder.Value, context.Level.GetLevel().Random, context);
    }
}
