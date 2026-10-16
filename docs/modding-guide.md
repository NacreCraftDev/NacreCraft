# NacreCraft Modding Guide

What an NC mod is, how the runtime loads and rewrites it, and what a project has to satisfy. This is the descriptive half of the documentation: it explains the model, not every API.

For the three parts of the system:

| You want to | Read |
| --- | --- |
| Subscribe to events, use the `Nc*` facades, look up an API detail | [mod-api.md](mod-api.md) |
| Put your own code inside the kernel: `hooks` / `mixins` in `ncmod.json`, `[Inject]` / `[Mixin]`, injection forms and their boundaries | [injection.md](injection.md) |
| Build a project, write the manifest, deploy, add dependencies or an icon | this document, sections [4](#4-basic-requirements-for-an-ncm) onwards |

---

## 1. What an ncm is

ncm means NacreCraft mod. An ncm is a .NET class library dll with an `ncmod.json` embedded in it, dropped into the runtime's `mods/` directory. There is no separate mod format, no build-time transformation of the kernel, and no obfuscation layer: the dll you compile is the dll that loads.

**Mods are loaded once, at startup, and never unloaded.** `ModManager` offers no dynamic loading or unloading, so a mod's `Init()` runs exactly once and the set of mods is fixed for the life of the process. Everything a mod needs to declare — identity, dependencies, injection rules — is read before a single assembly of it is loaded.

**The kernel is rewritten before it enters memory.** NC does not patch a running game. When the process starts, the loader reads every mod's metadata, assembles one rule table, and rewrites the kernel assemblies' bytes on their way in. By the time any kernel code runs, the mods are already inside it. The consequence is stated plainly in [injection.md](injection.md): a rule can only land where the target has not been loaded yet.

**Two ways a mod attaches to the kernel, and they are parallel, not layered.** `NacreCraft.ModApi` exposes two namespaces, and they answer different questions:

| Namespace | Contents | What you get |
| --- | --- | --- |
| `NacreCraft.ModApi.Wrapper` | `ServerEvents` / `ClientEvents` / `NetworkEvents`, `NcServer` / `NcWorld` / `NcPlayers` / `NcLists` / `NcRegistries` / `NcRecipes` / `NcStartup`, and object handles like `NcPlayer` / `NcLevel` | wrapper types; no kernel types on the public surface |
| `NacreCraft.ModApi.Extension` | `[Inject]` / `[Mixin]` annotations | rules bind to kernel class and method names |

- **For stability, use `Wrapper`**. The facades handle the kernel's tedious call ordering for you (writing a single block while touching the level, the player list, and the sync chain is one example), and event args are all wrapper types. The cost is that capabilities the facades do not expose are unavailable to you.
- **For completeness, use `Extension`**. Injection rules modify kernel classes and methods directly, but the target names you write are the kernel's names, so when the kernel changes the rules must change with it.

You can reference both. The `Wrapper` line is being consolidated toward "no kernel types on the public surface"; the player and level parts are done — `Player` / `Attacker` in player events and the in/out parameters of `NcPlayers` are `NcPlayer` handles, `NcWorld.Overworld` / `Nether` / `End` / `Get` and `LevelTickArgs.Level` are `NcLevel` handles, and block coordinates are plain `x y z` ints; entities and the remaining value types (`BlockPos` / `BlockState` / `Vec3`) are not wrapped yet.

**Events come first; injection is the fallback.** ModApi already turns most kernel activity — lifecycle, players, chunks, blocks, commands, ticks, inbound packets — into events, and its own probes are what deliver them. Writing your own rule is for the places no event reaches yet. Keep in mind that **the wrapper layer does not shield injection**: the `hooks` rules or `[Inject]` annotations you write bind to kernel names and break just the same when the kernel changes.

**Two ways to declare a rule, one rule table.** A rule can be data in the manifest's `hooks` / `mixins` array, or an annotation on your own class or method. Both are read statically from metadata, both land in the same table, and the annotation wins where they overlap. `ncmod.json` is not optional either way: it is the only source of mod identity. See [injection.md section 1](injection.md#1-injection-styles-annotations-or-manifest-pick-one).

---

## 2. Runtime structure first

### 2.1 Three process entry points

NC has three entry points, and the mod injection pipeline is the same for all three:

| Entry | Purpose |
| --- | --- |
| `NacreCraft.Loader` | one exe for both sides: `--server` starts the server; `--client` or no mode flag starts the client |
| `NacreCraft.ServerExe` | standalone server executable |
| `NacreCraft.ClientExe` | standalone client executable |

`Main` itself is a thin shell that only registers callbacks and hands the work to the next method. Take `NacreCraft.ServerExe`:

```csharp
public static int Main(string[] args)
{
    EmbeddedAssemblyLoader.Initialize();   // register the kernel assembly resolution callback
    BootMods(args);                        // run the mod bootstrap
    return Launch(args);                   // only now enter the business implementation
}
```

This separation is not a matter of style. When the JIT compiles a method it resolves **all types** appearing in that method body, and this happens before the method executes. If `Main` called `ServerMain.Run(args)` directly, `NacreCraft.Server.dll` would be pulled up the instant `Main` is JIT-compiled, before the mod bootstrap has run, and the rewrite window would be gone. So both `BootMods` and `Launch` must be marked `MethodImplOptions.NoInlining` — without the marker the JIT inlines them back into `Main`, defeating the split.

`NacreCraft.Loader` has the same structure, except its mode detection and mod bootstrap are both in `Launch`, and `Main` keeps only the two steps `Initialize` and `Launch`.

### 2.2 Kernel assemblies in the kernel/ subdirectory

The output directory after a build looks like this:

```
NacreCraft.ServerExe.exe
NacreCraft.dll              <- main library, embeds all lower-level sub-libraries
NacreCraft.ModLoader.dll    <- the loader itself
NacreCraft.ServerExe.dll    <- entry assembly
kernel/
  NacreCraft.Game.dll
  NacreCraft.Server.dll
  ... remaining kernel assemblies
mods/
  your-mod.dll
```

Why move them into `kernel/` instead of leaving them in the root?

The .NET host treats assemblies registered in `deps.json` as TPA (Trusted Platform Assemblies). For assemblies in the TPA, the runtime resolves **by path** — bytes passed to `AssemblyLoadContext.LoadFromStream` are simply ignored. That is, even if we feed in rewritten bytes ahead of time, the runtime still reads the unrewritten copy from disk. Only by removing the kernel assemblies from `deps.json` and moving the files away does the runtime call back into `AssemblyLoadContext.Resolving` on resolution failure, giving us the chance to hand over the rewritten bytes.

The three kinds left in the root cannot be moved: the main library (it is the embedding host and must start first), the loader itself (the bootstrap code lives in it), and the entry assembly (the apphost starts from it).

**Cost**: the entry assembly itself cannot be injected into. If your hook target happens to live in the `NacreCraft.ServerExe.dll` assembly, it is ineffective. Kernel business code is all under `kernel/`, so normally this is not a problem.

### 2.3 Mod loading sequence

```
EmbeddedAssemblyLoader.Initialize()
  └─ install the Resolving callback
BootMods → ModBootstrap.Run(current side)
  ├─ statically scan mods/*.dll (MetadataReader reads embedded ncmod.json, no assembly is loaded)
  ├─ filter out mods whose environment does not match the current side
  ├─ assemble injection rules and hand the rewriter to the main library
  ├─ preload target assemblies: read bytes → run through rewriter → LoadFromStream
  └─ call each mod entry's Init()
Launch → ServerMain/ClientMain.Run(args)
  └─ kernel business starts running; the probes are already inside
```

Note the order: **declarations are scanned, rewritten bytes are loaded, and entry code runs last**. By the time a mod's `Init()` executes, the kernel assemblies have already been replaced.

---

## 3. Key differences from Fabric

| Dimension | Fabric | NacreCraft |
| --- | --- | --- |
| Language / runtime | Java / JVM | C# / .NET 10 (CoreCLR) |
| Mod carrier | jar containing `fabric.mod.json` | dll embedding `ncmod.json` |
| Declaration reading | read a file inside the jar | `MetadataReader` statically reads embedded resources without loading assemblies |
| Code injection | Mixin (annotations in source; members are mixed into the target class at class load) | `NacreCraft.Hook` (rules declared in a manifest or annotations; bytes are rewritten in place during assembly resolution, and mixins move members into the target type) |
| Injection granularity | any line in a method body, including locals and intermediate expression values | fourteen forms (call site, single call argument, field read/write, constructor, type check, boxing, local variable, constant, whole-method-body replacement, probes, etc.), with insert-before or insert-after |
| Loading model | Fabric Loader + Knot class loader | single default ALC + `AssemblyLoadContext.Resolving` |
| Official API scope | Fabric API has a great many modules | NacreCraft-ModApi currently has only event and command extension points |

### 3.1 Concept mapping

| Fabric | NacreCraft |
| --- | --- |
| `fabric.mod.json` | the `ncmod.json` embedded in the dll |
| `ModInitializer.onInitialize()` | the entry class's `public Task Init()` |
| `@Inject` / `@Redirect` | the `[Inject]` annotation, or rules like `Mark` / `Probe` / `CallSite` in `hooks` |
| `@ModifyVariable` | `LocalRead` / `LocalWrite`, see [injection.md 5](injection.md#5-in-method-body-anchors-local-variables-and-constants) |
| `@ModifyConstant` | `Constant`, see [injection.md 5](injection.md#5-in-method-body-anchors-local-variables-and-constants) |
| `@ModifyArg` | `CallArg`, see [injection.md 9](injection.md#9-modifying-a-single-call-argument); `@ModifyArgs` (all arguments in one callback) has no equivalent |
| `@Slice` | `SliceFrom` / `SliceTo`, see [injection.md 4](injection.md#4-narrowing-to-one-site-host-scoping-and-placement) |
| `@Accessor` / `@Invoker` | `NcAccess`, reflective handles with caching, see [mod-api.md 4.3](mod-api.md#43-private-member-access); `private` members also need no visibility widening when writing a rule |
| `Registry.register(...)` | the kernel registry (`BuiltInRegistries`) |
| `ServerLifecycleEvents.SERVER_STARTED` | `ServerEvents.Started` |
| `ServerTickEvents.END_SERVER_TICK` | `ServerEvents.Tick` |
| `CommandRegistrationCallback` | `ServerEvents.CommandRegister` |
| `ClientTickEvents.END_CLIENT_TICK` | `ClientEvents.Tick` |
| `FabricLoader.getInstance().getModContainer(id)` | no equivalent yet (`ModManager` is not open to mods) |
| `@Mixin` / `@Unique` / `@Implements` | the `[Mixin]` annotation or the manifest's `mixins`, see [injection.md 8](injection.md#8-mixins-adding-members-to-a-target-type) |

### 3.2 What does not carry over

- **AccessWidener**: none. Visibility is no obstacle to IL rewriting in NC; `private` methods can be hooked the same way (the rewriter works at the byte level).
- **Yarn / Mojang mappings**: not needed. NC is C# source translated directly from vanilla, with type and method names corresponding to vanilla, only with naming style following C#.
- **The vast majority of Fabric API modules**: only capabilities covered by `NacreCraft-ModApi` are available; for the rest, write your own injection rules or wait for the API to catch up.
- **Mixin's targeting syntax**: there is no `@At`-style string, and replacing bytecode at an arbitrary line is not offered. What NC has instead, and what it can and cannot reach, is the whole subject of [injection.md](injection.md).

### 3.3 A side-by-side example

Fabric: log a line when the server starts and register a command.

```java
public class MyMod implements ModInitializer {
    @Override
    public void onInitialize() {
        ServerLifecycleEvents.SERVER_STARTED.register(server -> {
            System.out.println("server started");
        });
        CommandRegistrationCallback.EVENT.register((dispatcher, registry, env) -> {
            dispatcher.register(CommandManager.literal("mymod")
                .executes(ctx -> { ctx.getSource().sendSuccess(() -> Text.literal("hi"), false); return 1; }));
        });
    }
}
```

NC:

```csharp
public sealed class MyModEntry
{
    public Task Init()
    {
        ServerEvents.Started.Subscribe(_ => Log.Info("server started"));
        ServerEvents.CommandRegister.Subscribe(args =>
            args.Register("mymod", "my mod command", builder =>
                builder.Executes(context =>
                {
                    context.GetSource().SendSuccess("hi");
                    return 1;
                })));
        return Task.CompletedTask;
    }
}
```

Manifest (`ncmod.json`, as an embedded resource):

```json
{
  "id": "my-mod",
  "version": "0.1.0",
  "environment": "server",
  "entry": "MyMod.MyModEntry",
  "hooks": []
}
```

Note: even an empty `hooks` works here — events like `ServerEvents.Started` are provided by `NacreCraft-ModApi`'s own probes, and your mod only needs to subscribe (`ServerEvents` is under `NacreCraft.ModApi.Wrapper`). You only need to write your own hook rules when you want to hook a place in the kernel where ModApi does not yet provide an event.

---

## 4. Basic requirements for an ncm

The easiest way to start is the template:

```
dotnet new install NacreCraft.ModsProjectType
dotnet new ncm -n MyMod -e server
```

If the local package has not been published yet, use `dotnet new install <nupkg path>`, or run `dotnet pack` in the repository and then install the output.

`-e` takes `both` (default) / `server` / `client`, determining the manifest's `environment` and which side's subscription code is generated in the entry class. The template ships the NC reference assemblies, so no project reference is needed, and `ncmod.json`'s id / entry are filled in from the project name.

Starting at 4.1, the following covers what a hand-written project must satisfy. The injection rule fields themselves are in [injection.md 11](injection.md#11-hook-rule-fields).

### 4.1 Project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Private must be turned off, otherwise the NC assemblies get embedded into the mod dll by EmbedDependencies in 4.4 -->
    <ProjectReference Include="xxx\NacreCraft\NacreCraft.csproj" Private="false" />
    <ProjectReference Include="xxx\NacreCraft.ModApi\NacreCraft.ModApi.csproj" Private="false" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="ncmod.json" LogicalName="ncmod.json" />
  </ItemGroup>
</Project>
```

`LogicalName` must be `ncmod.json`; the scanner recognizes only that name.

The template takes a different route: assembly references under `libs/` (`Reference Include="libs\*.dll" Private="false"`). Follow that when you do not have the NC source. What both approaches share is that **NC's own dlls must never enter the output directory** — `EmbedDependencies` from [4.4](#44-third-party-dependencies) embeds third-party dlls from the output directory into the mod, and if NC's assemblies get embedded too there will be two sets of type identity.

### 4.2 ncmod.json fields

```json
{
  "id": "my-mod",
  "version": "0.1.0",
  "name": "My Mod",
  "description": "one-line description",
  "authors": ["someone"],
  "license": "MIT",
  "contact": { "homepage": "https://...", "sources": "https://..." },
  "icon": "icon.png",
  "environment": "both",
  "entry": "MyMod.ModEntry",
  "hooks": []
}
```

| Field | Required | Notes |
| --- | --- | --- |
| `id` | Yes | mod identifier, used by dependencies and lookups. An empty string is skipped by the scanner |
| `version` | Recommended | version number; when depended on by other mods, version constraints are judged by it, see below |
| `name` | No | display name; this is what the mod page shows, defaulting back to `id` |
| `description` | No | one-line description |
| `authors` / `contributors` | No | credits, a string array |
| `license` | No | license identifier |
| `contact` | No | external links; may take `homepage` / `sources` / `issues` |
| `icon` | No | the icon's embedded resource name, see [4.5](#45-icon-and-display-info) |
| `environment` | No | `both` / `client` / `server`, default `both`. When it does not match the current side the whole mod is not loaded |
| `entry` | Yes | full name of the entry class; the class must have `public Task Init()` |
| `depends` | No | other mods it depends on and required versions, see below |
| `hooks` | No | list of injection rules; an empty array means subscribing only to events ModApi already has. Field-by-field reference: [injection.md 11](injection.md#11-hook-rule-fields) |
| `mixins` | No | list of mixin rules; moves members of one of this mod's classes into the target type. See [injection.md 8](injection.md#8-mixins-adding-members-to-a-target-type) |

`depends` declares other mods it depends on and required versions; the key is a mod id and the value is a version constraint:

```json
{
  "depends": {
    "othermod": "^1.0.0",
    "another-mod": ">=2.1"
  }
}
```

**Dependencies not written into `depends` carry no version constraint**. Inter-mod dependencies are already inferred automatically from compile-time references (see [6.1](#61-mod-dependency--injecting-the-depended-on-mod)), and `depends` only adds version constraints on top. When the depended-on mod is not on the current side the check is skipped — that case is left to assembly resolution to report.

| Constraint syntax | Meaning |
| --- | --- |
| `*` | any version, equivalent to omitting the entry |
| `1.2.3` | segment prefix; `1.2` matches `1.2` and `1.2.9` but not `1.3` |
| `^1.2.3` | same major version and not less than the baseline; when the major version is 0 the minor version is used instead, so `0.1` and `0.2` count as incompatible |
| `>=1.2.3` | not less than the baseline |

Version numbers take only the leading digits of each segment, so `26.2-nacrecraft` participates as `26.2`. When versions do not match, **only the declaring mod is skipped** and the rest load as usual; the startup log states "dependency X requires version …, actual version …".

The judgment basis is the `version` field in the depended-on mod's manifest. So **a mod intended to be depended on must set `version` correctly** — an empty version number satisfies no specific constraint.

### 4.3 Deployment

Copy the compiled dll into the output directory's `mods/` (top level, no subdirectory recursion) and restart the process.

**This step is already automated by the build**: `DeployModToHosts` in `NacreCraft.ModApi.csproj` copies the dll into the `mods/` directory of each host project's output directory after building. The host list is the `ModHostProjects` property; when creating your own host project, just add its name. If no rule takes effect and there is no error at all, first check whether the dll in the host project's `mods/` is stale.

The startup log prints:

```
Rewrote and preloaded assembly NacreCraft.Server
Rewrote and preloaded assembly NacreCraft.Game
Mod scan finished: 1 found, injection targets NacreCraft.Server,NacreCraft.Game runtime
Mod init finished: 1 loaded, 0 skipped
```

Troubleshooting order:

- No rule takes effect: first check whether the dll in `mods/` is stale (most common).
- If you do not see `Rewrote and preloaded assembly`, the target assembly was already loaded before the bootstrap, so the rule was written too late.
- If you see `injection targets` but the target is wrong, it is usually a mistyped `target`; follow [injection.md 10](injection.md#10-what-can-and-cannot-be-hooked) to check which assembly the type belongs to.
- A rule with `environment` set to `client` is silently skipped on the server (a mismatch at the mod level means the whole mod is not loaded); this is expected behavior, not a fault.

### 4.4 Third-party dependencies

Corresponds to Fabric's Jar-in-Jar.

Projects created from the template **do not need to worry about this**: libraries added via `dotnet add package` are automatically embedded into the mod dll at build time, and when the loader cannot resolve an assembly it looks through the mod's embedded `.dll` resources.

```
dotnet add package Newtonsoft.Json
```

That's all; `ncmod.json` needs no changes.

A hand-written project must copy the `EmbedDependencies` target from the template csproj, or embed it yourself:

```xml
<ItemGroup>
  <EmbeddedResource Include="deps\MyLib.dll" LogicalName="MyLib.dll" />
</ItemGroup>
```

Dependencies are not written into the manifest; resolution only looks at the mod's embedded `.dll` resources, and it just needs the resource name to match the assembly name.

Three notes:

- **Matching is by assembly name**. The loader compares the requested assembly name: after removing `.dll`, the resource name either equals the assembly name or ends with `.` + the assembly name. So both `MyLib.dll` and the default `MyProject.deps.MyLib.dll` work.
- **Only the mod's own embedded resources are recognized**. Libraries not embedded cannot be resolved and will not be searched for elsewhere.
- **Resolution order is kernel first**. `EmbeddedAssemblyLoader` looks among kernel assemblies and embedded sub-libraries first, and only falls back to mods if nothing is found, so mods should not embed assemblies with the same name as kernel ones.

The template's target uses `WithMetadataValue` to filter `.dll` rather than writing `Condition`, because the template engine evaluates `Condition` in `.csproj` at template time, when `%(...)` has no value, and the whole line would be deleted.

### 4.5 Icon and display info

Name, description, authors, links, and icon are all written in `ncmod.json`, corresponding to `name` / `description` / `authors` / `contact` / `icon` in vanilla's `fabric.mod.json`.

**The icon is an embedded resource**, not an external file, using the same resource-naming convention as embedded dependencies:

```xml
<ItemGroup>
  <EmbeddedResource Include="icon.png" LogicalName="icon.png" />
</ItemGroup>
```

```json
{ "icon": "icon.png" }
```

The template already ships an `icon.png` with both places configured; just replace that image. A 64×64 or 128×128 PNG is recommended.

The icon lookup order is: what the manifest's `icon` points to → an embedded resource named `icon.png` → if neither exists, the UI's default image (a gray question mark).

This information is visible on the **MODS** page of the server GUI. On the left is the mod list (small icon + display name + version + status); on the right are the description, credits, links, dependencies, initialization time, and injection rules of the selected one. Mod names in the dependency column are clickable and jump straight to that entry.

Mods that failed to load or were skipped are also in the list, marked in the status column — when diagnosing "why did my mod not take effect", check this column first.

---

## 5. Known limitations

Limitations that come from the injection model itself — probe signatures, `CallSite` replacement, placement spelling, slices, mixins, runtime injection — are listed in [injection.md](injection.md) next to the feature they belong to. What follows is the rest.

- **Dlls under `mods/` are deployed automatically by `DeployModToHosts`**; when creating your own host project, remember to add its name to `ModHostProjects`.
- **Entry assemblies cannot be injected**: if your target and hook fall in an entry assembly, it is ineffective.
- **The main library and loader cannot be hooked**; this is a design constraint preventing mods from changing the loading process itself.
- **Mismatched `environment` means the whole mod is not loaded**, not "some rules fail".
- When testing and debugging, if language tables or model resources are used, an `assets` directory (extracted from the vanilla jar) is required, otherwise the related features degrade to translation keys or placeholder textures.

---

## 6. Known behaviors

This section is a record of observed behavior, not a specification. Behaviors specific to injection — annotation resolution, the profiler's cost, two mods on one target — are in [injection.md](injection.md) sections 12 to 14.

### 6.1 Mod dependency + injecting the depended-on mod

**Scenario**: b depends on a and also injects a.

**Conclusion**: it works and does not form a cycle.

The chain has three steps:

1. The rule table is built purely by reading PE metadata, without loading any assembly. The rule "b injects a" requires neither a nor b to be present.
2. `PreloadReplacers` loads all replacement classes (including b) before `ModManager`, at which point a is not loaded yet. `LoadFromStream` reads metadata only and does not JIT method bodies, so b's reference to a is lazy at this moment and loading does not fail.
3. `ModManager` then topologically sorts by `AssemblyRef`, with a before b. When rewriting a, the replacement class b is already in `Default`, so the type is fetched directly and one `AssemblyRef` pointing to b is added to a's metadata. **a does not need to know b exists at all.**

**Hard requirement**: when referencing the injected mod in csproj, you must write `Private="false"`. By default it copies `a.dll` into the output directory, which is then embedded into `b.dll` by `EmbedDependencies` as an embedded dependency, and at runtime `ModLibs` resolving `a` picks up the second copy, causing type identity checks between the two to fail.

**Verification case**: two template projects `NacreCraft.Test1` (a) and `NacreCraft.Test2` (b); a provides `Test1Api.Greet` and calls it in its own `ModEntry.Server`, while b replaces that call site with `Test2Probe.OnGreet` and also calls `Greet` once in b's `ModEntry.Server` as a control. Actual run log:

```
Mod nacrecraft.test2 injects NacreCraft.Test1!NacreCraft.Test1.Test1Api::Greet replaced by NacreCraft.Test2.Test2Probe::OnGreet [CallSite/ILRewrite]
Mod scan finished: 2 found, injection targets NacreCraft.Server,NacreCraft.Game,NacreCraft.Storage,NacreCraft.Test1 runtime
Test1 internal greeting: Test2-rewritten greeting self    ← injection took effect
Test2 internal greeting: Test1 original greeting test2    ← control group: the rule only rewrites the target assembly, so b's own internal call site is untouched
```

Dependencies derive order only from `AssemblyRef` and carry no version constraint. To constrain versions, declare them in the manifest's `depends`, see [4.2](#42-ncmodjson-fields).

### 6.2 Not yet verified

- **Runtime injection working on a real server**: `RuntimeInject` mode has been verified end-to-end in `__hookverify/RuntimeProbe` (after registration the target method's behavior is swapped to the replacement's), and the mod assembly side also has test coverage for routing and downgrade; but no build step currently places `lead_hook_native` into NC's run directory, so running this chain on a real server requires first putting the library in the program root (or pointing to it with `NC_PROFILER_PATH`). This step is not done.
- **A replacement class referencing the injected mod's types**: by reasoning, at the moment of rewriting a it would resolve a, while a is stuck just before load completion (it is not yet in `Default`, and neither `ModLibs` nor the kernel resolution callback recognizes mod assemblies), so `PrepareMod` is expected to throw and record into `result.Errors`. Not yet actually run. Note [injection.md 13](injection.md#13-annotation-injection) only proves that **`typeof` in an annotation** is not a reference; **the type appearing in a method signature** is another matter.
