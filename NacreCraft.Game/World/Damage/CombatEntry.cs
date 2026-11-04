namespace NacreCraft.Game.World.Damage;

//CombatEntry one recorded hit in a combat tracker, maps to vanilla net.minecraft.world.damagesource.CombatEntry
public sealed record CombatEntry(DamageSource Source, float Damage, FallLocation? FallLocation, float FallDistance);
