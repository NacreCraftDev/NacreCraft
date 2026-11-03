using NacreCraft.Registry;

namespace NacreCraft.Client.SearchTree;

//IdentifierSearchTree indexes namespaces and paths separately, maps to vanilla net.minecraft.client.searchtree.IdentifierSearchTree
public interface IdentifierSearchTree<T>
{
    IReadOnlyList<T> SearchNamespace(string @namespace);

    IReadOnlyList<T> SearchPath(string path);

    public static IdentifierSearchTree<T> Empty() => new DelegateIdentifierSearchTree<T>(_ => Array.Empty<T>(), _ => Array.Empty<T>());

    public static IdentifierSearchTree<T> Create(IReadOnlyList<T> elements, Func<T, IEnumerable<Identifier>> idGetter)
    {
        if (elements.Count == 0) return Empty();
        var namespaceTree = new SuffixArray<T>();
        var pathTree = new SuffixArray<T>();
        foreach (var element in elements)
            foreach (var elementId in idGetter(element))
            {
                namespaceTree.Add(element, elementId.Namespace.ToLowerInvariant());
                pathTree.Add(element, elementId.Path.ToLowerInvariant());
            }
        namespaceTree.Generate();
        pathTree.Generate();
        return new DelegateIdentifierSearchTree<T>(namespaceTree.Search, pathTree.Search);
    }

    private sealed class DelegateIdentifierSearchTree<T>(
        Func<string, IReadOnlyList<T>> searchNamespace,
        Func<string, IReadOnlyList<T>> searchPath) : IdentifierSearchTree<T>
    {
        public IReadOnlyList<T> SearchNamespace(string @namespace) => searchNamespace(@namespace);

        public IReadOnlyList<T> SearchPath(string path) => searchPath(path);
    }
}
