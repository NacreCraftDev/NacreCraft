using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Storage.Updates;

//IEntityInsideBehaviour, a block's response to an entity entering, maps to vanilla BlockBehaviour.entityInside
//Dispatched from the entity's own movement, once per move, to each block its bounding box covers
public interface IEntityInsideBehaviour
{
    //OnEntityInside, an entity entered this block; it may be called more than once for the same entity in one tick if it moved more than once
    //entity is null only on the player path: the game layer injects player boxes and a player is not an entity in this codebase yet,
    //so that call has no entity to hand over
    void OnEntityInside(ServerLevel level, BlockPos pos, BlockState state, NacreCraft.Registry.Entity? entity);
}
