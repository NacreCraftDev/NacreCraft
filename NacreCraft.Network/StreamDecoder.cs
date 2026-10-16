namespace NacreCraft.Network;

//StreamDecoder reads a value out of a buffer, maps to vanilla net.minecraft.network.codec.StreamDecoder
//Kept apart from StreamCodec so that code which only reads can be passed where a decoder is expected
public interface StreamDecoder<in B, V>
{
    //decode deserializes a value out of buffer
    V Decode(B buf);
}
