namespace NacreCraft.Game.World.Entity;

//Pose the body pose an entity is in, maps to vanilla net.minecraft.world.entity.Pose
//Only the poses the avatar dimensions use are declared; more are added when their consumers land
public enum Pose
{
    Standing,
    Sleeping,
    FallFlying,
    Swimming,
    SpinAttack,
    Crouching,
    Dying,
    Sitting,
}
