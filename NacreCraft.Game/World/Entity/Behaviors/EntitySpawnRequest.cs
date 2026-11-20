namespace NacreCraft.Game.World.Entity;

//EntitySpawnRequest a spawn reason plus whether validation checks are skipped, maps to vanilla net.minecraft.world.entity.EntitySpawnRequest
public sealed record EntitySpawnRequest(EntitySpawnReason Reason, bool IgnoreChecks);
