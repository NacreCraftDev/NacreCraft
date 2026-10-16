# NacreCraft Injection Guide

How to put your own code inside the kernel: declaring rules, what each injection form can reach, and where the boundaries are.

Every rule has two declaration routes — a `hooks` / `mixins` entry in `ncmod.json`, or an `[Inject]` / `[Mixin]` annotation in source — and both end up in the same rule table. This document covers only injection; for events, the `Nc*` facades and the rest of the public API see [mod-api.md](mod-api.md), and for the project, manifest and deployment side see [modding-guide.md](modding-guide.md).

Reach for injection **last**. `NacreCraft-ModApi` already turns most kernel activity into events, and the wrapper layer covers the common actions; write your own rule only when you need a place in the kernel that no event exposes yet. From here on, "kernel" also means any other mod under `mods/` — the same rules apply to both.

---

## 1. Injection styles: annotations or manifest, pick one

Fabric's Mixin is annotated **in source**:

```java
@Inject(method = "tick", at = @At("HEAD"))
private void onTick(CallbackInfo ci) { ... }
```

NC supports both styles, but their prerequisites differ: the annotation form relies on `InjectAttribute` in `NacreCraft.ModApi.Extension`, so a mod that does not reference it cannot use annotations; the manifest form is pure data written in `ncmod.json` and requires no reference for injection rules.

**Annotation**, placed on your own replacement method:

```csharp
[Inject(typeof(DedicatedServer), nameof(DedicatedServer.Tick),
        HookType = "Mark", Label = "server_tick", Environment = "server")]
public static void OnTick(object self) { ... }
```

**Manifest**, written in `hooks` in `ncmod.json`:

```json
{
  "target": "NacreCraft.Game.Server.DedicatedServer",
  "method": "Tick",
  "type": "Mark",
  "label": "server_tick",
  "environment": "server",
  "replaceType": "MyMod.Probe",
  "replaceMethod": "OnTick"
}
```

During assembly the two routes merge into one rule table, and **if the same injection point is written in both, the annotation wins**. After merging they are indistinguishable; the difference is in prerequisites and ergonomics:

| | Annotation | Manifest |
| --- | --- | --- |
| Where it is written | on the replacement method | in `hooks` in `ncmod.json` |
| Prerequisite | must reference `NacreCraft.ModApi` | none, pure data |
| Type names | `typeof` / `nameof`, checked by the compiler | hand-written strings; typos are only found during assembly |
| What it can carry | injection rules only | mod identity (id, entry, environment, display info) and injection rules |

