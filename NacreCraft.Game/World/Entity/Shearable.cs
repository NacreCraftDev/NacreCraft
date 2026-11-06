using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//Shearable entities that can be sheared, maps to vanilla net.minecraft.world.entity.Shearable
//Sheep and similar mobs implement it so shears work from both the hand and a dispenser
public interface Shearable
{
    //ReadyForShearing whether the entity currently has something to give, maps to vanilla readyForShearing
    bool ReadyForShearing();

    //Shear performs the shear, maps to vanilla shear; the sound source is blocks when triggered by a dispenser
    void Shear(ServerLevel level, SoundSource source, ItemStack tool);
}
