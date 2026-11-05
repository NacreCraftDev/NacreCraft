using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//NumberRangeInput a slider input, maps to vanilla net.minecraft.server.dialog.input.NumberRangeInput
public sealed record NumberRangeInput(int Width, Component Label, string LabelFormat, NumberRangeInput.RangeInfo RangeInfoValue)
    : InputControl
{
    public static readonly MapCodec<NumberRangeInput> MAP_CODEC = RecordCodecBuilder.Of4(
        InputControls.WidthCodec.OptionalFieldOf("width", 200).ForGetter((NumberRangeInput input) => input.Width),
        ComponentSerialization.JsonCodec.FieldOf("label").ForGetter((NumberRangeInput input) => input.Label),
        Codecs.String.OptionalFieldOf("label_format", "options.generic_value")
            .ForGetter((NumberRangeInput input) => input.LabelFormat),
        RangeInfo.MAP_CODEC.ForGetter((NumberRangeInput input) => input.RangeInfoValue),
        (width, label, labelFormat, rangeInfo) => new NumberRangeInput(width, label, labelFormat, rangeInfo));

    public static readonly MapCodec<RegistryInputControl> ControlCodec =
        new XmapMapCodec<NumberRangeInput, RegistryInputControl>(MAP_CODEC, value => value,
            control => (NumberRangeInput)control);

    public MapCodec<RegistryInputControl> MapCodec() => ControlCodec;

    //ComputeLabel the label shown for the current slider value, maps to vanilla computeLabel
    public Component ComputeLabel(string value) => Component.Translatable(LabelFormat, Label, value);

    public sealed record RangeInfo(float Start, float End, Optional<float> Initial, Optional<float> Step)
    {
        public static readonly MapCodec<RangeInfo> MAP_CODEC = RecordCodecBuilder.Of4(
            Codecs.Float.FieldOf("start").ForGetter((RangeInfo range) => range.Start),
            Codecs.Float.FieldOf("end").ForGetter((RangeInfo range) => range.End),
            Codecs.Float.OptionalFieldOf("initial").ForGetter((RangeInfo range) => range.Initial),
            ExtraCodecs.PositiveFloat.OptionalFieldOf("step").ForGetter((RangeInfo range) => range.Step),
            (start, end, initial, step) => new RangeInfo(start, end, initial, step))
            .ComapFlatMap(Validate, range => range);

        private static DataResult<RangeInfo> Validate(RangeInfo range)
        {
            if (!range.Initial.IsPresent) return DataResult<RangeInfo>.Success(range);
            var initial = range.Initial.Get();
            var min = Math.Min(range.Start, range.End);
            var max = Math.Max(range.Start, range.End);
            return initial < min || initial > max
                ? DataResult<RangeInfo>.Error(() => $"Initial value {initial} is outside of range [{min}, {max}]")
                : DataResult<RangeInfo>.Success(range);
        }

        //ComputeScaledValue snaps a slider value onto the step grid, maps to vanilla computeScaledValue
        public float ComputeScaledValue(float sliderValue)
        {
            var valueInRange = Mth.Lerp(sliderValue, Start, End);
            if (!Step.IsPresent) return valueInRange;
            var step = Step.Get();
            var initialValue = InitialScaledValue();
            var deltaToInitial = valueInRange - initialValue;
            var stepsOutsideInitial = (int)MathF.Floor(deltaToInitial / step + 0.5f);
            var result = initialValue + stepsOutsideInitial * step;
            if (!IsOutOfRange(result)) return result;
            var oneStepLess = stepsOutsideInitial - Math.Sign(stepsOutsideInitial);
            return initialValue + oneStepLess * step;
        }

        private bool IsOutOfRange(float scaledValue)
        {
            var sliderPos = ScaledValueToSlider(scaledValue);
            return sliderPos < 0.0f || sliderPos > 1.0f;
        }

        private float InitialScaledValue()
            => Initial.IsPresent ? Initial.Get() : (Start + End) / 2.0f;

        //InitialSliderValue the slider position of the initial value, maps to vanilla initialSliderValue
        public float InitialSliderValue() => ScaledValueToSlider(InitialScaledValue());

        private float ScaledValueToSlider(float value)
            => Start == End ? 0.5f : Mth.InverseLerp(value, Start, End);
    }
}
