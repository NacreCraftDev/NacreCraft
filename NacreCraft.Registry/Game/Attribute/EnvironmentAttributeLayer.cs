using NacreCraft.Primitives;

namespace NacreCraft.Registry.Environment;

//EnvironmentAttributeLayer a modifier layer applied on top of a base attribute value
//maps to vanilla net.minecraft.world.attribute.EnvironmentAttributeLayer
//Vanilla makes the outer interface generic and nests the shapes inside it; C# nested types would then carry the outer type
//parameter as well, so the outer stays non-generic here and every shape brings its own
public interface EnvironmentAttributeLayer
{
    //Layer the untyped shape every layer exposes, so the sampler can drive layers of unknown value type
    //positional tells a position-dependent layer to apply; the non-positional sampler keeps the value unchanged
    public interface Layer
    {
        object Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
            bool positional);
    }

    //IPositionalLayer marks a layer that depends on position, including layers built from a weak-typed attribute
    public interface IPositionalLayer : Layer
    {
    }

    //IConstantLayer marks a layer that ignores time and position, so the sampler can fold it into the base value
    public interface IConstantLayer : Layer
    {
    }

    //Constant a layer that rewrites the value without looking at time or position
    public interface Constant<Value> : IConstantLayer
    {
        Value ApplyConstant(Value value);

        object Layer.Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
            bool positional)
            => ApplyConstant((Value)value)!;
    }

    //TimeBased a layer whose result depends on the world clock
    public interface TimeBased<Value> : Layer
    {
        Value ApplyTimeBased(Value value, int cacheTickId);

        object Layer.Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
            bool positional)
            => ApplyTimeBased((Value)value, cacheTickId)!;
    }

    //Positional a layer whose result depends on the sampling position
    public interface Positional<Value> : IPositionalLayer
    {
        Value ApplyPositional(Value value, Vec3 position, SpatialAttributeInterpolator? biomeWeights);

        object Layer.Apply(object value, int cacheTickId, Vec3 position, SpatialAttributeInterpolator? biomeWeights,
            bool positional)
            => positional ? ApplyPositional((Value)value, position, biomeWeights)! : value;
    }
}
