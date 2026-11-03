using NacreCraft.Registry;
using NacreCraft.Util.Collection;

namespace NacreCraft.Client.SearchTree;

//IdSearchTree searches identifiers, splitting a "namespace:path" query into two index lookups
//maps to vanilla net.minecraft.client.searchtree.IdSearchTree
public class IdSearchTree<T> : SearchTree<T> where T : notnull
{
    protected readonly Func<T, int> AdditionOrder;
    protected readonly IdentifierSearchTree<T> IdentifierTree;

    public IdSearchTree(Func<T, IEnumerable<Identifier>> idGetter, IReadOnlyList<T> contents)
    {
        AdditionOrder = IndexLookup.CreateIndexLookup(contents);
        IdentifierTree = IdentifierSearchTree<T>.Create(contents, idGetter);
    }

    public virtual IReadOnlyList<T> Search(string text)
    {
        var colon = text.IndexOf(':');
        if (colon == -1) return SearchPlainText(text);
        return SearchIdentifier(text[..colon].Trim(), text[(colon + 1)..].Trim());
    }

    protected virtual IReadOnlyList<T> SearchPlainText(string text) => IdentifierTree.SearchPath(text);

    protected virtual IReadOnlyList<T> SearchIdentifier(string @namespace, string path)
        => new IntersectionIterator<T>(
            IdentifierTree.SearchNamespace(@namespace),
            IdentifierTree.SearchPath(path),
            CompareOrder).ToList();

    //CompareOrder orders results by the position they were added in, maps to vanilla additionOrder
    protected int CompareOrder(T a, T b) => AdditionOrder(a).CompareTo(AdditionOrder(b));
}
