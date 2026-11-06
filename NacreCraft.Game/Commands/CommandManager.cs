using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Execution;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Commands.Tree;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Profiling;

namespace NacreCraft.Game.Commands;

//CommandManager command manager, maps to vanilla net.minecraft.commands.Commands
//Holds the dispatcher, registers built-in commands, provides command tree dispatch and the player command execution entry
public sealed class CommandManager
{
    private readonly CommandDispatcher<CommandSourceStack> _dispatcher = new();
    private readonly MinecraftServer _server;

    //debugCommands explicitly specifies whether to register /debug; null takes nc-debug-commands from server.properties
    //This parameter exists so tests can enable the debug tree without server.properties
    public CommandManager(MinecraftServer server, bool? debugCommands = null)
    {
        _server = server;
        RegisterHelp();
        RegisterGameMode();
        RegisterTeleport();
        RegisterTime();
        RegisterGive();
        RegisterItem();
        RegisterData();
        RegisterClear();
        RegisterKill();
        RegisterSetBlock();
        RegisterFill();
        RegisterGameRule();
        RegisterWorldBorder();
        RegisterWeather();
        RegisterForceLoad();
        RegisterSpreadPlayers();
        RegisterSay();
        RegisterTellraw();
        RegisterTitle();
        RegisterStop();
        RegisterOp();
        RegisterKick();
        RegisterBan();
        RegisterListPlayers();
        RegisterEmote();
        RegisterMsg();
        RegisterSeed();
        RegisterVersion();
        RegisterSwing();
        RegisterStopwatch();
        //server null only happens in tests using the command tree construction directly; treated as on, production always reads server.properties
        RegisterDebug(debugCommands ?? server?.Settings?.NcDebugCommands ?? true);
        RegisterTransfer();
        RegisterDifficulty();
        RegisterDefaultGameMode();
        RegisterSave();
        RegisterDamage();
        RegisterParticle();
        RegisterPlaySound();
        RegisterStopSound();
        RegisterTick();
        RegisterPerf();
        RegisterSetWorldSpawn();
        RegisterRotate();
        RegisterWhiteList();
        RegisterClone();
        RegisterSpawnPoint();
        RegisterSetIdleTimeout();
        RegisterFillBiome();
        RegisterExperience();
        RegisterAttribute();
        RegisterTag();
        RegisterRecipe();
        RegisterSpectate();
        RegisterEffect();
        RegisterEnchant();
        RegisterRandom();
        RegisterRide();
        RegisterSummon();
        RegisterTrigger();
        RegisterWardenSpawnTracker();
        RegisterSpawnArmorTrims();
        RegisterServerPack();
        RegisterFetchProfile();
        RegisterAdvancement();
        RegisterBossBar();
        RegisterChase();
        RegisterDialog();
        RegisterWaypoint();
        RegisterDebugConfig();
        RegisterDebugMobSpawning();
        RegisterDebugPath();
        RegisterSchedule();
        RegisterDataPack();
        RegisterFunction();
        RegisterReturn();
        RegisterExecute();
        RegisterLocate();
        RegisterLoot();
        RegisterPlace();
        RegisterRaid();
        RegisterScoreboard();
        RegisterTeam();
    }

    public CommandDispatcher<CommandSourceStack> Dispatcher => _dispatcher;

    //SendCommands sends the command tree to a player, called when entering the world
    //Only dispatches nodes the player has permission to run, maps to vanilla Commands.fillUsableCommands
    public void SendCommands(ServerPlayer player)
        => player.Connection.Send(new ClientboundCommandsPacket(BuildUsableTree(player)));

    //BuildUsableTree trims the command tree by the player's current permissions
    private RootCommandNode<CommandSourceStack> BuildUsableTree(ServerPlayer player)
    {
        var source = new ServerCommandSource(player, _server);
        var root = new RootCommandNode<CommandSourceStack>();
        CopyUsable(_dispatcher.GetRoot(), root, source);
        return root;
    }

    //CopyUsable depth-first copies usable nodes; copied nodes have their permission predicate cleared and inaccessible redirects removed
    private static void CopyUsable(CommandNode<CommandSourceStack> node, CommandNode<CommandSourceStack> target, CommandSourceStack source)
    {
        foreach (var child in node.GetChildren())
        {
            if (!child.CanUse(source)) continue;
            var builder = child.CreateBuilder();
            builder.Requires(_ => true);
            if (builder.GetRedirect() is { } redirect && !redirect.CanUse(source))
                builder.Forward(null, (RedirectModifier<CommandSourceStack>?)null, false);
            var copy = builder.Build();
            target.AddChild(copy);
            CopyUsable(child, copy, source);
        }
    }

