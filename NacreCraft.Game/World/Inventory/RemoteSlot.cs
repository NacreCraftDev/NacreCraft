using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//RemoteSlot what the client currently knows about one menu slot, maps to vanilla net.minecraft.world.inventory.RemoteSlot
//The server records every forced value and every hashed value the client reports, so broadcastChanges can skip unchanged slots
public interface RemoteSlot
{
    //Placeholder slot that never reports a difference, maps to vanilla PLACEHOLDER
    static readonly RemoteSlot Placeholder = new PlaceholderSlot();

    void Force(ItemStack outgoing);

    void Receive(HashedStack incoming);

    bool Matches(ItemStack local);

    //Synchronized remote slot holding either the last forced stack or the last hashed stack
    //Vanilla also hashes the local stack to validate the client prediction; the component hasher is not ported, so a hashed value only compares item and count
    sealed class Synchronized : RemoteSlot
    {
        private ItemStack? _remoteStack;
        private HashedStack? _remoteHash;

        public void Force(ItemStack outgoing)
        {
            _remoteStack = outgoing.Copy();
            _remoteHash = null;
        }

        public void Receive(HashedStack incoming)
        {
            _remoteStack = null;
            _remoteHash = incoming;
        }

        public bool Matches(ItemStack local)
        {
            if (_remoteStack is not null)
                return SameStack(_remoteStack, local);
            if (_remoteHash is HashedStack.ActualItem hashed)
            {
                if (hashed.Count != local.GetCount()) return false;
                if (local.IsEmpty() || !ReferenceEquals(hashed.Item.Value, local.GetItem())) return false;
                _remoteStack = local.Copy();
                return true;
            }
            return _remoteHash is not null && local.IsEmpty();
        }

        public void CopyFrom(Synchronized other)
        {
            _remoteStack = other._remoteStack;
            _remoteHash = other._remoteHash;
        }

        private static bool SameStack(ItemStack a, ItemStack b)
        {
            if (a.IsEmpty() || b.IsEmpty()) return a.IsEmpty() && b.IsEmpty();
            return a.GetCount() == b.GetCount() && a.IsSameItemAndComponentsAs(b);
        }
    }

    private sealed class PlaceholderSlot : RemoteSlot
    {
        public void Force(ItemStack outgoing) { }

        public void Receive(HashedStack incoming) { }

        public bool Matches(ItemStack local) => true;
    }
}
