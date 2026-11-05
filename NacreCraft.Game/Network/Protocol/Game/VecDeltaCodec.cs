//VecDeltaCodec maps to vanilla net.minecraft.network.protocol.game.VecDeltaCodec
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//VecDeltaCodec quantizes absolute positions to 1/4096 of a block and encodes the difference from a baseline
//Each axis delta is a long in vanilla; the move packets narrow them to short after checking the displacement
//EntityTracker currently does the same 4096-scale encoding inline, so this class is not yet used by any packet
public sealed class VecDeltaCodec
{
    //TruncationSteps fixed-point precision, maps to vanilla 4096.0
    private const double TruncationSteps = 4096.0;

    //_base position the deltas are measured from, maps to vanilla base
    private Vec3 _base = Vec3.Zero;

    //Encode quantizes one absolute coordinate, maps to vanilla static encode
    public static long Encode(double input) => (long)Math.Round(input * TruncationSteps);

    //Decode converts a quantized coordinate back to a double, maps to vanilla static decode
    public static double Decode(long value) => value / TruncationSteps;

    //Decode applies per-axis deltas to the baseline, maps to vanilla decode(long,long,long)
    //A zero delta means that axis is unchanged and takes the baseline value without re-quantizing
    public Vec3 Decode(long xa, long ya, long za)
    {
        if (xa == 0 && ya == 0 && za == 0) return _base;
        var x = xa == 0 ? _base.X : Decode(Encode(_base.X) + xa);
        var y = ya == 0 ? _base.Y : Decode(Encode(_base.Y) + ya);
        var z = za == 0 ? _base.Z : Decode(Encode(_base.Z) + za);
        return new Vec3(x, y, z);
    }

    //EncodeX encodes the X delta from the baseline, maps to vanilla encodeX
    public long EncodeX(Vec3 pos) => Encode(pos.X) - Encode(_base.X);

    //EncodeY encodes the Y delta from the baseline, maps to vanilla encodeY
    public long EncodeY(Vec3 pos) => Encode(pos.Y) - Encode(_base.Y);

    //EncodeZ encodes the Z delta from the baseline, maps to vanilla encodeZ
    public long EncodeZ(Vec3 pos) => Encode(pos.Z) - Encode(_base.Z);

    //Delta returns the unquantized displacement from the baseline, maps to vanilla delta
    public Vec3 Delta(Vec3 pos) => pos.Subtract(_base);

    //SetBase replaces the baseline, maps to vanilla setBase
    public void SetBase(Vec3 basePos) => _base = basePos;

    //GetBase returns the current baseline, maps to vanilla getBase
    public Vec3 GetBase() => _base;
}
