namespace NacreCraft.Codec;

//RecordCodecBuilder, mirroring vanilla com.mojang.serialization.codecs.RecordCodecBuilder
//N-ary overloads emulate the vanilla group(...).apply(instance, ctor) chain
//Covers Of2..Of16, the usual record field counts
public static class RecordCodecBuilder
{
    public static Codec<T> Of1<T, F1>(FieldCodec<T, F1> f1, Func<F1, T> ctor)
        => new RecordCodec1<T, F1>(f1, ctor);

    public static Codec<T> Of2<T, F1, F2>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, Func<F1, F2, T> ctor)
        => new RecordCodec2<T, F1, F2>(f1, f2, ctor);

    public static Codec<T> Of3<T, F1, F2, F3>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        Func<F1, F2, F3, T> ctor)
        => new RecordCodec3<T, F1, F2, F3>(f1, f2, f3, ctor);

    public static Codec<T> Of4<T, F1, F2, F3, F4>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        Func<F1, F2, F3, F4, T> ctor)
        => new RecordCodec4<T, F1, F2, F3, F4>(f1, f2, f3, f4, ctor);

    public static Codec<T> Of5<T, F1, F2, F3, F4, F5>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, Func<F1, F2, F3, F4, F5, T> ctor)
        => new RecordCodec5<T, F1, F2, F3, F4, F5>(f1, f2, f3, f4, f5, ctor);

    public static Codec<T> Of6<T, F1, F2, F3, F4, F5, F6>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, Func<F1, F2, F3, F4, F5, F6, T> ctor)
        => new RecordCodec6<T, F1, F2, F3, F4, F5, F6>(f1, f2, f3, f4, f5, f6, ctor);

    public static Codec<T> Of7<T, F1, F2, F3, F4, F5, F6, F7>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        Func<F1, F2, F3, F4, F5, F6, F7, T> ctor)
        => new RecordCodec7<T, F1, F2, F3, F4, F5, F6, F7>(f1, f2, f3, f4, f5, f6, f7, ctor);

    public static Codec<T> Of8<T, F1, F2, F3, F4, F5, F6, F7, F8>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, T> ctor)
        => new RecordCodec8<T, F1, F2, F3, F4, F5, F6, F7, F8>(f1, f2, f3, f4, f5, f6, f7, f8, ctor);

    public static Codec<T> Of9<T, F1, F2, F3, F4, F5, F6, F7, F8, F9>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, T> ctor)
        => new RecordCodec9<T, F1, F2, F3, F4, F5, F6, F7, F8, F9>(f1, f2, f3, f4, f5, f6, f7, f8, f9, ctor);

    public static Codec<T> Of10<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, T> ctor)
        => new RecordCodec10<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, ctor);

    public static Codec<T> Of11<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, T> ctor)
        => new RecordCodec11<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, ctor);

    public static Codec<T> Of12<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11, FieldCodec<T, F12> f12,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, T> ctor)
        => new RecordCodec12<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, ctor);

    public static Codec<T> Of13<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11, FieldCodec<T, F12> f12,
        FieldCodec<T, F13> f13, Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, T> ctor)
        => new RecordCodec13<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, ctor);

    public static Codec<T> Of14<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11, FieldCodec<T, F12> f12,
        FieldCodec<T, F13> f13, FieldCodec<T, F14> f14,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, T> ctor)
        => new RecordCodec14<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, ctor);

    public static Codec<T> Of15<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11, FieldCodec<T, F12> f12,
        FieldCodec<T, F13> f13, FieldCodec<T, F14> f14, FieldCodec<T, F15> f15,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, T> ctor)
        => new RecordCodec15<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, ctor);

    public static Codec<T> Of16<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16>(
        FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3, FieldCodec<T, F4> f4,
        FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7, FieldCodec<T, F8> f8,
        FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11, FieldCodec<T, F12> f12,
        FieldCodec<T, F13> f13, FieldCodec<T, F14> f14, FieldCodec<T, F15> f15, FieldCodec<T, F16> f16,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16, T> ctor)
        => new RecordCodec16<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16>(f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, f16, ctor);
}

