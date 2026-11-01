using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Bootstrap;

//ContentBootstrap fills the content registries that the original fills from code rather than from data files
//Each entry is registered idempotently; touching RegisterAll before the registries freeze is enough
public static class ContentBootstrap
{
    //DecoratedPotPatterns id -> asset id, maps to vanilla DecoratedPotPatterns
    private static readonly (string Id, string Asset)[] PotPatterns =
    {
        ("angler", "angler_pottery_pattern"),
        ("archer", "archer_pottery_pattern"),
        ("arms_up", "arms_up_pottery_pattern"),
        ("blade", "blade_pottery_pattern"),
        ("brewer", "brewer_pottery_pattern"),
        ("burn", "burn_pottery_pattern"),
        ("danger", "danger_pottery_pattern"),
        ("explorer", "explorer_pottery_pattern"),
        ("flow", "flow_pottery_pattern"),
        ("friend", "friend_pottery_pattern"),
        ("guster", "guster_pottery_pattern"),
        ("heart", "heart_pottery_pattern"),
        ("heartbreak", "heartbreak_pottery_pattern"),
        ("howl", "howl_pottery_pattern"),
        ("miner", "miner_pottery_pattern"),
        ("mourner", "mourner_pottery_pattern"),
        ("plenty", "plenty_pottery_pattern"),
        ("prize", "prize_pottery_pattern"),
        ("scrape", "scrape_pottery_pattern"),
        ("sheaf", "sheaf_pottery_pattern"),
        ("shelter", "shelter_pottery_pattern"),
        ("skull", "skull_pottery_pattern"),
        ("snort", "snort_pottery_pattern"),
        ("blank", "decorated_pot_side"),
    };

    //VillagerBiomes villager type names, maps to vanilla VillagerType.register
    private static readonly string[] VillagerBiomes =
    {
        "desert", "jungle", "plains", "savanna", "snow", "swamp", "taiga",
    };

    //VillagerJobs villager profession names, maps to vanilla VillagerProfession bootstrap
    private static readonly string[] VillagerJobs =
    {
        "armorer", "butcher", "cartographer", "cleric", "farmer", "fisherman", "fletcher",
        "leatherworker", "librarian", "mason", "nitwit", "none", "shepherd", "toolsmith", "weaponsmith",
    };

    private static bool _registered;

    //RegisterAll registers every code driven content entry before the registries freeze
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;

        foreach (var (id, asset) in PotPatterns)
            Registry<DecoratedPotPattern>.Register(BuiltInRegistries.DECORATED_POT_PATTERN,
                ResourceKey<DecoratedPotPattern>.Create(Registries.DECORATED_POT_PATTERN,
                    Identifier.WithDefaultNamespace(id)),
                new DecoratedPotPattern(Identifier.WithDefaultNamespace(asset)));

        foreach (var name in VillagerBiomes)
            Registry<VillagerType>.Register(BuiltInRegistries.VILLAGER_TYPE,
                ResourceKey<VillagerType>.Create(Registries.VILLAGER_TYPE, Identifier.WithDefaultNamespace(name)),
                new VillagerType(Identifier.WithDefaultNamespace($"village_type/{name}")));

        foreach (var name in VillagerJobs)
            Registry<VillagerProfession>.Register(BuiltInRegistries.VILLAGER_PROFESSION,
                ResourceKey<VillagerProfession>.Create(Registries.VILLAGER_PROFESSION,
                    Identifier.WithDefaultNamespace(name)),
                new VillagerProfession(Identifier.WithDefaultNamespace($"point_of_interest_type/{name}"),
                    Identifier.WithDefaultNamespace($"entity.villager.work_{name}")));

        //Sound events and custom stats are two long name tables in the original; the names live in the generated list
        foreach (var name in ContentTables.SoundEventNames)
        {
            var id = Identifier.WithDefaultNamespace(name);
            Registry<SoundEvent>.Register(BuiltInRegistries.SOUND_EVENT,
                ResourceKey<SoundEvent>.Create(Registries.SOUND_EVENT, id), new SoundEvent(id));
        }

        foreach (var name in ContentTables.CustomStatNames)
        {
            var id = Identifier.WithDefaultNamespace(name);
            Registry<object>.Register(BuiltInRegistries.CUSTOM_STAT,
                ResourceKey<object>.Create(Registries.CUSTOM_STAT, id), id);
        }
    }
}
