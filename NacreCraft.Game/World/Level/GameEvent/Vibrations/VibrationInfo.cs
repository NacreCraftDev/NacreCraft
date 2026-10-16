using NacreCraft.Game.World.Entity;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.GameEvents.Vibrations;

//VibrationInfo one vibration candidate, maps to vanilla net.minecraft.world.level.gameevent.vibrations.VibrationInfo
//Carries the event, how far it travelled, where it came from and who produced it
//The codec is omitted for now: it would need a GameEvent codec, which does not exist yet, and nothing persists this data
public sealed record VibrationInfo(
    GameEvent GameEvent, float Distance, Vec3 Pos, Guid? Uuid, Guid? ProjectileOwnerUuid,
    NacreCraft.Registry.Entity? Entity)
{
    public VibrationInfo(GameEvent gameEvent, float distance, Vec3 pos, Guid? uuid, Guid? projectileOwnerUuid)
        : this(gameEvent, distance, pos, uuid, projectileOwnerUuid, null)
    {
    }

    //VibrationInfo built from a live entity, maps to the vanilla entity constructor
    //A projectile also records its shooter, which is what a sculk sensor reports as the culprit
    public VibrationInfo(GameEvent gameEvent, float distance, Vec3 pos, NacreCraft.Registry.Entity? entity)
        : this(gameEvent, distance, pos, entity?.Uuid, GetProjectileOwner(entity), entity)
    {
    }

    private static Guid? GetProjectileOwner(NacreCraft.Registry.Entity? entity)
        => entity is Projectile projectile ? projectile.OwnerUuid : null;

    //GetEntity resolves the source entity, maps to vanilla getEntity
    //The live entity wins; otherwise the uuid is looked up in the level
    public NacreCraft.Registry.Entity? GetEntity(ServerLevel level)
        => Entity ?? (Uuid is { } uuid ? level.GetEntity(uuid) : null);

    //GetProjectileOwnerEntity resolves the shooter when the source was a projectile, maps to vanilla getProjectileOwner
    public NacreCraft.Registry.Entity? GetProjectileOwnerEntity(ServerLevel level)
    {
        if (GetEntity(level) is Projectile projectile && projectile.OwnerUuid is { } ownerUuid)
            return level.GetEntity(ownerUuid);
        return ProjectileOwnerUuid is { } uuid ? level.GetEntity(uuid) : null;
    }
}
