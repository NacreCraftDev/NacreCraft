using NacreCraft.Registry;

namespace NacreCraft.Client.SearchTree;

//FullTextSearchTree searches identifiers and display names at the same time
//maps to vanilla net.minecraft.client.searchtree.FullTextSearchTree
public sealed class FullTextSearchTree<T> : IdSearchTree<T> where T : notnull
{
    private readonly SearchTree<T> _plainTextSearchTree;

    public FullTextSearchTree(
        Func<T, IEnumerable<string>> nameGetter,
        Func<T, IEnumerable<Identifier>> idGetter,
        IReadOnlyList<T> contents)
        : base(idGetter, contents)
        => _plainTextSearchTree = SearchTree<T>.PlainText(contents, nameGetter);

    protected override IReadOnlyList<T> SearchPlainText(string text) => _plainTextSearchTree.Search(text);

    protected override IReadOnlyList<T> SearchIdentifier(string @namespace, string path)
    {
        var namespaces = IdentifierTree.SearchNamespace(@namespace);
        var paths = IdentifierTree.SearchPath(path);
        var names = _plainTextSearchTree.Search(path);
        var mergedPathsAndNames = new MergingUniqueIterator<T>(paths, names, CompareOrder);
        return new IntersectionIterator<T>(namespaces, mergedPathsAndNames, CompareOrder).ToList();
    }
}
