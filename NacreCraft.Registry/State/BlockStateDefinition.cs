using System.Text.RegularExpressions;

namespace NacreCraft.Registry.State;

//BlockStateDefinition non-generic version dedicated to building the BlockState struct
//Maps to vanilla StateDefinition<Block, BlockState> but stores data in BlockStateRegistry
//Enumerates all possible states via Cartesian product to build neighbors, using int indexes to avoid BlockState references
public sealed class BlockStateDefinition
{
    private static readonly Regex NamePattern = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    private readonly Block _owner;
    private readonly SortedDictionary<string, PropertyBase> _propertiesByName;
    private readonly IReadOnlyList<BlockState> _states;

    public BlockStateDefinition(Block owner, IDictionary<string, PropertyBase> properties)
    {
        _owner = owner;
        //Vanilla uses ImmutableSortedMap.copyOf to sort by property name in string order; Ordinal must be used here to align
        //Using the default comparer would sort by culture, which differs from Java's UTF-16 code point order on some characters
        _propertiesByName = new SortedDictionary<string, PropertyBase>(properties, StringComparer.Ordinal);
        _states = _propertiesByName.Count switch
        {
            0 => CreateSingletonState(owner),
            _ => CreateMultiPropertyStates(owner, _propertiesByName)
        };
    }

    //A singleton state with no properties
    private static IReadOnlyList<BlockState> CreateSingletonState(Block owner)
    {
        var state = BlockStateRegistry.Register(owner, Array.Empty<PropertyBase>(), Array.Empty<object?>());
        BlockStateRegistry.InitializeNeighbors(state.Id, Array.Empty<int[]>());
        return new List<BlockState> { state };
    }

    //Enumerate all states via a mixed-radix index and build neighbors by arithmetic
    //The index packs the per-property value digits, so neither the state table nor the neighbor table needs a
    //dictionary lookup or a temporary value list; this was the dominant allocation in block registration
    private static IReadOnlyList<BlockState> CreateMultiPropertyStates(Block owner, SortedDictionary<string, PropertyBase> propertiesByName)
    {
        var propertyKeys = propertiesByName.Values.ToArray();
        var propertyCount = propertyKeys.Length;
        var counts = new int[propertyCount];
        var strides = new int[propertyCount];
        var total = 1;
        for (var i = propertyCount - 1; i >= 0; i--)
        {
            var count = propertyKeys[i].PossibleValuesAsObjects.Count;
            counts[i] = count;
            strides[i] = total;
            total *= count;
        }

        var states = new List<BlockState>(total);
        //BlockStateData keeps the value array, so every state still needs its own instance
        for (var index = 0; index < total; index++)
        {
            var values = new object?[propertyCount];
            for (var i = 0; i < propertyCount; i++)
                values[i] = propertyKeys[i].PossibleValuesAsObjects[(index / strides[i]) % counts[i]];
            states.Add(BlockStateRegistry.Register(owner, propertyKeys, values));
        }

        for (var index = 0; index < total; index++)
        {
            var neighbors = new int[propertyCount][];
            for (var i = 0; i < propertyCount; i++)
            {
                var current = (index / strides[i]) % counts[i];
                var baseIndex = index - current * strides[i];
                var values = new int[counts[i]];
                for (var j = 0; j < counts[i]; j++)
                    values[j] = states[baseIndex + j * strides[i]].Id;
                neighbors[i] = values;
            }
            BlockStateRegistry.InitializeNeighbors(states[index].Id, neighbors);
        }
        return states;
    }

    public Block Owner => _owner;
    public IReadOnlyList<BlockState> PossibleStates => _states;
    public BlockState Any() => _states[0];
    public IReadOnlyCollection<PropertyBase> Properties => _propertiesByName.Values;
    public bool IsSingletonState => _propertiesByName.Count == 0;

    public PropertyBase? GetProperty(string name)
        => _propertiesByName.TryGetValue(name, out var p) ? p : null;

    public override string ToString()
        => $"BlockStateDefinition(owner={_owner}, properties=[{string.Join(",", _propertiesByName.Values.Select(p => p.Name))}])";

    //Builder, maps to vanilla StateDefinition.Builder
    public class Builder
    {
        private readonly Block _owner;
        private readonly Dictionary<string, PropertyBase> _properties = new();

        public Builder(Block owner) => _owner = owner;

        public Builder Add(params PropertyBase[] properties)
        {
            foreach (var property in properties)
            {
                ValidateProperty(property);
                _properties[property.Name] = property;
            }
            return this;
        }

        private void ValidateProperty(PropertyBase property)
        {
            var name = property.Name;
            if (!NamePattern.IsMatch(name))
                throw new ArgumentException($"{_owner} has invalidly named property: {name}");
            if (property.PossibleValuesAsObjects.Count <= 1)
                throw new ArgumentException($"{_owner} attempted use property {name} with <= 1 possible values");
            if (_properties.ContainsKey(name))
                throw new ArgumentException($"{_owner} has duplicate property: {name}");
        }

        public BlockStateDefinition Create() => new(_owner, _properties);
    }
}
