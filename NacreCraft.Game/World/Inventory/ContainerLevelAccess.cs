using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Inventory;

//ContainerLevelAccess binds a menu to a level position, maps to vanilla net.minecraft.world.inventory.ContainerLevelAccess
//Block entity backed menus use it to run work where the block lives, NULL covers menus opened without a position
//The vanilla interface takes Level; the server is the only side here, so it is typed as ServerLevel
public interface ContainerLevelAccess
{
    //Null unbound access, evaluate always yields empty, maps to vanilla NULL
    static readonly ContainerLevelAccess Null = new NullAccess();

    //Create binds a level and position
    static ContainerLevelAccess Create(ServerLevel level, BlockPos pos) => new BoundAccess(level, pos);

    Optional<T> Evaluate<T>(Func<ServerLevel, BlockPos, T> action);

    //Evaluate with a fallback value used when the access is unbound
    T Evaluate<T>(Func<ServerLevel, BlockPos, T> action, T defaultValue)
        => Evaluate(action).OrElse(defaultValue);

    //Execute runs a side effect at the position, ignoring the result
    void Execute(Action<ServerLevel, BlockPos> action)
        => Evaluate<object?>((level, pos) =>
        {
            action(level, pos);
            return null;
        });

    private sealed class NullAccess : ContainerLevelAccess
    {
        public Optional<T> Evaluate<T>(Func<ServerLevel, BlockPos, T> action) => Optional<T>.Empty();
    }

    private sealed class BoundAccess(ServerLevel level, BlockPos pos) : ContainerLevelAccess
    {
        public Optional<T> Evaluate<T>(Func<ServerLevel, BlockPos, T> action)
            => Optional<T>.Of(action(level, pos));
    }
}
