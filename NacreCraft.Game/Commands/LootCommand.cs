using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Loot;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//LootCommand loot command, maps to vanilla net.minecraft.server.commands.LootCommand
//Loot table resolution uses LootTables.Active and writes into containers/inventories or spawns item entities for real
//kill has no entity loot table source yet, replace entity has no general slot access, and mine ignores the tool argument
//because block drops are resolved through the block behaviour; those gaps are reported per branch
public static class LootCommand
{
    private static readonly SimpleCommandExceptionType ErrorTargetNotAContainer =
        new(new LiteralMessage("the target is not a container"));

    private static readonly DynamicCommandExceptionType ErrorInapplicableSlot =
        new(slot => new LiteralMessage($"the target has no slot {slot}"));

    private static readonly DynamicCommandExceptionType ErrorUnknownLootTable =
        new(id => new LiteralMessage($"unknown loot table {id}"));

    private static readonly SimpleCommandExceptionType ErrorNotLoaded =
        new(new LiteralMessage("that position is not loaded"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var loot = LiteralArgumentBuilder<CommandSourceStack>.Literal("loot")
            .Requires(s => s.HasPermission(2));

        //replace entity <entities> <slot> [<count>]
        var entitySlot = Arg<int>("slot", SlotArgument.Slot());
        AddSources(entitySlot, (context, drops) => Fail(context, "entity slot replacement is not wired yet"));
        var entityCount = Arg<int>("count", IntegerArgumentType.Integer(0));
        AddSources(entityCount, (context, drops) => Fail(context, "entity slot replacement is not wired yet"));
        entitySlot.Then(entityCount);
        loot.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("replace")
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("entity")
                .Then(Arg<EntitySelector>("entities", EntityArgument.Entities()).Then(entitySlot))));

