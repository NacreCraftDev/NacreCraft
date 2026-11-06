using NacreCraft.Registry;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using GameAlias = NacreCraft.Game.World.Level.LevelGen.Structure.PoolAliasBinding;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using GameEmpty = NacreCraft.Game.World.Level.LevelGen.Structure.EmptyPoolElement;
using StructurePoolElementEntry = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElementEntry;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//Pools structure pool data entry point, maps to vanilla net.minecraft.data.worldgen.Pools
//Registers the empty pool and then every family in vanilla order; the helper applies the pool projection to each element because
//the NC pool element carries its own projection while the vanilla two-argument constructor applies it from the pool
public static class Pools
{
    //EMPTY the empty pool every family uses as the default fallback, maps to vanilla Pools.EMPTY
    public static readonly ResourceKey<RegistryPool> EMPTY = CreateKey("empty");

    //CreateKey builds a template pool key from a name in the default namespace, maps to vanilla createKey(String)
    public static ResourceKey<RegistryPool> CreateKey(string name)
        => CreateKey(Identifier.WithDefaultNamespace(name));

    //CreateKey from an identifier, maps to vanilla createKey(Identifier)
    public static ResourceKey<RegistryPool> CreateKey(Identifier location)
        => ResourceKey<RegistryPool>.Create(Registries.TEMPLATE_POOL, location);

    //Register registers one named pool, maps to vanilla Pools.register
    public static void Register(BootstrapContext<RegistryPool> context, string name, GamePool pool)
        => context.Register(CreateKey(name), pool);

    //Entries builds the weighted element list, setting the pool projection on every element
    //maps to the vanilla StructureTemplatePool(fallback, List<Pair<Function<Projection,Element>,Integer>>, Projection) constructor
    public static IReadOnlyList<StructurePoolElementEntry> Entries(GamePool.Projection projection,
        params object[] entries)
    {
        var result = new List<StructurePoolElementEntry>();
        foreach (var entry in entries) AddEntry(result, entry, projection);
        return result;
    }

    private static void AddEntry(List<StructurePoolElementEntry> result, object entry, GamePool.Projection projection)
    {
        if (entry is System.Runtime.CompilerServices.ITuple { Length: 2 } tuple
            && tuple[0] is GameElement element && tuple[1] is int weight)
        {
            //The empty element is a shared singleton whose projection is meaningless; do not disturb it
            if (element is not GameEmpty) element.SetProjection(projection);
            result.Add(new StructurePoolElementEntry(element, weight));
            return;
        }
        if (entry is System.Collections.IEnumerable sequence)
        {
            foreach (var item in sequence) AddEntry(result, item!, projection);
            return;
        }
        throw new ArgumentException($"Unsupported structure pool entry: {entry}");
    }

    //RegisterTargetsAsPools registers one empty placeholder pool per alias target, maps to vanilla registerTargetsAsPools
    //The alias targets are spawned by jigsaw placement rather than by a real template pool, so the empty pool is what the alias resolves to
    public static void RegisterTargetsAsPools(BootstrapContext<RegistryPool> context,
        Holder<RegistryPool> emptyPool, IReadOnlyList<GameAlias> aliasBindings)
    {
        foreach (var binding in aliasBindings)
        {
            foreach (var target in binding.AllTargets())
            {
                var path = target.Path;
                Register(context, path, new GamePool(emptyPool,
                    Entries(GamePool.Projection.Rigid, (GameElement.Single(path, GamePool.Projection.Rigid), 1))));
            }
        }
    }

    //Bootstrap registers the empty pool and every pool family, maps to vanilla Pools.bootstrap
    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(EMPTY);
        context.Register(EMPTY, new GamePool(empty, Entries(GamePool.Projection.Rigid)));
        BastionPieces.Bootstrap(context);
        PillagerOutpostPools.Bootstrap(context);
        VillagePools.Bootstrap(context);
        AncientCityStructurePieces.Bootstrap(context);
        TrailRuinsStructurePools.Bootstrap(context);
        TrialChambersStructurePools.Bootstrap(context);
    }
}
