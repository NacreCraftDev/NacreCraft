using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//LocateCommand locate command, maps to vanilla net.minecraft.server.commands.LocateCommand
//The structure/biome/poi registries exist but no world generator search (nearest structure, closest biome, poi lookup) is
//exposed to commands yet, so the vanilla tree is registered and each branch reports that locating is unavailable
public static class LocateCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var structure = LiteralArgumentBuilder<CommandSourceStack>.Literal("structure")
            .Then(RequiredArgumentBuilder<CommandSourceStack, ResourceOrTagKeyArgument<NacreCraft.Registry.Structure>.Result>
                .Argument("structure", new ResourceOrTagKeyArgument<NacreCraft.Registry.Structure>(BuiltInRegistries.STRUCTURE))
                .Executes(context => Fail(context, "structure")));

        var biome = LiteralArgumentBuilder<CommandSourceStack>.Literal("biome")
            .Then(RequiredArgumentBuilder<CommandSourceStack, ResourceOrTagArgument<Biome>.Result>
                .Argument("biome", new ResourceOrTagArgument<Biome>(BuiltInRegistries.BIOME))
                .Executes(context => Fail(context, "biome")));

        var poi = LiteralArgumentBuilder<CommandSourceStack>.Literal("poi")
            .Then(RequiredArgumentBuilder<CommandSourceStack, ResourceOrTagArgument<PoiType>.Result>
                .Argument("poi", new ResourceOrTagArgument<PoiType>(BuiltInRegistries.POINT_OF_INTEREST_TYPE))
                .Executes(context => Fail(context, "poi")));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("locate")
            .Requires(s => s.HasPermission(2))
            .Then(structure)
            .Then(biome)
            .Then(poi));
    }

    //Fail reports that the world search for the requested element kind is not wired up
    private static int Fail(CommandContext<CommandSourceStack> context, string kind)
    {
        context.GetSource().SendFailure($"{kind} locating is not wired to the world generator yet");
        return 0;
    }
}