//1-field record codec
internal sealed class RecordCodec1<T, F1> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly Func<F1, T> _ctor;

    public RecordCodec1(FieldCodec<T, F1> f1, Func<F1, T> ctor)
    {
        _f1 = f1; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        return DataResult<T>.Success(_ctor(v1));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        return builder;
    }
}

//2-field record codec
//Decode walks the fields in order and calls the constructor; EncodeTo walks them into the builder
internal sealed class RecordCodec2<T, F1, F2> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly Func<F1, F2, T> _ctor;

    public RecordCodec2(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, Func<F1, F2, T> ctor)
    {
        _f1 = f1; _f2 = f2; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        return DataResult<T>.Success(_ctor(v1, v2));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        return builder;
    }
}

//3-field record codec
internal sealed class RecordCodec3<T, F1, F2, F3> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly Func<F1, F2, F3, T> _ctor;

    public RecordCodec3(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        Func<F1, F2, F3, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        return DataResult<T>.Success(_ctor(v1, v2, v3));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        return builder;
    }
}

//4-field record codec
internal sealed class RecordCodec4<T, F1, F2, F3, F4> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly Func<F1, F2, F3, F4, T> _ctor;

    public RecordCodec4(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, Func<F1, F2, F3, F4, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        return builder;
    }
}

//5-field record codec
internal sealed class RecordCodec5<T, F1, F2, F3, F4, F5> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly Func<F1, F2, F3, F4, F5, T> _ctor;

    public RecordCodec5(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, Func<F1, F2, F3, F4, F5, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        return builder;
    }
}

//6-field record codec
internal sealed class RecordCodec6<T, F1, F2, F3, F4, F5, F6> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly Func<F1, F2, F3, F4, F5, F6, T> _ctor;

    public RecordCodec6(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6,
        Func<F1, F2, F3, F4, F5, F6, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        return builder;
    }
}

//7-field record codec
internal sealed class RecordCodec7<T, F1, F2, F3, F4, F5, F6, F7> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, T> _ctor;

    public RecordCodec7(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        Func<F1, F2, F3, F4, F5, F6, F7, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        return builder;
    }
}

//8-field record codec
internal sealed class RecordCodec8<T, F1, F2, F3, F4, F5, F6, F7, F8> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, T> _ctor;

    public RecordCodec8(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, Func<F1, F2, F3, F4, F5, F6, F7, F8, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        return builder;
    }
}

//9-field record codec
internal sealed class RecordCodec9<T, F1, F2, F3, F4, F5, F6, F7, F8, F9> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, T> _ctor;

    public RecordCodec9(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        return builder;
    }
}

//10-field record codec
internal sealed class RecordCodec10<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, T> _ctor;

    public RecordCodec10(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        return builder;
    }
}

//11-field record codec
internal sealed class RecordCodec11<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, T> _ctor;

    public RecordCodec11(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        return builder;
    }
}

//12-field record codec
internal sealed class RecordCodec12<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly FieldCodec<T, F12> _f12;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, T> _ctor;

    public RecordCodec12(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        FieldCodec<T, F12> f12, Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _f12 = f12; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        var r12 = _f12.Codec.Decode(ops, input);
        if (!r12.TryGet(out var v12)) return Fail(r12);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        _f12.Codec.EncodeTo(ops, _f12.Getter(value), builder);
        return builder;
    }
}

//13-field record codec
internal sealed class RecordCodec13<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly FieldCodec<T, F12> _f12;
    private readonly FieldCodec<T, F13> _f13;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, T> _ctor;

    public RecordCodec13(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        FieldCodec<T, F12> f12, FieldCodec<T, F13> f13,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _f12 = f12; _f13 = f13; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        var r12 = _f12.Codec.Decode(ops, input);
        if (!r12.TryGet(out var v12)) return Fail(r12);
        var r13 = _f13.Codec.Decode(ops, input);
        if (!r13.TryGet(out var v13)) return Fail(r13);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        _f12.Codec.EncodeTo(ops, _f12.Getter(value), builder);
        _f13.Codec.EncodeTo(ops, _f13.Getter(value), builder);
        return builder;
    }
}

