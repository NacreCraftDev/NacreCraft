using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.Portal;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//PortalProcessor tracks how long an entity has been inside a portal, maps to vanilla net.minecraft.world.entity.PortalProcessor
//The timer counts up while the entity stays inside and decays when it leaves; once it passes the portal transition time the
//caller asks the portal for a destination
public sealed class PortalProcessor
{
    private readonly Portal _portal;
    private BlockPos _entryPosition;
    private int _portalTime;
    private bool _insidePortalThisTick = true;

    public PortalProcessor(Portal portal, BlockPos portalEntryPosition)
    {
        _portal = portal;
        _entryPosition = portalEntryPosition;
    }

    //ProcessPortalTeleportation advances the timer and reports whether the transition should happen now
    public bool ProcessPortalTeleportation(ServerLevel serverLevel, NacreCraft.Registry.Entity entity,
        bool allowedToTeleport)
    {
        if (_insidePortalThisTick)
        {
            _insidePortalThisTick = false;
            if (allowedToTeleport)
            {
                var time = _portalTime;
                _portalTime = time + 1;
                if (time >= _portal.GetPortalTransitionTime(serverLevel, entity)) return true;
            }
            return false;
        }
        DecayTick();
        return false;
    }

    public TeleportTransition? GetPortalDestination(ServerLevel serverLevel, NacreCraft.Registry.Entity entity)
        => _portal.GetPortalDestination(serverLevel, entity, _entryPosition);

    public Portal.Transition GetPortalLocalTransition() => _portal.GetLocalTransition();

    private void DecayTick() => _portalTime = Math.Max(_portalTime - 4, 0);

    public bool HasExpired() => _portalTime <= 0;

    public BlockPos GetEntryPosition() => _entryPosition;

    public void UpdateEntryPosition(BlockPos entryPosition) => _entryPosition = entryPosition;

    public int GetPortalTime() => _portalTime;

    public bool IsInsidePortalThisTick() => _insidePortalThisTick;

    public void SetAsInsidePortalThisTick(bool insidePortal) => _insidePortalThisTick = insidePortal;

    public bool IsSamePortal(Portal portal) => ReferenceEquals(_portal, portal);
}
