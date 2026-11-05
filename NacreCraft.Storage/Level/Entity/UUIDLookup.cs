using NacreCraft.Registry;

namespace NacreCraft.Storage;

//UUIDLookup looks an identified object up by UUID, maps to vanilla net.minecraft.world.level.entity.UUIDLookup
public interface UUIDLookup<IdentifiedType> where IdentifiedType : UniquelyIdentifyable
{
    IdentifiedType? Lookup(Guid uuid);
}
