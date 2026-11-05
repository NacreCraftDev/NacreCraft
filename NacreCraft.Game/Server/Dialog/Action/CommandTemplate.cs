using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using RegistryAction = NacreCraft.Registry.Action;

namespace NacreCraft.Game.Server.Dialog;

//CommandTemplate a dialog action running a macro command, maps to vanilla net.minecraft.server.dialog.action.CommandTemplate
public sealed record CommandTemplate(ParsedTemplate Template) : Action
{
    public static readonly MapCodec<CommandTemplate> MAP_CODEC = RecordCodecBuilder.Of1(
        ParsedTemplate.CODEC.FieldOf("template").ForGetter((CommandTemplate value) => value.Template),
        template => new CommandTemplate(template));

    public static readonly MapCodec<RegistryAction> ActionCodec =
        new XmapMapCodec<CommandTemplate, RegistryAction>(MAP_CODEC, value => value, action => (CommandTemplate)action);

    public MapCodec<RegistryAction> Codec() => ActionCodec;

    public Optional<ClickEvent> CreateAction(IReadOnlyDictionary<string, Action.ValueGetter> parameters)
        => Optional<ClickEvent>.Of(new ClickEvent.RunCommand(
            Template.Instantiate(Action.ValueGetter.GetAsTemplateSubstitutions(parameters))));
}

