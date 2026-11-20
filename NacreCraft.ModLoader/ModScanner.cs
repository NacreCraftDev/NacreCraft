using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace NacreCraft.ModLoader;

//ScannedMod: a mod entry produced by static scanning
public sealed class ScannedMod
{
    //AssemblyPath: the mod dll path
    public string AssemblyPath { get; init; } = string.Empty;
    //AssemblyName: the assembly name, used to map AssemblyRef back to the mod
    public string AssemblyName { get; init; } = string.Empty;
    //Manifest: the parse result of the embedded ncmod.json
    public ModManifest Manifest { get; init; } = new();
    //EmbeddedResources: all embedded resource names in this assembly
    //A mod's third-party dependencies are embedded as resources directly by the build; dependency resolution relies on this list
    //Listed so resolution does not need to open the file again
    public IReadOnlyList<string> EmbeddedResources { get; init; } = Array.Empty<string>();
    //ReferencedAssemblies: assembly names this assembly references
    public IReadOnlyList<string> ReferencedAssemblies { get; init; } = Array.Empty<string>();
    //AnnotatedHooks: injection rules statically scanned from Inject annotations
    //Same shape as the manifest hooks; both paths merge at assembly time, and when both declare the same injection point the annotation wins
    public IReadOnlyList<ModHookRule> AnnotatedHooks { get; init; } = Array.Empty<ModHookRule>();
    //AnnotatedMixins: mixin rules statically scanned from Mixin annotations, same shape as the manifest mixins
    public IReadOnlyList<ModMixinRule> AnnotatedMixins { get; init; } = Array.Empty<ModMixinRule>();
    //UndecodableAnnotations: annotations whose arguments could not be decoded, recorded as "where" strings
    //They never become rules, so without this they would simply not appear anywhere in the results
    public IReadOnlyList<string> UndecodableAnnotations { get; init; } = Array.Empty<string>();
}

//ModScanner: the static mod scanner
//Uses MetadataReader to read the dll's metadata tables and embedded resources without loading any assembly
//Loading an assembly by itself does not trigger reference resolution, but resolving a type inside the mod pulls up the kernel along the way
//Statically scanning the environment side first before deciding what to load is a prerequisite under a single ALC, since a loaded assembly cannot be unloaded
//Manifest v2 validation lives here; its rejections correspond to pit's ManifestChecker codes one for one, so both tools say the same thing
public static class ModScanner
{
    //ManifestResourceName: the fixed embedded resource name used for the mod declaration
    public const string ManifestResourceName = "ncmod.json";

    //InjectHostAssemblies: assemblies that can carry Inject or Mixin annotations
    //A mod that references none of them cannot have annotations, so iteration is skipped, saving the cost of scanning the attribute table of every method
    //The ModApi spelling is the legacy home, the ModLoader spelling is the contract home; both stay recognized during the annotation migration
    private static readonly string[] InjectHostAssemblies = ["NacreCraft.ModApi", "NacreCraft.ModLoader"];

    //Phases/Environments: the vocabularies of the manifest v2 contract, next to pit's ManifestChecker
    private static readonly string[] Phases = ["preLaunch", "main", "client", "server"];
    private static readonly string[] Environments = ["both", "client", "server"];

