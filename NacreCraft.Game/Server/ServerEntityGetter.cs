namespace NacreCraft.Game.Server;

//ServerEntityGetter entity lookup by id, maps to vanilla net.minecraft.server.level.ServerEntityGetter
//Implemented by the level so packet handlers can resolve an entity reference from the wire id
public interface ServerEntityGetter
{
    NacreCraft.Registry.Entity? GetEntity(int entityId);
}
