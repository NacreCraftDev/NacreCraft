using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//TNT, maps to vanilla net.minecraft.world.level.block.TntBlock
//A redstone signal, flint and steel, a fire charge or another blast sets it off
//The blast that removes it drops nothing, and breaking an unstable charge (one placed by a dispenser) sets it off instead
public static partial class Blocks
{
    public static readonly TntBlock TNT = new();

    //RegisterTnt registers TNT into the real block table, see Blocks.Bootstrap
    private static void RegisterTnt(Dictionary<string, BlockBehaviour> real)
        => real[TNT.Id.Path] = TNT;

    public sealed class TntBlock : BlockBehaviour
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("tnt");

        //Properties follow unstable:bool in blocks.txt; the table builds its own property instance that GetValue cannot
        //find, so it has to be declared here or the state lookup misses
        public override IDictionary<string, PropertyBase> Properties => new Dictionary<string, PropertyBase>
        {
            ["unstable"] = BlockStateProperties.Unstable,
        };

        //DropFromExplosion TNT never drops itself when caught in a blast, maps to vanilla dropFromExplosion
        public override bool DropFromExplosion(Explosion explosion) => false;

        //OnPlace a signal already present primes it right after placement, maps to vanilla onPlace
        public override void OnPlace(ServerLevel level, BlockPos pos, BlockState state, BlockState oldState,
            bool movedByPiston)
        {
            if (oldState.Owner == state.Owner) return;
            if (level.HasNeighborSignal(pos) && Prime(level, pos, null)) level.SetBlock(pos, AIR.DefaultBlockState);
        }

        //NeighborChanged a redstone signal primes it, maps to vanilla neighborChanged
        public override void NeighborChanged(ServerLevel level, BlockPos pos, BlockState state,
            NacreCraft.Registry.Block changedBlock, bool movedByPiston)
        {
            if (level.HasNeighborSignal(pos) && Prime(level, pos, null)) level.SetBlock(pos, AIR.DefaultBlockState);
        }

        //PlayerWillDestroy breaking an unstable charge primes it, maps to vanilla playerWillDestroy
        //Only dispenser-placed charges are unstable; a creative breaker never sets one off
        //The breaker is not credited: the server player object and the registry entity are separate hierarchies here
        public override void PlayerWillDestroy(ServerLevel level, ServerPlayer player, BlockPos pos, BlockState state)
        {
            if (!state.GetValue(BlockStateProperties.Unstable) || player.GameType == GameType.Creative) return;
            Prime(level, pos, null);
        }

        //WasExploded a blast next to a charge primes a fresh one instead of removing it silently, maps to vanilla wasExploded
        //The new fuse is randomised shorter so a chain reaction does not go off all on one tick
        public override void WasExploded(ServerLevel level, BlockPos pos, Explosion explosion)
        {
            if (level.GameRules?.GetBool(GameRules.TntExplodes) == false) return;
            var primed = new NacreCraft.Game.World.Entity.PrimedTnt(NacreCraft.Game.World.Entity.EntityTypes.TNT,
                pos.X + 0.5, pos.Y, pos.Z + 0.5, explosion.IndirectSourceEntity);
            primed.Fuse = NacreCraft.Game.World.Entity.PrimedTnt.GetRandomShortFuse(primed.Fuse, level.Random);
            level.AddEntity(primed);
        }

        //Prime ignites the block into a primed TNT entity, maps to vanilla prime
        //Refused when tnt_explodes is off, which is how a server disables TNT without a restart
        public static bool Prime(ServerLevel level, BlockPos pos, NacreCraft.Registry.Entity? source)
        {
            if (level.GameRules?.GetBool(GameRules.TntExplodes) == false) return false;
            var primed = new NacreCraft.Game.World.Entity.PrimedTnt(NacreCraft.Game.World.Entity.EntityTypes.TNT,
                pos.X + 0.5, pos.Y, pos.Z + 0.5, source);
            level.AddEntity(primed);
            level.GameEvent(source, NacreCraft.Registry.GameEvent.PrimeFuse,
                new Vec3(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5));
            return true;
        }
    }
}
