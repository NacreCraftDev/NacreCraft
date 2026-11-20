using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Equipment;

//AllowedEntitiesProvider resolves the entity types a piece of equipment may be worn by, maps to vanilla AllowedEntitiesProvider
//Vanilla takes a HolderGetter; NC passes the entity type registry directly, which already answers holder lookups
public interface AllowedEntitiesProvider
{
    HolderSet<EntityType<object>> Get(Registry<EntityType<object>> entityRegistry);
}
