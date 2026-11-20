using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Entity;

//InterpolationHandler smooths server-driven position and rotation changes over several ticks, maps to vanilla net.minecraft.world.entity.InterpolationHandler
//The entity keeps moving locally while the server target is reached step by step; a zero-step setup snaps immediately
public sealed class InterpolationHandler
{
    public const int DefaultInterpolationSteps = 3;

    private readonly NacreCraft.Registry.Entity _entity;
    private readonly Action<InterpolationHandler>? _onInterpolationStart;
    private int _interpolationSteps;
    private int _steps;
    private Vec3 _position = Vec3.Zero;
    private float _yRot;
    private float _xRot;
    private Vec3? _previousTickPosition;
    private Vec2? _previousTickRot;

    public InterpolationHandler(NacreCraft.Registry.Entity entity) : this(entity, DefaultInterpolationSteps, null)
    {
    }

    public InterpolationHandler(NacreCraft.Registry.Entity entity, int interpolationSteps)
        : this(entity, interpolationSteps, null)
    {
    }

    public InterpolationHandler(NacreCraft.Registry.Entity entity, Action<InterpolationHandler>? onInterpolationStart)
        : this(entity, DefaultInterpolationSteps, onInterpolationStart)
    {
    }

    public InterpolationHandler(NacreCraft.Registry.Entity entity, int interpolationSteps,
        Action<InterpolationHandler>? onInterpolationStart)
    {
        _entity = entity;
        _interpolationSteps = interpolationSteps;
        _onInterpolationStart = onInterpolationStart;
    }

    public Vec3 Position => _steps > 0 ? _position : _entity.Pos;
    public float YRot => _steps > 0 ? _yRot : _entity.YRot;
    public float XRot => _steps > 0 ? _xRot : _entity.XRot;

    public bool HasActiveInterpolation() => _steps > 0;

    public void SetInterpolationLength(int steps) => _interpolationSteps = steps;

    //InterpolateTo starts interpolation toward a target, maps to vanilla interpolateTo
    public void InterpolateTo(Vec3 position, float yRot, float xRot)
    {
        if (_interpolationSteps == 0)
        {
            _entity.SetPos(position, yRot, xRot);
            Cancel();
            return;
        }
        if (HasActiveInterpolation() && YRot.Equals(yRot) && XRot.Equals(xRot) && Position.Equals(position)) return;
        _steps = _interpolationSteps;
        _position = position;
        _yRot = yRot;
        _xRot = xRot;
        _previousTickPosition = _entity.Pos;
        _previousTickRot = new Vec2(_entity.XRot, _entity.YRot);
        _onInterpolationStart?.Invoke(this);
    }

    //Interpolate advances one step toward the target, maps to vanilla interpolate
    //NC drops the vanilla collision adjustment; the interpolation still follows local entity movement
    public void Interpolate()
    {
        if (!HasActiveInterpolation())
        {
            Cancel();
            return;
        }
        var alpha = 1.0 / _steps;
        if (_previousTickPosition is { } previous)
        {
            var delta = _entity.Pos.Subtract(previous);
            _position = _position.Add(delta);
        }
        if (_previousTickRot is { } previousRot)
            _yRot += _entity.YRot - previousRot.Y;
        if (_previousTickRot is { } previousRotPitch)
            _xRot += _entity.XRot - previousRotPitch.X;
        var x = Mth.Lerp(alpha, _entity.Pos.X, _position.X);
        var y = Mth.Lerp(alpha, _entity.Pos.Y, _position.Y);
        var z = Mth.Lerp(alpha, _entity.Pos.Z, _position.Z);
        var newPosition = new Vec3(x, y, z);
        var newYRot = Mth.RotLerp((float)alpha, _entity.YRot, _yRot);
        var newXRot = Mth.Lerp((float)alpha, _entity.XRot, _xRot);
        _entity.SetPos(newPosition, newYRot, newXRot);
        _steps--;
        _previousTickPosition = newPosition;
        _previousTickRot = new Vec2(_entity.XRot, _entity.YRot);
    }

    public void Cancel()
    {
        _steps = 0;
        _previousTickPosition = null;
        _previousTickRot = null;
    }
}
