using NacreCraft.Codec;

namespace NacreCraft.Game.World.Items.Enchanting;

//Enchantable enchantability value of an item, maps to vanilla net.minecraft.world.item.enchantment.Enchantable
//Higher values make the enchanting table offer better levels for the item
public sealed record Enchantable(int Value)
{
    //Codec persistence codec, positive integer field value, maps to vanilla CODEC
    public static readonly Codec<Enchantable> Codec = RecordCodecBuilder.Of1<Enchantable, int>(
        Codecs.Int.FieldOf("value").ForGetter<Enchantable, int>(enchantable => enchantable.Value),
        value => value > 0
            ? new Enchantable(value)
            : throw new ArgumentOutOfRangeException(nameof(value),
                $"Enchantment value must be positive, but was {value}"));
}
