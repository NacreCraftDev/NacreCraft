using NacreCraft.Client.Render.Block;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage.Light;

namespace NacreCraft.Client.Render.Block;

//MovingBlockRenderState the stand-in level view used while rendering a piston's moving block, maps to vanilla
//net.minecraft.client.renderer.block.MovingBlockRenderState
//Only the moving position resolves to the moving state; every other query sees air
public sealed class MovingBlockRenderState : BlockAndTintGetter
{
    public BlockPos RandomSeedPos = BlockPos.Zero;
    public BlockPos BlockPos = BlockPos.Zero;
    public BlockState BlockState = Blocks.AIR.DefaultBlockState;
    public Holder<NacreCraft.Registry.Biome>? Biome;
    public LevelLightEngine LightEngine = LevelLightEngine.Empty;

    public int MinSectionY => BlockPos.Y >> 4;

    public int MaxSectionY => MinSectionY;

    public int SectionsCount => 1;

    public LevelLightEngine GetLightEngine() => LightEngine;

    public int GetBlockTint(BlockPos pos, ColorResolver color)
        => Biome is null ? -1 : color.GetColor(Biome.Value, pos.X, pos.Z);

    public BlockState GetBlockState(BlockPos pos) => GetBlockState(pos.X, pos.Y, pos.Z);

    public BlockState GetBlockState(int x, int y, int z)
        => x == BlockPos.X && y == BlockPos.Y && z == BlockPos.Z ? BlockState : Blocks.AIR.DefaultBlockState;

    public FluidState GetFluidState(BlockPos pos) => GetBlockState(pos).FluidState;
}
