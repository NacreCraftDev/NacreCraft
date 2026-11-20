using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World;

//InteractionResult the outcome of a player interaction, maps to vanilla net.minecraft.world.InteractionResult
//Success carries a swing source and the held-item context; Fail/Pass/TryWithEmptyHand are the other three outcomes
public abstract class InteractionResult
{
    public static readonly SuccessResult Success = new(SwingSource.Client, ItemContext.Default);
    public static readonly SuccessResult SuccessServer = new(SwingSource.Server, ItemContext.Default);
    public static readonly SuccessResult Consume = new(SwingSource.None, ItemContext.Default);
    public static readonly FailResult Fail = new();
    public static readonly PassResult Pass = new();
    public static readonly TryEmptyHandInteractionResult TryWithEmptyHand = new();

    public virtual bool ConsumesAction => false;

    public enum SwingSource
    {
        None,
        Client,
        Server,
    }

    //ItemContext whether the interaction used an item and what the held item turned into
    public sealed record ItemContext(bool WasItemInteraction, ItemStack? HeldItemTransformedTo)
    {
        public static readonly ItemContext None = new(false, null);
        public static readonly ItemContext Default = new(true, null);
    }

    //Success an interaction that consumed the action, maps to vanilla InteractionResult.Success
    public sealed class SuccessResult(SwingSource swingSource, ItemContext itemContext) : InteractionResult
    {
        public SwingSource Source { get; } = swingSource;
        public ItemContext Context { get; } = itemContext;
        public override bool ConsumesAction => true;

        public SuccessResult HeldItemTransformedTo(ItemStack itemStack)
            => new(Source, new ItemContext(true, itemStack));

        public SuccessResult WithoutItem() => new(Source, ItemContext.None);

        public bool WasItemInteraction() => Context.WasItemInteraction;

        public ItemStack? HeldItemTransformedTo() => Context.HeldItemTransformedTo;
    }

    public sealed class FailResult : InteractionResult;

    public sealed class PassResult : InteractionResult;

    public sealed class TryEmptyHandInteractionResult : InteractionResult;
}
