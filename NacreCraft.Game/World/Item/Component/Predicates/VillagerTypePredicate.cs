using NacreCraft.Codec;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component.Predicates;

//VillagerTypePredicate villager variant predicate, checks whether the variant reference falls in the given set
//Maps to vanilla net.minecraft.core.component.predicates.VillagerTypePredicate
public sealed record VillagerTypePredicate(HolderSet<VillagerType> VillagerTypes)
    : SingleComponentItemPredicate<Holder<VillagerType>>
{
    //Codec persistence codec, the payload is just the villager type set, maps to vanilla CODEC
    public static readonly Codec<VillagerTypePredicate> Codec = HolderSetCodecs.VillagerTypeSet.ComapFlatMap(
        set => DataResult<VillagerTypePredicate>.Success(new VillagerTypePredicate(set)),
        predicate => predicate.VillagerTypes);

    public DataComponentType<object> ComponentType => DataComponents.VILLAGER_VARIANT;

    public bool MatchesValue(Holder<VillagerType> value) => VillagerTypes.Contains(value);
}
