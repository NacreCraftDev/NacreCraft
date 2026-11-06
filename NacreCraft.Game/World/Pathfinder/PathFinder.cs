using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Pathfinder;

//PathFinder A* over the node evaluator, maps to vanilla net.minecraft.world.level.pathfinder.PathFinder
//The profiler instrumentation is not ported; the search itself keeps the vanilla order and cost rules
public class PathFinder
{
    private const float Fudging = 1.5f;
    private readonly NodeEvaluator _nodeEvaluator;
    private readonly Node[] _neighbors = new Node[32];
    private readonly BinaryHeap _openSet = new();
    private int _maxVisitedNodes;
    private Func<bool> _captureDebug = () => false;

    public PathFinder(NodeEvaluator nodeEvaluator, int maxVisitedNodes)
    {
        _nodeEvaluator = nodeEvaluator;
        _maxVisitedNodes = maxVisitedNodes;
    }

    public void SetCaptureDebug(Func<bool> captureDebug) => _captureDebug = captureDebug;

    public void SetMaxVisitedNodes(int maxVisitedNodes) => _maxVisitedNodes = maxVisitedNodes;

    //FindPath searches from the mob to any of the target positions, maps to vanilla findPath
    //Null means no path was found at all, same as the vanilla empty Optional
    public Path? FindPath(PathNavigationRegion level, Mob entity, IReadOnlySet<BlockPos> targets,
        float maxPathLength, int reachRange, float maxVisitedNodesMultiplier)
    {
        _openSet.Clear();
        _nodeEvaluator.Prepare(level, entity);
        var from = _nodeEvaluator.GetStart();
        if (from is null) return null;
        var targetsByNode = new Dictionary<Target, BlockPos>();
        foreach (var pos in targets)
            targetsByNode[_nodeEvaluator.GetTarget(pos.X, pos.Y, pos.Z)] = pos;
        var path = FindPath(from, targetsByNode, maxPathLength, reachRange, maxVisitedNodesMultiplier);
        _nodeEvaluator.Done();
        return path;
    }

    private Path? FindPath(Node from, Dictionary<Target, BlockPos> targetMap, float maxPathLength,
        int reachRange, float maxVisitedNodesMultiplier)
    {
        var targets = new HashSet<Target>(targetMap.Keys);
        from.G = 0.0f;
        from.H = GetBestH(from, targets);
        from.F = from.H;
        _openSet.Clear();
        _openSet.Insert(from);
        var captureDebug = _captureDebug();
        var closedSet = captureDebug ? new HashSet<Node>() : null;
        var count = 0;
        var reachedTargets = new HashSet<Target>();
        var maxVisitedNodesAdjusted = (int)(_maxVisitedNodes * maxVisitedNodesMultiplier);
        while (!_openSet.IsEmpty())
        {
            count++;
            if (count >= maxVisitedNodesAdjusted) break;
            var current = _openSet.Pop();
            current.Closed = true;
            foreach (var target in targets)
            {
                if (current.DistanceManhattan(target) <= reachRange)
                {
                    target.SetReached();
                    reachedTargets.Add(target);
                }
            }
            if (reachedTargets.Count > 0) break;
            if (captureDebug) closedSet!.Add(current);
            if (current.DistanceTo(from) < maxPathLength)
            {
                var neighborCount = _nodeEvaluator.GetNeighbors(_neighbors, current);
                for (var i = 0; i < neighborCount; i++)
                {
                    var neighbor = _neighbors[i];
                    var distance = Distance(current, neighbor);
                    neighbor.WalkedDistance = current.WalkedDistance + distance;
                    var tentativeGScore = current.G + distance + neighbor.CostMalus;
                    if (neighbor.WalkedDistance < maxPathLength
                        && (!neighbor.InOpenSet() || tentativeGScore < neighbor.G))
                    {
                        neighbor.CameFrom = current;
                        neighbor.G = tentativeGScore;
                        neighbor.H = GetBestH(neighbor, targets) * Fudging;
                        if (neighbor.InOpenSet())
                        {
                            _openSet.ChangeCost(neighbor, neighbor.G + neighbor.H);
                        }
                        else
                        {
                            neighbor.F = neighbor.G + neighbor.H;
                            _openSet.Insert(neighbor);
                        }
                    }
                }
            }
        }
        Path? path;
        if (reachedTargets.Count > 0)
        {
            path = null;
            foreach (var target in reachedTargets)
            {
                var candidate = ReconstructPath(target.GetBestNode(), targetMap[target], true);
                if (candidate is null) continue;
                if (path is null || candidate.GetNodeCount() < path.GetNodeCount()) path = candidate;
            }
        }
        else
        {
            path = null;
            foreach (var target in targets)
            {
                var candidate = ReconstructPath(target.GetBestNode(), targetMap[target], false);
                if (candidate is null) continue;
                if (path is null
                    || candidate.DistToTarget < path.DistToTarget
                    || (candidate.DistToTarget == path.DistToTarget && candidate.GetNodeCount() < path.GetNodeCount()))
                    path = candidate;
            }
        }
        if (path is not null && captureDebug)
            path.SetDebug(_openSet.GetHeap(), closedSet!.ToArray(), targets);
        return path;
    }

    protected virtual float Distance(Node from, Node to) => from.DistanceTo(to);

    private static float GetBestH(Node from, IEnumerable<Target> targets)
    {
        var bestH = float.MaxValue;
        foreach (var target in targets)
        {
            var h = from.DistanceTo(target);
            target.UpdateBest(h, from);
            bestH = Math.Min(h, bestH);
        }
        return bestH;
    }

    private static Path? ReconstructPath(Node? closest, BlockPos target, bool reached)
    {
        if (closest is null) return null;
        var nodes = new List<Node>();
        var node = closest;
        nodes.Insert(0, node);
        while (node.CameFrom is not null)
        {
            node = node.CameFrom;
            nodes.Insert(0, node);
        }
        return new Path(nodes, target, reached);
    }
}
