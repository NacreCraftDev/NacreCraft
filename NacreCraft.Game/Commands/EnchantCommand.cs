using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Enchanting;
using NacreCraft.Registry;
using GameEnchantment = NacreCraft.Game.World.Items.Enchanting.Enchantment;

namespace NacreCraft.Game.Commands;

//EnchantCommand enchant command, maps to vanilla net.minecraft.server.commands.EnchantCommand
//enchant <targets> <enchantment> [<level>] enchants the main hand item of each living target
//Compatibility is checked against the supported item set and the exclusive set of the enchantments already on the stack
public static class EnchantCommand
{
    private static readonly DynamicCommandExceptionType ErrorNotLivingEntity =
        new(target => new LiteralMessage($"{target} is not a living entity"));

    private static readonly DynamicCommandExceptionType ErrorNoItem =
        new(target => new LiteralMessage($"{target} has no item in its main hand"));

    private static readonly DynamicCommandExceptionType ErrorIncompatible =
        new(item => new LiteralMessage($"{item} cannot support that enchantment"));

    private static readonly Dynamic2CommandExceptionType ErrorLevelTooHigh =
        new((level, max) => new LiteralMessage($"level {level} is higher than the maximum {max}"));

    private static readonly SimpleCommandExceptionType ErrorNothingHappened =
        new(new LiteralMessage("no entity was enchanted"));

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var enchantment = RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                "enchantment", new ResourceArgument(Registries.ENCHANTMENT.Identifier))
            .Executes(context => Enchant(context, 1))
            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("level", IntegerArgumentType.Integer(0))
                .Executes(context => Enchant(context, IntegerArgumentType.GetInteger(context, "level"))));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("enchant")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("targets", EntityArgument.Entities())
                .Then(enchantment)));
    }

    //Enchant applies the enchantment to each target's main hand item, maps to vanilla enchant
    private static int Enchant(CommandContext<CommandSourceStack> context, int level)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var targets = EntityArgument.GetEntities(context, "targets");
        var holder = EnchantmentOf(context);
        if (holder.Value is not GameEnchantment enchantment)
        {
            source.SendFailure("enchantment definition is not loaded");
            return 0;
        }
        if (level > enchantment.Definition.MaxLevel)
            throw ErrorLevelTooHigh.Create(level, enchantment.Definition.MaxLevel);

        var success = 0;
        foreach (var target in targets)
        {
            if (target.Source is not LivingEntity living)
            {
                if (targets.Count == 1) throw ErrorNotLivingEntity.Create(target.Name);
                continue;
            }
            var stack = living.MainHandItem;
            if (stack.IsEmpty())
            {
                if (targets.Count == 1) throw ErrorNoItem.Create(target.Name);
                continue;
            }
            if (!enchantment.Definition.SupportedItems.Contains(stack.TypeHolder) || !Compatible(holder, stack))
            {
                if (targets.Count == 1) throw ErrorIncompatible.Create(stack.GetItem().Id.ToString());
                continue;
            }
            ApplyEnchantment(stack, holder, level);
            success++;
        }
        if (success == 0) throw ErrorNothingHappened.Create();

        var name = EnchantmentName(holder);
        if (targets.Count == 1)
            source.SendSuccess($"enchanted {targets[0].Name}'s main hand with {name} {level}");
        else
            source.SendSuccess($"enchanted {success} entities with {name} {level}");
        return success;
    }

    //EnchantmentOf resolves the parsed identifier into a holder, maps to vanilla ResourceArgument.getEnchantment
    private static Holder<NacreCraft.Registry.Enchantment> EnchantmentOf(CommandContext<CommandSourceStack> context)
    {
        var id = ResourceArgument.GetResource(context, "enchantment");
        return BuiltInRegistries.ENCHANTMENT.Get(id) ?? throw ResourceArgument.ErrorResourceNotFound.Create(id);
    }

    //Compatible rejects the enchantment when either side of the exclusive set overlaps, maps to vanilla isEnchantmentCompatible
    private static bool Compatible(Holder<NacreCraft.Registry.Enchantment> holder, ItemStack stack)
    {
        foreach (var (existing, _) in EnchantmentHelper.GetEnchantmentsForCrafting(stack).Levels)
        {
            if (existing.Value is GameEnchantment other && other.ExclusiveSet.Contains(holder)) return false;
            if (holder.Value is GameEnchantment enchantment && enchantment.ExclusiveSet.Contains(existing)) return false;
        }
        return true;
    }

    //ApplyEnchantment writes the level through the enchantment component, level 0 removes the entry like vanilla
    //The component is created when absent, so a plain item can be enchanted even though its base components carry no enchantment map
    private static void ApplyEnchantment(ItemStack stack, Holder<NacreCraft.Registry.Enchantment> holder, int level)
    {
        var mutable = new ItemEnchantments.Mutable(EnchantmentHelper.GetEnchantmentsForCrafting(stack));
        mutable.Set(holder, level);
        EnchantmentHelper.SetEnchantments(stack, mutable.ToImmutable());
    }

    //EnchantmentName the enchantment's short registry name, for the reply
    private static string EnchantmentName(Holder<NacreCraft.Registry.Enchantment> holder)
        => BuiltInRegistries.ENCHANTMENT.GetKey(holder.Value)?.ToShortString() ?? "unknown enchantment";
}
