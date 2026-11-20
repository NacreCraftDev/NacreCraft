namespace NacreCraft.Game.World.Entity;

//StackedContents a multiset of items used to test whether a recipe can be crafted, maps to vanilla net.minecraft.world.entity.player.StackedContents
//The item-to-count map uses reference identity, matching vanilla Reference2IntOpenHashMap
public sealed class StackedContents<T> where T : notnull
{
    //Amounts available count per item, maps to vanilla amounts
    private readonly Dictionary<T, int> _amounts = new(ReferenceEqualityComparer<T>.Instance);

    //IngredientInfo whether an item satisfies the ingredient, maps to vanilla IngredientInfo
    public interface IngredientInfo
    {
        bool AcceptsItem(T item);
    }

    //Output receives the item chosen for each ingredient, maps to vanilla Output
    public interface Output
    {
        void Accept(T item);
    }

    public int GetAmount(T item) => _amounts.TryGetValue(item, out var count) ? count : 0;

    public void Account(T item, int count) => Put(item, count);

    public void Clear() => _amounts.Clear();

    //TryPick assigns one item per ingredient from the available counts, maps to vanilla tryPick
    public bool TryPick(IReadOnlyList<IngredientInfo> ingredients, int amount, Output? output)
        => new RecipePicker(this, ingredients).TryPick(amount, output);

    //TryPickAll largest craftable amount up to maxSize, maps to vanilla tryPickAll
    public int TryPickAll(IReadOnlyList<IngredientInfo> ingredients, int maxSize, Output? output)
        => new RecipePicker(this, ingredients).TryPickAll(maxSize, output);

    //GetResultUpperBound the largest count any single ingredient can accept, maps to vanilla getResultUpperBound
    public int GetResultUpperBound(IReadOnlyList<IngredientInfo> ingredients)
    {
        var result = int.MaxValue;
        foreach (var ingredient in ingredients)
        {
            var best = 0;
            foreach (var (item, count) in _amounts)
            {
                if (count > best && ingredient.AcceptsItem(item)) best = count;
                if (best >= result) break;
            }
            result = best;
            if (result == 0) break;
        }
        return result;
    }

    private bool HasAtLeast(T item, int count) => GetAmount(item) >= count;

    private void Take(T item, int amount)
    {
        var previous = _amounts[item];
        if (previous < amount) throw new InvalidOperationException($"Took {amount} items, but only had {previous}");
        _amounts[item] = previous - amount;
    }

    private void Put(T item, int count)
        => _amounts[item] = GetAmount(item) + count;

    private List<T> GetUniqueAvailableIngredientItems(IReadOnlyList<IngredientInfo> ingredients)
    {
        var result = new List<T>();
        foreach (var (item, count) in _amounts)
        {
            if (count > 0 && AnyIngredientMatches(ingredients, item)) result.Add(item);
        }
        return result;
    }

    private static bool AnyIngredientMatches(IReadOnlyList<IngredientInfo> ingredients, T item)
    {
        foreach (var ingredient in ingredients)
        {
            if (ingredient.AcceptsItem(item)) return true;
        }
        return false;
    }

    //RecipePicker bipartite matching between ingredients and available items, maps to vanilla StackedContents.RecipePicker
    private sealed class RecipePicker
    {
        private readonly StackedContents<T> _owner;
        private readonly IReadOnlyList<IngredientInfo> _ingredients;
        private readonly int _ingredientCount;
        private readonly List<T> _items;
        private readonly int _itemCount;
        private readonly bool[] _visitedIngredients;
        private readonly bool[] _visitedItems;
        private readonly bool[] _satisfied;
        private readonly bool[] _connections;
        private readonly bool[] _assigned;
        private readonly List<int> _path = new();

        public RecipePicker(StackedContents<T> owner, IReadOnlyList<IngredientInfo> ingredients)
        {
            _owner = owner;
            _ingredients = ingredients;
            _ingredientCount = ingredients.Count;
            _items = owner.GetUniqueAvailableIngredientItems(ingredients);
            _itemCount = _items.Count;
            _visitedIngredients = new bool[_ingredientCount];
            _visitedItems = new bool[_itemCount];
            _satisfied = new bool[_ingredientCount];
            _connections = new bool[_ingredientCount * _itemCount];
            _assigned = new bool[_ingredientCount * _itemCount];
        }

