namespace NetCraft.Network;

//StreamMemberEncoder writes a value that carries its own buffer, maps to vanilla StreamMemberEncoder
//The argument order is reversed against StreamEncoder, which is what lets a method such as PacketType::encode be used directly
public interface StreamMemberEncoder<in B, V>
{
    //encode serializes value, taking the buffer from the value itself
    void Encode(V value, B buf);
}
