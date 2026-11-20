using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Items.Equipment;

//Equippable the equippable item component, maps to vanilla net.minecraft.world.item.equipment.Equippable
//Describes which slot an item goes into, its equip/shearing sounds, optional asset and camera overlay, allowed wearers and
//the behaviour flags. The vanilla swapWithEquipmentSlot/equipOnTarget interaction paths need an InteractionResult and a real
//inventory/drop pipeline, neither of which exists yet, so this port keeps the data and codecs and leaves the interaction to a later batch
public sealed class Equippable : IEquatable<Equippable>
{
    //AssetKeyCodec equipment asset key codec, maps to vanilla ResourceKey.codec(EquipmentAssets.ROOT_ID)
    public static readonly Codec<ResourceKey<EquipmentAsset>> AssetKeyCodec =
        IdentifierCodec.Instance.ComapFlatMap(
            id => DataResult<ResourceKey<EquipmentAsset>>.Success(
                ResourceKey<EquipmentAsset>.Create(EquipmentAssets.RootId, id)),
            key => key.Identifier);

    //Codec persistence codec, field names slot/equip_sound/asset_id/camera_overlay/allowed_entities and the five flags, maps to vanilla CODEC
    public static readonly Codec<Equippable> Codec = RecordCodecBuilder.Of11(
        EquipmentSlotExtensions.Codec.FieldOf("slot").ForGetter((Equippable e) => e.Slot),
        SoundEventCodec.Instance.OptionalFieldOf("equip_sound", SoundEvents.ArmorEquipGeneric)
            .ForGetter((Equippable e) => e.EquipSound),
        AssetKeyCodec.OptionalFieldOf("asset_id").ForGetter((Equippable e) => e.AssetId),
        IdentifierCodec.Instance.OptionalFieldOf("camera_overlay").ForGetter((Equippable e) => e.CameraOverlay),
        HolderSetCodecs.EntityTypeSet.OptionalFieldOf("allowed_entities")
            .ForGetter((Equippable e) => e.AllowedEntities),
        Codecs.Bool.OptionalFieldOf("dispensable", true).ForGetter((Equippable e) => e.Dispensable),
        Codecs.Bool.OptionalFieldOf("swappable", true).ForGetter((Equippable e) => e.Swappable),
        Codecs.Bool.OptionalFieldOf("damage_on_hurt", true).ForGetter((Equippable e) => e.DamageOnHurt),
        Codecs.Bool.OptionalFieldOf("equip_on_interact", false).ForGetter((Equippable e) => e.EquipOnInteract),
        Codecs.Bool.OptionalFieldOf("can_be_sheared", false).ForGetter((Equippable e) => e.CanBeSheared),
        SoundEventCodec.Instance.OptionalFieldOf("shearing_sound", SoundEvents.ShearsSnip)
            .ForGetter((Equippable e) => e.ShearingSound),
        (slot, equipSound, assetId, cameraOverlay, allowedEntities, dispensable, swappable, damageOnHurt,
                equipOnInteract, canBeSheared, shearingSound) =>
            new Equippable(slot, equipSound, assetId, cameraOverlay, allowedEntities, dispensable, swappable,
                damageOnHurt, equipOnInteract, canBeSheared, shearingSound));

    //StreamCodec network codec, same fields in declaration order, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<RegistryFriendlyByteBuf, Equippable> StreamCodec = new EquippableStreamCodec();

    public Equippable(
        EquipmentSlot slot,
        SoundEvent equipSound,
        Optional<ResourceKey<EquipmentAsset>> assetId,
        Optional<Identifier> cameraOverlay,
        Optional<HolderSet<EntityType<object>>> allowedEntities,
        bool dispensable,
        bool swappable,
        bool damageOnHurt,
        bool equipOnInteract,
        bool canBeSheared,
        SoundEvent shearingSound)
    {
        Slot = slot;
        EquipSound = equipSound;
        AssetId = assetId;
        CameraOverlay = cameraOverlay;
        AllowedEntities = allowedEntities;
        Dispensable = dispensable;
        Swappable = swappable;
        DamageOnHurt = damageOnHurt;
        EquipOnInteract = equipOnInteract;
        CanBeSheared = canBeSheared;
        ShearingSound = shearingSound;
    }