    //Execute runs a player command; parse failures and syntax errors are reported to the executor like vanilla
    public int Execute(ServerPlayer player, string command)
        => Execute(new ServerCommandSource(player, _server), command);

    //Execute runs a command from any source, for cases with no executing player such as the console
    //The command goes through the execution engine like vanilla performCommand: the same queue, quota and fork limits apply,
    //and /return inside a function can hand a value back through the frame return consumer
    public int Execute(CommandSourceStack source, string command)
    {
        var input = TrimOptionalPrefix(command);
        //A manager built without a server (tree construction tests) has no gamerules to size the queue, so it keeps the direct path
        if (_server is null) return _dispatcher.Execute(input, source);
        try
        {
            var parse = _dispatcher.Parse(input, source);
            var chain = FinishParsing(parse, source);
            if (chain is null) return 0;
            //The engine reports the value through the source callback; capture it so callers still get the brigadier-style int
            var result = 0;
            var callback = new CapturingResultCallback(value => result = value);
            ExecuteInContext(source, context => ExecutionContext<CommandSourceStack>.QueueInitialCommandExecution(
                context, input, chain, source.WithCallback(callback), callback));
            return result;
        }
        catch (CommandSyntaxException e)
        {
            source.SendFailure(e.Message);
            return 0;
        }
    }

    //TrimOptionalPrefix drops one leading slash, maps to vanilla Commands.trimOptionalPrefix
    private static string TrimOptionalPrefix(string command)
        => command.StartsWith('/') ? command[1..] : command;

    //FinishParsing validates the parse and flattens it into a context chain; a syntax error is reported and yields null
    //maps to vanilla Commands.finishParsing
    private static ContextChain<CommandSourceStack>? FinishParsing(ParseResults<CommandSourceStack> parse,
        CommandSourceStack source)
    {
        try
        {
            NacreCraft.Commands.Functions.CommandFunction<CommandSourceStack>.ValidateParseResults(parse);
            var chain = ContextChain<CommandSourceStack>.TryFlatten(parse.GetContext().Build(parse.GetReader().String));
            if (!chain.IsPresent)
            {
                throw CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()
                    .CreateWithContext(parse.GetReader());
            }
            return chain.Get();
        }
        catch (CommandSyntaxException e)
        {
            source.SendFailure(e.Message);
            return null;
        }
    }

    //CapturingResultCallback forwards the final result to the caller of Execute
    private sealed class CapturingResultCallback(Action<int> sink) : CommandResultCallback
    {
        public void OnResult(bool success, int result) => sink(success ? result : 0);
    }

    //_currentExecutionContext the execution context running on this thread, maps to vanilla CURRENT_EXECUTION_CONTEXT
    private static readonly ThreadLocal<ExecutionContext<CommandSourceStack>?> _currentExecutionContext = new();

    //ExecuteInContext queues commands in the execution context and drains the queue, maps to vanilla Commands.executeCommandInContext
    //When a function runs a nested command it reuses the thread's existing context; the queue is drained only by the outermost layer
    public void ExecuteInContext(CommandSourceStack source, Action<ExecutionContext<CommandSourceStack>> configure)
    {
        var current = _currentExecutionContext.Value;
        if (current is not null)
        {
            configure(current);
            return;
        }
        //The chain length and fork limits come from gamerules, maps to vanilla MAX_COMMAND_SEQUENCE_LENGTH and MAX_COMMAND_FORKS
        var chainLimit = Math.Max(1, _server.GameRules.GetInt(NacreCraft.Game.World.Level.GameRules.MaxCommandSequenceLength));
        var forkLimit = Math.Max(1, _server.GameRules.GetInt(NacreCraft.Game.World.Level.GameRules.MaxCommandForks));
        //System.Threading also has a non-generic ExecutionContext; fully qualified to avoid ambiguity
        using var context = new NacreCraft.Commands.Execution.ExecutionContext<CommandSourceStack>(chainLimit, forkLimit, Profiler.Get());
        _currentExecutionContext.Value = context;
        try
        {
            configure(context);
            context.RunCommandQueue();
        }
        finally
        {
            _currentExecutionContext.Value = null;
        }
    }

