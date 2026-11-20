using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//SulfurCubeArchetype a data-driven sulfur cube behaviour preset, maps to vanilla net.minecraft.world.entity.SulfurCubeArchetype
//NC has no sulfur cube entity or the bounciness/friction/air-drag attributes yet, so the preset keeps the item tag, the numeric
//movement modifiers and the floats flag; explosion/contact-damage/sound settings are left to the entity implementation
public sealed record SulfurCubeArchetype(
    TagKey<Item> Blocks,
    IReadOnlyList<SulfurCubeArchetype.AttributeEntry> Attributes,
    bool Floats) : NacreCraft.Registry.SulfurCubeArchetype
{
    //AttributeEntry one attribute modifier amount, maps to the vanilla nested record
    public sealed record AttributeEntry(string Attribute, double Amount, bool Multiply);
}
