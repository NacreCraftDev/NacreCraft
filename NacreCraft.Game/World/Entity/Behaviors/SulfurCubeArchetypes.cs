using NacreCraft.Game.Data.WorldGen;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//SulfurCubeArchetypes the built-in sulfur cube archetype presets, maps to vanilla net.minecraft.world.entity.SulfurCubeArchetypes
//NC has no bounciness/friction/air-drag attributes or sulfur cube entity yet, so the presets carry the numeric movement values as
//attribute-name entries and the sulfur-cube item tags; explosion/contact-damage/sound settings are not represented
public static class SulfurCubeArchetypes
{
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Regular = CreateKey("regular");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Bouncy = CreateKey("bouncy");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> SlowBouncy = CreateKey("slow_bouncy");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> SlowFlat = CreateKey("slow_flat");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> FastFlat = CreateKey("fast_flat");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Light = CreateKey("light");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> FastSliding = CreateKey("fast_sliding");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> SlowSliding = CreateKey("slow_sliding");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> HighResistance = CreateKey("high_resistance");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Sticky = CreateKey("sticky");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Explosive = CreateKey("explosive");
    public static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> Hot = CreateKey("hot");

    private static readonly ResourceKey<NacreCraft.Registry.SulfurCubeArchetype>[] All =
    {
        Regular, Bouncy, SlowBouncy, SlowFlat, FastFlat, Light, FastSliding, SlowSliding, HighResistance, Sticky,
        Explosive, Hot,
    };

    //Bootstrap registers every archetype, maps to vanilla bootstrap
    public static void Bootstrap(BootstrapContext<NacreCraft.Registry.SulfurCubeArchetype> context)
    {
        Register(context, Regular, "sulfur_cube_archetype_regular", 1f, 0.5f, 0.3f, 0.1f, true);
        Register(context, Bouncy, "sulfur_cube_archetype_bouncy", 2f, 0.9f, 0.3f, 0.01f, true);
        Register(context, SlowBouncy, "sulfur_cube_archetype_slow_bouncy", -0.4f, 0.6f, 0.3f, 0.05f, false);
        Register(context, SlowFlat, "sulfur_cube_archetype_slow_flat", -0.5f, 0.4f, 0.4f, 0.1f, false);
        Register(context, FastFlat, "sulfur_cube_archetype_fast_flat", 1f, 0.5f, 0.2f, 0.01f, false);
        Register(context, Light, "sulfur_cube_archetype_light", 1f, 1f, 0.3f, 1.8f, true);
        Register(context, FastSliding, "sulfur_cube_archetype_fast_sliding", -0.5f, 0.1f, 0.05f, 0.01f, false);
        Register(context, SlowSliding, "sulfur_cube_archetype_slow_sliding", -0.8f, 0.1f, 0.05f, 0.01f, false);
        Register(context, Sticky, "sulfur_cube_archetype_sticky", 2f, 0f, 2f, 0.01f, false);
        Register(context, HighResistance, "sulfur_cube_archetype_high_resistance", -0.7f, 0.2f, 1f, 0.01f, false);
        Register(context, Explosive, "sulfur_cube_archetype_explosive", 1f, 0.5f, 0.3f, 0.3f, true);
        Register(context, Hot, "sulfur_cube_archetype_hot", 1f, 0.5f, 0.3f, 0.1f, true);
    }

    private static void Register(BootstrapContext<NacreCraft.Registry.SulfurCubeArchetype> context,
        ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> key, string tagName, float speed, float bounce,
        float friction, float drag, bool floats)
    {
        var tag = TagKey<Item>.Create(Registries.ITEM, Identifier.WithDefaultNamespace(tagName));
        var attributes = new List<SulfurCubeArchetype.AttributeEntry>
        {
            new("minecraft:knockback_resistance", -speed, false),
            new("minecraft:explosion_knockback_resistance", -speed, false),
            new("minecraft:bounciness", bounce, false),
            new("minecraft:friction_modifier", friction, true),
            new("minecraft:air_drag_modifier", drag, true),
        };
        context.Register(key, new SulfurCubeArchetype(tag, attributes, floats));
    }

    private static ResourceKey<NacreCraft.Registry.SulfurCubeArchetype> CreateKey(string name)
        => ResourceKey<NacreCraft.Registry.SulfurCubeArchetype>.Create(Registries.SULFUR_CUBE_ARCHETYPE,
            Identifier.WithDefaultNamespace(name));
}