    //GetCompletions computes completion candidates by player permission, maps to vanilla CommandDispatcher.getCompletionSuggestions
    //The client sends a completion request on tab; this computes and replies with a suggestion packet; candidates like entities/items can only come from the server
    public Suggestions GetCompletions(ServerPlayer player, string command)
        => GetCompletions(new ServerCommandSource(player, _server), command, command.Length);

    //GetCompletions computes completion candidates for any command source; the server console terminal goes through this
    //Only parses up to the cursor; content after the cursor does not participate, consistent with vanilla client completion
    public Suggestions GetCompletions(CommandSourceStack source, string command, int cursor)
    {
        var head = cursor >= command.Length ? command : command[..cursor];
        var parse = _dispatcher.Parse(head, source);
        return _dispatcher.GetCompletionSuggestions(parse, cursor).GetAwaiter().GetResult();
    }

    //RequirePlayer gets the player command source; this project has no console/command block source yet
    private static ServerCommandSource? RequirePlayer(CommandContext<CommandSourceStack> context)
        => context.GetSource() as ServerCommandSource;

    //RegisterGameMode registers /gamemode via the vanilla-named command class
    private void RegisterGameMode()
        => GameModeCommand.Register(_dispatcher);

    //RegisterHelp registers the vanilla help command tree
    private void RegisterHelp()
        => HelpCommand.Register(_dispatcher);

    //RegisterTeleport registers the vanilla teleport/tp command tree
    private void RegisterTeleport()
        => TeleportCommand.Register(_dispatcher);

    //RegisterTime registers the vanilla time command tree
    private void RegisterTime()
        => TimeCommand.Register(_dispatcher);

    //RegisterDebug registers the debug command tree for inspecting world block data; disabled by default, enable with nc-debug-commands in server.properties
    private void RegisterDebug(bool enabled)
    {
        if (!enabled) return;
        DebugCommand.Register(_dispatcher);
    }

    //RegisterStop registers /stop via the vanilla-named command class
    private void RegisterStop()
        => StopCommand.Register(_dispatcher);

    //RegisterOp registers /op /deop for reading and writing the operator list
    private void RegisterOp()
    {
        OpCommand.Register(_dispatcher);
        DeOpCommands.Register(_dispatcher);
    }

    //RegisterKick registers the /kick command
    private void RegisterKick()
        => KickCommand.Register(_dispatcher);

    //RegisterBan registers /ban /ban-ip /pardon /pardon-ip /banlist through their vanilla-named classes
    private void RegisterBan()
    {
        BanPlayerCommands.Register(_dispatcher);
        BanIpCommands.Register(_dispatcher);
        BanListCommands.Register(_dispatcher);
        PardonCommand.Register(_dispatcher);
        PardonIpCommand.Register(_dispatcher);
    }

    //RegisterListPlayers registers /list for the online player list
    private void RegisterListPlayers()
        => ListPlayersCommand.Register(_dispatcher);

    //RegisterEmote registers /me action broadcast
    private void RegisterEmote()
        => EmoteCommands.Register(_dispatcher);

    //RegisterMsg registers /msg /tell /w private message
    private void RegisterMsg()
        => MsgCommand.Register(_dispatcher);

    //RegisterSeed registers /seed world seed
    private void RegisterSeed()
        => SeedCommand.Register(_dispatcher);

    //RegisterVersion registers /version server version
    private void RegisterVersion()
        => VersionCommand.Register(_dispatcher);

    //RegisterSwing registers /swing swing animation
    private void RegisterSwing()
        => SwingCommand.Register(_dispatcher);

    //RegisterStopwatch registers /stopwatch debug timer
    private void RegisterStopwatch()
        => StopwatchCommand.Register(_dispatcher);

    //RegisterTransfer registers /transfer hand-off to another server
    private void RegisterTransfer()
        => TransferCommand.Register(_dispatcher);

    //RegisterDifficulty registers /difficulty
    private void RegisterDifficulty()
        => DifficultyCommand.Register(_dispatcher);

    //RegisterDefaultGameMode registers /defaultgamemode
    private void RegisterDefaultGameMode()
        => DefaultGameModeCommands.Register(_dispatcher);

    //RegisterSave registers /save-all /save-off /save-on flush control
    private void RegisterSave()
    {
        SaveAllCommand.Register(_dispatcher);
        SaveOffCommand.Register(_dispatcher);
        SaveOnCommand.Register(_dispatcher);
    }

    //RegisterDamage registers /damage to deal damage
    private void RegisterDamage()
        => DamageCommand.Register(_dispatcher);

    //RegisterParticle registers /particle to emit particles
    private void RegisterParticle()
        => ParticleCommand.Register(_dispatcher);

