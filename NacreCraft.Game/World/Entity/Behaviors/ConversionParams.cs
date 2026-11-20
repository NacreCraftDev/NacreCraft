using NacreCraft.Game.World.Scores;

namespace NacreCraft.Game.World.Entity;

//ConversionParams the options of a mob conversion, maps to vanilla net.minecraft.world.entity.ConversionParams
//The conversion type decides how passengers/leashes are handled; keepEquipment copies the equipment slots
public sealed record ConversionParams(
    ConversionType Type,
    bool KeepEquipment,
    bool PreserveCanPickUpLoot,
    PlayerTeam? Team)
{
    //AfterConversion a callback run with the converted mob, maps to the vanilla nested functional interface
    public delegate void AfterConversion<in T>(T mob) where T : Mob;

    //Single builds the common single-mob conversion params, maps to vanilla single
    public static ConversionParams Single(Mob mob, bool keepEquipment, bool preserveCanPickUpLoot)
    {
        _ = mob;
        return new ConversionParams(ConversionType.SINGLE, keepEquipment, preserveCanPickUpLoot, null);
    }
}
