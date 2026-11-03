using NacreCraft.Codec;
using NacreCraft.Registry.Context;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Loot;

//LootContext the per-resolution state a loot table reads, maps to vanilla LootContext
public sealed class LootContext
{
    private readonly LootParams _params;
    private readonly HashSet<object> _visited = new();

    private LootContext(LootParams parameters, RandomSource random)
    {
        _params = parameters;
        Random = random;
    }

    public RandomSource Random { get; }

    public float Luck => _params.Luck;

    public ServerLevel Level => _params.Level;

    public bool HasParameter(ContextKey key) => _params.Params.Has(key);

    public T GetParameter<T>(ContextKey<T> key) => _params.Params.GetOrThrow(key);

    public T? GetOptionalParameter<T>(ContextKey<T> key) => _params.Params.GetOptional(key);

    //PushVisitedElement stops a loot table from recursing into itself, maps to vanilla pushVisitedElement
    public bool PushVisitedElement(object element) => _visited.Add(element);

    public void PopVisitedElement(object element) => _visited.Remove(element);

    //EntityTarget which entity of the context an entity condition reads, maps to vanilla LootContext.EntityTarget
    public enum EntityTarget
    {
        This,
        Attacker,
        DirectAttacker,
        AttackingPlayer,
        TargetEntity,
        InteractingEntity,
    }

    //EntityTargets name codec and lookup for the entity target, maps to vanilla LootContext.EntityTarget.CODEC
    public static class EntityTargets
    {
        //Codec encodes the target by its serialized name
        public static readonly Codec<EntityTarget> Codec = Codecs.String.ComapFlatMap(
            name => TryParse(name) is { } target
                ? DataResult<EntityTarget>.Success(target)
                : DataResult<EntityTarget>.Error(() => $"Invalid entity target {name}"),
            GetSerializedName);

        //GetEntity reads the entity the target points at, null when the context does not carry it
        public static NacreCraft.Registry.Entity? GetEntity(EntityTarget target, LootContext context) => target switch
        {
            EntityTarget.This => context.GetOptionalParameter(LootContextParams.ThisEntity),
            EntityTarget.Attacker => context.GetOptionalParameter(LootContextParams.AttackingEntity),
            EntityTarget.DirectAttacker => context.GetOptionalParameter(LootContextParams.DirectAttackingEntity),
            EntityTarget.AttackingPlayer => context.GetOptionalParameter(LootContextParams.LastDamagePlayer),
            EntityTarget.TargetEntity => context.GetOptionalParameter(LootContextParams.TargetEntity),
            _ => context.GetOptionalParameter(LootContextParams.InteractingEntity),
        };

        public static string GetSerializedName(EntityTarget target) => target switch
        {
            EntityTarget.This => "this",
            EntityTarget.Attacker => "attacker",
            EntityTarget.DirectAttacker => "direct_attacker",
            EntityTarget.AttackingPlayer => "attacking_player",
            EntityTarget.TargetEntity => "target_entity",
            _ => "interacting_entity",
        };

        private static EntityTarget? TryParse(string name) => name switch
        {
            "this" => EntityTarget.This,
            "attacker" => EntityTarget.Attacker,
            "direct_attacker" => EntityTarget.DirectAttacker,
            "attacking_player" => EntityTarget.AttackingPlayer,
            "target_entity" => EntityTarget.TargetEntity,
            "interacting_entity" => EntityTarget.InteractingEntity,
            _ => null,
        };
    }

    public sealed class Builder
    {
        private readonly LootParams _params;
        private RandomSource? _random;

        public Builder(LootParams parameters) => _params = parameters;

        public Builder WithOptionalRandomSource(RandomSource random)
        {
            _random = random;
            return this;
        }

        public LootContext Create() => new(_params, _random ?? _params.Level.Random);
    }
}
