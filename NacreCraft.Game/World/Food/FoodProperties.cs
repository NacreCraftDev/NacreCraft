using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Food;

//FoodProperties the food component of an item, maps to vanilla net.minecraft.world.food.FoodProperties
//Carries nutrition, the saturation the item grants and whether it can be eaten with a full hunger bar
//Consumption plays the eating sound and feeds the player through FoodData, matching vanilla onConsume minus the Consumable component
public sealed class FoodProperties : IEquatable<FoodProperties>
{
    //Codec persistence codec, field names nutrition, saturation and can_always_eat, maps to vanilla DIRECT_CODEC
    public static readonly Codec<FoodProperties> Codec = RecordCodecBuilder.Of3(
        Codecs.Int.FieldOf("nutrition").ForGetter((FoodProperties food) => food.Nutrition),
        Codecs.Float.FieldOf("saturation").ForGetter((FoodProperties food) => food.Saturation),
        Codecs.Bool.OptionalFieldOf("can_always_eat", false).ForGetter((FoodProperties food) => food.CanAlwaysEat),
        (nutrition, saturation, canAlwaysEat) => new FoodProperties(nutrition, saturation, canAlwaysEat));

    //StreamCodec network codec: nutrition VarInt, saturation float, canAlwaysEat bool, maps to vanilla DIRECT_STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, FoodProperties> StreamCodec =
        new FoodPropertiesStreamCodec();

    public FoodProperties(int nutrition, float saturation, bool canAlwaysEat)
    {
        Nutrition = nutrition;
        Saturation = saturation;
        CanAlwaysEat = canAlwaysEat;
    }

    //Nutrition hunger points the item restores
    public int Nutrition { get; }

    //Saturation saturation the item restores, already multiplied by the saturation modifier
    public float Saturation { get; }

    //CanAlwaysEat whether the item can be eaten even with a full hunger bar
    public bool CanAlwaysEat { get; }

    //OnConsume plays the eating sound, feeds the player and plays the burp, maps to vanilla ConsumableListener.onConsume
    //The Consumable component is not ported yet, so the consumed sound is passed in by the caller
    public void OnConsume(ServerLevel level, LivingEntity user, ItemStack stack, SoundEvent consumedSound)
    {
        var random = user.GetRandom();
        level.PlaySound(consumedSound, SoundSource.Neutral, user.BlockPosition(), 1f, random.Triangle(1f, 0.4f));
        if (user is not Player player) return;
        player.Food.Eat(this);
        level.PlaySound(SoundEvents.PlayerBurp, SoundSource.Players, player.BlockPosition(), 0.5f,
            random.NextFloat() * 0.1f + 0.9f);
    }

    public bool Equals(FoodProperties? other)
        => other is not null
           && Nutrition == other.Nutrition
           && Saturation.Equals(other.Saturation)
           && CanAlwaysEat == other.CanAlwaysEat;

    public override bool Equals(object? obj) => Equals(obj as FoodProperties);

    public override int GetHashCode() => HashCode.Combine(Nutrition, Saturation, CanAlwaysEat);

    public override string ToString()
        => $"FoodProperties[nutrition={Nutrition}, saturation={Saturation}, canAlwaysEat={CanAlwaysEat}]";

    //Builder builds a FoodProperties from nutrition and a saturation modifier, maps to vanilla FoodProperties.Builder
    //Saturation is derived by FoodConstants.SaturationByModifier at build time
    public sealed class Builder
    {
        private int _nutrition;
        private float _saturationModifier;
        private bool _canAlwaysEat;

        public Builder Nutrition(int nutrition)
        {
            _nutrition = nutrition;
            return this;
        }

        public Builder SaturationModifier(float saturationModifier)
        {
            _saturationModifier = saturationModifier;
            return this;
        }

        public Builder AlwaysEdible()
        {
            _canAlwaysEat = true;
            return this;
        }

        public FoodProperties Build()
            => new(_nutrition, FoodConstants.SaturationByModifier(_nutrition, _saturationModifier), _canAlwaysEat);
    }
}

//FoodPropertiesStreamCodec nutrition VarInt plus saturation float plus boolean, maps to vanilla DIRECT_STREAM_CODEC
internal sealed class FoodPropertiesStreamCodec : StreamCodec<RegistryFriendlyByteBuf, FoodProperties>
{
    public FoodProperties Decode(RegistryFriendlyByteBuf buf)
        => new(buf.ReadVarInt(), buf.ReadFloat(), buf.ReadBoolean());

    public void Encode(RegistryFriendlyByteBuf buf, FoodProperties value)
    {
        buf.WriteVarInt(value.Nutrition);
        buf.WriteFloat(value.Saturation);
        buf.WriteBoolean(value.CanAlwaysEat);
    }
}