    //ScanAll: scans all dlls in the directory, skipping those without a declaration
    //problems collects the skip reasons, warnings the advisory lines; duplicate ids drop every copy
    public static List<ScannedMod> ScanAll(string folderPath, List<string>? problems = null, List<string>? warnings = null)
    {
        var scanned = new List<ScannedMod>();
        if (!Directory.Exists(folderPath))
            return scanned;

        foreach (var path in Directory.GetFiles(folderPath, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var mod = Scan(path, problems, warnings);
            if (mod is not null)
                scanned.Add(mod);
        }

        //Duplicate ids cannot be told apart, so every copy is dropped; keeping one would load by file order
        var duplicates = scanned
            .GroupBy(mod => mod.Manifest.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToList();
        foreach (var group in duplicates)
            problems?.Add($"Mod id duplicate {group.Key}: {string.Join(" vs ", group.Select(mod => mod.AssemblyPath))}, all skipped");

        var dropped = new HashSet<string>(
            duplicates.SelectMany(group => group.Select(mod => mod.AssemblyPath)),
            StringComparer.Ordinal);
        return scanned.Where(mod => !dropped.Contains(mod.AssemblyPath)).ToList();
    }

    //Scan: scans one dll, returns null when there is no embedded declaration, not a managed assembly,
    //or the manifest fails the v2 contract; the reason lands in problems when there is one
    public static ScannedMod? Scan(string dllPath, List<string>? problems = null, List<string>? warnings = null)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
                return null;

            var reader = pe.GetMetadataReader();
            var manifestBytes = ReadResource(pe, reader, ManifestResourceName);
            if (manifestBytes is null)
                return null;

            var assemblyName = reader.GetString(reader.GetAssemblyDefinition().Name);
            var referenced = reader.AssemblyReferences
                .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
                .ToList();

            //Annotation rules and manifest rules are scanned in the same pass and merged into one table at assembly time with no particular order
            var annotated = new List<ModHookRule>();
            var annotatedMixins = new List<ModMixinRule>();
            //Where an annotation could not be decoded; kept so the assembly step can report it rather than losing it
            var undecodable = new List<string>();
            if (referenced.Any(InjectHostAssemblies.Contains))
            {
                annotated = InjectScanner.Scan(reader, undecodable);
                annotatedMixins = InjectScanner.ScanMixins(reader, undecodable);
            }

            if (!ValidateManifest(manifestBytes, dllPath, problems, warnings))
                return null;

            var manifest = JsonSerializer.Deserialize<ModManifest>(manifestBytes);
            if (manifest is null || manifest.Id.Length == 0)
                return null;
            //The structural check reported unknown phases and duplicates; the typed model only ever keeps known phases, once each
            manifest.Entrypoints = SanitizeEntrypoints(manifest.Entrypoints);

            return new ScannedMod
            {
                AssemblyPath = dllPath,
                AssemblyName = assemblyName,
                Manifest = manifest,
                EmbeddedResources = ListResourceNames(reader),
                ReferencedAssemblies = referenced,
                AnnotatedHooks = annotated,
                AnnotatedMixins = annotatedMixins,
                UndecodableAnnotations = undecodable,
            };
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            return null;
        }
        catch (JsonException)
        {
            //A manifest that parses structurally but fails while binding to the typed model, for example a bad hooks entry
            problems?.Add($"Mod manifest failed to parse: {dllPath}");
            return null;
        }
    }

