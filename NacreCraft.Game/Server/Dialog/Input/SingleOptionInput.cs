using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//SingleOptionInput a radio-button input, maps to vanilla net.minecraft.server.dialog.input.SingleOptionInput
public sealed record SingleOptionInput(int Width, IReadOnlyList<SingleOptionInput.Entry> Entries, Component Label,
    bool LabelVisible) : InputControl
{
    public static readonly MapCodec<SingleOptionInput> MAP_CODEC = RecordCodecBuilder.Of4(
        InputControls.WidthCodec.OptionalFieldOf("width", 200).ForGetter((SingleOptionInput input) => input.Width),
        ExtraCodecs.NonEmptyList(Entry.CODEC.ListOf()).FieldOf("options")
            .ForGetter((SingleOptionInput input) => input.Entries),
        ComponentSerialization.JsonCodec.FieldOf("label").ForGetter((SingleOptionInput input) => input.Label),
        Codecs.Bool.OptionalFieldOf("label_visible", true).ForGetter((SingleOptionInput input) => input.LabelVisible),
        (width, entries, label, labelVisible) => new SingleOptionInput(width, entries, label, labelVisible))
        .ComapFlatMap(Validate, input => input);

    public static readonly MapCodec<RegistryInputControl> ControlCodec =
        new XmapMapCodec<SingleOptionInput, RegistryInputControl>(MAP_CODEC, value => value,
            control => (SingleOptionInput)control);

    public MapCodec<RegistryInputControl> MapCodec() => ControlCodec;

    //Initial the entry selected by default, maps to vanilla initial
    public Optional<Entry> Initial()
    {
        foreach (var entry in Entries)
            if (entry.Initial) return Optional<Entry>.Of(entry);
        return Optional<Entry>.Empty();
    }

    private static DataResult<SingleOptionInput> Validate(SingleOptionInput input)
    {
        var initialCount = input.Entries.Count(entry => entry.Initial);
        return initialCount > 1
            ? DataResult<SingleOptionInput>.Error(() => "Multiple initial values")
            : DataResult<SingleOptionInput>.Success(input);
    }

    public sealed record Entry(string Id, Optional<Component> Display, bool Initial)
    {
        public static readonly Codec<Entry> FULL_CODEC = RecordCodecBuilder.Of3(
            Codecs.String.FieldOf("id").ForGetter((Entry entry) => entry.Id),
            ComponentSerialization.JsonCodec.OptionalFieldOf("display").ForGetter((Entry entry) => entry.Display),
            Codecs.Bool.OptionalFieldOf("initial", false).ForGetter((Entry entry) => entry.Initial),
            (id, display, initial) => new Entry(id, display, initial));

        public static readonly Codec<Entry> CODEC = Codecs.WithAlternative(FULL_CODEC,
            Codecs.String.ComapFlatMap(id => DataResult<Entry>.Success(new Entry(id, Optional<Component>.Empty(), false)),
                entry => entry.Id));

        //DisplayOrDefault the display text, falling back to the id, maps to vanilla displayOrDefault
        public Component DisplayOrDefault() => Display.IsPresent ? Display.Get() : Component.Literal(Id);
    }
}