        public bool TryPick(int capacity, Output? output)
        {
            if (capacity <= 0) return true;
            SetInitialConnections();
            var satisfiedIngredientCount = 0;
            while (true)
            {
                var path = TryAssigningNewItem(capacity);
                if (path is null) break;
                var assignedItem = path[0];
                _owner.Take(_items[assignedItem], capacity);
                var satisfiedIngredient = path.Count - 1;
                _satisfied[path[satisfiedIngredient]] = true;
                satisfiedIngredientCount++;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    if (IsPathIndexItem(i))
                    {
                        var item = path[i];
                        var ingredient = path[i + 1];
                        Assign(item, ingredient);
                    }
                    else
                    {
                        var item = path[i + 1];
                        var ingredient = path[i];
                        Unassign(item, ingredient);
                    }
                }
            }
            var valid = satisfiedIngredientCount == _ingredientCount;
            var hasOutput = valid && output is not null;
            Array.Clear(_visitedIngredients);
            Array.Clear(_visitedItems);
            Array.Clear(_satisfied);
            for (var ingredient = 0; ingredient < _ingredientCount; ingredient++)
            {
                for (var item = 0; item < _itemCount; item++)
                {
                    if (!IsAssigned(item, ingredient)) continue;
                    Unassign(item, ingredient);
                    _owner.Put(_items[item], capacity);
                    if (hasOutput) output!.Accept(_items[item]);
                }
            }
            return valid;
        }

        public int TryPickAll(int maxSize, Output? output)
        {
            var min = 0;
            var max = Math.Min(maxSize, _owner.GetResultUpperBound(_ingredients)) + 1;
            int mid;
            while (true)
            {
                mid = (min + max) / 2;
                if (TryPick(mid, null))
                {
                    if (max - min <= 1) break;
                    min = mid;
                }
                else
                {
                    max = mid;
                }
            }
            if (mid > 0) TryPick(mid, output);
            return mid;
        }

        private void SetInitialConnections()
        {
            for (var ingredient = 0; ingredient < _ingredientCount; ingredient++)
            {
                for (var item = 0; item < _itemCount; item++)
                {
                    if (_ingredients[ingredient].AcceptsItem(_items[item])) SetConnection(item, ingredient);
                }
            }
        }

        private static bool IsPathIndexItem(int index) => (index & 1) == 0;

        private List<int>? TryAssigningNewItem(int capacity)
        {
            Array.Clear(_visitedIngredients);
            Array.Clear(_visitedItems);
            for (var item = 0; item < _itemCount; item++)
            {
                if (!_owner.HasAtLeast(_items[item], capacity)) continue;
                var path = FindNewItemAssignmentPath(item);
                if (path is not null) return path;
            }
            return null;
        }

        private List<int>? FindNewItemAssignmentPath(int startingItem)
        {
            _path.Clear();
            _visitedItems[startingItem] = true;
            _path.Add(startingItem);
            while (_path.Count > 0)
            {
                var pathLength = _path.Count;
                if (IsPathIndexItem(pathLength - 1))
                {
                    var itemToAssign = _path[pathLength - 1];
                    for (var ingredient = 0; ingredient < _ingredientCount; ingredient++)
                    {
                        if (_visitedIngredients[ingredient] || !HasConnection(itemToAssign, ingredient)
                                                            || IsAssigned(itemToAssign, ingredient)) continue;
                        _visitedIngredients[ingredient] = true;
                        _path.Add(ingredient);
                        break;
                    }
                }
                else
                {
                    var lastAssignedIngredient = _path[pathLength - 1];
                    if (!_satisfied[lastAssignedIngredient]) return new List<int>(_path);
                    for (var item = 0; item < _itemCount; item++)
                    {
                        if (_visitedItems[item] || !IsAssigned(item, lastAssignedIngredient)) continue;
                        _visitedItems[item] = true;
                        _path.Add(item);
                    }
                }
                if (_path.Count == pathLength) _path.RemoveAt(pathLength - 1);
            }
            return null;
        }

        private void SetConnection(int item, int ingredient) => _connections[item * _ingredientCount + ingredient] = true;

        private bool HasConnection(int item, int ingredient) => _connections[item * _ingredientCount + ingredient];

        private bool IsAssigned(int item, int ingredient) => _assigned[item * _ingredientCount + ingredient];

        private void Assign(int item, int ingredient) => _assigned[item * _ingredientCount + ingredient] = true;

        private void Unassign(int item, int ingredient) => _assigned[item * _ingredientCount + ingredient] = false;
    }

    //ReferenceEqualityComparer compares T by object identity, standing in for vanilla Reference2IntOpenHashMap
    private sealed class ReferenceEqualityComparer<TKey> : IEqualityComparer<TKey> where TKey : notnull
    {
        public static readonly ReferenceEqualityComparer<TKey> Instance = new();

        public bool Equals(TKey? x, TKey? y) => ReferenceEquals(x, y);

        public int GetHashCode(TKey obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
