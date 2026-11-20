using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//Interaction an invisible entity that records the last player attack/interaction, maps to vanilla net.minecraft.world.entity.Interaction
//Used by the /summon interaction entity for datapack-driven hitboxes; NC stores width/height/response as plain fields instead of
//synced data and resolves player uuids through the level
public sealed class Interaction : NacreCraft.Registry.Entity, Attackable, Targeting
{
    private PlayerAction? _attack;
    private PlayerAction? _interaction;
    private float _width = 1f;
    private float _height = 1f;
    private bool _response;

    public override Identifier Id => Identifier.WithDefaultNamespace("interaction");

    public override double Width => _width;
    public override double Height => _height;

    public EntityDimensions Dimensions => EntityDimensions.Scalable(_width, _height);

    public override AABB MakeBoundingBox(Vec3 pos) => Dimensions.MakeBoundingBox(pos);

    public override bool CanBeHitByProjectile => false;
    public override bool IsPickable => true;
    public override bool IsIgnoringBlockTriggers => true;
    public override NacreCraft.Registry.Enums.PushReaction PistonPushReaction
        => NacreCraft.Registry.Enums.PushReaction.ignore;

    public float GetWidth() => _width;
    public void SetWidth(float width) => _width = width;
    public float GetHeight() => _height;
    public void SetHeight(float height) => _height = height;
    public bool GetResponse() => _response;
    public void SetResponse(bool response) => _response = response;

    //SkipAttackInteraction records the attacking player on the server, maps to vanilla skipAttackInteraction
    public override bool SkipAttackInteraction(NacreCraft.Registry.Entity? source)
    {
        if (source is not Player player || Level is not ServerLevel level) return false;
        _attack = new PlayerAction(player.Uuid, level.GameTime);
        return !_response;
    }

    //Hurt the interaction entity takes no damage, maps to vanilla hurtServer returning false
    public override bool Hurt(float amount, Vec3? knockbackSource = null) => false;

    //Interact records the interacting player, maps to vanilla interact
    public InteractionResult Interact(Player player, InteractionHand hand, Vec3 location)
    {
        _ = hand;
        _ = location;
        if (Level is ServerLevel level) _interaction = new PlayerAction(player.Uuid, level.GameTime);
        return _response ? InteractionResult.Success : InteractionResult.Consume;
    }

    public override void Tick()
    {
    }

    public LivingEntity? GetLastAttacker() => ResolvePlayer(_attack);

    public LivingEntity? GetTarget() => ResolvePlayer(_interaction);

    private LivingEntity? ResolvePlayer(PlayerAction? action)
        => action is not null && Level is ServerLevel level && level.GetEntity(action.Player) is Player player
            ? player
            : null;

    protected override void AddAdditionalSaveData(CompoundTag tag)
    {
        base.AddAdditionalSaveData(tag);
        tag.PutFloat("width", _width);
        tag.PutFloat("height", _height);
        tag.PutBoolean("response", _response);
        WriteAction(tag, "attack", _attack);
        WriteAction(tag, "interaction", _interaction);
    }

    protected override void ReadAdditionalSaveData(CompoundTag tag)
    {
        base.ReadAdditionalSaveData(tag);
        _width = tag.GetFloatValue("width");
        _height = tag.GetFloatValue("height");
        if (_width <= 0) _width = 1f;
        if (_height <= 0) _height = 1f;
        _response = tag.GetBooleanOr("response", false);
        _attack = ReadAction(tag, "attack");
        _interaction = ReadAction(tag, "interaction");
    }

    private static void WriteAction(CompoundTag tag, string key, PlayerAction? action)
    {
        if (action is null) return;
        tag.PutString(key + "Player", action.Player.ToString("D"));
        tag.PutLong(key + "Timestamp", action.Timestamp);
    }

    private static PlayerAction? ReadAction(CompoundTag tag, string key)
        => Guid.TryParse(tag.GetStringValue(key + "Player"), out var player)
            ? new PlayerAction(player, tag.GetLongValue(key + "Timestamp"))
            : null;

    //PlayerAction one player action with its game time, maps to the vanilla nested record
    private sealed record PlayerAction(Guid Player, long Timestamp);
}
