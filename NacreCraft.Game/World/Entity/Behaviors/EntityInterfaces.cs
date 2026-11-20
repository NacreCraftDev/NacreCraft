using NacreCraft.Game.World.Inventory;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//Attackable an entity that remembers its last attacker, maps to vanilla net.minecraft.world.entity.Attackable
public interface Attackable
{
    LivingEntity? GetLastAttacker();
}

//Targeting an entity with a current attack target, maps to vanilla net.minecraft.world.entity.Targeting
public interface Targeting
{
    LivingEntity? GetTarget();
}

//TraceableEntity an entity that knows the entity that spawned it, maps to vanilla net.minecraft.world.entity.TraceableEntity
public interface TraceableEntity
{
    NacreCraft.Registry.Entity? GetOwner();
}

//ContainerUser an entity that can have a container open, maps to vanilla net.minecraft.world.entity.ContainerUser
public interface ContainerUser
{
    bool HasContainerOpen(object container, BlockPos blockPos);

    double GetContainerInteractionRange();

    //GetLivingEntity the container user viewed as a living entity; NC has no generic self cast, callers pass the entity
    LivingEntity LivingEntity { get; }
}

//EntityProcessor post-processing hook for a spawned entity, maps to vanilla net.minecraft.world.entity.EntityProcessor
//The functional shape is a delegate; the class keeps the static NOP entry point so the vanilla call sites read the same
public delegate NacreCraft.Registry.Entity EntityProcessorDelegate(NacreCraft.Registry.Entity input);

public static class EntityProcessor
{
    public static readonly EntityProcessorDelegate Nop = input => input;
}

//HasCustomInventoryScreen an entity with its own inventory screen, maps to vanilla net.minecraft.world.entity.HasCustomInventoryScreen
public interface HasCustomInventoryScreen
{
    void OpenCustomInventoryScreen(Player player);
}

//ItemSteerable an entity controlled by an item, maps to vanilla net.minecraft.world.entity.ItemSteerable
public interface ItemSteerable
{
    bool Boost();
}

//PlayerRideable marker for entities a player may ride, maps to vanilla net.minecraft.world.entity.PlayerRideable
public interface PlayerRideable
{
}

//PlayerRideableJumping a rideable entity with a jump charge, maps to vanilla net.minecraft.world.entity.PlayerRideableJumping
public interface PlayerRideableJumping : PlayerRideable
{
    void OnPlayerJump(int jumpAmount);

    bool CanJump();

    void HandleStartJump(int jumpScale);

    void HandleStopJump();

    int GetJumpCooldown() => 0;

    float GetPlayerJumpPendingScale(int jumpAmount)
        => jumpAmount >= 90 ? 1f : 0.4f + 0.4f * jumpAmount / 90f;
}

//SpawnGroupData marker for data shared by a spawn group, maps to vanilla net.minecraft.world.entity.SpawnGroupData
public interface SpawnGroupData
{
}

//SpawnPlacementType validates and adjusts a spawn position, maps to vanilla net.minecraft.world.entity.SpawnPlacementType
public interface SpawnPlacementType
{
    bool IsSpawnPositionOk(NacreCraft.Storage.ServerLevel level, BlockPos blockPos, NacreCraft.Registry.EntityType<object> type);

    BlockPos AdjustSpawnPosition(NacreCraft.Storage.ServerLevel level, BlockPos candidate) => candidate;
}

//ReputationEventHandler an entity whose reputation changes on villager events, maps to vanilla net.minecraft.world.entity.ReputationEventHandler
public interface ReputationEventHandler
{
    void OnReputationEventFrom(object type, NacreCraft.Registry.Entity source);
}
