using NetCraft.Registry.State;

namespace NetCraft.Registry;

//GameEvent game event definition, maps to vanilla net.minecraft.world.level.gameevent.GameEvent
//The notification radius decides how far a listener may sit from the source and still be notified
//The constants register themselves in declaration order, which fixes the registry ids the client expects
public sealed record GameEvent(int NotificationRadius)
{
    //DefaultNotificationRadius radius used by every event that does not override it
    public const int DefaultNotificationRadius = 16;

    public static readonly GameEvent BlockActivate = Register("block_activate");
    public static readonly GameEvent BlockAttach = Register("block_attach");
    public static readonly GameEvent BlockChange = Register("block_change");
    public static readonly GameEvent BlockClose = Register("block_close");
    public static readonly GameEvent BlockDeactivate = Register("block_deactivate");
    public static readonly GameEvent BlockDestroy = Register("block_destroy");
    public static readonly GameEvent BlockDetach = Register("block_detach");
    public static readonly GameEvent BlockOpen = Register("block_open");
    public static readonly GameEvent BlockPlace = Register("block_place");
    public static readonly GameEvent Bounce = Register("bounce");
    public static readonly GameEvent ContainerClose = Register("container_close");
    public static readonly GameEvent ContainerOpen = Register("container_open");
    public static readonly GameEvent Drink = Register("drink");
    public static readonly GameEvent Eat = Register("eat");
    public static readonly GameEvent ElytraGlide = Register("elytra_glide");
    public static readonly GameEvent EntityDamage = Register("entity_damage");
    public static readonly GameEvent EntityDie = Register("entity_die");
    public static readonly GameEvent EntityDismount = Register("entity_dismount");
    public static readonly GameEvent EntityInteract = Register("entity_interact");
    public static readonly GameEvent EntityMount = Register("entity_mount");
    public static readonly GameEvent EntityPlace = Register("entity_place");
    public static readonly GameEvent EntityAction = Register("entity_action");
    public static readonly GameEvent Equip = Register("equip");
    public static readonly GameEvent Explode = Register("explode");
    public static readonly GameEvent Flap = Register("flap");
    public static readonly GameEvent FluidPickup = Register("fluid_pickup");
    public static readonly GameEvent FluidPlace = Register("fluid_place");
    public static readonly GameEvent HitGround = Register("hit_ground");
    public static readonly GameEvent InstrumentPlay = Register("instrument_play");
    public static readonly GameEvent ItemInteractFinish = Register("item_interact_finish");
    public static readonly GameEvent ItemInteractStart = Register("item_interact_start");
    public static readonly GameEvent JukeboxPlay = Register("jukebox_play", 10);
    public static readonly GameEvent JukeboxStopPlay = Register("jukebox_stop_play", 10);
    public static readonly GameEvent LightningStrike = Register("lightning_strike");
    public static readonly GameEvent NoteBlockPlay = Register("note_block_play");
    public static readonly GameEvent PrimeFuse = Register("prime_fuse");
    public static readonly GameEvent ProjectileLand = Register("projectile_land");
    public static readonly GameEvent ProjectileShoot = Register("projectile_shoot");
    public static readonly GameEvent SculkSensorTendrilsClicking = Register("sculk_sensor_tendrils_clicking");
    public static readonly GameEvent Shear = Register("shear");
    public static readonly GameEvent Shriek = Register("shriek", 32);
    public static readonly GameEvent Splash = Register("splash");
    public static readonly GameEvent Step = Register("step");
    public static readonly GameEvent Swim = Register("swim");
    public static readonly GameEvent Teleport = Register("teleport");
    public static readonly GameEvent Unequip = Register("unequip");
    public static readonly GameEvent Resonate1 = Register("resonate_1");
    public static readonly GameEvent Resonate2 = Register("resonate_2");
    public static readonly GameEvent Resonate3 = Register("resonate_3");
    public static readonly GameEvent Resonate4 = Register("resonate_4");
    public static readonly GameEvent Resonate5 = Register("resonate_5");
    public static readonly GameEvent Resonate6 = Register("resonate_6");
    public static readonly GameEvent Resonate7 = Register("resonate_7");
    public static readonly GameEvent Resonate8 = Register("resonate_8");
    public static readonly GameEvent Resonate9 = Register("resonate_9");
    public static readonly GameEvent Resonate10 = Register("resonate_10");
    public static readonly GameEvent Resonate11 = Register("resonate_11");
    public static readonly GameEvent Resonate12 = Register("resonate_12");
    public static readonly GameEvent Resonate13 = Register("resonate_13");
    public static readonly GameEvent Resonate14 = Register("resonate_14");
    public static readonly GameEvent Resonate15 = Register("resonate_15");

    //Register registers one event under the built-in namespace
    private static GameEvent Register(string path, int notificationRadius = DefaultNotificationRadius)
    {
        var gameEvent = new GameEvent(notificationRadius);
        var id = Identifier.WithDefaultNamespace(path);
        BuiltInRegistries.GAME_EVENT.Register(
            ResourceKey<GameEvent>.Create(Registries.GAME_EVENT, id),
            gameEvent, RegistrationInfo.BuiltIn);
        return gameEvent;
    }

    //Context extra information carried by one dispatch, maps to vanilla GameEvent.Context
    //Either part may be absent: a block event has no source entity, an entity event has no affected state
    public sealed record Context(Entity? SourceEntity, BlockState? AffectedState)
    {
        public static Context Of(Entity sourceEntity) => new(sourceEntity, null);

        public static Context Of(BlockState affectedState) => new(null, affectedState);

        public static Context Of(Entity? sourceEntity, BlockState? affectedState) => new(sourceEntity, affectedState);
    }
}
