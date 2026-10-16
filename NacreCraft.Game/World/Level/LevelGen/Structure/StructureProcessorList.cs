using NacreCraft.Codec;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//StructureProcessorList processor list, maps to vanilla StructureProcessorList
//A processor list is one processing chain; template pools reference it by the processor_list registry name
public sealed class StructureProcessorList : NacreCraft.Registry.StructureProcessorList
{
    //ElementCodec registry element codec, maps to vanilla LIST_OBJECT_CODEC and DIRECT_CODEC
    public static readonly Codec<NacreCraft.Registry.StructureProcessorList> ElementCodec = new StructureProcessorListCodec();

    public IReadOnlyList<StructureProcessor> Processors { get; }

    public StructureProcessorList(IReadOnlyList<StructureProcessor> processors) => Processors = processors;

    //List returns the processor list, maps to vanilla list
    public IReadOnlyList<StructureProcessor> List() => Processors;

    public override string ToString()
        => $"ProcessorList[{string.Join(", ", Processors.Select(p => p.GetType().Name))}]";
}

//StructureProcessorListCodec processor list codec; handles vanilla's two shapes, wrapped in processors or a bare array
internal sealed class StructureProcessorListCodec : ScalarCodec<NacreCraft.Registry.StructureProcessorList>
{
    public override DataResult<NacreCraft.Registry.StructureProcessorList> Parse<U>(DynamicOps<U> ops, U input)
    {
        //Form one {"processors":[...]}, form two a bare processor array
        var mapResult = ops.GetMap(input);
        if (mapResult.Result().IsPresent)
        {
            var processorsTag = mapResult.GetOrThrow().Get("processors");
            if (!processorsTag.IsPresent)
                return DataResult<NacreCraft.Registry.StructureProcessorList>.Error(() => "processor_list is missing processors");
            return ParseList(ops, processorsTag.Get());
        }
        return ParseList(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, NacreCraft.Registry.StructureProcessorList value)
        => DataResult<U>.Error(() => "processor list encoding not implemented yet");

    //ParseList dispatches each entry by processor_type
    private static DataResult<NacreCraft.Registry.StructureProcessorList> ParseList<U>(DynamicOps<U> ops, U input)
    {
        var stream = ops.GetStream(input);
        if (!stream.Result().IsPresent)
            return DataResult<NacreCraft.Registry.StructureProcessorList>.Error(() => "processors must be an array");
        var processors = new List<StructureProcessor>();
        foreach (var element in stream.GetOrThrow())
        {
            var parsed = StructureProcessorType.SingleCodec.Parse(ops, element);
            if (!parsed.Result().IsPresent)
                return DataResult<NacreCraft.Registry.StructureProcessorList>.Error(() => "failed to parse an entry in the processor list");
            processors.Add(parsed.GetOrThrow());
        }
        return DataResult<NacreCraft.Registry.StructureProcessorList>.Success(new StructureProcessorList(processors));
    }
}