    //RegisterPlaySound registers /playsound to play a sound
    private void RegisterPlaySound()
        => PlaySoundCommand.Register(_dispatcher);

    //RegisterStopSound registers /stopsound to stop sounds
    private void RegisterStopSound()
        => StopSoundCommand.Register(_dispatcher);

    //RegisterTick registers /tick tick rate and freeze control
    private void RegisterTick()
        => TickCommand.Register(_dispatcher);

    //RegisterPerf registers /perf performance recording
    private void RegisterPerf()
        => PerfCommand.Register(_dispatcher);

    //RegisterSetWorldSpawn registers /setworldspawn
    private void RegisterSetWorldSpawn()
        => SetWorldSpawnCommand.Register(_dispatcher);

    //RegisterRotate registers /rotate facing control
    private void RegisterRotate()
        => RotateCommand.Register(_dispatcher);

    //RegisterWhiteList registers /whitelist
    private void RegisterWhiteList()
        => WhitelistCommand.Register(_dispatcher);

    //RegisterClone registers /clone region copy
    private void RegisterClone()
        => CloneCommands.Register(_dispatcher);

    //RegisterSpawnPoint registers /spawnpoint personal respawn point
    private void RegisterSpawnPoint()
        => SetSpawnCommand.Register(_dispatcher);

    //RegisterSetIdleTimeout registers /setidletimeout idle kick timeout
    private void RegisterSetIdleTimeout()
        => SetPlayerIdleTimeoutCommand.Register(_dispatcher);

    //RegisterFillBiome registers /fillbiome region biome replacement
    private void RegisterFillBiome()
        => FillBiomeCommand.Register(_dispatcher);

    //RegisterExperience registers /experience player xp and level
    private void RegisterExperience()
        => ExperienceCommand.Register(_dispatcher);

    //RegisterAttribute registers /attribute entity attribute read/write
    private void RegisterAttribute()
        => AttributeCommand.Register(_dispatcher);

    //RegisterTag registers /tag entity tags
    private void RegisterTag()
        => TagCommand.Register(_dispatcher);

    //RegisterRecipe registers /recipe player recipes
    private void RegisterRecipe()
        => RecipeCommand.Register(_dispatcher);

    //RegisterSpectate registers the vanilla spectate command tree
    private void RegisterSpectate()
        => SpectateCommand.Register(_dispatcher);

    //RegisterEffect registers the vanilla effect command tree
    private void RegisterEffect()
        => EffectCommand.Register(_dispatcher);

    //RegisterEnchant registers the vanilla enchant command tree
    private void RegisterEnchant()
        => EnchantCommand.Register(_dispatcher);

    //RegisterRandom registers the vanilla random command tree
    private void RegisterRandom()
        => RandomCommand.Register(_dispatcher);

    //RegisterRide registers the vanilla ride command tree
    private void RegisterRide()
        => RideCommand.Register(_dispatcher);

    //RegisterSummon registers the vanilla summon command tree
    private void RegisterSummon()
        => SummonCommand.Register(_dispatcher);

    //RegisterTrigger registers the vanilla trigger command tree
    private void RegisterTrigger()
        => TriggerCommand.Register(_dispatcher);

    //RegisterWardenSpawnTracker registers the vanilla warden_spawn_tracker command tree
    private void RegisterWardenSpawnTracker()
        => WardenSpawnTrackerCommand.Register(_dispatcher);

    //RegisterSpawnArmorTrims registers the vanilla spawn_armor_trims command tree
    private void RegisterSpawnArmorTrims()
        => SpawnArmorTrimsCommand.Register(_dispatcher);

    //RegisterServerPack registers the vanilla serverpack command tree
    private void RegisterServerPack()
        => ServerPackCommand.Register(_dispatcher);

    //RegisterFetchProfile registers the vanilla fetchprofile command tree
    private void RegisterFetchProfile()
        => FetchProfileCommand.Register(_dispatcher);

    //RegisterAdvancement registers the vanilla advancement command tree
    private void RegisterAdvancement()
        => AdvancementCommands.Register(_dispatcher);

    //RegisterBossBar registers the vanilla bossbar command tree
    private void RegisterBossBar()
        => BossBarCommands.Register(_dispatcher);

    //RegisterChase registers the vanilla chase command tree
    private void RegisterChase()
        => ChaseCommand.Register(_dispatcher);

    //RegisterDialog registers the vanilla dialog command tree
    private void RegisterDialog()
        => DialogCommand.Register(_dispatcher);

