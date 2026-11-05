using NacreCraft.Primitives;

namespace NacreCraft.Game.Server;

//ServerEntity the server-side tracking state of one entity, maps to vanilla net.minecraft.server.level.ServerEntity
//Decides when position, rotation and metadata changes are sent to the clients tracking the entity
//The packet pipeline is not wired here, so the class only holds the tracked state and the change detection thresholds
public sealed class ServerEntity(NacreCraft.Registry.Entity entity, int updateInterval)
{
    public NacreCraft.Registry.Entity Entity { get; } = entity;

    //UpdateInterval ticks between position updates, 20 for most entities
    public int UpdateInterval { get; } = Math.Max(1, updateInterval);

    public Vec3 LastPos { get; private set; } = entity.Pos;

    public int TicksSinceLastUpdate { get; private set; }

    //Tick counts the update interval, maps to vanilla ServerEntity.tick
    public void Tick() => TicksSinceLastUpdate++;

    //ShouldSendPosition whether enough ticks passed and the entity moved, maps to vanilla the position branch
    public bool ShouldSendPosition()
    {
        if (TicksSinceLastUpdate < UpdateInterval) return false;
        TicksSinceLastUpdate = 0;
        var moved = Entity.Pos.DistanceToSqr(LastPos) > 0.0001;
        LastPos = Entity.Pos;
        return moved;
    }
}
