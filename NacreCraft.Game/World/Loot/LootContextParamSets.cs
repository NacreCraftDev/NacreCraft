using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;
using NacreCraft.Registry.Context;

namespace NacreCraft.Game.World.Loot;

//LootContextParamSets the named context key sets loot tables declare, maps to vanilla LootContextParamSets
//Vanilla keeps a bimap keyed by identifier so the "type" field of a loot table resolves back to a set
public static class LootContextParamSets
{
    private static readonly Dictionary<Identifier, ContextKeySet> Registry = new();

    //CODEC resolves a parameter set by identifier, maps to vanilla LootContextParamSets.CODEC
    public static readonly Codec<ContextKeySet> CODEC = new ParamSetCodec();

    public static readonly ContextKeySet Empty = Register("empty", _ => { });

    public static readonly ContextKeySet Chest = Register("chest", b => b
        .Required(LootContextParams.Origin)
        .Optional(LootContextParams.ThisEntity));

    public static readonly ContextKeySet Command = Register("command", b => b
        .Required(LootContextParams.Origin)
        .Optional(LootContextParams.ThisEntity));

    public static readonly ContextKeySet Selector = Register("selector", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity));

    public static readonly ContextKeySet VillagerTrade = Register("villager_trade", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.AdditionalCostComponentAllowed));

    public static readonly ContextKeySet Fishing = Register("fishing", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.Tool)
        .Optional(LootContextParams.ThisEntity));

    public static readonly ContextKeySet Equipment = Register("equipment", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity));

    public static readonly ContextKeySet Archaeology = Register("archaeology", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Tool));

    public static readonly ContextKeySet Gift = Register("gift", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity));

    public static readonly ContextKeySet PiglinBarter = Register("barter", b => b
        .Required(LootContextParams.ThisEntity));

    public static readonly ContextKeySet Vault = Register("vault", b => b
        .Required(LootContextParams.Origin)
        .Optional(LootContextParams.ThisEntity)
        .Optional(LootContextParams.Tool));

    public static readonly ContextKeySet AdvancementReward = Register("advancement_reward", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Origin));

    public static readonly ContextKeySet AdvancementEntity = Register("advancement_entity", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Origin));

    public static readonly ContextKeySet AdvancementLocation = Register("advancement_location", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.Tool)
        .Required(LootContextParams.BlockState));

    public static readonly ContextKeySet BlockUse = Register("block_use", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.BlockState));

    public static readonly ContextKeySet Shearing = Register("shearing", b => b
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Tool));

    public static readonly ContextKeySet EntityInteract = Register("entity_interact", b => b
        .Required(LootContextParams.TargetEntity)
        .Optional(LootContextParams.InteractingEntity)
        .Required(LootContextParams.Tool));

    public static readonly ContextKeySet BlockInteract = Register("block_interact", b => b
        .Required(LootContextParams.BlockState)
        .Optional(LootContextParams.BlockEntity)
        .Optional(LootContextParams.InteractingEntity)
        .Optional(LootContextParams.Tool));

    public static readonly ContextKeySet EnchantedDamage = Register("enchanted_damage", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.EnchantmentLevel)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.DamageSource)
        .Optional(LootContextParams.DirectAttackingEntity)
        .Optional(LootContextParams.AttackingEntity));

    public static readonly ContextKeySet EnchantedItem = Register("enchanted_item", b => b
        .Required(LootContextParams.Tool)
        .Required(LootContextParams.EnchantmentLevel));

    public static readonly ContextKeySet EnchantedLocation = Register("enchanted_location", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.EnchantmentLevel)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.EnchantmentActive));

    public static readonly ContextKeySet EnchantedEntity = Register("enchanted_entity", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.EnchantmentLevel)
        .Required(LootContextParams.Origin));

    public static readonly ContextKeySet HitBlock = Register("hit_block", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.EnchantmentLevel)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.BlockState));

    public static readonly ContextKeySet Block = Register("block", b => b
        .Required(LootContextParams.BlockState)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.Tool)
        .Optional(LootContextParams.ThisEntity)
        .Optional(LootContextParams.BlockEntity)
        .Optional(LootContextParams.ExplosionRadius));

    public static readonly ContextKeySet Entity = Register("entity", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.DamageSource)
        .Optional(LootContextParams.AttackingEntity)
        .Optional(LootContextParams.DirectAttackingEntity)
        .Optional(LootContextParams.LastDamagePlayer));

    public static readonly ContextKeySet AllParams = Register("generic", b => b
        .Required(LootContextParams.ThisEntity)
        .Required(LootContextParams.LastDamagePlayer)
        .Required(LootContextParams.DamageSource)
        .Required(LootContextParams.AttackingEntity)
        .Required(LootContextParams.DirectAttackingEntity)
        .Required(LootContextParams.Origin)
        .Required(LootContextParams.BlockState)
        .Required(LootContextParams.BlockEntity)
        .Required(LootContextParams.Tool)
        .Required(LootContextParams.ExplosionRadius)
        .Required(LootContextParams.AdditionalCostComponentAllowed));

    //Get resolves a parameter set by id, the reverse of the lookup the "type" field needs
    public static ContextKeySet? Get(Identifier id) => Registry.GetValueOrDefault(id);

    private static ContextKeySet Register(string name, Action<ContextKeySet.Builder> configure)
    {
        var builder = new ContextKeySet.Builder();
        configure(builder);
        var result = builder.Build();
        Registry[Identifier.WithDefaultNamespace(name)] = result;
        return result;
    }
}

//ParamSetCodec reads a parameter set identifier; vanilla reverses the bimap to encode, which NC does not need yet
internal sealed class ParamSetCodec : ScalarCodec<ContextKeySet>
{
    public override DataResult<ContextKeySet> Parse<U>(DynamicOps<U> ops, U input)
        => IdentifierCodec.Instance.Parse(ops, input).FlatMap(id =>
        {
            var set = LootContextParamSets.Get(id);
            return set is null
                ? DataResult<ContextKeySet>.Error(() => $"No parameter set exists with id: '{id}'")
                : DataResult<ContextKeySet>.Success(set);
        });

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, ContextKeySet value)
        => DataResult<U>.Error(() => "Parameter sets are not encodable");
}
