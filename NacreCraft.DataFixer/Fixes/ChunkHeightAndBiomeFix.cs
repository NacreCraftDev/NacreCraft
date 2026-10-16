namespace NacreCraft.DataFixer.Fixes;

using System;
using NacreCraft.DataFixer.Schemas;

//chunk height and biome fix, maps to vanilla net.minecraft.util.datafix.fixes.ChunkHeightAndBiomeFix
//upgrades old 16-section chunks to 24 sections, adding the -4..19 range to align with 1.18's new height
//the full MakeRule implementation depends on SerializableChunkData.copyOf/read + NoiseRouterData + JigsawBlockEntity
//all three belong to the NacreCraft.Game business domain, to be wired up once the game module is developed
public class ChunkHeightAndBiomeFix : DataFix
{
    //DATAFIXER_CONTEXT_TAG data fix context tag name; SimpleRegionStorage holds a constant with the same name
    public const string DatafixerContextTag = "__context";

    public ChunkHeightAndBiomeFix(Schema outputSchema) : base(outputSchema, changesType: true) { }

    //the full makeRule implementation depends on NacreCraft.Game's SerializableChunkData and other game content
    //NacreCraft.Game is an optional business module; implement this once it is finished
    protected override TypeRewriteRule MakeRule()
        => throw new NotSupportedException("ChunkHeightAndBiomeFix.MakeRule pending NacreCraft.Game business module integration");
}
