namespace NacreCraft.Util.Random;

//BitRandomSource bit-driven random source, maps to vanilla net.minecraft.world.level.levelgen.BitRandomSource
//Vanilla gives every RandomSource method a default implementation in terms of next(bits); C# interfaces cannot re-implement
//the inherited abstract members, so each implementation expands those defaults explicitly
public interface BitRandomSource : RandomSource
{
    //FloatMultiplier float unit, maps to vanilla FLOAT_MULTIPLIER
    const float FloatMultiplier = 5.9604645E-8f;

    //DoubleMultiplier double unit, maps to vanilla DOUBLE_MULTIPLIER
    const double DoubleMultiplier = 1.1102230246251565E-16d;

    //Next advances the source and returns the top bits
    int Next(int bits);
}