//14-field record codec
internal sealed class RecordCodec14<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly FieldCodec<T, F12> _f12;
    private readonly FieldCodec<T, F13> _f13;
    private readonly FieldCodec<T, F14> _f14;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, T> _ctor;

    public RecordCodec14(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        FieldCodec<T, F12> f12, FieldCodec<T, F13> f13, FieldCodec<T, F14> f14,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _f12 = f12; _f13 = f13; _f14 = f14; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        var r12 = _f12.Codec.Decode(ops, input);
        if (!r12.TryGet(out var v12)) return Fail(r12);
        var r13 = _f13.Codec.Decode(ops, input);
        if (!r13.TryGet(out var v13)) return Fail(r13);
        var r14 = _f14.Codec.Decode(ops, input);
        if (!r14.TryGet(out var v14)) return Fail(r14);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        _f12.Codec.EncodeTo(ops, _f12.Getter(value), builder);
        _f13.Codec.EncodeTo(ops, _f13.Getter(value), builder);
        _f14.Codec.EncodeTo(ops, _f14.Getter(value), builder);
        return builder;
    }
}

//15-field record codec
internal sealed class RecordCodec15<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly FieldCodec<T, F12> _f12;
    private readonly FieldCodec<T, F13> _f13;
    private readonly FieldCodec<T, F14> _f14;
    private readonly FieldCodec<T, F15> _f15;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, T> _ctor;

    public RecordCodec15(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        FieldCodec<T, F12> f12, FieldCodec<T, F13> f13, FieldCodec<T, F14> f14, FieldCodec<T, F15> f15,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _f12 = f12; _f13 = f13; _f14 = f14; _f15 = f15; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        var r12 = _f12.Codec.Decode(ops, input);
        if (!r12.TryGet(out var v12)) return Fail(r12);
        var r13 = _f13.Codec.Decode(ops, input);
        if (!r13.TryGet(out var v13)) return Fail(r13);
        var r14 = _f14.Codec.Decode(ops, input);
        if (!r14.TryGet(out var v14)) return Fail(r14);
        var r15 = _f15.Codec.Decode(ops, input);
        if (!r15.TryGet(out var v15)) return Fail(r15);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        _f12.Codec.EncodeTo(ops, _f12.Getter(value), builder);
        _f13.Codec.EncodeTo(ops, _f13.Getter(value), builder);
        _f14.Codec.EncodeTo(ops, _f14.Getter(value), builder);
        _f15.Codec.EncodeTo(ops, _f15.Getter(value), builder);
        return builder;
    }
}

//16-field record codec
internal sealed class RecordCodec16<T, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16> : AbstractMapCodec<T>
{
    private readonly FieldCodec<T, F1> _f1;
    private readonly FieldCodec<T, F2> _f2;
    private readonly FieldCodec<T, F3> _f3;
    private readonly FieldCodec<T, F4> _f4;
    private readonly FieldCodec<T, F5> _f5;
    private readonly FieldCodec<T, F6> _f6;
    private readonly FieldCodec<T, F7> _f7;
    private readonly FieldCodec<T, F8> _f8;
    private readonly FieldCodec<T, F9> _f9;
    private readonly FieldCodec<T, F10> _f10;
    private readonly FieldCodec<T, F11> _f11;
    private readonly FieldCodec<T, F12> _f12;
    private readonly FieldCodec<T, F13> _f13;
    private readonly FieldCodec<T, F14> _f14;
    private readonly FieldCodec<T, F15> _f15;
    private readonly FieldCodec<T, F16> _f16;
    private readonly Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16, T> _ctor;

