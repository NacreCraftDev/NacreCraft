using NacreCraft.Game.World.Level.Portal;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//Portal behavior contract, maps to vanilla net.minecraft.world.level.block.Portal
//A block that can move entities to another level; NC's NetherPortalBlock does not implement it yet, so the defaults are empty
public interface Portal
{
    //Transition the local portal animation kind, maps to vanilla Portal.Transition
    public enum Transition
    {
        NONE,
        CONFUSION,
        NETHER_PORTAL,
        END_PORTAL,
        END_GATEWAY,
    }

    //GetLocalTransition the local animation, maps to vanilla getLocalTransition
    Transition GetLocalTransition() => Transition.NONE;

    //GetPortalDestination the transition this portal would perform, maps to vanilla getPortalDestination
    TeleportTransition? GetPortalDestination(ServerLevel level, NacreCraft.Registry.Entity entity, BlockPos entryPosition)
        => null;

    //GetPortalTransitionTime ticks an entity must stay inside before teleporting, maps to vanilla getPortalTransitionTime
    int GetPortalTransitionTime(ServerLevel level, NacreCraft.Registry.Entity entity) => 0;
}
