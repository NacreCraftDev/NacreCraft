namespace NacreCraft.Client.SearchTree;

//MergingUniqueIterator merges two sorted sequences without duplicates, maps to vanilla MergingUniqueIterator
public sealed class MergingUniqueIterator<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _first;
    private readonly IEnumerable<T> _second;
    private readonly Comparison<T> _comparator;

    public MergingUniqueIterator(IEnumerable<T> first, IEnumerable<T> second, Comparison<T> comparator)
    {
        _first = first;
        _second = second;
        _comparator = comparator;
    }

    public IEnumerator<T> GetEnumerator()
    {
        using var first = _first.GetEnumerator();
        using var second = _second.GetEnumerator();
        var hasFirst = first.MoveNext();
        var hasSecond = second.MoveNext();
        while (true)
        {
            if (!hasFirst && !hasSecond) yield break;
            if (!hasFirst)
            {
                yield return second.Current;
                hasSecond = second.MoveNext();
                continue;
            }
            if (!hasSecond)
            {
                yield return first.Current;
                hasFirst = first.MoveNext();
                continue;
            }
            var compare = _comparator(first.Current, second.Current);
            if (compare == 0)
            {
                hasSecond = second.MoveNext();
                yield return first.Current;
                hasFirst = first.MoveNext();
            }
            else if (compare < 0)
            {
                yield return first.Current;
                hasFirst = first.MoveNext();
            }
            else
            {
                yield return second.Current;
                hasSecond = second.MoveNext();
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
