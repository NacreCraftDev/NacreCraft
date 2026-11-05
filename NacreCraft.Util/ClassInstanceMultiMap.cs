namespace NacreCraft.Util;

//ClassInstanceMultiMap keeps elements grouped by their runtime class, maps to vanilla net.minecraft.util.ClassInstanceMultiMap
//Adding an element also adds it to every base class bucket, so Find walks the hierarchy in one lookup
public sealed class ClassInstanceMultiMap<T> : IEnumerable<T> where T : class
{
    private readonly Type _baseClass;
    private readonly Dictionary<Type, List<T>> _byClass = new();
    private readonly List<T> _allInstances = new();

    public ClassInstanceMultiMap(Type baseClass)
    {
        _baseClass = baseClass;
        _byClass[baseClass] = _allInstances;
    }

    public int Count => _allInstances.Count;

    public bool IsEmpty => _allInstances.Count == 0;

    public bool Add(T value)
    {
        if (!_baseClass.IsInstanceOfType(value)) return false;
        for (var type = value.GetType(); type is not null && _baseClass.IsAssignableFrom(type); type = type.BaseType)
        {
            if (!_byClass.TryGetValue(type, out var list))
            {
                list = new List<T>();
                _byClass[type] = list;
            }
            list.Add(value);
        }
        return true;
    }

    public bool Remove(T value)
    {
        if (!_baseClass.IsInstanceOfType(value)) return false;
        for (var type = value.GetType(); type is not null && _baseClass.IsAssignableFrom(type); type = type.BaseType)
            if (_byClass.TryGetValue(type, out var list)) list.Remove(value);
        return true;
    }

    //Find every element assignable to the given class, maps to vanilla find
    public IReadOnlyList<T> Find(Type type)
        => _byClass.TryGetValue(type, out var list) ? list : Array.Empty<T>();

    public IReadOnlyList<T> GetAll() => _allInstances;

    public IEnumerator<T> GetEnumerator() => _allInstances.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
