using NacreCraft.Game.World.Level.LevelGen.Structure;

namespace NacreCraft.Game.Commands.Arguments;

//TemplateMirrorArgument structure mirror, maps to vanilla net.minecraft.commands.arguments.TemplateMirrorArgument
public sealed class TemplateMirrorArgument : StringRepresentableArgument<Mirror>
{
    private static readonly Mirror[] Values = { Mirror.None, Mirror.LeftRight, Mirror.FrontBack };

    private TemplateMirrorArgument()
        : base(() => Values, mirror => mirror.Name(), StructureTransforms.TryParseMirror)
    {
    }

    public static StringRepresentableArgument<Mirror> TemplateMirror() => new TemplateMirrorArgument();
}
