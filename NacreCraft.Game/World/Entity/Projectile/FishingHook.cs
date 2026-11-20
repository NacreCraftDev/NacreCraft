using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;
using Mth = NacreCraft.Util.Mth;

namespace NacreCraft.Game.World.Entity;

//FishingHook the fishing bobber, maps to vanilla net.minecraft.world.entity.projectile.FishingHook
//NC has no loot-table or stats pipeline yet, so the bobber performs the flight/bob/bite cycle and the reel-in lifecycle but
//does not generate loot or experience; the hooked-entity drag is ported
public sealed class FishingHook : Projectile
{
    private enum FishHookState
    {
        Flying,
        HookedInEntity,
        Bobbing,
    }

    private const int MaxOutOfWaterTime = 10;
    private const int MaxLifetime = 1200;

    private readonly RandomSource _random = RandomSource.Create();
    private readonly int _luck;
    private readonly int _lureSpeed;
    private Player? _playerOwner;
    private NacreCraft.Registry.Entity? _hookedIn;
    private FishHookState _state = FishHookState.Flying;
    private bool _biting;
    private int _outOfWaterTime;
    private int _life;
    private int _nibble;
    private int _timeUntilLured;
    private int _timeUntilHooked;
    private float _fishAngle;
    private bool _openWater = true;

    public FishingHook(EntityType<object> type, int luck = 0, int lureSpeed = 0) : base(type)
    {
        _luck = Math.Max(0, luck);
        _lureSpeed = Math.Max(0, lureSpeed);
        ResetCatchTimer();
    }

