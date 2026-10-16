namespace NacreCraft.Network;

//StreamEncoder writes a value into a buffer, maps to vanilla net.minecraft.network.codec.StreamEncoder
//Kept apart from StreamCodec so that code which only writes can be passed where an encoder is expected
public interface StreamEncoder<in B, V>
{
    //encode serializes value into buffer
    void Encode(B buf, V value);
}
