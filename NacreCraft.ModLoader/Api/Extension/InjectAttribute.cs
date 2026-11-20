namespace NacreCraft.ModLoader.Api.Extension;

//Inject declares this method as the replacement implementation of one call site
//Equivalent to a hooks entry in ncmod.json; the two can be mixed, and when both declare the same
//injection point the annotation wins
//The replacement class and method are the class and method carrying this annotation, so those two
//fields cannot be written wrong
//The rules are read statically from metadata by the loader, same path as the manifest, so they are
//readable even when nothing is loaded and no assembly is ever pulled in early
//The ModApi spellings of these attributes stay recognized during the migration; new code uses these
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InjectAttribute : Attribute
{
    //Inject target: the type being replaced; method: the name of the method being replaced
    public InjectAttribute(Type target, string method)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Method = method ?? throw new ArgumentNullException(nameof(method));
    }

    //Target: the type being replaced
    //What ends up in the metadata is the type's serialized name, just a string; reading the rule
    //never needs that type to be loaded
    public Type Target { get; }

    //Method: the name of the method being replaced; write it with nameof so it follows renames
    public string Method { get; }

    //HookType: the injection form, one of CallSite/MethodBody/NewObj and so on, default CallSite
    public string HookType { get; set; } = "CallSite";

    //PatchMode: how the rule lands, ILRewrite or RuntimeInject, default ILRewrite
    //The anchor parameters are identical; only the timing differs. ILRewrite changes bytes before the
    //target assembly loads. RuntimeInject uses the CLR's ReJIT to change already loaded code and
    //requires the native injection layer to be attached at process start
    public string PatchMode { get; set; } = "ILRewrite";

    //Label: the probe label, used only by Probe and Mark
    public string? Label { get; set; }

    //Ordinal: when one anchor matches several places in the host method, which one to use, zero-based
    //Absent means rewrite every occurrence; when the host has fewer occurrences this rule does not land
    //Declared as object rather than int? because attribute parameter types do not allow Nullable<T>
    //It is written as a plain int and read boxed by the scanner, the same scheme as ConstantValue
    public object? Ordinal { get; set; }

    //ArgumentIndex: which argument to rewrite, zero-based; this counts as argument 0 for instance calls.
    //Used only when HookType is CallArg; the type restriction is the same as Ordinal
    public object? ArgumentIndex { get; set; }

    //SliceFrom/SliceTo: an intra-method range written as "FullTypeName::MethodName"
    //Matching narrows to between the first call to SliceFrom and the first call to SliceTo in the host
    //method; either end may be left out
    public string? SliceFrom { get; set; }

    public string? SliceTo { get; set; }

    //InType/InMethod: host restriction, matching anchors only inside this method body, both may be left out
    //LocalRead/LocalWrite/Constant do not use them: for those three the host is the constructor's type and method
    public string? InType { get; set; }

    public string? InMethod { get; set; }

    //Placement: how the anchor lands, Replace/Before/After, default Replace
    //Before and After keep the original call and insert one callback before or after it; the callback
    //receives the host method's arguments
    //A string rather than an enum, because the scanner reads names from metadata and decoding an enum
    //would skip the whole rule
    public string Placement { get; set; } = "Replace";

    //LocalIndex: the local variable slot, zero-based, used only when HookType is LocalRead or LocalWrite
    //The type restriction is the same as Ordinal
    public object? LocalIndex { get; set; }

    //ConstantValue: the constant to match, used only when HookType is Constant, compared by boxed type
    //5 and 5L are two different anchors, the former matches ldc.i4 and the latter ldc.i8
    public object? ConstantValue { get; set; }

    //Locals: the local variable slots handed to the callback, zero-based; empty keeps the old callback signature
    public List<int>? Locals { get; set; }

    //Environment: the side this rule applies to, both/client/server, default both
    public string Environment { get; set; } = "both";
}
