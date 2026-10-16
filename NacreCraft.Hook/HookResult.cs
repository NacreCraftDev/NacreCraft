namespace NacreCraft.Hook;

public sealed class HookResult
{
    public byte[] RewrittenAssembly { get; }

    //RewritesPerformed 本次改写落地的总处数
    public int RewritesPerformed { get; }

    //RulesApplied 至少落地一处的规则数
    //不是全部规则数：那一个数看不出哪条没生效 正是 R1 要修的地方
    public int RulesApplied { get; }

    //Outcomes 逐规则结果 哪条没生效、为什么 都在这里
    public IReadOnlyList<HookOutcome> Outcomes { get; }

    public bool HasRewrites => RewritesPerformed > 0;

    internal HookResult(byte[] rewrittenAssembly, int rewritesPerformed, int rulesApplied,
        IReadOnlyList<HookOutcome> outcomes)
    {
        RewrittenAssembly = rewrittenAssembly;
        RewritesPerformed = rewritesPerformed;
        RulesApplied = rulesApplied;
        Outcomes = outcomes;
    }
}