    public EquipmentSlot Slot { get; }
    public SoundEvent EquipSound { get; }
    public Optional<ResourceKey<EquipmentAsset>> AssetId { get; }
    public Optional<Identifier> CameraOverlay { get; }
    public Optional<HolderSet<EntityType<object>>> AllowedEntities { get; }
    public bool Dispensable { get; }
    public bool Swappable { get; }
    public bool DamageOnHurt { get; }
    public bool EquipOnInteract { get; }
    public bool CanBeSheared { get; }
    public SoundEvent ShearingSound { get; }

    //CanBeEquippedBy whether the given entity type may wear the item, maps to vanilla canBeEquippedBy
    //An empty allowed set means every entity can wear it
    public bool CanBeEquippedBy(Holder<EntityType<object>> type)
        => !AllowedEntities.IsPresent || AllowedEntities.Get().Contains(type);

    //Builder starts an equippable builder for the given slot, maps to vanilla builder
    public static Builder CreateBuilder(EquipmentSlot slot) => new(slot);

    public bool Equals(Equippable? other)
        => other is not null
           && Slot == other.Slot
           && EquipSound.Equals(other.EquipSound)
           && AssetId.Equals(other.AssetId)
           && CameraOverlay.Equals(other.CameraOverlay)
           && Dispensable == other.Dispensable
           && Swappable == other.Swappable
           && DamageOnHurt == other.DamageOnHurt
           && EquipOnInteract == other.EquipOnInteract
           && CanBeSheared == other.CanBeSheared
           && ShearingSound.Equals(other.ShearingSound);

    public override bool Equals(object? obj) => Equals(obj as Equippable);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Slot);
        hash.Add(EquipSound);
        hash.Add(AssetId);
        hash.Add(Dispensable);
        hash.Add(Swappable);
        hash.Add(DamageOnHurt);
        hash.Add(EquipOnInteract);
        hash.Add(CanBeSheared);
        hash.Add(ShearingSound);
        return hash.ToHashCode();
    }

    public override string ToString() => $"Equippable[slot={Slot}, asset={AssetId}]";

    //Builder accumulates the equippable fields with the vanilla defaults
    public sealed class Builder
    {
        private readonly EquipmentSlot _slot;
        private bool _equipOnInteract;
        private bool _canBeSheared;
        private SoundEvent _equipSound = SoundEvents.ArmorEquipGeneric;
        private Optional<ResourceKey<EquipmentAsset>> _assetId = Optional<ResourceKey<EquipmentAsset>>.Empty();
        private Optional<Identifier> _cameraOverlay = Optional<Identifier>.Empty();
        private Optional<HolderSet<EntityType<object>>> _allowedEntities =
            Optional<HolderSet<EntityType<object>>>.Empty();
        private bool _dispensable = true;
        private bool _swappable = true;
        private bool _damageOnHurt = true;
        private SoundEvent _shearingSound = SoundEvents.ShearsSnip;

        internal Builder(EquipmentSlot slot) => _slot = slot;

        public Builder SetEquipSound(SoundEvent equipSound)
        {
            _equipSound = equipSound;
            return this;
        }

        public Builder SetAsset(ResourceKey<EquipmentAsset> assetId)
        {
            _assetId = Optional<ResourceKey<EquipmentAsset>>.Of(assetId);
            return this;
        }

        public Builder SetCameraOverlay(Identifier cameraOverlay)
        {
            _cameraOverlay = Optional<Identifier>.Of(cameraOverlay);
            return this;
        }

        public Builder SetAllowedEntities(params EntityType<object>[] allowedEntities)
        {
            var holders = new List<Holder<EntityType<object>>>(allowedEntities.Length);
            foreach (var type in allowedEntities)
                holders.Add(BuiltInRegistries.ENTITY_TYPE.WrapAsHolder(type));
            return SetAllowedEntities(new DirectHolderSet<EntityType<object>>(holders));
        }

        public Builder SetAllowedEntities(HolderSet<EntityType<object>> allowedEntities)
        {
            _allowedEntities = Optional<HolderSet<EntityType<object>>>.Of(allowedEntities);
            return this;
        }

        public Builder SetDispensable(bool dispensable)
        {
            _dispensable = dispensable;
            return this;
        }

        public Builder SetSwappable(bool swappable)
        {
            _swappable = swappable;
            return this;
        }

        public Builder SetDamageOnHurt(bool damageOnHurt)
        {
            _damageOnHurt = damageOnHurt;
            return this;
        }

        public Builder SetEquipOnInteract(bool equipOnInteract)
        {
            _equipOnInteract = equipOnInteract;
            return this;
        }

        public Builder SetCanBeSheared(bool canBeSheared)
        {
            _canBeSheared = canBeSheared;
            return this;
        }

        public Builder SetShearingSound(SoundEvent shearingSound)
        {
            _shearingSound = shearingSound;
            return this;
        }

        public Equippable Build()
            => new(_slot, _equipSound, _assetId, _cameraOverlay, _allowedEntities, _dispensable, _swappable,
                _damageOnHurt, _equipOnInteract, _canBeSheared, _shearingSound);
    }
}