So `ncmod.json` must be written whether or not you use annotations; it is the sole source of mod identity. Annotations only make rules less error-prone to write. The manifest has no dependency field; dependency relationships are inferred from assembly references (see [modding-guide.md 6.1](modding-guide.md#61-mod-dependency--injecting-the-depended-on-mod)) and need no declaration.

Conversely, **a mod that does not reference `NacreCraft.ModApi` can only use the manifest** — this affects more than injection rules: events like `ServerEvents` and the `Nc*` facades are also in ModApi (under `NacreCraft.ModApi.Wrapper`), so a mod that cannot use annotations also cannot use them.

The differences from Fabric remain:

- **How and when changes happen**: Mixin has a transformer **mix members of the mixin class into** the target class at **class load**, so what gets loaded is a synthesized new class and the original no longer exists; NC rewrites the target method's instructions in place **before the assembly enters memory**, so the class is still the same class, only its method body changes. Both rewrite at load time, neither modifies bytecode at compile time — Mixin's annotation processor only generates a refmap (obfuscation mapping) and performs validation at build time, while NC is not obfuscated and has no such layer at all.
- Mixin can inject at **any position in the middle of a method body**; NC can target a specific call site, field access, construction, local variable read/write, or constant within a specified host method, and can insert before or after it (`InType`/`InMethod` narrow the scope, `Placement` decides insert or replace), but it **cannot reach an arbitrary line number** and cannot change a jump target. Rewriting one argument of a call is reachable through `CallArg` (see [9](#9-modifying-a-single-call-argument)).
- Mixin targets use a string method name plus descriptor; NC uses "full type name + method name", so same-name overloads all match, and precision to a single one requires `InType`/`InMethod`.

**Which layer handles annotations**: the annotation type (`InjectAttribute`) is provided by `NacreCraft.ModApi.Extension`, and it is resolved by `NacreCraft.ModLoader` — when scanning mods it statically reads the `CustomAttribute` table with `MetadataReader`, without loading assemblies. **`NacreCraft.Hook` does not recognize annotations**; it only sees the merged rule table, and the native injection layer recognizes only the description bytes compiled on the managed side, not even reading `ncmod.json`.

This determines what annotations can express: what you can write depends entirely on which fields `InjectAttribute` has. Currently there are fifteen — target type, method name, `HookType`, `PatchMode`, `Label`, `Environment`, `Ordinal`, `ArgumentIndex`, `SliceFrom`, `SliceTo`, and the five covered in [4](#4-narrowing-to-one-site-host-scoping-and-placement) and [5](#5-in-method-body-anchors-local-variables-and-constants): `InType`, `InMethod`, `Placement`, `LocalIndex`, `ConstantValue`. The manifest carries the same set plus `replaceType` / `replaceMethod`, which an annotation takes from the method it sits on.

**Where a rule goes, and when it lands.** A mod never hands a rule to anyone. `ncmod.json` is read as an embedded resource and annotations as rows of the `CustomAttribute` metadata table, both **statically**, without loading a single assembly. The loader merges the two lists into one rule table, rewrites the kernel assemblies with it, and preloads the results — and only after that are mods loaded and each entry class's `public Task Init()` called.

So registering a rule from `Init()` is not merely late: by then every target assembly has been rewritten and is already in memory, and the loader accepts no rule submission from mod code in the first place. The C# spellings that appear in this document — `new HookRule(...)`, `HookBuilder` — are `NacreCraft.Hook`'s own API, which NC's loader never calls; they are used below to explain what the fields mean. In a mod the same rule is always a `hooks` entry or an `[Inject]` annotation.

One caveat on the annotation side: `Placement` is written as a **string** (`"Replace"` / `"Before"` / `"After"`), not an enum — the scanner decodes the metadata table by name, and an enum-typed argument makes it skip the whole rule. In the manifest, `constantValue` has no type tag either: a whole number is taken as `int` while it fits, then `long`, and a fractional one as `double`, so matching a `long` constant on `ldc.i8` needs the annotation spelling `ConstantValue = 5L`.

A second caveat: `Ordinal`, `ArgumentIndex` and `LocalIndex` are **written as plain ints, but declared as `object`** on `InjectAttribute`. C# does not allow `Nullable<T>` as an attribute parameter type, so declaring them `int?` would make `[Inject(..., Ordinal = 1)]` fail to compile with `CS0655` and the rule would be inexpressible from an annotation at all. Write them like any other int — `Ordinal = 1` — and the scanner reads the boxed value; there is nothing to declare explicitly and no sentinel to remember. The C# API keeps them as `int?` because it has no such restriction.

For the fourteen injection forms see the [appendix](#15-appendix-hooktype-overview).

## 2. An important constraint: probe classes must not carry kernel types in signatures

NC rewriting happens **before** the kernel assemblies are loaded. When assembling rules, `NacreCraft.Hook` uses reflection to find your replacement method and build a method reference, and this process resolves every parameter type and return type in the signature.

Therefore: **a replacement method's signature may only use BCL types and `object`**. Once a `NacreCraft.*` type appears in the signature, resolving it pulls up the kernel assemblies early and injection fails outright.

When you need a kernel object, declare the parameter as `object` and cast inside the method body:

```csharp
//assembly only sees object; the method body is JIT-compiled after the kernel starts
public static void OnCommandsReady(object dispatcher)
{
    var typed = (CommandDispatcher<CommandSourceStack>)dispatcher;
    ...
}
```

## 3. CallSite is replacement, not insertion

A `CallSite` rule's replacement method **replaces** the original call, so the original method is not executed. To preserve the original behavior you must restore it yourself in the replacement method:

```csharp
public static void OnCommandsReady(object dispatcher)
{
    var typed = (CommandDispatcher<CommandSourceStack>)dispatcher;
    EffectCommand.Register(typed);              // restore the replaced call
    ServerEvents.CommandRegister.Publish(...);  // then add the mod's own logic
}
```

Miss this step and the original functionality disappears entirely.

A few details about carrying this out:

- **`this` on an instance method counts as a parameter too**. Whether the target method is an instance method determines whether the replacement method needs an extra leading parameter. In IL both `call` and `callvirt` count as instance calls — for a non-virtual method on a `sealed` type the compiler emits `call`.
- **One rule key covers all overloads under that class name**; overloads with the same parameter count share one replacement method. `Disconnect(string)` and `Disconnect(Component)` share this way, and the replacement method dispatches by the real argument type.
- **A private method cannot be called from outside**, so the replacement method cannot restore the original call. Either give up this hook point or call it once via reflection (acceptable at low frequency).
- **Value-type parameters and return values cannot be declared as `object`**, `object` is a reference on the stack while `float`/`bool` are values, and a mismatch is invalid IL. Keep these two positions as their real types.
- **Unicast callbacks cannot be assigned directly**. Some kernel callback properties (e.g., the three chunk callbacks on `ServerChunkCache`) are `Action<T>` rather than `event`, and the kernel already occupies them. A mod assigning directly overrides the kernel's copy with no error at all. The correct approach is to hook the property's setter and, at the moment of assignment, chain your logic and the kernel callback into one wrapper delegate.

## 4. Narrowing to one site: host scoping and placement

**Insert mode changes what the callback receives.** With `Placement` at `Before` or `After`, the replacement is called with the **host method's** parameters (`this` included for an instance host), not the arguments of the anchored call; the anchored call itself still runs and never has to be restored. That also makes insertion a per-host affair: two call sites in two different host methods would need two callbacks with two different signatures, so pair it with `InType` / `InMethod` when the anchor appears in more than one host.

An instruction-level rule's default scope is **the entire assembly** — every place that calls the target method or reads/writes the target field matches. To narrow to one site, use two optional parameters:

| Parameter | Effect |
| --- | --- |
| `InType` / `InMethod` | match anchors only inside the specified host method body; both empty means unrestricted |
| `Placement` | `Replace` replaces the anchor (default); `Before` / `After` keep the anchor and insert one call before or after it |
| `Ordinal` | when the same anchor matches multiple places in the host method, pick which one, 0-based. Omitted means every place is modified |
| `SliceFrom` / `SliceTo` | fence the matches to the stretch between the first call to one method and the first call to another, each written `"Full.Type::Method"`. Either end may be omitted |

```csharp
//example: instrument only when LevelChunk reads block state; PalettedContainer::Get elsewhere is untouched
new HookRule("NacreCraft.Storage.PalettedContainer", "Get", typeof(MyProbe), nameof(MyProbe.OnGet),
    HookType.CallSite, PatchMode.ILRewrite,
    inType: "NacreCraft.Storage.LevelChunk", inMethod: "GetBlockState",
    placement: HookPlacement.Before)
```

The two modes impose different requirements on the callback signature:

- **Replace mode** aligns with the arguments of the replaced call (including `this` for instance calls); whether the callback restores the original call is up to you.
- **Insert mode** passes the **host method's parameters** (including `this`), consistent with the `MethodBody` convention. Insertion does not disturb the stack the anchor has already built up; the original call runs as usual, just with one extra callback before or after it.

A few boundaries:

- `InType` and `InMethod` are independent; you may specify just one. Both empty is equivalent to no scoping.
- Multiple rules may hook the same anchor, each scoped to a different host; **the first host match wins**.
- `Placement` only applies to instruction-level forms (`CallSite`, `NewObj`, field read/write, `TypeCheck`, `Box`, `FunctionPointer`, and the three kinds in [5](#5-in-method-body-anchors-local-variables-and-constants)); `MethodBody` always replaces the whole thing.
- `Ordinal` counts the **order of matches**, regardless of whether that site is ultimately modified; if the rule does not occur enough times in the host method, the rule does not land. Same idea as Mixin's `@At(ordinal)`.
- `SliceFrom` / `SliceTo` fence the search instead of counting it, corresponding to Mixin's `@Slice`. Occurrences outside the fence are not even counted, so `Ordinal` numbers from inside the slice. Prefer a fence when the host calls the same anchor several times: a count shifts as soon as someone edits the method above the anchor, a fence does not. An end whose anchor is missing anywhere in the method makes the rule land nowhere — it fails silently rather than widening back to the whole body.
- `InType` / `InMethod` / `Placement` / `LocalIndex` / `ConstantValue` are available from `ncmod.json` and `[Inject]` alike ([1](#1-injection-styles-annotations-or-manifest-pick-one)); `placement` is the only one whose annotation spelling is a string rather than an enum.

## 5. In-method-body anchors: local variables and constants

`LocalIndex` and `ConstantValue` come from the rule on both routes ([1](#1-injection-styles-annotations-or-manifest-pick-one)); without one of them the three forms below have no position to anchor to, so any rule using them has to carry it.

The previous kinds anchor to a **referenced entity** (a method, field, or constructor), whereas `LocalRead` / `LocalWrite` / `Constant` anchor to **a position inside the host method body**, corresponding to Mixin's `@ModifyVariable` and `@ModifyConstant`. For these three, `OriginalType` / `OriginalMethod` name the **host method**, not a referenced entity.

| Form | Extra parameter | Selected positions |
| --- | --- | --- |
| `LocalRead` | `LocalIndex` | every read of that slot (0-based) |
| `LocalWrite` | `LocalIndex` | every write to that slot |
| `Constant` | `ConstantValue` | every load of that constant, compared by boxed type |

```csharp
//example: insert one callback before the write to slot 0 in G
new HookRule("TargetLib.Host", "G", typeof(MyProbe), nameof(MyProbe.OnWrite),
    HookType.LocalWrite, PatchMode.ILRewrite,
    localIndex: 0, placement: HookPlacement.Before)

//example: replace the constant 5 in G with the return value of OnConst()
new HookRule("TargetLib.Host", "G", typeof(MyProbe), nameof(MyProbe.OnConst),
    HookType.Constant, PatchMode.ILRewrite, constantValue: 5)
```

In replace mode the callback signature aligns with the **instruction's stack effect**, not the host parameters:

| Anchor | Stack effect | Replacement method signature |
| --- | --- | --- |
| `LocalRead` | pushes one value | zero parameters, returns that value |
| `LocalWrite` | pops one value | one parameter |
| `Constant` | pushes one value | zero parameters, returns that value |

`ConstantValue` is compared by boxed type, so `5` (int) and `5L` (long) are two different anchors; to match `ldc.i8` you must pass `long`.

A slot is the compiled local variable index; the same source may change it under a different compiler version, so do not treat it as a stable identifier when porting across versions.

## 6. Runtime injection: modifying already-running code

The injection discussed so far all happens **before assembly load** — bytes are rewritten first, then handed to the runtime. The premise is that the target assembly has not been loaded yet.

`NacreCraft.Hook` has another route: using the CLR's Profiler interface (ReJIT) to modify code that is **already loaded, or whose methods have already run**. Both share the same `HookRule`, and the parameters from [4](#4-narrowing-to-one-site-host-scoping-and-placement) and [5](#5-in-method-body-anchors-local-variables-and-constants) remain available. The snippet below drives `NacreCraft.Hook` directly, which is not the route a mod takes ([1](#1-injection-styles-annotations-or-manifest-pick-one)); the manifest form is right after the table.

```csharp
var engine = new HookEngine();
engine.AddRule(new HookRule("TargetLib.Host", "Callee", typeof(Hooks), nameof(Hooks.Double),
    hookType: HookType.CallSite, patchMode: PatchMode.RuntimeInject, inMethod: "A"));

//one call and injection is done; no restart and no file changes
RuntimeInjector.Inject(typeof(Host).Assembly, engine);
```

| | Load-time rewriting | Runtime injection |
| --- | --- | --- |
| Manifest `patchMode` | `ILRewrite` (default) | `RuntimeInject` |
| Timing | before the assembly enters memory | any time after the process has started |
| Foundation | Mono.Cecil byte rewriting | CLR Profiler ReJIT |
| Prerequisite | the target has not been loaded | the target is already in the process |
| Modify already-JIT-compiled code | not possible | possible |

**Why rules are shared**: Cecil still does the rewriting here, but the result is not written to disk; instead it is compiled into a description handed to the native layer, which submits the new method body to the CLR at runtime, with the remaining version management left to the CLR.

**How a mod does it**: add a `patchMode` entry to the rule item in `ncmod.json`; the `[Inject]` annotation has a parameter of the same name.

```json
{ "target": "NacreCraft.Game.Server.DedicatedServer", "method": "Tick",
  "type": "CallSite", "patchMode": "RuntimeInject",
  "replaceType": "MyMod.Probe", "replaceMethod": "OnTick" }
```

```csharp
[Inject(typeof(DedicatedServer), nameof(DedicatedServer.Tick), PatchMode = "RuntimeInject")]
public static void OnTick(object self) { }
```

**What assembly does**: these rules do not go through the load-time rewriting path (`ModHooks.Rewrite` applies only `ILRewrite`); at assembly time a separate runtime target table is recorded. After the kernel assemblies are preloaded and before mods' `Init()`, the loader takes each of their **already loaded** instances and submits the rewritten method bodies to the CLR. If a target is not loaded at that moment it is skipped with a warning, and it will not be loaded early on its behalf — the constraint from [2](#2-an-important-constraint-probe-classes-must-not-carry-kernel-types-in-signatures) that "pulling up the kernel early misses the window" is reversed in direction here, with the same conclusion: if it is not present, it cannot be done.

**To hook the native injection layer**: the ReJIT switch can only be set via environment variables at process start (`CORECLR_ENABLE_PROFILING` / `CORECLR_PROFILER` / `CORECLR_PROFILER_PATH`); setting them after startup has no effect. When the loader detects `RuntimeInject` rules early during startup and the process is not yet hooked, it **restarts the process with the same command line** carrying these three variables (`NC_PROFILER_ATTACHED=1` guards against repeated restarts when "hooked but not effective"). The native library `lead_hook_native` must be placed in the program root, or pointed elsewhere via `NC_PROFILER_PATH`; if neither is present, the whole batch of rules is downgraded to a single warning and startup is not blocked.

**Do not mix it with load-time rewriting on the same target**: a method body submitted by runtime injection is derived from **original bytes** and does not include changes load-time rewriting made to the same method — if the same method is hit by both rule types, the load-time version is overwritten entirely. Assembly cannot tell whether two rules hit the same method, so it can only make a coarse judgment by target assembly and record a warning.

**Annotations do not take part in this path directly**: the native injection layer does not recognize `InjectAttribute` or read `ncmod.json` — it recognizes only description bytes. Annotations and the manifest are both **assembly-time** things (see [1](#1-injection-styles-annotations-or-manifest-pick-one)), parsed by `NacreCraft.ModLoader` into `HookRule` and then handed to `RuntimeInjector`; the mod side does not need to handle them differently.

**Limitations** (narrower than load-time rewriting): methods with exception handling tables are not supported, the local variable table cannot be changed, generic types and methods are not supported, and operands recognize only method references (field references, string constants, and type tokens throw `NotSupportedException`).

**Performance**: injection happens only at registration; afterward the method is ordinary JIT code with the same call overhead as without injection. Attaching the profiler has a one-time cost — enabling ReJIT requires disabling ReadyToRun images at the same time, and measured process startup is about 80–110 ms slower; steady-state computation shows no difference. For a server like NC whose startup is already measured in seconds, this is negligible.

## 7. Do two mods modifying the same class conflict like Mixin?

First, why things conflict on the Mixin side. Mixin **mixes members into the target class** and applies them at class load: when multiple mixins mix into the same class, cases like injecting at the same place repeatedly or adding same-named members to the same class throw `MixinApplyError`, and the default fail-hard **kills the game outright**; moreover this detection happens at the instant of class load, when the game may already be half-running.

NC's model is different, and the surface where conflicts can happen is much smaller:

| | Mixin | NacreCraft |
| --- | --- | --- |
| Landing method | mix members into the target class + rewrite bytecode | rewrite bytecode in place, and move members in through [8](#8-mixins-adding-members-to-a-target-type) |
| Structural conflicts (same-named members, inheritance conflicts) | yes | only a mixin can run into a same-named member, and there it is skipped with a warning instead of throwing; instruction rules have no structural conflict at all |
| When rules are validated | at class load | at assembly time, statically reading metadata |
| Two rules hitting the same place | throws | first come, first served; the latter silently fails |
| One mod fails | may drag down the whole load | affects only itself |

**Static validation**: rules are not built by loading assemblies and reflecting over types, but by reading PE metadata tables. So problems like "the target type is not in any known assembly" or "the injection form is misspelled" are recorded and skipped **early at startup**, without waiting for a class to load before exploding.

**Failure isolation**: when a mod's rules fail to parse, the replacement class fails to load, or the entry `Init()` throws, only **that one mod** is marked as failed (status `Error`, shown as "load failed" on the MODS page) and the other mods load as usual. One wording needs correcting here: NC has **no runtime unloading** — mods are loaded once, and `ModManager` explicitly does not offer dynamic loading/unloading. So-called "auto-unload on failure" is actually **load-time isolation**: a failed mod is not initialized, but it is also not "unloaded".

**Dynamic injection**: on the ReJIT route from [6](#6-runtime-injection-modifying-already-running-code), the semantics of several mods competing for the same method match the static case — the first registered wins, and later requests are sent but cannot claim it (`GetReJITParameters` claims by "module + method", taking the first).

We hit a pitfall on this route once, worth recording: in an early implementation, `FindTypeRef`'s **resolution scope parameter was passed as `mdTokenNil`**, whose semantics are "match only TypeRefs with no resolution scope" — our references all hang off `AssemblyRef`, so the one we had built was never found. It manifested as: the first injection succeeded, but on the second injection reference resolution failed, no new method body could be built, the CLR fell back to the original IL, and **the first injection was lost along with it** (the target method reverted to its uninjected behavior). After the fix it no longer reproduced, but the limitation remains: **metadata injection of references must complete within the window right after the target module loads; the later it is, the more likely it fails**.

**The cost must be stated clearly**: NC's non-crashing behavior comes at the cost of conflicts being easily missed — Mixin at least interrupts loading, whereas NC lets the later one silently fail. To address this, assembly performs a **same-anchor conflict check**: when the same injection point is declared by multiple mods, the later-assembled one is recorded in `ModHooks.Warnings` and reported as a warning in the startup log (without blocking loading):

```
Mod injection conflict mod my-mod-b's injection NacreCraft.Game.Server.DedicatedServer::Tick[CallSite/ILRewrite] is already taken by mod my-mod-a; this rule will not take effect
```

The detection key is "target type + method + injection form + patch mode". **Host scoping is not part of the key** — `InType`/`InMethod` can be written on both routes, but they narrow where a rule applies rather than making two rules distinct injection points, so two rules that only differ by host still share one key and the later one does not land. Rules added through `NacreCraft.Hook`'s own C# API skip this check, since those may each hit a different host and are inherently non-conflicting; no mod rule reaches the loader that way, however ([1](#1-injection-styles-annotations-or-manifest-pick-one)).

**Runtime injection goes through this check too**: it enters the same assembly entry point, and the patch mode in the detection key keeps it separate from load-time rewriting; when both types land on the same target assembly there is a separate overwrite notice (see [12](#12-two-mods-injecting-the-same-target)).

## 8. Mixins: adding members to a target type

The instruction rewriting in the previous sections cannot create anything new. To **add fields, methods, or interfaces** to a target type, use a mixin.

The relationship to Mixin's syntax is as follows:

| Mixin | NC |
| --- | --- |
| `@Mixin(X.class)` on the mixin class | `[Mixin(typeof(X))]` on the source class |
| members of the mixin class are mixed into the target class | fields and methods of the source class are moved into the target type |
| `@Unique` adds a private field | write an ordinary field in the source class; it is moved over the same way |
| `@Shadow` references an existing member of the target class | not needed; write `X`'s members directly and then hook them |
| `@Implements` / `implements` | `Interfaces` |

It can also be written in the manifest:

```json
{
  "target": "NacreCraft.Game.World.Entity.SomeEntity",
  "source": "MyMod.SomeEntityMixin",
  "interfaces": ["MyMod.ITagged"]
}
```

```csharp
[Mixin(typeof(SomeEntity), Interfaces = new[] { typeof(ITagged) })]
public class SomeEntityMixin
{
    //after being moved over, this is an instance field on the target type
    public int MyCounter = 5;

    //a mixed-in method; it reads/writes the field moved along with it
    public int Bump() => MyCounter + 1;

    //the implementation the interface requires; after being moved over the target type implements ITagged
    public string Describe() => $"tagged:{MyCounter}";
}
```

**Move, not copy**. These members are removed from the source class, leaving only an empty shell — just like Mixin's mixin class, **mod code should no longer use that class** (`new SomeEntityMixin()` or calling its methods will fail to find the members).

A few landing rules:

- **Only applies to load-time rewriting**. The target type must be in a kernel assembly or mod assembly that has not entered memory yet. Runtime injection only submits method bodies and cannot change type layout, so this form cannot exist on it.
- **Constructor initializers come along**. Values written in the source class's field initializers are merged into every instance constructor of the target type; static field initializers are merged into the static constructor (created if the target has none). The base-class chaining call inside the source class's constructor is stripped, so the base constructor is not run twice.
- **Rules with interfaces mark the moved public instance methods as virtual**. Interface dispatch only recognizes the vtable, and without the marker the CLR would determine the interface is not implemented and fail to load outright. So do not expect those methods to stay non-virtual when mixing in an interface.
- **Same-named members are skipped**. When the target type already has a field or method with the same name, that one item is not moved and the rest proceed as usual. When two mods mix into the same target type, both land, and only the name-colliding part of the latter is skipped — gentler than the "latter fails entirely" behavior of hooks in [7](#7-do-two-mods-modifying-the-same-class-conflict-like-mixin).
- **Nested types are not moved**, and nested types or generic methods in the source class are also currently outside this path's coverage.

The source type must be in **the mod's own assembly**, so neither the manifest nor the annotation writes an assembly name.

## 9. Modifying a single call argument

`CallArg` rewrites **one argument** of a call and leaves the call itself in place — corresponding to Mixin's `@ModifyArg`. Reach for it when you want to change what a kernel method is called with, without taking the call over.

```csharp
//example: H calls Sum(value, 5, 7); hand it 50 instead of 5
new HookRule("TargetLib.Host", "Sum", typeof(MyProbe), nameof(MyProbe.OnBump),
    HookType.CallArg, PatchMode.ILRewrite,
    inType: "TargetLib.Host", inMethod: "H", argumentIndex: 1)

//the callback receives the host's parameters, then the argument's original value
public static int OnBump(object self, int value, int original) => original * 10;
```

`ArgumentIndex` and the scoping fields are all available from `ncmod.json` and `[Inject]` ([1](#1-injection-styles-annotations-or-manifest-pick-one)), so the rule above is written as a `hooks` entry with `"type": "CallArg"` and `"argumentIndex": 1` exactly as it reads.

| Parameter | Meaning |
| --- | --- |
| `OriginalType` / `OriginalMethod` | the **called** method, same as `CallSite` |
| `InType` / `InMethod` / `Ordinal` / `SliceFrom` / `SliceTo` | which call site, same as [4](#4-narrowing-to-one-site-host-scoping-and-placement) |
| `ArgumentIndex` | which argument to rewrite, 0-based in stack order; `this` is argument 0 for instance calls |

Boundaries:

- **The call is always kept.** `Placement` does not apply — unlike `CallSite` in replace mode, you never have to restore the original call yourself.
- **The callback signature is host parameters plus the original argument value**, and its return value becomes the new argument. Whether the original call runs is untouched; only what it is handed changes.
- **Multi-instruction arguments work.** The argument is located by walking the IL stack backwards, so `Sum(a * 2 + 1, ...)` is taken as one unit and handed over whole. An out-of-range `ArgumentIndex` is skipped.
- **Skipped, not guessed.** If a branch or an exception-handler boundary lands right after the argument, the rule is skipped: a jump into that point would bypass the callback and run with the wrong stack.
- **The returned value must match the parameter type**, since it is stored through a temporary local of that type; a mismatch produces invalid IL rather than a silent conversion.

## 10. What can and cannot be hooked

Can be hooked: kernel assemblies under `kernel/`, and other mods under `mods/`.

Cannot be hooked:

- the main library `NacreCraft.dll`
- the loader `NacreCraft.ModLoader.dll`
- entry assemblies (`NacreCraft.ServerExe.dll`, etc.)
- `NacreCraft.Util.dll` — the loader's own logging has already loaded it by the time rewriting starts, so the startup log reports "Assembly NacreCraft.Util was already loaded early, its injection rules are too late". Everything in it is out of reach, `Log` and `CrashHandler` / `CrashReport` included; to react to a crash, anchor on a caller that does live in a rewriteable assembly, such as the `Stop()` call inside `MinecraftServer.Run`'s catch block.

Every hook's `target` must be findable in the kernel or in some mod assembly, otherwise assembly reports "the injection target is not in any known assembly". Note that **a namespace does not imply an assembly** — `NacreCraft.Game.Server.DedicatedServer` actually lives in `NacreCraft.Server.dll`; the loader looks it up by an index built from metadata tables, so just write the full name.

Mod injection of mods follows the same system: the target mod is rewritten **the moment it is itself loaded**, regardless of the order in `mods/`. Cyclic rules (A injects B and B injects A) report "cyclic loading" at load time; such rules have no solution at the IL rewriting level, so just remove one of them.

Note the injected mod **cannot be your own mod** — self-injection also counts as a cycle.

## 11. Hook rule fields

| Field | Notes |
| --- | --- |
| `target` | full name of the target type; must be in a kernel assembly or one of the mod assemblies under `mods/` |
| `method` | target method name; same-name overloads all match |
| `type` | injection form, see the [appendix](#15-appendix-hooktype-overview) |
| `patchMode` | landing method, `ILRewrite` (default) or `RuntimeInject`, see [6](#6-runtime-injection-modifying-already-running-code) |
| `placement` | how the anchor lands, `Replace` (default) / `Before` / `After`; insertion keeps the original call, see [4](#4-narrowing-to-one-site-host-scoping-and-placement) |
| `ordinal` | when the same anchor matches multiple places in the host method, pick which, 0-based, see [4](#4-narrowing-to-one-site-host-scoping-and-placement) |
| `argumentIndex` | which argument `CallArg` rewrites, 0-based; for instance calls `this` counts as 0, see [9](#9-modifying-a-single-call-argument) |
| `sliceFrom` / `sliceTo` | fence the match, written as `"TypeFullName::MethodName"`; see [4](#4-narrowing-to-one-site-host-scoping-and-placement) |
| `inType` / `inMethod` | match the anchor only inside that host method body; see [4](#4-narrowing-to-one-site-host-scoping-and-placement) |
| `localIndex` | which local slot `LocalRead` / `LocalWrite` targets, 0-based, see [5](#5-in-method-body-anchors-local-variables-and-constants) |
| `constantValue` | which constant `Constant` matches, compared by boxed type, see [5](#5-in-method-body-anchors-local-variables-and-constants) |
| `replaceType` | full name of the class containing the replacement method |
| `replaceMethod` | replacement method name |
| `label` | probe label, used only by `Mark` and `Probe` |
| `environment` | the side the rule applies to, default `both`; a rule targeting a server type run on the client has no target at all and is filtered out by `environment` |

`sourceMod` is not written by mods: the assembly step stamps every rule with the `id` of the mod that declared it, and the outcome list uses it to name who a rule belongs to.

The same rule can also be written as an annotation on the replacement method; the correspondence is:

```csharp
[Inject(typeof(SomeType), nameof(SomeType.SomeMethod),
        HookType = "CallSite", Label = "some_label", Environment = "server")]
public static void OnSomeMethod(object self) { }
```

| Manifest field | Annotation form |
| --- | --- |
| `target` | the first constructor parameter, written as `typeof(...)` |
| `method` | the second constructor parameter, preferably `nameof(...)` |
| `type` | named parameter `HookType`, default `CallSite` |
| `patchMode` | named parameter `PatchMode`, default `ILRewrite` |
| `placement` | named parameter `Placement`, **written as a string**: `"Replace"` / `"Before"` / `"After"` |
| `ordinal` | named parameter `Ordinal`, written as a plain int (property is `object`, see [1](#1-injection-styles-annotations-or-manifest-pick-one)) |
| `argumentIndex` | named parameter `ArgumentIndex`, written as a plain int |
| `sliceFrom` / `sliceTo` | named parameters `SliceFrom` / `SliceTo` |
| `inType` / `inMethod` | named parameters `InType` / `InMethod` |
| `localIndex` | named parameter `LocalIndex`, written as a plain int |
| `constantValue` | named parameter `ConstantValue`, e.g. `ConstantValue = 5L` |
| `label` | named parameter `Label` |
| `environment` | named parameter `Environment`, default `both` |
| `replaceType` | not written; taken automatically from the class it annotates |
| `replaceMethod` | not written; taken automatically from the method it annotates |

The annotations come from `InjectAttribute` in `NacreCraft.ModApi.Extension`, so mods using annotations must reference it. When annotations and the manifest both exist they are merged, and if the same injection point is declared in both **the annotation wins**.

Mixin rules use a different set of fields: `target` (full name of the target type), `source` (full name of the source type, which must be in the mod's own assembly), `interfaces` (optional, an array of interface full names); for semantics see [8](#8-mixins-adding-members-to-a-target-type). `[Mixin(typeof(target))]` on the source class is equivalent to the manifest entry.

## 12. Two mods injecting the same target

**Scenario**: two mods each declare a rule that hits the same target method (`MethodBody` form, different replacement methods).

**Conclusion**: no error, no crash; **the first-assembled rule wins and the later one silently fails**.

Both rules enter the rule table — no cross-mod deduplication is done. When rewriting, the **first** entry in the same-key list for `OriginalType::OriginalMethod` is taken; host scoping behaves the same way, with `InType`/`InMethod` being "the first match wins". Which is first is decided by a **stable order: mods are sorted by their `id` before assembly**, so the same two mods pick the same winner from run to run instead of following whatever order the filesystem handed back. There is still **no priority field and it cannot be controlled by declaring dependencies** (dependencies affect only the order of `Init()`, not the assembly of injection rules).

**Runtime injection is first-come, first-served too**: a later registration request is sent normally, but `GetReJITParameters` claims by "module + method" and always matches the first request, so the later registration does not land. Measured after a second injection, the target method's behavior stays at the first result.

**One bad rule does not affect the others**: problems like the target type not being in any known assembly or a misspelled injection form are recorded in `ModHooks.Errors` at assembly time and that entry is skipped, while other mods' rules are assembled as usual.

**Conflicts are recorded**: when assembly detects the same injection point declared by multiple mods, the later one is written to `ModHooks.Warnings` and reported at startup, naming both mods and the fact that the order came from the mod id. It is a warning, not an error, does not affect loading, and the rule itself remains in the table (just unreachable). The losing rule also comes back as `ConflictLost` in the outcome list — see [12.1](#121-per-rule-outcomes).

**Runtime injection goes through this check too**: the conflict detection key includes `patchMode`, so one mod writing `ILRewrite` and another writing `RuntimeInject` does not count as a collision (two independent paths, each doing its own thing); only two of the same mode are judged conflicting and warned. Its actual landing is likewise first-come, first-served — `GetReJITParameters` claims by "module + method", matching the first request, so later ones are sent but do not land.

**The one hard crash point**: when two mods both patch the same method in `RuntimePatch` mode, the second hits `RuntimeHookEngine`'s duplicate-registration check and throws `InvalidOperationException`, and this path is not caught, so startup fails outright. `RuntimePatch` is on its way out (see [6](#6-runtime-injection-modifying-already-running-code)); do not use it in new rules.

**Verification case**: the `modinjection` module of `NacreCraft.Test`, entries `same anchor first mod wins quietly` and `one bad rule does not sink the rest`.

### 12.1 Per-rule outcomes

Assembly no longer reports a single count. Every rule produces exactly one `HookOutcome`, exposed as `HookEngine.Outcomes` and `ModHooks.Outcomes` and read after rewriting has run. `HookOutcome` carries the `Rule`, its `SourceMod`, how many places landed (`Matches`), a human-readable `Detail`, and a `Kind`:

| Kind | Meaning |
| --- | --- |
| `Applied` | landed at least one place; `Matches` says how many |
| `NoSuchMethod` | the host type is in the assembly but has no method of that name |
| `NoAnchor` | the host exists; the anchor matched nothing |
| `OrdinalExceeded` | `ordinal` is past the number of matches |
| `ConstantTypeMismatch` | the host matched but the constant's value or boxed type differs |
| `HostNotFound` | the `inType` / `inMethod` host does not exist in the target assembly |
| `HostNotRewritable` | no method body (`abstract` / `extern`) |
| `OutOfWindow` | the runtime target was not loaded when injection ran |
| `SkippedByBoundary` | the anchor sat on a branch or exception boundary and was skipped on purpose |
| `ConflictLost` | another mod claimed the same injection point first |
| `SkippedUndecodable` | the annotation's arguments could not be decoded, so no rule was built at all |

Two properties make the list trustworthy:

- **`Applied` outranks every failure reason.** A rule first recorded as failed while one assembly was rewritten, and later landed while another was, still reports `Applied`. A finding never overwrites a success.
- **Assembly-scope attribution.** `NoSuchMethod` and `HostNotFound` are only reported when the type in question really belongs to the assembly being rewritten, so a `CallSite` rule — whose anchor is an external type — is not blamed once per assembly.

`SkippedUndecodable` is the one kind with no rule behind it: the annotation failed to decode, so there is nothing to attach. `HookOutcome.Rule` is null there and `Subject` names where it was found.

`HookResult.RulesApplied` now means the number of rules that landed **at least one place**, not the total rule count; the whole list is on `HookResult.Outcomes`.

At startup `ModBootstrap` prints one line per rule that did not land, with its source mod and reason, then a summary:

```
Mod injection not applied [nacrecraft-example] [CallSite] Some.Type::Do -> mod!Patch::On in Other::Run reason=OrdinalExceeded matches=0
Mod injection outcomes: 26 of 30 rules landed
```

Rules that did land are only counted, so a large mod set does not flood the log on every start.

## 13. Annotation injection

**Scenario**: `[Inject(typeof(X), nameof(X.M))]` on the replacement method, with no rule in `ncmod.json`.

**Conclusion**: it works and needs no manifest declaration. Annotations and the manifest share one source, are merged at assembly, and the annotation wins for the same injection point.

**Why no declaration is needed**: annotations are just the `CustomAttribute` table in metadata, and the loader already reads the same metadata when scanning mods (manifest, embedded resources, and AssemblyRef — three items), so reading one more table introduces no new loading or timing constraint. The only precondition is that the mod references `NacreCraft.ModApi` (the annotations' host).

**The premise is static reading**: reading annotations must go through `MetadataReader` and **must not use `Assembly.Load` + `GetCustomAttributes`** — the latter pulls up the mod assembly just to read rules, and the rewrite window is gone on the spot.

**`typeof` does not constitute a type reference**: what `typeof(X)` compiles into the parameter is the type's serialized name (`full name, assembly, Version=…`), which resolves to just a string and does not require `X` to be present. So writing `typeof(injected-mod)` in an annotation does **not** violate the constraint in [2](#2-an-important-constraint-probe-classes-must-not-carry-kernel-types-in-signatures) that "a replacement class must not reference the injected mod's types" — a name appearing in metadata and resolving a type at runtime are two different things.

**Verification case**: `NacreCraft.Test2` against two methods of `NacreCraft.Test1`; `Greet` goes through the annotation and `Farewell` through the manifest. Both rules are installed and both call sites are replaced:

```
Mod nacrecraft.test2 injects NacreCraft.Test1!NacreCraft.Test1.Test1Api::Greet replaced by NacreCraft.Test2.Test2Probe::OnGreet [CallSite/ILRewrite]        ← annotation
Mod nacrecraft.test2 injects NacreCraft.Test1!NacreCraft.Test1.Test1Api::Farewell replaced by NacreCraft.Test2.Test2Probe::OnFarewell [CallSite/ILRewrite]  ← manifest
Test1 internal greeting: Test2-rewritten greeting self
Test1 internal farewell: Test2-rewritten farewell self
Test2 internal greeting: Test1 original greeting test2     ← control group: the rule only rewrites the target assembly
Test2 internal farewell: Test1 original farewell test2     ← control group
```

The same case also incidentally verified that the annotation's named parameter (`Environment = "server"`) is resolved too.

## 14. The performance cost of attaching the profiler

**Scenario**: the same pure-computation program (a hundred million modulo-and-accumulate iterations), run once without the profiler and once with it attached (the three `CORECLR_ENABLE_PROFILING` environment variables). Five runs each.

**Conclusion**: no difference in steady state; the cost is entirely at startup.

| | Total process time (5 runs, ms) | In-program computation time |
| --- | --- | --- |
| without | 306 / 260 / 278 / 253 / 300 | 225 ms |
| with | 406 / 370 / 340 / 325 / 354 | 193 ms |

Total time is about 80–110 ms longer. The source is `COR_PRF_DISABLE_ALL_NGEN_IMAGES` — enabling ReJIT requires disabling ReadyToRun images at the same time, so framework code can only go through JIT; the timed loop inside the program is identical once JIT-compiled, showing no difference (the run with the profiler was actually slightly faster, which is noise).

**A waste also fixed along the way**: the initial implementation subscribed to `COR_PRF_MONITOR_JIT_COMPILATION`, a native callback fired after every method compiles, which we never used. After removing it the event mask changed from `0x80040024` to `0x80040004`; the table above is the data after removal.

**Verification case**: `__hookverify/BenchProbe`.

## 15. Appendix: HookType overview

| Form | Effect | Requirement on the replacement method signature |
| --- | --- | --- |
| `CallSite` | replace call sites to the target method with your method | parameter count matches the called method (instance call +1) |
| `MethodBody` | replace the entire target method body | matches the replaced method |
| `NewObj` | replace `new X(...)` | parameter count matches the constructor |
| `FieldRead` | instrument field reads | by the read type |
| `FieldWrite` | instrument field writes | by the write type |
| `TypeCheck` | instrument `isinst` / `castclass` | by the checked type |
| `Box` | instrument boxing/unboxing | by the element type |
| `FunctionPointer` | instrument function pointer loads | by the delegate type |
| `LocalRead` | instrument local variable reads | zero parameters, returns the variable's value |
| `LocalWrite` | instrument local variable writes | one parameter, receives the written value |
| `Constant` | instrument constant loads | zero parameters, returns the constant's value |
| `CallArg` | rewrite one argument of a call, keeping the call itself, see [9](#9-modifying-a-single-call-argument) | the host method's parameters, then the argument's original value; returns the new value |
| `Probe` | keep the original method body, instrumenting the entry and every exit; with `LabelArgumentIndex`, one argument can be folded into the label | `Begin()` returns long, `End(string, long)` |
| `Mark` | report once at method entry only, without timing | `void method(string label)` |

`Probe` and `Mark` pass only the label text (`Probe` can also include one argument's `ToString()`); they cannot obtain object references. To get actual arguments, use `CallSite`.

`InType`/`InMethod`, `Placement`, `Ordinal` and `SliceFrom`/`SliceTo` (see [4](#4-narrowing-to-one-site-host-scoping-and-placement)) are meaningful only for instruction-level forms: the ten entries in the table above other than `MethodBody`, `Probe`, `Mark` and `CallArg` can choose replace or insert-before/after, and can use `Ordinal` to pick a single occurrence; `MethodBody` always replaces the whole thing, `Probe`/`Mark` ignore these parameters, and `CallArg` always keeps the call — `ArgumentIndex` picks the argument instead.

For the three kinds `LocalRead` / `LocalWrite` / `Constant`, the host method is written in `target` rather than a referenced entity, and `localIndex` or `constantValue` is additionally required; see [5](#5-in-method-body-anchors-local-variables-and-constants).
