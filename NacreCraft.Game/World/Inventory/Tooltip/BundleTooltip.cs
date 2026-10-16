using NacreCraft.Game.World.Items.Component;

namespace NacreCraft.Game.World.Inventory.Tooltip;

//BundleTooltip bundle tooltip component, maps to vanilla net.minecraft.world.inventory.tooltip.BundleTooltip
//Only carries the contents, actual drawing is dispatched by type in the render layer
public sealed record BundleTooltip(BundleContents Contents) : TooltipComponent;
