using NacreCraft.Primitives;
using NacreCraft.Registry.Context;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Loot;

//LootContextParams the context keys loot data reads, maps to vanilla LootContextParams
//The entity type is fully qualified because this namespace's parent owns an Entity segment
public static class LootContextParams
{
    public static readonly ContextKey<NacreCraft.Registry.Entity> ThisEntity =
        ContextKey<NacreCraft.Registry.Entity>.Vanilla("this_entity");

    public static readonly ContextKey<NacreCraft.Registry.Entity> InteractingEntity =
        ContextKey<NacreCraft.Registry.Entity>.Vanilla("interacting_entity");

    public static readonly ContextKey<NacreCraft.Registry.Entity> TargetEntity =
        ContextKey<NacreCraft.Registry.Entity>.Vanilla("target_entity");

    public static readonly ContextKey<NacreCraft.Registry.Entity> AttackingEntity =
        ContextKey<NacreCraft.Registry.Entity>.Vanilla("attacking_entity");

    public static readonly ContextKey<NacreCraft.Registry.Entity> DirectAttackingEntity =
        ContextKey<NacreCraft.Registry.Entity>.Vanilla("direct_attacking_entity");

    public static readonly ContextKey<NacreCraft.Game.World.Entity.Player> LastDamagePlayer =
        ContextKey<NacreCraft.Game.World.Entity.Player>.Vanilla("last_damage_player");

    public static readonly ContextKey<NacreCraft.Game.World.Damage.DamageSource> DamageSource =
        ContextKey<NacreCraft.Game.World.Damage.DamageSource>.Vanilla("damage_source");

    public static readonly ContextKey<Vec3> Origin = ContextKey<Vec3>.Vanilla("origin");

    public static readonly ContextKey<BlockState> BlockState = ContextKey<BlockState>.Vanilla("block_state");

    public static readonly ContextKey<NacreCraft.Game.World.Level.Block.BlockEntity> BlockEntity =
        ContextKey<NacreCraft.Game.World.Level.Block.BlockEntity>.Vanilla("block_entity");

    public static readonly ContextKey<NacreCraft.Game.World.Items.ItemStack> Tool =
        ContextKey<NacreCraft.Game.World.Items.ItemStack>.Vanilla("tool");

    public static readonly ContextKey<float> ExplosionRadius = ContextKey<float>.Vanilla("explosion_radius");
}
