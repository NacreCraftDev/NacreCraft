using NacreCraft.Primitives;

namespace NacreCraft.Registry;

//Content types that used to be empty stubs and now carry the fields the data pack and the bootstrap need
//Description fields keep the raw json text of the component: the component type lives in the network layer, which this
//layer cannot reference, so the game layer decodes it when it reads the table

//DecoratedPotPattern decorated pot pattern, a single asset id, maps to the data driven shard pattern table
public sealed record DecoratedPotPattern(Identifier AssetId);

//JukeboxSong jukebox song, maps to net.minecraft.world.item.JukeboxSong
//The sound event is kept by registry id instead of a resolved reference: the sound event table is filled by code and a
//song must still decode when the entry it names is not registered yet
public sealed record JukeboxSong(Identifier SoundEventId, string Description, float LengthInSeconds, int ComparatorOutput);

//TrimMaterial armour trim material, maps to net.minecraft.world.item.equipment.trim.TrimMaterial
public sealed record TrimMaterial(string AssetName, string Description,
    IReadOnlyDictionary<string, string> OverrideArmorAssets);

//TrimPattern armour trim pattern, maps to net.minecraft.world.item.equipment.trim.TrimPattern
public sealed record TrimPattern(Identifier AssetId, bool Decal, string Description);

//VillagerType villager biome type, maps to net.minecraft.world.entity.npc.VillagerType
public sealed record VillagerType(Identifier BiomeTag);

//VillagerProfession villager profession, maps to net.minecraft.world.entity.npc.VillagerProfession
public sealed record VillagerProfession(Identifier PointOfInterest, Identifier WorkSound);
