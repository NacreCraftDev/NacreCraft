using NacreCraft.Util;

namespace NacreCraft.Game.World.Entity;

//ElytraAnimationState eases the elytra/body rotation of a living entity, maps to vanilla net.minecraft.world.entity.ElytraAnimationState
//Falling steeply tilts the wings, crouching folds them, and the current value eases toward the target each tick
public sealed class ElytraAnimationState
{
    private const float DefaultXRot = 0.2617994f;
    private const float DefaultZRot = -0.2617994f;

    private readonly LivingEntity _entity;
    private float _rotX;
    private float _rotY;
    private float _rotZ;
    private float _rotXOld;
    private float _rotYOld;
    private float _rotZOld;

    public ElytraAnimationState(LivingEntity entity) => _entity = entity;

    //Tick advances the eased rotation toward the pose target, maps to vanilla tick
    public void Tick()
    {
        _rotXOld = _rotX;
        _rotYOld = _rotY;
        _rotZOld = _rotZ;
        float targetX, targetY, targetZ;
        if (_entity.IsFallFlying)
        {
            var ratio = 1f;
            var movement = _entity.Velocity;
            if (movement.Y < 0)
            {
                var normalized = movement.Normalize();
                ratio = 1f - (float)Math.Pow(-normalized.Y, 1.5);
            }
            targetX = Mth.Lerp(ratio, DefaultXRot, 0.34906584f);
            targetZ = Mth.Lerp(ratio, DefaultZRot, -1.5707964f);
            targetY = 0f;
        }
        else if (_entity.IsCrouching)
        {
            targetX = 0.6981317f;
            targetZ = -0.7853982f;
            targetY = 0.08726646f;
        }
        else
        {
            targetX = DefaultXRot;
            targetZ = DefaultZRot;
            targetY = 0f;
        }
        _rotX += (targetX - _rotX) * 0.3f;
        _rotY += (targetY - _rotY) * 0.3f;
        _rotZ += (targetZ - _rotZ) * 0.3f;
    }

    public float GetRotX(float partialTicks) => Mth.Lerp(partialTicks, _rotXOld, _rotX);
    public float GetRotY(float partialTicks) => Mth.Lerp(partialTicks, _rotYOld, _rotY);
    public float GetRotZ(float partialTicks) => Mth.Lerp(partialTicks, _rotZOld, _rotZ);
}
