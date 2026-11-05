using NacreCraft.Registry;
using NcEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Game.World.Waypoints;

//PartialTickSupplier supplies an entity's partial tick for interpolation, maps to vanilla net.minecraft.world.waypoints.PartialTickSupplier
public delegate float PartialTickSupplier(NcEntity entity);