    //RegisterWaypoint registers the vanilla waypoint command tree
    private void RegisterWaypoint()
        => WaypointCommand.Register(_dispatcher);

    //RegisterDebugConfig registers the vanilla debugconfig command tree
    private void RegisterDebugConfig()
        => DebugConfigCommand.Register(_dispatcher);

    //RegisterDebugMobSpawning registers the vanilla debugmobspawning command tree
    private void RegisterDebugMobSpawning()
        => DebugMobSpawningCommand.Register(_dispatcher);

    //RegisterDebugPath registers the vanilla debugpath command tree
    private void RegisterDebugPath()
        => DebugPathCommand.Register(_dispatcher);

    //RegisterSchedule registers the vanilla schedule command tree
    private void RegisterSchedule()
        => ScheduleCommand.Register(_dispatcher);

    //RegisterDataPack registers the vanilla datapack command tree
    private void RegisterDataPack()
        => DataPackCommand.Register(_dispatcher);

    //RegisterFunction registers the vanilla function command tree
    private void RegisterFunction()
        => FunctionCommand.Register(_dispatcher);

    //RegisterReturn registers the vanilla return command tree
    private void RegisterReturn()
        => ReturnCommand.Register(_dispatcher);

    //RegisterExecute registers the vanilla execute command tree
    private void RegisterExecute()
        => ExecuteCommand.Register(_dispatcher);

    //RegisterLocate registers the vanilla locate command tree
    private void RegisterLocate()
        => LocateCommand.Register(_dispatcher);

    //RegisterLoot registers the vanilla loot command tree
    private void RegisterLoot()
        => LootCommand.Register(_dispatcher);

    //RegisterPlace registers the vanilla place command tree
    private void RegisterPlace()
        => PlaceCommand.Register(_dispatcher);

    //RegisterRaid registers the vanilla raid command tree
    private void RegisterRaid()
        => RaidCommand.Register(_dispatcher);

    //RegisterScoreboard registers the vanilla scoreboard command tree
    private void RegisterScoreboard()
        => ScoreboardCommand.Register(_dispatcher);

    //RegisterTeam registers the vanilla team command tree
    private void RegisterTeam()
        => TeamCommand.Register(_dispatcher);

    //RegisterClear registers the vanilla clear command tree
    private void RegisterClear()
        => ClearInventoryCommands.Register(_dispatcher);

    //RegisterKill registers the vanilla kill command tree
    private void RegisterKill()
        => KillCommand.Register(_dispatcher);

    //RegisterSetBlock registers the vanilla setblock command tree
    private void RegisterSetBlock()
        => SetBlockCommand.Register(_dispatcher);

    //RegisterFill registers the vanilla fill command tree
    private void RegisterFill()
        => FillCommand.Register(_dispatcher);

    //RegisterGameRule registers the vanilla gamerule command tree
    private void RegisterGameRule()
        => GameRuleCommand.Register(_dispatcher);

    //RegisterWorldBorder registers the vanilla worldborder command tree
    private void RegisterWorldBorder()
        => WorldBorderCommand.Register(_dispatcher);

    //RegisterWeather registers the vanilla weather command tree
    private void RegisterWeather()
        => WeatherCommand.Register(_dispatcher);

    //RegisterForceLoad registers the vanilla forceload command tree
    private void RegisterForceLoad()
        => ForceLoadCommand.Register(_dispatcher);

    //RegisterSpreadPlayers registers the vanilla spreadplayers command tree
    private void RegisterSpreadPlayers()
        => SpreadPlayersCommand.Register(_dispatcher);

    //RegisterSay registers the vanilla say command tree
    private void RegisterSay()
        => SayCommand.Register(_dispatcher);

    //RegisterTellraw registers the vanilla tellraw command tree
    private void RegisterTellraw()
        => TellRawCommand.Register(_dispatcher);

    //RegisterTitle registers the vanilla title command tree
    private void RegisterTitle()
        => TitleCommand.Register(_dispatcher);

    //RegisterGive registers /give via the vanilla-named command class
    private void RegisterGive()
        => GiveCommand.Register(_dispatcher);

    //RegisterItem registers the vanilla item command tree, reading/writing items by slot name
    private void RegisterItem()
        => ItemCommands.Register(_dispatcher);

    //RegisterData registers the vanilla data command tree, reading/writing NBT of block entities/entities/command storage
    private void RegisterData()
        => DataCommands.Register(_dispatcher);
}
