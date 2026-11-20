using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Network.Chat;
using ChatComponent = NacreCraft.Network.Chat.Component;

namespace NacreCraft.Game.World.Items.Component;

//ItemLore the lore lines of an item, maps to vanilla net.minecraft.world.item.component.ItemLore
//styledLines is exposed as the same list until the italic styling pass is ported
public sealed record ItemLore(IReadOnlyList<ChatComponent> Lines, IReadOnlyList<ChatComponent>? Styled = null)
{
    public const int MaxLines = 256;

    public static readonly ItemLore Empty = new(Array.Empty<ChatComponent>());

    public static readonly Codec<ItemLore> Codec =
        ComponentSerialization.Codec.ListOf().Xmap(lines => new ItemLore(lines), lore => lore.Lines);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, ItemLore> StreamCodec =
        ByteBufCodecs.ListOf(ComponentSerialization.StreamCodec, MaxLines)
            .Map(lines => new ItemLore(lines), lore => lore.Lines.ToList());

    public IReadOnlyList<ChatComponent> StyledLines => Styled ?? Lines;

    public ItemLore WithLineAdded(ChatComponent line)
    {
        var lines = new List<ChatComponent>(Lines) { line };
        return new ItemLore(lines);
    }
}

//UseRemainder the item left behind after one use, maps to vanilla net.minecraft.world.item.component.UseRemainder
public sealed record UseRemainder(ItemStackTemplate ConvertInto)
{
    public static readonly Codec<UseRemainder> Codec =
        ItemStackTemplate.PersistentCodec.Xmap(template => new UseRemainder(template), value => value.ConvertInto);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, UseRemainder> StreamCodec =
        ItemStackTemplate.StreamCodec.Map(template => new UseRemainder(template), value => value.ConvertInto);
}

//ChargedProjectiles the projectiles loaded into a crossbow, maps to vanilla net.minecraft.world.item.component.ChargedProjectiles
public sealed record ChargedProjectiles(IReadOnlyList<ItemStackTemplate> Items)
{
    public static readonly ChargedProjectiles Empty = new(Array.Empty<ItemStackTemplate>());

    public static readonly Codec<ChargedProjectiles> Codec =
        ItemStackTemplate.PersistentCodec.ListOf().Xmap(items => new ChargedProjectiles(items), value => value.Items);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, ChargedProjectiles> StreamCodec =
        ByteBufCodecs.ListOf(ItemStackTemplate.StreamCodec, 1024)
            .Map(items => new ChargedProjectiles(items), value => value.Items.ToList());

    public static ChargedProjectiles Of(ItemStackTemplate stack)
        => stack.Count <= 0 ? Empty : new ChargedProjectiles(new[] { stack });

    public static ChargedProjectiles OfNonEmpty(IReadOnlyList<ItemStack> items)
    {
        var list = new List<ItemStackTemplate>(items.Count);
        foreach (var stack in items)
        {
            if (!stack.IsEmpty() && list.Count < 1024) list.Add(ItemStackTemplate.FromStack(stack));
        }
        return list.Count == 0 ? Empty : new ChargedProjectiles(list);
    }

    public bool Contains(Item item)
    {
        foreach (var projectile in Items)
        {
            if (ReferenceEquals(projectile.Item.Value, item)) return true;
        }
        return false;
    }
}

//SulfurCubeContent the block absorbed by a sulfur cube, maps to vanilla net.minecraft.world.item.component.SulfurCubeContent
public sealed record SulfurCubeContent(ItemStackTemplate AbsorbedBlockItemStack)
{
    public static readonly Codec<SulfurCubeContent> Codec =
        ItemStackTemplate.PersistentCodec.Xmap(template => new SulfurCubeContent(template),
            value => value.AbsorbedBlockItemStack);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, SulfurCubeContent> StreamCodec =
        ItemStackTemplate.StreamCodec.Map(template => new SulfurCubeContent(template),
            value => value.AbsorbedBlockItemStack);

    public static SulfurCubeContent OfNonEmpty(ItemStack stack)
        => new(ItemStackTemplate.FromNonEmptyStack(stack));
}
