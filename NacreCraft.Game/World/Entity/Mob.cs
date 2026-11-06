using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Pathfinder;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Registry.EntityAttribute;
using NacreCraft.Util;
using NacreCraft.Util.Random;
//The attribute constants class shares a name with the Attributes instance property on Entity, so referencing the constants needs an alias
using EntityAttributes = NacreCraft.Registry.EntityAttribute.Attributes;

namespace NacreCraft.Game.World.Entity;

//Mob mob entity, maps to vanilla net.minecraft.world.entity.Mob
//Extends Entity and holds an entity type reference and AI flags
//Concrete mob classes (zombie/pig etc.) are added as needed once AI/pathfinding lands; for now the type factory builds Mob directly
public class Mob : LivingEntity
{
    private readonly EntityType<object> _type;

    //PathfindingMalus per-mob path type costs, maps to vanilla pathfindingMalus
    //A type without an explicit cost falls back to the cost baked into the type itself
    private readonly Dictionary<PathType, float> _pathfindingMalus = new();
    //_guaranteedDrops slots whose equipment must drop on death, maps to vanilla guaranteedDrop
    //The drop table is not wired up yet, so the set only records the intent
    private readonly HashSet<EquipmentSlot> _guaranteedDrops = new();
    private RandomSource? _random;

    //Mob constructor, the entity type is passed at registry registration and determines the registry name and network index
    //The attribute map is looked up by type from DefaultAttributes, all species differences live there, an unregistered type falls back to an empty map
    public Mob(EntityType<object> type)
    {
        _type = type;
        SetAttributes(new AttributeMap(DefaultAttributes.GetSupplier(type) ?? AttributeSupplier.Empty));
    }

    //A living entity's gravity/knockback resistance/safe fall distance/fall damage multiplier/step height all go through attributes
    //Maps to the points where vanilla LivingEntity overrides Entity and reads attributes
    public override double DefaultGravity => GetAttributeValue(EntityAttributes.Gravity);
    public override double KnockbackResistance => GetAttributeValue(EntityAttributes.KnockbackResistance);
    public override double SafeFallDistance => GetAttributeValue(EntityAttributes.SafeFallDistance);
    public override double FallDamageMultiplier => GetAttributeValue(EntityAttributes.FallDamageMultiplier);
    public override double MaxUpStep => GetAttributeValue(EntityAttributes.StepHeight);

    //Id entity registry name taken from the bound type
    public override Identifier Id => _type.Id;

    //Type entity type, the tracker uses it to get the tracking range and network index
    public override EntityType<object>? Type => _type;

    //A mob is a living entity and blocks block placement, maps to blocksBuilding being enabled in the vanilla LivingEntity constructor
    public override bool BlocksBuilding => true;

    //A mob is a living entity and takes fall damage, maps to vanilla LivingEntity overriding Entity
    public override bool TakesFallDamage => true;

    //NoAi whether AI is disabled, default false, aligned with the vanilla NoAI NBT tag
    public bool NoAi { get; set; }

    //TargetUuid UUID of the current attack target, a placeholder until the AI subsystem is ready
    public Guid? TargetUuid { get; set; }

    //PersistenceRequired whether to force persistence without unloading, default false
    public bool PersistenceRequired { get; set; }

    //GetPathfindingMalus extra cost this mob assigns to a path type, maps to vanilla getPathfindingMalus
    public virtual float GetPathfindingMalus(PathType pathType)
        => _pathfindingMalus.TryGetValue(pathType, out var malus) ? malus : pathType.GetMalus();

    public void SetPathfindingMalus(PathType pathType, float malus) => _pathfindingMalus[pathType] = malus;

    //OnPathfindingStart hooks a mob can use to adjust itself around a pathfinding run, maps to vanilla onPathfindingStart
    public virtual void OnPathfindingStart() { }

    //OnPathfindingDone hook paired with OnPathfindingStart, maps to vanilla onPathfindingDone
    public virtual void OnPathfindingDone() { }

    //GetMaxFallDistance how far this mob is willing to drop while pathfinding, maps to vanilla getMaxFallDistance
    //Vanilla lowers it when the mob has a target and is hurt; the AI target is not wired up, so it answers the targetless value
    public virtual int GetMaxFallDistance() => Mth.Floor(SafeFallDistance);

    //GetRandom the mob's random source, taken from the level when the level is reachable, maps to vanilla getRandom
    public RandomSource GetRandom() => _random ??= (Level as LevelAccessor)?.Random ?? RandomSource.Create();

    public IReadOnlyCollection<EquipmentSlot> GuaranteedDrops => _guaranteedDrops;

    //SetGuaranteedDrop marks one slot as a guaranteed drop, maps to vanilla setGuaranteedDrop
    public void SetGuaranteedDrop(EquipmentSlot slot) => _guaranteedDrops.Add(slot);

    //SetPersistenceRequired forces this mob to stay when the chunk unloads, maps to vanilla setPersistenceRequired
    public void SetPersistenceRequired() => PersistenceRequired = true;

    protected override void AddAdditionalSaveData(CompoundTag tag)
    {
        base.AddAdditionalSaveData(tag);
        tag.PutBoolean("NoAI", NoAi);
        tag.PutBoolean("PersistenceRequired", PersistenceRequired);
    }

    protected override void ReadAdditionalSaveData(CompoundTag tag)
    {
        base.ReadAdditionalSaveData(tag);
        NoAi = tag.GetBooleanOr("NoAI", false);
        PersistenceRequired = tag.GetBooleanOr("PersistenceRequired", false);
    }
}
