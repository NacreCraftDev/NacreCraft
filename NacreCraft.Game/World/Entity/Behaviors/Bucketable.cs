using NacreCraft.Codec;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//Bucketable an entity that can be caught in a bucket, maps to vanilla net.minecraft.world.entity.Bucketable
//The entity-specific tag hooks are abstract; the default data helpers store the common mob flags and health.
//NC lacks CUSTOM_NAME/BUCKET_ENTITY_DATA and several entity flags, so the common data goes into CUSTOM_DATA and only the
//NoAI/persistence/health parts are preserved
public interface Bucketable
{
    bool FromBucket();

    void SetFromBucket(bool fromBucket);

    void SaveToBucketTag(ItemStack bucket);

    void LoadFromBucketTag(CompoundTag tag);

    ItemStack GetBucketItemStack();

    SoundEvent GetPickupSound();

    //SaveDefaultDataToBucketTag stores the shared mob state on the bucket, maps to vanilla saveDefaultDataToBucketTag
    static void SaveDefaultDataToBucketTag(Mob entity, ItemStack bucket)
    {
        var existing = bucket.Get(DataComponents.CUSTOM_DATA) as CustomData ?? CustomData.Empty;
        var updated = existing.Update(tag =>
        {
            if (entity.NoAi) tag.PutBoolean("NoAI", true);
            if (entity.PersistenceRequired) tag.PutBoolean("PersistenceRequired", true);
            tag.PutFloat("Health", entity.Health);
        });
        if (!updated.IsEmpty) bucket.Set(DataComponents.CUSTOM_DATA, updated);
    }

    //LoadDefaultDataFromBucketTag restores the shared mob state, maps to vanilla loadDefaultDataFromBucketTag
    static void LoadDefaultDataFromBucketTag(Mob entity, CompoundTag tag)
    {
        if (tag.Contains("NoAI")) entity.NoAi = tag.GetBooleanOr("NoAI", false);
        if (tag.GetBooleanOr("PersistenceRequired", false)) entity.SetPersistenceRequired();
        if (tag.Contains("Health")) entity.SetHealth(tag.GetFloatValue("Health"));
    }

    //BucketMobPickup catches the entity when the held item is a water bucket, maps to vanilla bucketMobPickup
    //A null result means the interaction should pass through to the normal entity interaction
    static InteractionResult? BucketMobPickup<T>(Player player, InteractionHand hand, T pickupEntity)
        where T : LivingEntity, Bucketable
    {
        var held = player.GetItemInHand(hand);
        if (!pickupEntity.CanBePickedUpWithBucket(held) || !!pickupEntity.IsDeadOrDying) return null;
        var bucket = pickupEntity.GetBucketItemStack();
        pickupEntity.SaveToBucketTag(bucket);
        if (held.Count <= 1)
        {
            player.SetItemInHand(hand, bucket);
        }
        else
        {
            held.Shrink(1);
            if (player is ServerPlayer serverPlayer) serverPlayer.Inventory.Add(bucket);
        }
        pickupEntity.Discard();
        return InteractionResult.Success;
    }

    //CanBePickedUpWithBucket whether the held stack may catch this entity, maps to vanilla canBePickedUpWithBucket
    bool CanBePickedUpWithBucket(ItemStack itemStack)
        => itemStack.GetItem() == BuiltInRegistries.ITEM.GetValue(Identifier.WithDefaultNamespace("water_bucket"));
}
