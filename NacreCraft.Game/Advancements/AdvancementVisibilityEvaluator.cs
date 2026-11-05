namespace NacreCraft.Game.Advancements;

//AdvancementVisibilityEvaluator evaluates which advancement nodes are visible to a player, maps to vanilla
//net.minecraft.server.advancements.AdvancementVisibilityEvaluator
//The vanilla node type AdvancementNode lives in net.minecraft.advancements, which is not ported yet; the algorithm is
//ported against a minimal node contract so it can be reused once the advancement tree lands
public static class AdvancementVisibilityEvaluator
{
    //VisibilityDepth how many ancestor rules decide the visibility of an unfinished node, maps to vanilla VISIBILITY_DEPTH
    private const int VisibilityDepth = 2;

    //Output receives every node with its computed visibility, maps to vanilla AdvancementVisibilityEvaluator.Output
    public interface Output
    {
        void Accept(IAdvancementNode advancement, bool visible);
    }

    //IAdvancementNode the minimal advancement node contract this evaluator needs
    //Maps to the parts of vanilla AdvancementNode used here: display info, children and root lookup
    public interface IAdvancementNode
    {
        //HasDisplay whether the advancement carries display info, maps to advancement.display().isPresent()
        bool HasDisplay { get; }

        //IsHidden whether the display info is hidden, maps to display().get().isHidden()
        bool IsHidden { get; }

        IReadOnlyList<IAdvancementNode> Children { get; }

        IAdvancementNode Root { get; }
    }

    private enum VisibilityRule
    {
        Show,
        Hide,
        NoChange,
    }

    //EvaluateVisibility evaluates the whole tree from the given node's root, maps to vanilla evaluateVisibility
    public static void EvaluateVisibility(IAdvancementNode node, Func<IAdvancementNode, bool> isDone, Output output)
    {
        var ascendants = new List<VisibilityRule>();
        for (var i = 0; i <= VisibilityDepth; i++) ascendants.Add(VisibilityRule.NoChange);
        EvaluateVisibility(node.Root, ascendants, isDone, output);
    }

    //EvaluateVisibility depth-first walk that pushes the current rule and reports the node afterwards, maps to the vanilla overload
    private static bool EvaluateVisibility(IAdvancementNode node, List<VisibilityRule> ascendants,
        Func<IAdvancementNode, bool> isDoneTest, Output output)
    {
        var isSelfDone = isDoneTest(node);
        var descendantVisibility = EvaluateVisibilityRule(node, isSelfDone);
        var isSelfOrDescendantDone = isSelfDone;
        ascendants.Add(descendantVisibility);
        foreach (var child in node.Children)
            isSelfOrDescendantDone |= EvaluateVisibility(child, ascendants, isDoneTest, output);
        var visible = isSelfOrDescendantDone || EvaluateVisibilityForUnfinishedNode(ascendants);
        ascendants.RemoveAt(ascendants.Count - 1);
        output.Accept(node, visible);
        return isSelfOrDescendantDone;
    }

    //EvaluateVisibilityRule picks the rule of one node, maps to vanilla evaluateVisibilityRule
    private static VisibilityRule EvaluateVisibilityRule(IAdvancementNode node, bool isDone)
    {
        if (!node.HasDisplay) return VisibilityRule.Hide;
        if (isDone) return VisibilityRule.Show;
        if (node.IsHidden) return VisibilityRule.Hide;
        return VisibilityRule.NoChange;
    }

    //EvaluateVisibilityForUnfinishedNode looks at the nearest ancestors, maps to vanilla evaluateVisiblityForUnfinishedNode
    //A shown ancestor up to the visibility depth reveals an unfinished node; a hidden one keeps it hidden
    private static bool EvaluateVisibilityForUnfinishedNode(List<VisibilityRule> ascendants)
    {
        for (var i = 0; i <= VisibilityDepth; i++)
        {
            var visibility = ascendants[ascendants.Count - 1 - i];
            if (visibility == VisibilityRule.Show) return true;
            if (visibility == VisibilityRule.Hide) return false;
        }
        return false;
    }
}
