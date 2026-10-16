namespace NacreCraft.Resources;

//PackRepository the packs the game can offer and the subset currently selected
//maps to vanilla net.minecraft.server.packs.repository.PackRepository
//available keeps packs in id order and selected keeps selection order, lowest priority first
public sealed class PackRepository
{
    private readonly IReadOnlyList<IRepositorySource> _sources;
    private SortedDictionary<string, Pack> _available = new(StringComparer.Ordinal);
    private List<Pack> _selected = new();

    public PackRepository(params IRepositorySource[] sources) => _sources = sources;

    //Available every discovered pack in id order
    public IReadOnlyCollection<Pack> Available => _available.Values;

    //Selected the selected packs lowest priority first, so the last entry wins a resource conflict
    public IReadOnlyList<Pack> Selected => _selected;

    public IReadOnlyCollection<string> AvailableIds => _available.Keys;

    //Reload re-discovers every source and rebuilds the selection, maps to vanilla PackRepository.reload
    public void Reload()
    {
        var previouslySelected = _selected.Select(p => p.Id).ToList();
        Refresh();
        _selected = RebuildSelected(previouslySelected);
    }

    //Refresh re-discovers the sources without touching the selection
    //Callers that only re-apply a selection skip this, which is what keeps a reload from rescanning the pack folder
    public void Refresh() => _available = DiscoverAvailable();

    //SetSelected replaces the selection with the given ids, order included
    public void SetSelected(IEnumerable<string> ids) => _selected = RebuildSelected(ids.ToList());

    public Pack? GetPack(string id) => _available.GetValueOrDefault(id);

    public bool IsAvailable(string id) => _available.ContainsKey(id);

    //GetSelectedIds the selected ids as a set, maps to vanilla getSelectedIds
    public IReadOnlySet<string> GetSelectedIds() => _selected.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);

    //OpenAllSelected hands back the content accessors in selection order, maps to vanilla openAllSelected
    //NC packs already hold an opened accessor, so this is a projection rather than a fresh open per pack
    public List<PackResources> OpenAllSelected() => _selected.Select(p => p.Open()).ToList();

    //DiscoverAvailable walks every source into an id-ordered map, maps to vanilla discoverAvailable
    //A later source overwriting the same id wins, which is how a user pack can shadow a built-in one
    private SortedDictionary<string, Pack> DiscoverAvailable()
    {
        var discovered = new SortedDictionary<string, Pack>(StringComparer.Ordinal);
        foreach (var source in _sources)
        {
            source.LoadPacks(pack => discovered[pack.Id] = pack);
        }
        return discovered;
    }

    //RebuildSelected turns an id list into packs and inserts every missing required pack, maps to vanilla rebuildSelected
    private List<Pack> RebuildSelected(IReadOnlyCollection<string> selectedIds)
    {
        var result = new List<Pack>();
        foreach (var id in selectedIds)
        {
            if (_available.TryGetValue(id, out var pack)) result.Add(pack);
        }
        foreach (var pack in _available.Values)
        {
            if (!pack.IsRequired || result.Contains(pack)) continue;
            pack.DefaultPosition.Insert(result, pack, p => p.SelectionConfig, false);
        }
        return result;
    }
}
