namespace NacreCraft.Hook;

//HookOutcomeKind 一条注入规则最终的处置结果
//用枚举而不是消息字符串 工具链（ncm 检查、启动期报错窗口）需要一个稳定可序列化的键
//Applied 只在至少落地一处时使用 零命中一律归 NoAnchor 两者靠 Matches 区分
public enum HookOutcomeKind
{
    //已经落地 至少改写了一处 落了几处看 Matches
    Applied,

    //宿主方法不存在 方法名写错落在这一类
    NoSuchMethod,

    //宿主存在但锚点一处也没命中
    NoAnchor,

    //Ordinal 超出了实际命中数
    OrdinalExceeded,

    //常量值或装箱类型不匹配 例如规则写 5 而方法里是 ldc.i8 的 5L
    ConstantTypeMismatch,

    //InType / InMethod 指定的宿主在目标程序集里找不到
    HostNotFound,

    //目标没有可改写的方法体（abstract / extern）
    HostNotRewritable,

    //替换方法的签名和锚点对不上 例如声明了 locals 却没有 ctx 参数 或 ctx 没有 Cancel 字段
    SignatureMismatch,

    //目标不在本次改写窗口里 运行时注入目标未加载时是这一类
    OutOfWindow,

    //锚点落在分支或异常处理边界上 按规则主动跳过
    SkippedByBoundary,

    //同一锚点被别的 mod 先占 这条规则不生效
    ConflictLost,

    //规则本身解不出来 注解参数出现了无法解码的形式（例如枚举）
    //这一类连规则对象都建不出来 结果里 Rule 为空 看 Subject
    SkippedUndecodable,
}

//HookOutcome 一条规则的处置结果
//这是 R1 的出口：今天引擎算完就丢 只有 _rewriteCount 一个汇总量 装配层看不见哪条规则没生效
public sealed class HookOutcome
{
    //Rule 出问题的规则
    //规则本身解不出来时（SkippedUndecodable）为空 那时看 Subject
    public HookRule? Rule { get; }

    //Subject Rule 为空时用来指认出问题的地方 例如注解所在的那个方法
    public string? Subject { get; }

    public HookOutcomeKind Kind { get; }

    //Matches 实际落地处数 只有 Kind 为 Applied 时有意义
    public int Matches { get; }

    //Detail 补充说明 面向人读 不作为稳定键
    public string? Detail { get; }

    //SourceMod 规则来自哪个 mod
    //同一锚点可能被两个 mod 声明 结果要能点名到具体的那个
    public string? SourceMod { get; }

    public HookOutcome(HookRule rule, HookOutcomeKind kind, int matches = 0, string? detail = null)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        SourceMod = rule.SourceMod;
        Subject = null;
        Kind = kind;
        Matches = matches;
        Detail = detail;
    }

    private HookOutcome(string? sourceMod, string subject, HookOutcomeKind kind, string? detail)
    {
        Rule = null;
        SourceMod = sourceMod;
        Subject = subject;
        Kind = kind;
        Matches = 0;
        Detail = detail;
    }

    //ForSubject 建一条没有规则对象的结果
    //注解在扫描期就解不出来 规则无从建起 只能记下它出现在哪个 mod 的哪一处
    public static HookOutcome ForSubject(string? sourceMod, string subject, HookOutcomeKind kind, string? detail = null)
        => new(sourceMod, subject, kind, detail);

    //IsApplied 是否至少落地一处
    public bool IsApplied => Kind == HookOutcomeKind.Applied;

    //Describe 结果指向的那条规则的文字形式 Rule 为空时退回到 Subject
    public string Describe() => Rule?.ToString() ?? Subject ?? "unknown rule";

    public override string ToString()
    {
        var owner = SourceMod ?? "?";
        var detail = Detail is null ? "" : $" {Detail}";
        return $"{owner}: {Describe()} -> {Kind} (matches={Matches}){detail}";
    }
}
