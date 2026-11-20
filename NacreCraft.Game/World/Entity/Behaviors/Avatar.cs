using NacreCraft.Network.Protocol.Common;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//Avatar the shared base of player-like entities, maps to vanilla net.minecraft.world.entity.Avatar
//Carries the profile, main arm, model customization byte and the pose-dependent dimensions; NC's Entity has no pose/dimension
//hook yet, so GetDefaultDimensions is a plain method the renderer can call rather than an override of Entity.getDimensions
public abstract class Avatar : LivingEntity
{
    public const int DefaultModelCustomization = 0;
    public const float DefaultEyeHeight = 1.62f;
    private const float CrouchBbHeight = 1.5f;
    public const float SwimmingBbWidth = 0.6f;
    public const float SwimmingBbHeight = 0.6f;
    public static readonly HumanoidArm DefaultMainHand = HumanoidArm.Right;
    public static readonly Vec3 DefaultVehicleAttachment = new(0, 0.6, 0);

    protected static readonly EntityDimensions StandingDimensions = EntityDimensions.Scalable(0.6f, 1.8f)
        .WithEyeHeight(DefaultEyeHeight)
        .WithAttachments(EntityAttachments.NewBuilder().Attach(EntityAttachment.VEHICLE, DefaultVehicleAttachment));

    protected static readonly IReadOnlyDictionary<Pose, EntityDimensions> Poses = new Dictionary<Pose, EntityDimensions>
    {
        [Pose.Standing] = StandingDimensions,
        [Pose.Sleeping] = EntityDimensions.Fixed(0.2f, 0.2f).WithEyeHeight(DefaultEyeHeight),
        [Pose.FallFlying] = EntityDimensions.Scalable(SwimmingBbWidth, SwimmingBbHeight).WithEyeHeight(0.4f),
        [Pose.Swimming] = EntityDimensions.Scalable(SwimmingBbWidth, SwimmingBbHeight).WithEyeHeight(0.4f),
        [Pose.SpinAttack] = EntityDimensions.Scalable(SwimmingBbWidth, SwimmingBbHeight).WithEyeHeight(0.4f),
        [Pose.Crouching] = EntityDimensions.Scalable(0.6f, CrouchBbHeight).WithEyeHeight(1.27f)
            .WithAttachments(EntityAttachments.NewBuilder().Attach(EntityAttachment.VEHICLE, DefaultVehicleAttachment)),
        [Pose.Dying] = EntityDimensions.Fixed(0.2f, 0.2f).WithEyeHeight(DefaultEyeHeight),
        [Pose.Sitting] = EntityDimensions.Scalable(0.6f, 1.8f).WithEyeHeight(DefaultEyeHeight),
    };

    private HumanoidArm _mainArm = DefaultMainHand;
    private byte _modelCustomization;

    public abstract ResolvableProfile GetProfile();

    public HumanoidArm GetMainArm() => _mainArm;
    public void SetMainArm(HumanoidArm mainArm) => _mainArm = mainArm;

    //IsModelPartShown whether the model part is enabled in the customization byte, maps to vanilla isModelPartShown
    public bool IsModelPartShown(PlayerModelPart part)
        => (_modelCustomization & part.GetMask()) == part.GetMask();

    public byte GetModelCustomization() => _modelCustomization;
    public void SetModelCustomization(byte customization) => _modelCustomization = customization;

    //GetDefaultDimensions the dimensions of a pose, maps to vanilla getDefaultDimensions
    public EntityDimensions GetDefaultDimensions(Pose pose)
        => Poses.TryGetValue(pose, out var dimensions) ? dimensions : StandingDimensions;
}
