namespace NacreCraft.Game.World.Entity;

//ConversionType how an entity conversion moves passengers, vehicles and equipment, maps to vanilla net.minecraft.world.entity.ConversionType
//NC's Mob has no brain/scoreboard/team hooks yet, so the common conversion copies position, velocity, effects, age, flags, tags and
//leash state; equipment drop chances and team membership are not available
public enum ConversionType
{
    SINGLE,
    SPLIT_ON_DEATH,
}

//ConversionTypes the conversion implementations, maps to the abstract convert method of the vanilla enum
public static class ConversionTypes
{
    public static void Convert(this ConversionType type, Mob from, Mob to, ConversionParams parameters)
    {
        if (type == ConversionType.SINGLE)
        {
            var rootPassenger = from.GetFirstPassenger();
            to.SetPos(from.Pos, from.YRot, from.XRot);
            to.Velocity = from.Velocity;
            if (rootPassenger is not null)
            {
                rootPassenger.StopRiding();
                rootPassenger.BoardingCooldown = 0;
                foreach (var passenger in to.Passengers.ToList()) passenger.StopRiding();
                rootPassenger.StartRiding(to);
            }
            var vehicle = from.Vehicle;
            if (vehicle is not null)
            {
                from.StopRiding();
                to.StartRiding(vehicle);
            }
        }
        else
        {
            from.GetFirstPassenger()?.StopRiding();
            from.DropLeash();
        }

        if (parameters.KeepEquipment)
        {
            foreach (var slot in Enum.GetValues<EquipmentSlot>())
            {
                var stack = from.GetItemBySlot(slot);
                if (!stack.IsEmpty()) to.SetItemSlot(slot, stack.Copy());
            }
        }

        to.SetPos(from.Pos, from.YRot, from.XRot);
        to.Velocity = from.Velocity;
        to.OnGround = from.OnGround;
        to.IsOnFire = from.IsOnFire;
        to.NoAi = from.NoAi;
        if (from.PersistenceRequired) to.SetPersistenceRequired();
        foreach (var effect in from.GetActiveEffects())
            to.AddEffect(new NacreCraft.Game.World.Effect.MobEffectInstance(effect));
        if (from is AgeableMob oldAgeable && to is AgeableMob newAgeable)
            newAgeable.SetAge(oldAgeable.GetAge());
        foreach (var tag in from.GetTags()) to.AddTag(tag);
        var leashHolder = from.GetLeashHolder();
        if (leashHolder is not null) to.SetLeashedTo(leashHolder);
    }

    //ShouldDiscardAfterConversion whether the source mob is removed after a successful conversion
    public static bool ShouldDiscardAfterConversion(this ConversionType type) => type == ConversionType.SINGLE;
}