    public RecordCodec16(FieldCodec<T, F1> f1, FieldCodec<T, F2> f2, FieldCodec<T, F3> f3,
        FieldCodec<T, F4> f4, FieldCodec<T, F5> f5, FieldCodec<T, F6> f6, FieldCodec<T, F7> f7,
        FieldCodec<T, F8> f8, FieldCodec<T, F9> f9, FieldCodec<T, F10> f10, FieldCodec<T, F11> f11,
        FieldCodec<T, F12> f12, FieldCodec<T, F13> f13, FieldCodec<T, F14> f14, FieldCodec<T, F15> f15,
        FieldCodec<T, F16> f16,
        Func<F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16, T> ctor)
    {
        _f1 = f1; _f2 = f2; _f3 = f3; _f4 = f4; _f5 = f5; _f6 = f6; _f7 = f7; _f8 = f8; _f9 = f9; _f10 = f10; _f11 = f11; _f12 = f12; _f13 = f13; _f14 = f14; _f15 = f15; _f16 = f16; _ctor = ctor;
    }

    public override DataResult<T> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var r1 = _f1.Codec.Decode(ops, input);
        if (!r1.TryGet(out var v1)) return Fail(r1);
        var r2 = _f2.Codec.Decode(ops, input);
        if (!r2.TryGet(out var v2)) return Fail(r2);
        var r3 = _f3.Codec.Decode(ops, input);
        if (!r3.TryGet(out var v3)) return Fail(r3);
        var r4 = _f4.Codec.Decode(ops, input);
        if (!r4.TryGet(out var v4)) return Fail(r4);
        var r5 = _f5.Codec.Decode(ops, input);
        if (!r5.TryGet(out var v5)) return Fail(r5);
        var r6 = _f6.Codec.Decode(ops, input);
        if (!r6.TryGet(out var v6)) return Fail(r6);
        var r7 = _f7.Codec.Decode(ops, input);
        if (!r7.TryGet(out var v7)) return Fail(r7);
        var r8 = _f8.Codec.Decode(ops, input);
        if (!r8.TryGet(out var v8)) return Fail(r8);
        var r9 = _f9.Codec.Decode(ops, input);
        if (!r9.TryGet(out var v9)) return Fail(r9);
        var r10 = _f10.Codec.Decode(ops, input);
        if (!r10.TryGet(out var v10)) return Fail(r10);
        var r11 = _f11.Codec.Decode(ops, input);
        if (!r11.TryGet(out var v11)) return Fail(r11);
        var r12 = _f12.Codec.Decode(ops, input);
        if (!r12.TryGet(out var v12)) return Fail(r12);
        var r13 = _f13.Codec.Decode(ops, input);
        if (!r13.TryGet(out var v13)) return Fail(r13);
        var r14 = _f14.Codec.Decode(ops, input);
        if (!r14.TryGet(out var v14)) return Fail(r14);
        var r15 = _f15.Codec.Decode(ops, input);
        if (!r15.TryGet(out var v15)) return Fail(r15);
        var r16 = _f16.Codec.Decode(ops, input);
        if (!r16.TryGet(out var v16)) return Fail(r16);
        return DataResult<T>.Success(_ctor(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, T value, RecordBuilder<U> builder)
    {
        _f1.Codec.EncodeTo(ops, _f1.Getter(value), builder);
        _f2.Codec.EncodeTo(ops, _f2.Getter(value), builder);
        _f3.Codec.EncodeTo(ops, _f3.Getter(value), builder);
        _f4.Codec.EncodeTo(ops, _f4.Getter(value), builder);
        _f5.Codec.EncodeTo(ops, _f5.Getter(value), builder);
        _f6.Codec.EncodeTo(ops, _f6.Getter(value), builder);
        _f7.Codec.EncodeTo(ops, _f7.Getter(value), builder);
        _f8.Codec.EncodeTo(ops, _f8.Getter(value), builder);
        _f9.Codec.EncodeTo(ops, _f9.Getter(value), builder);
        _f10.Codec.EncodeTo(ops, _f10.Getter(value), builder);
        _f11.Codec.EncodeTo(ops, _f11.Getter(value), builder);
        _f12.Codec.EncodeTo(ops, _f12.Getter(value), builder);
        _f13.Codec.EncodeTo(ops, _f13.Getter(value), builder);
        _f14.Codec.EncodeTo(ops, _f14.Getter(value), builder);
        _f15.Codec.EncodeTo(ops, _f15.Getter(value), builder);
        _f16.Codec.EncodeTo(ops, _f16.Getter(value), builder);
        return builder;
    }
}
