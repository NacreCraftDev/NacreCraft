# NacreCraft.Hook

The assembly rewrite and runtime patching engine used by the mod loader. It maps to the mixin injection model: a rule names an anchor in existing IL and a replacement method, and the engine either rewrites the assembly before it is loaded or replaces the method body at runtime. Gameplay and rendering code never reference it directly; `NacreCraft.ModLoader` builds the rules from `ncmod.json` and annotations.

## Features

- Rule model — `HookRule` pairs an anchor (`OriginalType`/`OriginalMethod`) with a replacement (`ReplacementAssembly`/`ReplacementTypeName`/`ReplacementMethod`). Replacement assemblies are resolved lazily, by a registered loader or by a metadata-only path, so a rule table can be built before the replacement assembly exists.
- Anchor kinds — `CallSite`, `MethodBody`, `NewObj`, `FieldRead`, `FieldWrite`, `TypeCheck`, `Box` and `FunctionPointer` for member-level injection; `Probe` and `Mark` for method-entry and exit observation; `LocalRead`, `LocalWrite`, `Constant` and `CallArg` for modifying local variables, loaded constants and call arguments. Together they cover the mixin `@Inject`, `@Overwrite`, `@Redirect`, `@ModifyVariable`, `@ModifyConstant` and `@ModifyArg` cases.
- Placement — `Replace`, `Before` and `After` decide whether the anchor call is replaced or kept and wrapped by the inserted call.
- Patch modes — `ILRewrite` rewrites the assembly bytes before the first load, `RuntimePatch` patches an already compiled method in place, and `RuntimeInject` hands a new body to the runtime through the native layer's ReJIT path.
- Mixins — `MixinRule` copies members from a source type into the target type in the same rewrite pass; copied methods take part in the instruction-level rules that follow.
- Load-time rewriting — `RewritingLoadContext` reads matching assemblies by prefix, rewrites them and only then lets the runtime see them, so JIT inlining cannot hide the instrumentation.
- Runtime patching — `RuntimeHookEngine` replaces and restores methods with `Patch`/`Unpatch`, keeping the original behind a trampoline.
- Deterministic ordering — rules targeting one host are ordered by `Priority` (default `1000`, the mixin default) and then by source mod; rules displaced by a winning `MethodBody` are reported rather than silently dropped.
- Outcomes — `RewriteWithResult` returns a `HookResult` whose `Outcomes` say whether each rule landed and why not.
- Native hand-off — `NativeInjector` reaches `ncn_request_rewrite` in `NacreCraft.Native` for `RuntimeInject`, which is the only mode that can change code that has already been compiled.
