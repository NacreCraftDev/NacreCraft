namespace NacreCraft.ModLoader;

//SolvedMods: the dependency solver's output, the mod set the loader will actually prepare and initialize
//Mods is in dependency order; Graph carries the combined depends + AssemblyRef edges resolved to real mod ids
//EnvSkipped are mods filtered out by side, SolverSkipped the ids the solver dropped; both land in LoadResult.Skipped
public sealed class SolvedMods
{
    public required IReadOnlyList<ScannedMod> Mods { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Graph { get; init; }
    public required IReadOnlyList<string> EnvSkipped { get; init; }
    public required IReadOnlyList<string> SolverSkipped { get; init; }
}

//ModDependencies: the dependency solver
//v1 only matched versions of dependencies that happened to exist; v2 enforces the declaration:
//a missing dependency, an unmet version and a broken mod each skip the declaring mod with a named report line
//Failure stays soft: every problem is a skip plus a line, the game keeps starting
public static class ModDependencies
{
    //LoaderVirtualId: the virtual dependency standing for the loader itself
    //Its version comes from the kernel main assembly's version resource, never by loading the kernel
    public const string LoaderVirtualId = "nc-loader";

    //Solve: filters by side, enforces depends/breaks/conflicts, and orders by the union of
    //depends edges and AssemblyRef edges; a cycle skips only its members
    public static SolvedMods Solve(IReadOnlyList<ScannedMod> scanned, ModEnvironment environment, List<string> problems)
    {
        var envSkipped = new List<string>();
        var kept = new List<ScannedMod>();
        foreach (var mod in scanned)
        {
            if (mod.Manifest.Environment.Matches(environment))
                kept.Add(mod);
            else
                envSkipped.Add(mod.Manifest.Id);
        }

        var byId = new Dictionary<string, ScannedMod>(StringComparer.Ordinal);
        foreach (var mod in kept)
            byId[mod.Manifest.Id] = mod;

        //Assembly name to mod id, the same mapping the v1 dependency graph was built from
        var byAssembly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in kept)
        {
            if (mod.AssemblyName.Length > 0)
                byAssembly[mod.AssemblyName] = mod.Manifest.Id;
        }

        //Aliases may stand in for a mod id in depends; a later claim of the same alias is ignored with a line
        var aliasOwner = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mod in kept)
        {
            foreach (var alias in mod.Manifest.Provides)
            {
                if (alias == mod.Manifest.Id)
                    continue;
                if (aliasOwner.TryAdd(alias, mod.Manifest.Id))
                    continue;
                problems.Add($"Mod provides duplicate alias {alias}: {aliasOwner[alias]} and {mod.Manifest.Id}, the later one ignored");
            }
        }

        var solverSkipped = new HashSet<string>(StringComparer.Ordinal);
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        //Pass one: every mod states its depends and breaks/conflicts against the mods present
        foreach (var mod in kept)
        {
            var id = mod.Manifest.Id;
            var dropped = false;

            var deps = new List<string>();
            foreach (var (name, constraint) in mod.Manifest.Depends)
            {
                if (name == LoaderVirtualId)
                {
                    if (!VersionConstraint.Matches(constraint, LoaderVersionProvider.Read()))
                    {
                        problems.Add($"Mod dependency unmet mod {id} requires dependency {name} {constraint} but found {LoaderVersionProvider.Read()}");
                        dropped = true;
                        break;
                    }
                    continue;
                }

                if (!TryResolve(name, byId, aliasOwner, out var dependency))
                {
                    problems.Add($"Mod dependency missing {name} required by {id}");
                    dropped = true;
                    break;
                }

                if (!VersionConstraint.Matches(constraint, dependency.Manifest.Version))
                {
                    var actual = dependency.Manifest.Version.Length > 0 ? dependency.Manifest.Version : "version not declared";
                    problems.Add($"Mod dependency unmet mod {id} requires dependency {name} {constraint} but found {actual}");
                    dropped = true;
                    break;
                }

                if (dependency.Manifest.Id != id && !deps.Contains(dependency.Manifest.Id))
                    deps.Add(dependency.Manifest.Id);
            }

            if (!dropped)
            {
                foreach (var (name, constraint) in mod.Manifest.Breaks)
                {
                    if (!TryResolve(name, byId, aliasOwner, out var target))
                        continue;
                    if (!VersionConstraint.Matches(constraint, target.Manifest.Version))
                        continue;

                    problems.Add($"Mod breaks conflict {id} declares breaks {name} {constraint}, found {target.Manifest.Id} {target.Manifest.Version}; {id} skipped");
                    dropped = true;
                    break;
                }
            }

            if (!dropped)
            {
                foreach (var (name, constraint) in mod.Manifest.Conflicts)
                {
                    if (!TryResolve(name, byId, aliasOwner, out var target))
                        continue;
                    if (!VersionConstraint.Matches(constraint, target.Manifest.Version))
                        continue;

                    //A conflict is advisory: both mods keep loading, the line is the whole point
                    problems.Add($"Mod conflict warning {id} declares conflicts {name} {constraint}, found {target.Manifest.Id} {target.Manifest.Version}");
                }
            }

            if (dropped)
            {
                solverSkipped.Add(id);
                continue;
            }

            //AssemblyRef edges are load-order facts: two mods that reference each other's assemblies must load in that order
            foreach (var referenced in mod.ReferencedAssemblies)
            {
                if (!byAssembly.TryGetValue(referenced, out var owner))
                    continue;
                if (owner == id || deps.Contains(owner))
                    continue;
                deps.Add(owner);
            }
            graph[id] = deps;
        }