//EquippableStreamCodec slot id, sound ids, optional asset/camera/entity set, five flags and the shearing sound
internal sealed class EquippableStreamCodec : StreamCodec<RegistryFriendlyByteBuf, Equippable>
{
    private readonly StreamCodec<RegistryFriendlyByteBuf, Holder<EntityType<object>>> _entityHolderCodec =
        ByteBufCodecs.Holder(Registries.ENTITY_TYPE);

    public Equippable Decode(RegistryFriendlyByteBuf buf)
    {
        var slot = EquipmentSlotExtensions.ById(buf.ReadVarInt());
        var equipSound = new SoundEvent(buf.ReadIdentifier());
        var assetId = buf.ReadBoolean()
            ? Optional<ResourceKey<EquipmentAsset>>.Of(ResourceKey<EquipmentAsset>.Create(
                EquipmentAssets.RootId, buf.ReadIdentifier()))
            : Optional<ResourceKey<EquipmentAsset>>.Empty();
        var cameraOverlay = buf.ReadBoolean()
            ? Optional<Identifier>.Of(buf.ReadIdentifier())
            : Optional<Identifier>.Empty();
        Optional<HolderSet<EntityType<object>>> allowedEntities = Optional<HolderSet<EntityType<object>>>.Empty();
        if (buf.ReadBoolean())
        {
            var size = buf.ReadVarInt();
            var holders = new List<Holder<EntityType<object>>>(Math.Min(size, ByteBufCodecs.MaxInitialCollectionSize));
            for (var i = 0; i < size; i++) holders.Add(_entityHolderCodec.Decode(buf));
            allowedEntities = Optional<HolderSet<EntityType<object>>>.Of(
                new DirectHolderSet<EntityType<object>>(holders));
        }
        var dispensable = buf.ReadBoolean();
        var swappable = buf.ReadBoolean();
        var damageOnHurt = buf.ReadBoolean();
        var equipOnInteract = buf.ReadBoolean();
        var canBeSheared = buf.ReadBoolean();
        var shearingSound = new SoundEvent(buf.ReadIdentifier());
        return new Equippable(slot, equipSound, assetId, cameraOverlay, allowedEntities, dispensable, swappable,
            damageOnHurt, equipOnInteract, canBeSheared, shearingSound);
    }

    public void Encode(RegistryFriendlyByteBuf buf, Equippable value)
    {
        buf.WriteVarInt(value.Slot.GetId());
        buf.WriteIdentifier(value.EquipSound.Location);
        WriteOptionalIdentifier(buf, value.AssetId.IsPresent ? value.AssetId.Get().Identifier : null);
        WriteOptionalIdentifier(buf, value.CameraOverlay.IsPresent ? value.CameraOverlay.Get() : null);
        if (value.AllowedEntities.IsPresent)
        {
            buf.WriteBoolean(true);
            var set = value.AllowedEntities.Get();
            buf.WriteVarInt(set.Size);
            for (var i = 0; i < set.Size; i++) _entityHolderCodec.Encode(buf, set.Get(i));
        }
        else
        {
            buf.WriteBoolean(false);
        }
        buf.WriteBoolean(value.Dispensable);
        buf.WriteBoolean(value.Swappable);
        buf.WriteBoolean(value.DamageOnHurt);
        buf.WriteBoolean(value.EquipOnInteract);
        buf.WriteBoolean(value.CanBeSheared);
        buf.WriteIdentifier(value.ShearingSound.Location);
    }

    private static void WriteOptionalIdentifier(RegistryFriendlyByteBuf buf, Identifier? id)
    {
        buf.WriteBoolean(id.HasValue);
        if (id.HasValue) buf.WriteIdentifier(id.Value);
    }
}
