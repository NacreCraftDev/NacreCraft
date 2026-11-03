namespace NacreCraft.Registry.Environment;

//EnvironmentAttributeLayer a modifier layer applied on top of a base attribute value
//maps to vanilla net.minecraft.world.attribute.EnvironmentAttributeLayer
//Vanilla makes the outer interface generic and nests the shapes inside it; C# nested types would then carry the outer type
//parameter as well, so the outer stays non-generic here and every shape brings its own
//Only the time-based and constant shapes are ported; the positional shape also needs the spatial interpolator, which has no port yet
public interface EnvironmentAttributeLayer
{
    //TimeBased a layer whose result depends on the world clock
    public interface TimeBased<Value>
    {
        Value ApplyTimeBased(Value value, int cacheTickId);
    }

    //Constant a layer that rewrites the value without looking at time or position
    public interface Constant<Value>
    {
        Value ApplyConstant(Value value);
    }
}