        //A mod that names a skipped mod in depends cannot load either; the closure runs until stable
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var mod in kept)
            {
                var id = mod.Manifest.Id;
                if (solverSkipped.Contains(id) || !graph.TryGetValue(id, out var deps))
                    continue;
                foreach (var dep in deps)
                {
                    if (!solverSkipped.Contains(dep))
                        continue;
                    problems.Add($"Mod dependency {dep} was skipped, {id} skipped too");
                    solverSkipped.Add(id);
                    changed = true;
                    break;
                }
            }
        }

        //Topological order over the kept mods; a cycle skips only its members and the rest still load
        var remaining = kept
            .Where(mod => !solverSkipped.Contains(mod.Manifest.Id))
            .Select(mod => mod.Manifest.Id)
            .ToList();
        while (remaining.Count > 0)
        {
            var order = TopologicalSort(remaining, graph);
            if (order.CycleChain is null)
            {
                remaining = order.Sorted;
                break;
            }

            problems.Add($"Mod dependency cycle {string.Join(" -> ", order.CycleChain)}; {string.Join(", ", order.CycleMembers!)} skipped");
            foreach (var member in order.CycleMembers!)
                solverSkipped.Add(member);
            remaining = remaining.Where(id => !order.CycleMembers!.Contains(id)).ToList();
        }

        var finalIds = remaining;
        var finalMods = finalIds
            .Select(id => byId[id])
            .ToList();
        var finalGraph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var id in finalIds)
            finalGraph[id] = graph.TryGetValue(id, out var deps) ? deps : (IReadOnlyList<string>)Array.Empty<string>();

        return new SolvedMods
        {
            Mods = finalMods,
            Graph = finalGraph,
            EnvSkipped = envSkipped,
            SolverSkipped = [.. solverSkipped],
        };
    }

    //TryResolve: finds a mod by id or through a provides alias; the loader's virtual id resolves to a synthetic target
    private static bool TryResolve(
        string name,
        Dictionary<string, ScannedMod> byId,
        Dictionary<string, string> aliasOwner,
        out ScannedMod dependency)
    {
        dependency = null!;

        if (name == LoaderVirtualId)
        {
            dependency = VirtualLoaderMod();
            return true;
        }

        if (byId.TryGetValue(name, out var mod))
        {
            dependency = mod;
            return true;
        }

        if (aliasOwner.TryGetValue(name, out var owner) && byId.TryGetValue(owner, out var provider))
        {
            dependency = provider;
            return true;
        }

        return false;
    }

    //VirtualLoaderMod: a synthetic ScannedMod standing for nc-loader in version checks only
    private static ScannedMod VirtualLoaderMod()
    {
        var version = LoaderVersionProvider.Read();
        return new ScannedMod
        {
            AssemblyPath = string.Empty,
            AssemblyName = LoaderVirtualId,
            Manifest = new ModManifest { Id = LoaderVirtualId, Version = version },
        };
    }

    //TopoOrder: one topological sort run; a cycle is reported as its chain and member set
    private sealed class TopoOrder
    {
        public List<string> Sorted { get; } = new();
        public List<string>? CycleChain { get; private set; }
        public List<string>? CycleMembers { get; private set; }

        public void Fail(List<string> chain, List<string> members)
        {
            CycleChain = chain;
            CycleMembers = members;
        }
    }

    //TopologicalSort: orders dependencies first; a cycle stops this run and names its members,
    //the caller removes them and sorts the remainder again
    private static TopoOrder TopologicalSort(List<string> ids, Dictionary<string, List<string>> graph)
    {
        var order = new TopoOrder();
        var visiting = new List<string>();
        var done = new HashSet<string>(StringComparer.Ordinal);

        bool Visit(string name)
        {
            if (done.Contains(name))
                return true;
            if (visiting.Contains(name))
            {
                var start = visiting.IndexOf(name);
                order.Fail(
                    [.. visiting.GetRange(start, visiting.Count - start), name],
                    [.. visiting.GetRange(start, visiting.Count - start)]);
                return false;
            }
            visiting.Add(name);
            if (graph.TryGetValue(name, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (ids.Contains(dep) && !Visit(dep))
                        return false;
                }
            }
            visiting.RemoveAt(visiting.Count - 1);
            done.Add(name);
            order.Sorted.Add(name);
            return true;
        }

        foreach (var id in ids)
        {
            if (!done.Contains(id) && !Visit(id))
                return order;
        }
        return order;
    }
}
