using NacreCraft.Game.World.Level.LevelGen.Structure;

namespace NacreCraft.Game.Commands.Arguments;

//TemplateRotationArgument structure rotation, maps to vanilla net.minecraft.commands.arguments.TemplateRotationArgument
public sealed class TemplateRotationArgument : StringRepresentableArgument<Rotation>
{
    private static readonly Rotation[] Values =
    {
        Rotation.None, Rotation.Clockwise90, Rotation.Clockwise180, Rotation.Counterclockwise90,
    };

    private TemplateRotationArgument()
        : base(() => Values, rotation => rotation.Name(), StructureTransforms.TryParseRotation)
    {
    }

    public static TemplateRotationArgument TemplateRotation() => new();
}