        //replace block <targetPos> <slot> [<count>]
        var blockSlot = Arg<int>("slot", SlotArgument.Slot());
        AddSources(blockSlot, BlockReplaceWithDropCount);
        var blockCount = Arg<int>("count", IntegerArgumentType.Integer(0));
        AddSources(blockCount, BlockReplaceWithExplicitCount);
        blockSlot.Then(blockCount);
        loot.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("replace")
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("block")
                .Then(Arg<BlockCoordinates>("targetPos", BlockPosArgument.BlockPos()).Then(blockSlot))));

        //insert <targetPos>
        var insert = Arg<BlockCoordinates>("targetPos", BlockPosArgument.BlockPos());
        AddSources(insert, InsertIntoContainer);
        loot.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("insert").Then(insert));

        //give <players>
        var give = Arg<EntitySelector>("players", EntityArgument.Players());
        AddSources(give, GiveToPlayers);
        loot.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("give").Then(give));

        //spawn <targetPos>
        var spawn = Arg<Coordinates>("targetPos", Vec3Argument.Vec3());
        AddSources(spawn, SpawnInWorld);
        loot.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("spawn").Then(spawn));

        dispatcher.Register(loot);
    }

    //AddSources attaches the fish/loot/kill/mine loot sources to a target node, maps to vanilla addTargets
    private static void AddSources<TParent>(TParent parent,
        Func<CommandContext<CommandSourceStack>, List<ItemStack>, int> consumer)
        where TParent : ArgumentBuilder<CommandSourceStack, TParent>
    {
        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("fish")
            .Then(Arg<Identifier>("loot_table", IdentifierArgument.Id())
                .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                    .Executes(context => Fish(context, ItemStack.Empty, consumer))
                    .Then(Arg<ItemInput>("tool", ItemArgument.Item())
                        .Executes(context => Fish(context, ToolStack(context, "tool"), consumer)))
                    .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("mainhand")
                        .Executes(context => Fish(context, HandStack(context, false), consumer)))
                    .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("offhand")
                        .Executes(context => Fish(context, HandStack(context, true), consumer))))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("loot")
            .Then(Arg<Identifier>("loot_table", IdentifierArgument.Id())
                .Executes(context => Chest(context, consumer))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("kill")
            .Then(Arg<EntitySelector>("target", EntityArgument.Entity())
                .Executes(context => Fail(context, "entity loot tables are not wired yet"))));

        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("mine")
            .Then(Arg<BlockCoordinates>("pos", BlockPosArgument.BlockPos())
                .Executes(context => Mine(context, consumer))
                .Then(Arg<ItemInput>("tool", ItemArgument.Item())
                    .Executes(context => Mine(context, consumer)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("mainhand")
                    .Executes(context => Mine(context, consumer)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("offhand")
                    .Executes(context => Mine(context, consumer)))));
    }

    //Fish rolls a fishing loot table, maps to vanilla dropFishingLoot
    private static int Fish(CommandContext<CommandSourceStack> context, ItemStack tool,
        Func<CommandContext<CommandSourceStack>, List<ItemStack>, int> consumer)
    {
        var pos = BlockPosArgument.GetBlockPos(context, "pos");
        var origin = new Vec3(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5);
        var drops = RollTable(context, "loot_table", LootContextParamSets.Fishing, origin, tool);
        return consumer(context, drops);
    }

    //Chest rolls a chest loot table, maps to vanilla dropChestLoot
    private static int Chest(CommandContext<CommandSourceStack> context,
        Func<CommandContext<CommandSourceStack>, List<ItemStack>, int> consumer)
    {
        var source = (ServerCommandSource)context.GetSource();
        var drops = RollTable(context, "loot_table", LootContextParamSets.Chest, source.Position, ItemStack.Empty);
        return consumer(context, drops);
    }

    //Mine resolves block drops through the block behaviour, maps to vanilla dropBlockLoot
    private static int Mine(CommandContext<CommandSourceStack> context,
        Func<CommandContext<CommandSourceStack>, List<ItemStack>, int> consumer)
    {
        var source = (ServerCommandSource)context.GetSource();
        if (source.Level is not { } level) return Fail(context, "loot needs a level context");
        var pos = BlockPosArgument.GetBlockPos(context, "pos");
        var state = level.GetBlockStateIfLoaded(pos) ?? throw ErrorNotLoaded.Create();
        var drops = state.Owner is BlockBehaviour behaviour
            ? behaviour.GetDrops(level, source.Player, pos, state).ToList()
            : new List<ItemStack>();
        return consumer(context, drops);
    }

    //RollTable builds loot parameters and resolves the named table, maps to vanilla drop/dropFishingLoot
    private static List<ItemStack> RollTable(CommandContext<CommandSourceStack> context, string argumentName,
        NacreCraft.Registry.Context.ContextKeySet keySet, Vec3 origin, ItemStack tool)
    {
        var source = (ServerCommandSource)context.GetSource();
        if (source.Level is not { } level) throw ErrorUnknownLootTable.Create(argumentName);
        var id = IdentifierArgument.GetId(context, argumentName);
        var table = LootTables.Active.Get(id) ?? throw ErrorUnknownLootTable.Create(id);

        var parameters = new LootParams.Builder(level)
            .WithParameter(LootContextParams.Origin, origin)
            .WithParameter(LootContextParams.Tool, tool)
            .WithOptionalParameter(LootContextParams.ThisEntity, source.Player)
            .Create(keySet);
        return table.GetRandomItems(parameters);
    }

    private static int BlockReplaceWithDropCount(CommandContext<CommandSourceStack> context, List<ItemStack> drops)
        => BlockReplace(context, drops, drops.Count);

    private static int BlockReplaceWithExplicitCount(CommandContext<CommandSourceStack> context, List<ItemStack> drops)
        => BlockReplace(context, drops, IntegerArgumentType.GetInteger(context, "count"));

    //BlockReplace writes drops into consecutive container slots, maps to vanilla blockReplace
    private static int BlockReplace(CommandContext<CommandSourceStack> context, List<ItemStack> drops, int count)
    {
        var source = (ServerCommandSource)context.GetSource();
        var container = GetContainer(context);
        var startSlot = SlotArgument.GetSlot(context, "slot");
        if (startSlot < 0 || startSlot >= container.Size)
            throw ErrorInapplicableSlot.Create(startSlot);

        var used = new List<ItemStack>();
        for (var i = 0; i < count; i++)
        {
            var slot = startSlot + i;
            if (slot >= container.Size) break;
            var stack = i < drops.Count ? drops[i] : ItemStack.Empty;
            if (!container.CanPlaceItem(slot, stack)) continue;
            container.SetItem(slot, stack);
            used.Add(stack);
        }
        container.SetChanged();
        source.SendSuccess($"replaced {used.Count} slot(s) in the container");
        return used.Count;
    }

    //InsertIntoContainer distributes every drop into the container, maps to vanilla blockDistribute
    private static int InsertIntoContainer(CommandContext<CommandSourceStack> context, List<ItemStack> drops)
    {
        var source = (ServerCommandSource)context.GetSource();
        var container = GetContainer(context);
        var used = 0;
        foreach (var drop in drops)
            if (ContainerHelper.AddItem(container, drop.Copy()).IsEmpty()) used++;
        source.SendSuccess($"inserted {used} stack(s) into the container");
        return used;
    }

    //GiveToPlayers inserts drops into player inventories, maps to vanilla playerGive
    private static int GiveToPlayers(CommandContext<CommandSourceStack> context, List<ItemStack> drops)
    {
        var source = (ServerCommandSource)context.GetSource();
        var players = EntityArgument.GetPlayers(context, "players");
        var used = 0;
        foreach (var drop in drops)
        {
            var accepted = false;
            foreach (var player in players)
                if (player.Inventory.Add(drop.Copy())) accepted = true;
            if (accepted) used++;
        }
        source.SendSuccess($"gave {used} stack(s) to {players.Count} player(s)");
        return used;
    }

    //SpawnInWorld creates item entities at the position, maps to vanilla dropInWorld
    private static int SpawnInWorld(CommandContext<CommandSourceStack> context, List<ItemStack> drops)
    {
        var source = (ServerCommandSource)context.GetSource();
        if (source.Level is not { } level) return Fail(context, "loot needs a level context");
        var pos = Vec3Argument.GetVec3(context, "targetPos");
        foreach (var drop in drops)
        {
            var entity = new ItemEntity(EntityTypes.ITEM, pos.X, pos.Y, pos.Z, drop.Copy());
            entity.SetDefaultPickUpDelay();
            level.AddEntity(entity);
        }
        source.SendSuccess($"dropped {drops.Count} stack(s) in the world");
        return drops.Count;
    }

    private static Container GetContainer(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var pos = BlockPosArgument.GetBlockPos(context, "targetPos");
        if (source.Level is not { } level) throw ErrorTargetNotAContainer.Create();
        return level.GetBlockEntity<Container>(pos) ?? throw ErrorTargetNotAContainer.Create();
    }

    //ToolStack creates a one-item stack from the item argument, maps to vanilla ItemArgument.getItem().createItemStack(1)
    private static ItemStack ToolStack(CommandContext<CommandSourceStack> context, string name)
    {
        var input = ItemArgument.GetItemInput(context, name);
        return new ItemStack(input.Item.BuiltInRegistryHolder, 1, input.Components);
    }

    //HandStack reads the executor's main or off hand, maps to vanilla getSourceHandItem
    private static ItemStack HandStack(CommandContext<CommandSourceStack> context, bool offHand)
    {
        var player = ((ServerCommandSource)context.GetSource()).PlayerOrThrow;
        return offHand
            ? player.Inventory.GetItem(PlayerInventory.OffhandSlot)
            : player.Inventory.GetSelectedItem();
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context, string message)
    {
        context.GetSource().SendFailure(message);
        return 0;
    }
}
