using NacreCraft.Codec;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Slots;

//ContainerComponentManipulator reads the stored items of a container component, maps to vanilla net.minecraft.world.level.storage.loot.ContainerComponentManipulator
//Only the read half is ported; writing back needs the mutable component forms
public interface ContainerComponentManipulator
{
    string ComponentId { get; }

    IEnumerable<ItemStack> GetContents(ItemStack stack);
}

//ContainerComponentManipulators the built-in manipulators and their id codec, maps to vanilla ContainerComponentManipulators
public static class ContainerComponentManipulators
{
    public static readonly ContainerComponentManipulator CONTAINER = new ContainerManipulator();
    public static readonly ContainerComponentManipulator BUNDLE_CONTENTS = new BundleManipulator();

    public static readonly IReadOnlyList<ContainerComponentManipulator> All = new[] { CONTAINER, BUNDLE_CONTENTS };

    //CODEC dispatch by component id, maps to vanilla CODEC
    public static readonly Codec<ContainerComponentManipulator> CODEC = Codecs.String.ComapFlatMap(
        name => ById(name) is { } manipulator
            ? DataResult<ContainerComponentManipulator>.Success(manipulator)
            : DataResult<ContainerComponentManipulator>.Error(() => $"No items in component: {name}"),
        manipulator => manipulator.ComponentId);

    private static ContainerComponentManipulator? ById(string name)
    {
        var id = Identifier.TryParse(name);
        if (id is null) return null;
        foreach (var manipulator in All)
            if (Identifier.TryParse(manipulator.ComponentId) is { } candidate && candidate == id.Value)
                return manipulator;
        return null;
    }

    private sealed class ContainerManipulator : ContainerComponentManipulator
    {
        public string ComponentId => "minecraft:container";

        public IEnumerable<ItemStack> GetContents(ItemStack stack)
            => stack.Get(DataComponents.CONTAINER) is ItemContainerContents contents
                ? contents.NonEmptyItems()
                : Enumerable.Empty<ItemStack>();
    }

    private sealed class BundleManipulator : ContainerComponentManipulator
    {
        public string ComponentId => "minecraft:bundle_contents";

        public IEnumerable<ItemStack> GetContents(ItemStack stack)
            => stack.Get(DataComponents.BUNDLE_CONTENTS) is BundleContents contents
                ? contents.ItemCopyStream()
                : Enumerable.Empty<ItemStack>();
    }
}
