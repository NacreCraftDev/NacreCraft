namespace NacreCraft.Game.World.Level.Block;

//MultifaceSpreadeableBlock a multiface block that can also grow across a surface on its own, maps to vanilla MultifaceSpreadeableBlock
//Sculk veins are the only user: the extra piece over MultifaceBlock is the spreader the sculk corrosion drives
public abstract class MultifaceSpreadeableBlock : MultifaceBlock
{
    //Spreader the spreader that grows this block, maps to vanilla getSpreader
    public abstract MultifaceSpreader Spreader { get; }
}
