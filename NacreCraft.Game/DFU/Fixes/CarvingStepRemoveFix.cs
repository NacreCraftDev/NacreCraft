using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Carving step removal, maps to vanilla CarvingStepRemoveFix
//1.21.2 moves the old CarvingMasks.AIR into carving_mask and drops CarvingMasks
public sealed class CarvingStepRemoveFix(Schema outputSchema) : DataFix(outputSchema, false)
{
    protected override TypeRewriteRule MakeRule()
        => WriteFixAndRead("CarvingStepRemoveFix",
            GetInputSchema().GetType(References.Chunk),
            GetOutputSchema().GetType(References.Chunk),
            chunk =>
            {
                var carvingMasks = chunk.Get("CarvingMasks").Result();
                if (carvingMasks.IsPresent && carvingMasks.Get().Get("AIR").Result() is { IsPresent: true } airMask)
                    chunk = chunk.Set("carving_mask", airMask.Get());
                return chunk.Remove("CarvingMasks");
            });
}
