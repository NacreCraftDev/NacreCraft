namespace NacreCraft.Game.World.Entity;

//PlayerModelPart a togglable part of the player model, maps to vanilla net.minecraft.world.entity.player.PlayerModelPart
//The bit index matches the vanilla model customization byte
public enum PlayerModelPart
{
    Cape,
    Jacket,
    LeftSleeve,
    RightSleeve,
    LeftPants,
    RightPants,
    Hat,
}

//PlayerModelPartExtensions bit mask helpers, maps to the vanilla getMask
public static class PlayerModelPartExtensions
{
    public static byte GetMask(this PlayerModelPart part) => (byte)(1 << (int)part);
}
