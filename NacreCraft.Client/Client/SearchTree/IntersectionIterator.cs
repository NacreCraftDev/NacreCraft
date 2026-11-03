namespace NacreCraft.Client.SearchTree;

//IntersectionIterator yields the elements two sorted sequences share, maps to vanilla IntersectionIterator
public sealed class IntersectionIterator<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _first;
    private readonly IEnumerable<T> _second;
    private readonly Comparison<T> _comparator;

    public IntersectionIterator(IEnumerable<T> first, IEnumerable<T> second, Comparison<T> comparator)
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
        while (hasFirst && hasSecond)
        {
            var compare = _comparator(first.Current, second.Current);
            if (compare == 0)
            {
                var value = first.Current;
                hasSecond = second.MoveNext();
                hasFirst = first.MoveNext();
                yield return value;
            }
            else if (compare < 0)
            {
                hasFirst = first.MoveNext();
            }
            else
            {
                hasSecond = second.MoveNext();
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
