using System.Threading;
using NacreCraft;
using NacreCraft.Game;
using NacreCraft.Game.Bootstrap;
using NacreCraft.Client;
using NacreCraft.Client.Language;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Dimension;
using NacreCraft.Game.World.Level.LevelGen.Synth;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Logging;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;
using NacreCraft.Resources;
using NacreCraft.Util;
using BootstrapClass = NacreCraft.Bootstrap.Bootstrap;
using GameConfiguredWorldCarver = NacreCraft.Game.World.Level.LevelGen.Carver.ConfiguredWorldCarver;
//The registry and level definitions each have their own DimensionType; the former is a marker interface, so this always refers to the real Game-layer type
using GameDimensionType = NacreCraft.Game.World.Level.LevelGen.Dimension.DimensionType;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;

namespace NacreCraft.Client; 

//ClientMain client main entry
//Maps to vanilla net.minecraft.client.main.Main
//Chains kernel initialization + launch argument parsing + asset extraction + client game logic scheduling
//This class is itself the EXE entry, and can also be referenced by NacreCraft.Loader and dispatched to
public static class ClientMain
{
    private static int _started;

    //Run client startup main function
    //The process entry is in NacreCraft.ClientExe and mod bootstrap happens there too; this only handles the startup flow itself
    //args command-line arguments; kernel-recognized ones are consumed, unhandled ones are passed to GameOptions via events
    public static void Run(string[] args)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            Log.Warning("ClientMain already started, ignoring duplicate call");
            //Log.Debug("Run exit");
            return;
        }
        Log.Debug($"Run entry args={string.Join(",", args)}");

        Log.SetClassSource(typeof(ClientMain));
        //The role segment in the crash report file name; the handler shared with the kernel uses it to tell client from server
        CrashHandler.Role = "client";
        Log.Info("NacreCraft client starting");

        //1. Create GameOptions and subscribe to the kernel's unhandled-argument event
        //   Must subscribe before NacreCraftKernel.Initialize to receive the event
        var options = new GameOptions();
        options.Subscribe();

        //2. Initialize the kernel (triggers LaunchOptions.Parse; kernel-recognized args are consumed, unhandled ones fire the event)
        NacreCraftKernel.Initialize(args);

        //3. Parse remaining pending --opt arguments; without a following value they degrade to flags
        options.FlushPending();

        //4. Set --output-dir to override the base; if not passed, use AppContext.BaseDirectory
        //   Downstream reads uniformly from AppPaths to avoid relative paths being interpreted as the runtime working directory
        AppPaths.SetOverride(options.GetOptionOrDefault("output-dir", string.Empty));

        //5. Extract jar and audio assets to a fixed directory
        //   Core game logic later loads directly from assets and assets/sounds without depending on this step
        //   Also copy pack.mcmeta to the root, alongside assets/data
        //   Must run before BootStrap because the latter freezes the registry; after freezing, data-driven loading has nowhere to write
        AssetsExtractor.Extract(options);

        //5.1 Land the assets the jar cannot supply
        //   The jar carries no pack.mcmeta, only en_us of the language tables, and its font entries are either empty
        //   placeholders or missing outright (unifont, which is where the CJK glyphs come from)
        //   All of them live in the asset objects the launcher downloads alongside the version, so this reads the vanilla
        //   argument names assetsDir/assetIndex
        //   Must run after jar extraction so these objects overwrite the placeholders, and before ResourceManager
        //   construction because the folder pack enumerates the directory's files all at once when constructed
        AssetObjects.Prepare(
            options.GetOptionOrDefault("assetsDir", string.Empty),
            options.GetOptionOrDefault("assetIndex", string.Empty));

        //6. First half of Game-layer bootstrap, must run before data-driven loading
        //   The density function type table/block element JSON codecs depend on it; missing it makes the whole batch fail to decode
        //   The environment attribute table must also be registered first; the biome's attributes field looks it up by key
        DataComponents.Bootstrap();
        GameBootstrap.BootstrapBeforeDataLoad();
        EnvironmentAttributes.RegisterAll();

        //6.5 Load the client config before pack selection, because options.txt decides which packs are enabled
        //    This used to run after the registry data load; the pack selection needs it earlier
        var gameConfig = GameConfig.Load(AppPaths.OptionsPath);
        if (options.HasFlag("demo")) gameConfig.Demo = true;
        if (options.HasFlag("fullscreen")) gameConfig.Fullscreen = true;
        Log.Info($"Client config loaded render distance {gameConfig.RenderDistance} FOV {gameConfig.Fov} language {gameConfig.Language}");

        //6.6 Install the table once for the client language code; falls back to en_us when an unsupported code is configured
        //    The resource reload in step 10 installs it again, this time also including the vanilla translations from resource packs
        NcLanguage.Load(gameConfig.Language);

        //6.7 Build the pack repository: the built-in vanilla pack plus every folder under resourcepacks/
        //    The vanilla pack is required, so it always sits under the user packs, matching vanilla ClientPackSource
        var packRepository = new PackRepository(
            new ClientPackSource(AppPaths.BaseDirectory),
            new FolderRepositorySource(AppPaths.ResourcePacksDir, PackType.ClientResources, PackSource.Default));
        packRepository.Reload();
        //An id saved by an older run that no longer resolves is skipped when the selection is rebuilt
        packRepository.SetSelected(gameConfig.ResourcePacks);
        Log.Info($"Client pack repository available={packRepository.Available.Count} selected={packRepository.Selected.Count}");

        //7. Construct ResourceManager from the selection and data-driven load registry elements, consistent with the server
        //   The client also needs vanilla-accurate biome/density function/noise/noise settings, otherwise singleplayer terrain differs from the server
        var registryAccess = BuiltInRegistries.CreateRegistryAccess();
        var resourceManager = new ResourceManager();
        resourceManager.SetPacks(packRepository.Selected);
        var registryLoad = RegistryDataLoader.Load(resourceManager, registryAccess, new RegistryData[]
        {
            new RegistryData<NoiseParameters>(
                (WritableRegistry<NoiseParameters>)BuiltInRegistries.NOISE, NoiseParameters.Codec),
            RegistryData.Boxed((WritableRegistry<object>)BuiltInRegistries.DENSITY_FUNCTION, DensityFunctionCodec.Instance),
            //configured_carver must come before biome; resolving the biome's carvers field looks them up by registry name
            new RegistryData<ConfiguredWorldCarver>(
                (WritableRegistry<ConfiguredWorldCarver>)BuiltInRegistries.CONFIGURED_CARVER,
                GameConfiguredWorldCarver.ElementCodec),
            //configured_feature / placed_feature must also come before biome; the biome's features field looks them up by registry name
            new RegistryData<NacreCraft.Registry.ConfiguredFeature>(
                (WritableRegistry<NacreCraft.Registry.ConfiguredFeature>)BuiltInRegistries.CONFIGURED_FEATURE,
                NacreCraft.Game.World.Level.LevelGen.Features.ConfiguredFeature.ElementCodec),
            new RegistryData<NacreCraft.Registry.PlacedFeature>(
                (WritableRegistry<NacreCraft.Registry.PlacedFeature>)BuiltInRegistries.PLACED_FEATURE,
                NacreCraft.Game.World.Level.LevelGen.Placement.PlacedFeature.ElementCodec),
            new RegistryData<Biome>((WritableRegistry<Biome>)BuiltInRegistries.BIOME, Biome.DirectCodec),
            RegistryData.Boxed((WritableRegistry<object>)BuiltInRegistries.MULTI_NOISE_BIOME_SOURCE_PARAMETER_LIST, MultiNoiseBiomeSourceParameterList.Codec),
            RegistryData.Boxed((WritableRegistry<object>)BuiltInRegistries.NOISE_SETTINGS, NoiseGeneratorSettings.Codec),
            //dimension_type dimension type; client rendering takes sky light/ceiling/skybox parameters by dimension
            new RegistryData<NacreCraft.Registry.DimensionType>(
                (WritableRegistry<NacreCraft.Registry.DimensionType>)BuiltInRegistries.DIMENSION_TYPE,
                GameDimensionType.ElementCodec),
            //The four structure-related kinds, in the same order as ServerMain; the client only needs to resolve the structure sets by registry name for syncing
            new RegistryData<NacreCraft.Registry.StructureProcessorList>(
                (WritableRegistry<NacreCraft.Registry.StructureProcessorList>)BuiltInRegistries.PROCESSOR_LIST,
                NacreCraft.Game.World.Level.LevelGen.Structure.StructureProcessorList.ElementCodec),
            new RegistryData<NacreCraft.Registry.StructureTemplatePool>(
                (WritableRegistry<NacreCraft.Registry.StructureTemplatePool>)BuiltInRegistries.TEMPLATE_POOL,
                NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool.ElementCodec),
            new RegistryData<NacreCraft.Registry.Structure>(
                (WritableRegistry<NacreCraft.Registry.Structure>)BuiltInRegistries.STRUCTURE,
                NacreCraft.Game.World.Level.LevelGen.Structure.StructureCodecs.ElementCodec),
            new RegistryData<NacreCraft.Registry.StructureSet>(
                (WritableRegistry<NacreCraft.Registry.StructureSet>)BuiltInRegistries.STRUCTURE_SET,
                NacreCraft.Game.World.Level.LevelGen.Structure.StructureSetCodecs.ElementCodec),
        });
        Log.Info($"Client registry data loaded: {registryLoad.LoadedCount} elements, {registryLoad.Errors.Count} errors");
        foreach (var error in registryLoad.Errors) Log.Warning($"Registry element failed to load: {error}");
        //Dimension type fallback: fill in built-in constants when there are no data packs; the client does not load level definitions (no chunk generator needed)
        DimensionTypes.RegisterBuiltin();

        //8. Second half of Game-layer bootstrap and built-in registry freeze
        GameBootstrap.BootstrapAfterDataLoad();
        BootstrapClass.BootStrap();

        //10. Trigger data-driven reload of Tags and the language table
        //   The client also needs Tags for item/block tag queries (such as tool tier checks)
        //   Must run after BootstrapClass.BootStrap because BindAll requires the Registry to be frozen
        //   The language table is attached as an extra listener; to switch languages change LanguageCode and run the reload once more to swap the table
        var clientLanguage = new ClientLanguage(gameConfig.Language);
        var rsr = ReloadableServerResources.LoadResources(resourceManager, registryAccess,
            new PreparableReloadListener[] { clientLanguage });
        Log.Info($"Client resources loaded: {rsr.Listeners.Count} listeners, {resourceManager.Packs.Count} packs, {clientLanguage.AvailableLanguages.Count} languages");

        //11. Create the MinecraftClient instance and start the main loop
        //   Phase 11.49 passes a VulkanGuiApp for window-driven mode
        //   MinecraftClient owns gpuApp and releases it on Dispose
        //   rsr is passed for later client Tags queries or /reload reloads
        //   resourceManager is passed to build the block atlas and world render chain once the swapchain is ready
        //   S3 texture injection and world render wiring are handled uniformly by MinecraftClient.EnsureWorldRenderer
        var gpuApp = new VulkanGuiApp(gameConfig.EnableVsync, 800, 600, clientResources: resourceManager, startFullscreen: gameConfig.Fullscreen);
        //Options carry the fullscreen video mode, the same place vanilla reads it from in its Window constructor
        gpuApp.SetPreferredFullscreenVideoMode(VideoMode.Read(gameConfig.FullscreenVideoMode));
        // minecraft.Run blocks the current thread until minecraft.Stop is called
        using var minecraft = new MinecraftClient(gameConfig, gpuApp, rsr: rsr, resourceManager: resourceManager,
            language: clientLanguage, packRepository: packRepository);

        //Register Ctrl+C to trigger a graceful shutdown
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            minecraft.Stop();
        };

        minecraft.Run();

        //12. Clean up and exit
        options.Unsubscribe();
        Log.Info("NacreCraft client stopped");
        //Log.Debug("Run exit");
    }
}
