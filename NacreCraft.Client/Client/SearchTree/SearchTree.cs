namespace NacreCraft.Client.SearchTree;

//SearchTree a text index for the creative inventory search box, maps to vanilla net.minecraft.client.searchtree.SearchTree
public interface SearchTree<T>
{
    IReadOnlyList<T> Search(string text);

    public static SearchTree<T> Empty() => new DelegateSearchTree<T>(_ => Array.Empty<T>());

    //PlainText builds a substring index over every string the getter yields, maps to vanilla plainText
    public static SearchTree<T> PlainText(IReadOnlyList<T> elements, Func<T, IEnumerable<string>> idGetter)
    {
        if (elements.Count == 0) return Empty();
        var tree = new SuffixArray<T>();
        foreach (var element in elements)
            foreach (var elementId in idGetter(element))
                tree.Add(element, elementId.ToLowerInvariant());
        tree.Generate();
        return new DelegateSearchTree<T>(tree.Search);
    }

    private sealed class DelegateSearchTree<T>(Func<string, IReadOnlyList<T>> search) : SearchTree<T>
    {
        public IReadOnlyList<T> Search(string text) => search(text);
    }
}