    //ValidateManifest: the structural half of the manifest v2 contract, checked on the parsed document
    //before the typed model, so every rejection names the field rather than a deserialize stack; warnings never block
    private static bool ValidateManifest(byte[] manifestBytes, string dllPath, List<string>? problems, List<string>? warnings)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(manifestBytes);
        }
        catch (JsonException)
        {
            problems?.Add($"Mod manifest is not valid JSON: {dllPath}");
            return false;
        }

        using (document)
            return ValidateRoot(document.RootElement, dllPath, problems, warnings);
    }

    //ValidateRoot: one manifest document against the v2 field rules; false means the mod is skipped
    private static bool ValidateRoot(JsonElement root, string dllPath, List<string>? problems, List<string>? warnings)
    {
        var ok = true;

        if (!root.TryGetProperty("schemaVersion", out var schema))
            Fail("Mod manifest schemaVersion missing");
        else if (schema.ValueKind != JsonValueKind.Number || schema.GetDouble() != 2)
            Fail($"Mod manifest schemaVersion {schema.GetRawText()} unsupported (2 required)");

        if (!TryText(root, "id", out var id))
            Fail("Mod manifest id empty");
        else if (!IsWellFormedId(id))
            Warn($"Mod manifest id \"{id}\" should start with a lowercase letter and use only lowercase letters, digits, \"-\", \"_\" and \".\"");

        if (!TryText(root, "version", out _))
            Fail("Mod manifest version empty");

        if (root.TryGetProperty("environment", out var environment)
            && (environment.ValueKind != JsonValueKind.String
                || !Environments.Contains(environment.GetString(), StringComparer.OrdinalIgnoreCase)))
            Warn($"Mod manifest environment \"{environment.GetRawText()}\" is not one of both, client or server, treated as both");

        if (root.TryGetProperty("entrypoints", out var entrypoints))
        {
            if (entrypoints.ValueKind != JsonValueKind.Object)
                Fail("Mod manifest entrypoints has to be an object mapping a phase to type names");
            else
                foreach (var phase in entrypoints.EnumerateObject())
                {
                    if (!Phases.Contains(phase.Name, StringComparer.Ordinal))
                    {
                        Warn($"Mod manifest entrypoints \"{phase.Name}\" is not an entrypoint phase, ignored");
                        continue;
                    }

                    if (phase.Value.ValueKind != JsonValueKind.Array)
                    {
                        Fail($"Mod manifest entrypoints.{phase.Name} has to be a list of type names");
                        continue;
                    }

                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    var index = 0;
                    foreach (var item in phase.Value.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                            Fail($"Mod manifest entrypoints.{phase.Name}[{index}] has to be a non-empty type name");
                        else if (!seen.Add(item.GetString()!))
                            Warn($"Mod manifest entrypoints.{phase.Name} lists \"{item.GetString()}\" more than once, kept once");
                        index++;
                    }
                }
        }

        ok &= CheckTable(root, "depends", dllPath, problems);
        ok &= CheckTable(root, "breaks", dllPath, problems);
        ok &= CheckTable(root, "conflicts", dllPath, problems);

        if (root.TryGetProperty("provides", out var provides))
        {
            if (provides.ValueKind != JsonValueKind.Array)
                Fail("Mod manifest provides has to be a list of mod ids");
            else
            {
                var index = 0;
                foreach (var item in provides.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                        Fail($"Mod manifest provides[{index}] has to be a non-empty mod id");
                    index++;
                }
            }
        }

        if (root.TryGetProperty("entry", out _))
            Warn("Mod manifest has removed field \"entry\", ignored");

        return ok;

        void Fail(string message)
        {
            ok = false;
            problems?.Add($"{message}: {dllPath}");
        }

        void Warn(string message) => warnings?.Add($"{message}: {dllPath}");
    }

    //CheckTable: depends/breaks/conflicts all map a mod id to a version constraint string
    private static bool CheckTable(JsonElement root, string name, string dllPath, List<string>? problems)
    {
        if (!root.TryGetProperty(name, out var table))
            return true;

        if (table.ValueKind != JsonValueKind.Object)
        {
            problems?.Add($"Mod manifest {name} has to be an object mapping a mod id to a version constraint: {dllPath}");
            return false;
        }

        foreach (var entry in table.EnumerateObject())
        {
            if (entry.Name.Length > 0 && entry.Value.ValueKind == JsonValueKind.String)
                continue;
            problems?.Add(entry.Name.Length == 0
                ? $"Mod manifest {name} has an entry without an id: {dllPath}"
                : $"Mod manifest {name}.{entry.Name} has to be a version constraint string: {dllPath}");
            return false;
        }
        return true;
    }

    //TryText: reads a required string field, false when it is absent, of another kind or blank
    private static bool TryText(JsonElement root, string name, out string text)
    {
        text = string.Empty;
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            return false;

        text = value.GetString() ?? string.Empty;
        return text.Length > 0 && !string.IsNullOrWhiteSpace(text);
    }

    //IsWellFormedId matches the suggested shape: a lowercase letter first, then lowercase letters, digits and separators
    private static bool IsWellFormedId(string id)
    {
        if (id.Length == 0 || !char.IsAsciiLetterLower(id[0]))
            return false;

        foreach (var character in id)
        {
            if (char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character)
                || character is '-' or '_' or '.')
                continue;
            return false;
        }
        return true;
    }

    //SanitizeEntrypoints: keeps the four known phases and deduplicates each phase's type list
    //The structural check already reported what was dropped
    private static Dictionary<string, string[]> SanitizeEntrypoints(Dictionary<string, string[]> entrypoints)
    {
        var cleaned = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (phase, types) in entrypoints)
        {
            if (!ModPhases.IsKnown(phase))
                continue;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            cleaned[phase] = types.Where(type => seen.Add(type)).ToArray();
        }
        return cleaned;
    }

    //ReadEmbeddedResource: reads any embedded resource bytes, used by dependency resolution and diagnostics
    //Does not swallow exceptions; the caller handles a non-managed assembly or a missing resource
    public static byte[]? ReadEmbeddedResource(string dllPath, string resourceName)
    {
        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        return pe.HasMetadata ? ReadResource(pe, pe.GetMetadataReader(), resourceName) : null;
    }

    //ListResourceNames: lists embedded resource names, resource entries pointing at external files are excluded
    private static List<string> ListResourceNames(MetadataReader reader)
    {
        var names = new List<string>();
        foreach (var handle in reader.ManifestResources)
        {
            var resource = reader.GetManifestResource(handle);
            if (resource.Implementation.IsNil)
                names.Add(reader.GetString(resource.Name));
        }
        return names;
    }

    //ReadResource: reads embedded resource bytes, returns null when the resource is missing or points at an external file
    //ManifestResource.Offset is an offset relative to the CLI resources directory RVA, not a file offset
    //The data in the directory begins with a 4-byte length, so it must be read from the block returned by GetSectionData(resources directory RVA) at that offset
    private static byte[]? ReadResource(PEReader pe, MetadataReader reader, string resourceName)
    {
        var resourcesRva = pe.PEHeaders.CorHeader?.ResourcesDirectory.RelativeVirtualAddress ?? 0;
        if (resourcesRva == 0)
            return null;

        var block = pe.GetSectionData(resourcesRva);
        foreach (var handle in reader.ManifestResources)
        {
            var resource = reader.GetManifestResource(handle);
            if (!resource.Implementation.IsNil)
                continue;
            if (reader.GetString(resource.Name) != resourceName)
                continue;

            var offset = (int)resource.Offset;
            var length = block.GetReader(offset, sizeof(int)).ReadInt32();
            return block.GetContent(offset + sizeof(int), length).ToArray();
        }
        return null;
    }
}