    public FishingHook(EntityType<object> type, Player player, int luck, int lureSpeed)
        : this(type, luck, lureSpeed)
    {
        _playerOwner = player;
        SetOwner(player);
        var yRot = player.YRot * Mth.DegToRad;
        var xRot = player.XRot * Mth.DegToRad;
        var yCos = Mth.Cos(-yRot - Math.PI);
        var ySin = Mth.Sin(-yRot - Math.PI);
        var xCos = -Mth.Cos(-xRot);
        var xSin = Mth.Sin(-xRot);
        var x = player.Pos.X - ySin * 0.3;
        var y = player.Pos.Y + player.EyeHeight;
        var z = player.Pos.Z - yCos * 0.3;
        SetPos(x, y, z);
        var movement = new Vec3(-ySin, Math.Clamp(-(xSin / xCos), -5.0, 5.0), -yCos);
        var distance = movement.Length();
        Velocity = new Vec3(
            movement.X * (0.6 / distance + Triangle(0.5, 0.0103365)),
            movement.Y * (0.6 / distance + Triangle(0.5, 0.0103365)),
            movement.Z * (0.6 / distance + Triangle(0.5, 0.0103365)));
        YRot = (float)(Math.Atan2(Velocity.X, Velocity.Z) * (180.0 / Math.PI));
        XRot = (float)(Math.Atan2(Velocity.Y, Math.Sqrt(Velocity.X * Velocity.X + Velocity.Z * Velocity.Z)) * (180.0 / Math.PI));
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("fishing_bobber");

    public override EntityType<object>? Type => EntityTypes.FISHING_BOBBER;

    public Player? GetPlayerOwner() => _playerOwner;

    public NacreCraft.Registry.Entity? GetHookedEntity() => _hookedIn;

    public void SetHookedEntity(NacreCraft.Registry.Entity? entity) => _hookedIn = entity;

    public bool IsBiting() => _biting;

    public bool IsOpenWaterFishing() => _openWater;

    public int GetLuck() => _luck;

    public int GetLureSpeed() => _lureSpeed;

    public override double DefaultGravity => 0.04;

    public override bool IsPickable => false;

    public override void Tick()
    {
        TickBase();
        var owner = _playerOwner;
        if (owner is null)
        {
            Discard();
            return;
        }
        if (Level is not ServerLevel level) return;
        if (!IsHoldingFishingRod(owner) || owner.Pos.DistanceToSqr(Pos) > 1024.0)
        {
            Discard();
            return;
        }
        if (OnGround)
        {
            _life++;
            if (_life >= MaxLifetime)
            {
                Discard();
                return;
            }
        }
        else
        {
            _life = 0;
        }
        var blockPos = BlockPosition();
        var fluid = level.GetFluidState(blockPos);
        var liquidHeight = fluid.IsWater ? fluid.OwnHeight : 0f;
        var inWater = liquidHeight > 0f;
        switch (_state)
        {
            case FishHookState.Flying when _hookedIn is not null:
                Velocity = Vec3.Zero;
                _state = FishHookState.HookedInEntity;
                return;
            case FishHookState.Flying when inWater:
                Velocity = new Vec3(Velocity.X * 0.3, Velocity.Y * 0.2, Velocity.Z * 0.3);
                _state = FishHookState.Bobbing;
                return;
            case FishHookState.Flying:
            {
                var hit = ProjectileUtil.GetHitResult(this);
                if (hit.Type != ProjectileHitType.miss) OnHit(hit);
                break;
            }
            case FishHookState.HookedInEntity:
            {
                if (_hookedIn is null || _hookedIn.IsRemoved)
                {
                    SetHookedEntity(null);
                    _state = FishHookState.Flying;
                    return;
                }
                SetPos(_hookedIn.Pos.X, _hookedIn.Pos.Y + _hookedIn.Height * 0.8, _hookedIn.Pos.Z);
                return;
            }
            case FishHookState.Bobbing:
            {
                var movement = Velocity;
                var force = Pos.Y + movement.Y - blockPos.Y - liquidHeight;
                if (Math.Abs(force) < 0.01) force += Math.Sign(force) * 0.1;
                Velocity = new Vec3(movement.X * 0.9, movement.Y - force * _random.NextFloat() * 0.2, movement.Z * 0.9);
                if (_nibble > 0 || _timeUntilHooked > 0) _openWater = _openWater && _outOfWaterTime < MaxOutOfWaterTime;
                else _openWater = true;
                if (inWater)
                {
                    _outOfWaterTime = Math.Max(0, _outOfWaterTime - 1);
                    if (_biting) Velocity = Velocity.Add(0.0, -0.1 * _random.NextFloat() * _random.NextFloat(), 0.0);
                    CatchFishTick();
                }
                else
                {
                    _outOfWaterTime = Math.Min(MaxOutOfWaterTime, _outOfWaterTime + 1);
                }
                break;
            }
        }
        if (!fluid.IsWater && !OnGround && _hookedIn is null) Velocity = Velocity.Add(0.0, -0.03, 0.0);
        Move(Velocity);
        UpdateRotation();
        if (_state == FishHookState.Flying && (OnGround || HorizontalCollision)) Velocity = Vec3.Zero;
        Velocity = Velocity.Multiply(0.92);
    }

    //Retrieve reels the bobber in, maps to vanilla retrieve; NC has no loot table, so the result is always empty
    public int Retrieve(ItemStack rod)
    {
        if (Level is not ServerLevel level || IsRemoved) return 0;
        Discard();
        return 0;
    }

    //CatchFishTick advances the nibble/bite timers, maps to the vanilla catchingFish state machine without loot generation
    private void CatchFishTick()
    {
        if (_timeUntilHooked > 0)
        {
            _timeUntilHooked--;
            if (_timeUntilHooked <= 0)
            {
                _biting = true;
                _nibble = 20;
            }
            return;
        }
        if (_nibble > 0)
        {
            _nibble--;
            if (_nibble <= 0)
            {
                _biting = false;
                ResetCatchTimer();
            }
            return;
        }
        if (_timeUntilLured > 0)
        {
            _timeUntilLured--;
            if (_timeUntilLured <= 0) _timeUntilHooked = 20 + _random.NextInt(20);
        }
    }

    private void ResetCatchTimer()
    {
        _biting = false;
        _nibble = 0;
        _timeUntilHooked = 0;
        _timeUntilLured = 100 + _random.NextInt(400) - _lureSpeed * 100;
        if (_timeUntilLured < 20) _timeUntilLured = 20;
        _fishAngle = _random.NextFloat() * 360.0f;
    }

    private static bool IsHoldingFishingRod(Player player)
    {
        var rod = BuiltInRegistries.ITEM.GetValue(Identifier.WithDefaultNamespace("fishing_rod"));
        if (rod is null) return true;
        return IsItem(player.GetItemInHand(InteractionHand.MainHand), rod)
               || IsItem(player.GetItemInHand(InteractionHand.OffHand), rod);
    }

    private static bool IsItem(ItemStack stack, Item item)
        => !stack.IsEmpty() && ReferenceEquals(stack.GetItem(), item);

    private static double Triangle(double mean, double deviation)
        => mean + deviation * (Random.Shared.NextDouble() - Random.Shared.NextDouble());
}
