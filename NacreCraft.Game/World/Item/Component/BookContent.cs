namespace NacreCraft.Game.World.Items.Component;

//BookContent the pages of a writable or written book, maps to vanilla net.minecraft.world.item.component.BookContent
//NC has no Filterable yet, so pages are stored as plain text; the component still exposes the same replacement surface
public interface BookContent<T, TSelf>
{
    IReadOnlyList<T> Pages { get; }

    TSelf WithReplacedPages(IReadOnlyList<T> pages);
}
