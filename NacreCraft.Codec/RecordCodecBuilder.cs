namespace NacreCraft.Codec;

//RecordCodecBuilder, mirroring vanilla com.mojang.serialization.codecs.RecordCodecBuilder
//The n-ary overloads emulate the vanilla group(...).apply(instance, ctor) chain
//C# has no variadic generics, so Of1..Of16 and RecordCodec1..16 are emitted by NacreCraft.SourceGenerator
[GenerateRecordCodecs(16)]
public static partial class RecordCodecBuilder
{
}

//GenerateRecordCodecs marks the partial class that receives the generated OfN factories
[AttributeUsage(AttributeTargets.Class)]
internal sealed class GenerateRecordCodecsAttribute : Attribute
{
    public GenerateRecordCodecsAttribute(int maxFields) => MaxFields = maxFields;

    public int MaxFields { get; }
}
