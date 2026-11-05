using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistrySpawnCondition = NacreCraft.Registry.SpawnCondition;

namespace NacreCraft.Game.World.Entity.Variant;

//SpawnCondition one spawn-time condition, maps to vanilla net.minecraft.world.entity.variant.SpawnCondition
//The registry element type is the Registry layer's SpawnCondition marker; this interface adds the predicate and codec half
public interface SpawnCondition : RegistrySpawnCondition, SelectorCondition<SpawnContext>
{
    public static readonly Codec<SpawnCondition> CODEC = new SpawnConditionDispatchCodec();

    MapCodec<RegistrySpawnCondition> Codec();
}

//SpawnConditionDispatchCodec dispatches on the type field through the spawn_condition_type registry, maps to vanilla CODEC
internal sealed class SpawnConditionDispatchCodec : ScalarCodec<SpawnCondition>
{
    public override DataResult<SpawnCondition> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<SpawnCondition>.Error(() => "spawn condition must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<SpawnCondition>.Error(() => "spawn condition is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<SpawnCondition>.Error(() => "spawn condition type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<SpawnCondition>.Error(() => $"Invalid spawn condition id: {typeText.Get()}");
        var codec = BuiltInRegistries.SPAWN_CONDITION_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<SpawnCondition>.Error(() => $"Unknown spawn condition type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(condition => condition is SpawnCondition typed
            ? DataResult<SpawnCondition>.Success(typed)
            : DataResult<SpawnCondition>.Error(() => $"Spawn condition {id.Value} is not a spawn condition"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, SpawnCondition value)
    {
        var codec = value.Codec();
        var id = BuiltInRegistries.SPAWN_CONDITION_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered spawn condition codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}
