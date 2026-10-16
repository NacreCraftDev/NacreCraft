using NacreCraft.Primitives;

namespace NacreCraft.Registry;

//IExplosion the part of an explosion the entity layer needs, maps to the subset of vanilla Explosion used by Entity
//Entity is the lowest layer and cannot reference the game level Explosion, so the blast contract is sunk down here
//The game level Explosion implements it and adds the level, the block interaction and the indirect source on top
public interface IExplosion
{
    //DirectSourceEntity the entity that actually went off, such as a primed TNT
    Entity? DirectSourceEntity { get; }

    //Radius blast radius in blocks
    float Radius { get; }

    //Center where the blast is centred
    Vec3 Center { get; }

    //ShouldAffectBlocklikeEntities whether blocks that behave like entities, such as item entities, are caught
    bool ShouldAffectBlocklikeEntities { get; }
}
