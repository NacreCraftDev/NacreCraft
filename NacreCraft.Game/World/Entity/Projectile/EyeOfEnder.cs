using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;
using Mth = NacreCraft.Util.Mth;

namespace NacreCraft.Game.World.Entity;

//EyeOfEnder the thrown eye of ender that flies toward a stronghold, maps to vanilla net.minecraft.world.entity.projectile.EyeOfEnder
//NC has no stronghold locator, so callers supply the target position; particles and sounds are not ported
public sealed class EyeOfEnder : NacreCraft.Registry.Entity, ItemSupplier
{
    private const float TooFarSignalHeight = 8.0f;
    private const float TooFarDistance = 12.0f;
    private const int DefaultLife = 80;

    private readonly RandomSource _random = RandomSource.Create();
    private Vec3? _target;
    private int _life;
    private bool _surviveAfterDeath;
    private ItemStack _item;

    public EyeOfEnder(EntityType<object> type) : base()
    {
        _item = ItemSupplier.DefaultStack("ender_eye");
    }

    public EyeOfEnder(EntityType<object> type, double x, double y, double z) : this(type)
    {
        SetPos(x, y, z);
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("eye_of_ender");

    public override EntityType<object>? Type => EntityTypes.EYE_OF_ENDER;

    public override bool IsPickable => false;

    public ItemStack GetItem() => _item;

    //SetItem stores the stack the eye renders and drops, maps to vanilla setItem
    public void SetItem(ItemStack stack) => _item = stack.IsEmpty() ? _item : stack.CopyWithCount(1);

    //SignalTo aims the eye at a target; distant targets are capped at 12 blocks and raised 8 blocks, maps to vanilla signalTo
    public void SignalTo(Vec3 target)
    {
        var delta = target.Subtract(Pos);
        var horizontal = HorizontalDistance(delta);
        _target = horizontal > TooFarDistance
            ? Pos.Add(delta.X / horizontal * TooFarDistance, TooFarSignalHeight, delta.Z / horizontal * TooFarDistance)
            : target;
        _life = 0;
        _surviveAfterDeath = _random.NextInt(5) > 0;
    }

    public override void Tick()
    {
        TickBase();
        var newPosition = Pos.Add(Velocity);
        if (Level is not null && _target is { } target)
            Velocity = UpdateDeltaMovement(Velocity, newPosition, target);
        SetPos(newPosition.X, newPosition.Y, newPosition.Z);
        if (Level is not ServerLevel) return;
        _life++;
        if (_life <= DefaultLife) return;
        if (Level is ServerLevel level)
        {
            Discard();
            if (_surviveAfterDeath)
            {
                level.AddEntity(new ItemEntity(EntityTypes.ITEM, Pos.X, Pos.Y, Pos.Z, _item));
            }
        }
    }

    //UpdateDeltaMovement steers horizontally toward the target and eases the vertical speed, maps to the vanilla helper
    private static Vec3 UpdateDeltaMovement(Vec3 oldMovement, Vec3 position, Vec3 target)
    {
        var horizontalDelta = new Vec3(target.X - position.X, 0.0, target.Z - position.Z);
        var horizontalLength = horizontalDelta.Length();
        var oldHorizontal = HorizontalDistance(oldMovement);
        var wantedSpeed = oldHorizontal + (horizontalLength - oldHorizontal) * 0.0025;
        var movementY = oldMovement.Y;
        if (horizontalLength < 1.0)
        {
            wantedSpeed *= 0.8;
            movementY *= 0.8;
        }
        var wantedMovementY = position.Y - oldMovement.Y < target.Y ? 1.0 : -1.0;
        if (horizontalLength == 0.0) return new Vec3(0.0, movementY + (wantedMovementY - movementY) * 0.015, 0.0);
        return horizontalDelta.Multiply(wantedSpeed / horizontalLength)
            .Add(0.0, movementY + (wantedMovementY - movementY) * 0.015, 0.0);
    }

    //HorizontalDistance length of the horizontal components, maps to vanilla Vec3.horizontalDistance
    private static double HorizontalDistance(Vec3 vec) => Math.Sqrt(vec.X * vec.X + vec.Z * vec.Z);
}
