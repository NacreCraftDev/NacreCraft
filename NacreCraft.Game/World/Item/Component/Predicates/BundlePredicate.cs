using NacreCraft.Codec;
using NacreCraft.Game.Advancements.Predicates;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component.Predicates;

//BundlePredicate bundle predicate, checks whether the items in the bundle satisfy the collection predicate
//Maps to vanilla net.minecraft.core.component.predicates.BundlePredicate
public sealed record BundlePredicate(CollectionPredicate<ItemStack, ItemPredicate> Contents)
    : SingleComponentItemPredicate<BundleContents>
{
    //Codec persistence codec, the payload is just the collection predicate, maps to vanilla CODEC
    public static readonly Codec<BundlePredicate> Codec =
        CollectionPredicate<ItemStack, ItemPredicate>.Codec(ItemPredicate.Codec).ComapFlatMap(
            predicate => DataResult<BundlePredicate>.Success(new BundlePredicate(predicate)),
            bundle => bundle.Contents);

    public DataComponentType<object> ComponentType => DataComponents.BUNDLE_CONTENTS;

    //MatchesValue the bundle stores templates, so they are materialized into item stacks before going to the collection predicate
    public bool MatchesValue(BundleContents value) => Contents.Test(value.ItemCopyStream().ToList());
}
