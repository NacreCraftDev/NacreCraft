using NacreCraft.Client.Level;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Network;
using NacreCraft.Primitives;
using EntityAttributes = NacreCraft.Registry.EntityAttribute.Attributes;

namespace NacreCraft.Client.Player;

//LocalPlayer client-side player, maps to vanilla net.minecraft.client.player.LocalPlayer
//Owns the local movement physics and the position/rotation packets; input is filled by the input layer, this class never touches the UI
public sealed class LocalPlayer : NacreCraft.Game.World.Entity.Player
{
    //Move threshold from vanilla LocalPlayer.sendPosition: Mth.square(2.0E-4)
    private const double MoveThresholdSqr = 4.0E-8;
    //A position packet is forced at least every 20 ticks, so a stationary player stays in sync
    private const int PositionReminderLimit = 20;
    //Vanilla noJumpDelay after a jump
    private const int NoJumpDelay = 10;

    private readonly ClientLevel _level;
    private int _noJumpDelay;
    private int _positionReminder;
    private Vec3 _lastAckPos;
    private float _lastAckYRot;
    private float _lastAckXRot;
    private bool _lastAckOnGround;
    private bool _lastAckHorizontalCollision;
    private bool _ackInitialized;

    public LocalPlayer(ClientLevel level)
    {
        _level = level;
        //Binds the level reference and the collision query; without it Move walks through blocks
        _level.AttachEntity(this);
    }

    //Current the local player of the running client, null before the client is up or after it is disposed
    //This is the entry a mod takes for movement input and rotation, without walking the client itself
    public static LocalPlayer? Current => NacreCraft.Client.MinecraftClient.Current?.Player;

    //Connection play connection used to send position packets, null before the handshake completes
    public Connection? Connection { get; set; }

    //CanSendPosition false until the server sends its first position sync; sending before that would fight the server correction
    public bool CanSendPosition { get; private set; }

    //--- movement input, filled by the input layer ---
    //ForwardInput/StrafeInput follow vanilla zza/xxa, -1..1, forward positive
    public float ForwardInput { get; set; }
    public float StrafeInput { get; set; }
    public bool JumpInput { get; set; }
    public bool SneakInput { get; set; }
    public bool SprintInput { get; set; }

    //CanFly mirror of the server ability flags, only true in creative and spectator
    public bool CanFly { get; set; }

    //FlyingSpeedAbility flying speed from the server abilities packet, vanilla default 0.05
    public float FlyingSpeedAbility { get; set; } = 0.05f;

    //SetRotation sets the look angles, pitch clamped to the vanilla range; the next tick sends them when they changed
    public void SetRotation(float yRot, float xRot)
    {
        YRot = yRot;
        XRot = Math.Clamp(xRot, -90f, 90f);
    }

    //AddRotation accumulates a look delta, for the camera controls
    public void AddRotation(float yRotDelta, float xRotDelta) => SetRotation(YRot + yRotDelta, XRot + xRotDelta);

    //ClientTick runs one 20 Hz client player tick: input decay, jump, travel, then the position packet
    //Maps to the movement part of vanilla LocalPlayer.aiStep plus Entity.baseTick
    public void ClientTick()
    {
        TickBase();
        ApplyInput();
        UpdateFlightInput();
        if (!IsFlying) UpdateJump();
        Travel(new Vec3(StrafeInput, 0.0, ForwardInput));
        CalculateEntityAnimation(false);
        SendPositionIfNeeded();
    }

    //Travel runs the vanilla Player.travel split: flying keeps 0.6 of the previous vertical speed
    public override void Travel(Vec3 input)
    {
        if (!IsFlying)
        {
            base.Travel(input);
            return;
        }
        //Vanilla saves the vertical speed before travel and restores it scaled to 0.6 afterwards, discarding gravity and collision clamping on that axis
        var verticalSpeed = Velocity.Y;
        base.Travel(input);
        Velocity = new Vec3(Velocity.X, verticalSpeed * 0.6, Velocity.Z);
    }

    //FlyingSpeed air acceleration, maps to vanilla Player.getFlyingSpeed
    protected override float FlyingSpeed
        => IsFlying
            ? (IsSprinting ? FlyingSpeedAbility * 2.0f : FlyingSpeedAbility)
            : (IsSprinting ? 0.026f : 0.02f);

    //UpdateFlightInput applies the vertical flight input, maps to the abilities.flying block of vanilla LocalPlayer.aiStep
    //Jump climbs and sneak descends; the value is an acceleration of flyingSpeed * 3 per tick
    private void UpdateFlightInput()
    {
        if (!IsFlying) return;
        var vertical = 0;
        if (SneakInput) vertical--;
        if (JumpInput) vertical++;
        if (vertical == 0) return;
        Velocity = Velocity.Add(new Vec3(0.0, vertical * FlyingSpeedAbility * 3.0, 0.0));
    }

    //ApplyInput decays the input each tick, maps to vanilla LivingEntity.applyInput
    private void ApplyInput()
    {
        StrafeInput *= 0.98f;
        ForwardInput *= 0.98f;
    }

    //UpdateJump starts a ground jump with the vanilla 10 tick delay
    private void UpdateJump()
    {
        if (_noJumpDelay > 0) _noJumpDelay--;
        if (!JumpInput || !OnGround || _noJumpDelay > 0) return;
        JumpFromGround();
        _noJumpDelay = NoJumpDelay;
    }

    //JumpFromGround sets the vertical speed to the jump strength, maps to vanilla LivingEntity.jumpFromGround
    private void JumpFromGround()
    {
        var jumpPower = (float)GetAttributeValue(EntityAttributes.JumpStrength);
        if (jumpPower <= 1.0E-5f) return;
        Velocity = new Vec3(Velocity.X, Math.Max(jumpPower, Velocity.Y), Velocity.Z);
        if (!IsSprinting) return;
        var angle = YRot * (MathF.PI / 180f);
        Velocity = Velocity.Add(new Vec3(-MathF.Sin(angle) * 0.2, 0.0, MathF.Cos(angle) * 0.2));
    }

    //TravelInAir walks the player on the ground and through the air, maps to vanilla LivingEntity.travelInAir
    //Overridden because the base implementation hardcodes 0.6 block friction and drops the friction-above-0.6 speed branch
    protected override void TravelInAir(Vec3 input)
    {
        var blockFriction = OnGround ? GetBlockFriction() : 1.0f;
        var speed = OnGround
            ? (blockFriction > 0.6f
                ? Speed * (0.21600002f / (blockFriction * blockFriction * blockFriction))
                : Speed)
            : FlyingSpeed;
        MoveRelative(speed, input);
        Move(Velocity);
        var movement = Velocity;
        var horizontal = blockFriction * (float)HorizontalDrag;
        Velocity = new Vec3(
            movement.X * horizontal,
            (movement.Y - DefaultGravity) * (float)VerticalDrag,
            movement.Z * horizontal);
    }

    //GetBlockFriction friction of the block below the feet scaled by the friction modifier attribute, maps to vanilla travelInAir
    private float GetBlockFriction()
        => _level.GetBlockState(GetBlockPosBelowThatAffectsMyMovement()).Owner.Friction
            * (float)GetAttributeValue(EntityAttributes.FrictionModifier);

    //SendPositionIfNeeded sends the position/rotation packet the same way vanilla LocalPlayer.sendPosition does
    //Exactly one of pos_rot/pos/rot/status_only is sent, and only when the thresholds or the 20 tick reminder fire
    private void SendPositionIfNeeded()
    {
        if (Connection is not { } connection || !CanSendPosition) return;
        if (!_ackInitialized)
        {
            ResetAck();
            return;
        }
        var dx = Pos.X - _lastAckPos.X;
        var dy = Pos.Y - _lastAckPos.Y;
        var dz = Pos.Z - _lastAckPos.Z;
        var dYRot = YRot - _lastAckYRot;
        var dXRot = XRot - _lastAckXRot;
        _positionReminder++;
        var moved = dx * dx + dy * dy + dz * dz > MoveThresholdSqr || _positionReminder >= PositionReminderLimit;
        var rotated = dYRot != 0f || dXRot != 0f;
        if (moved && rotated)
            connection.Send(new ServerboundMovePlayerPacket(Pos.X, Pos.Y, Pos.Z, YRot, XRot, OnGround, HorizontalCollision, true, true));
        else if (moved)
            connection.Send(new ServerboundMovePlayerPacket(Pos.X, Pos.Y, Pos.Z, 0f, 0f, OnGround, HorizontalCollision, true, false));
        else if (rotated)
            connection.Send(new ServerboundMovePlayerPacket(0.0, 0.0, 0.0, YRot, XRot, OnGround, HorizontalCollision, false, true));
        else if (OnGround != _lastAckOnGround || HorizontalCollision != _lastAckHorizontalCollision)
            connection.Send(new ServerboundMovePlayerPacket(0.0, 0.0, 0.0, 0f, 0f, OnGround, HorizontalCollision, false, false));
        if (moved)
        {
            _lastAckPos = Pos;
            _positionReminder = 0;
        }
        if (rotated)
        {
            _lastAckYRot = YRot;
            _lastAckXRot = XRot;
        }
        _lastAckOnGround = OnGround;
        _lastAckHorizontalCollision = HorizontalCollision;
    }

    //ResetAck snaps the last-sent state to the current one, used on join and after a server correction
    public void ResetAck()
    {
        _lastAckPos = Pos;
        _lastAckYRot = YRot;
        _lastAckXRot = XRot;
        _lastAckOnGround = OnGround;
        _lastAckHorizontalCollision = HorizontalCollision;
        _positionReminder = 0;
        _ackInitialized = true;
    }

    //OnServerPosition applies a server-authoritative position: the local velocity is dropped, the ack state snaps and sending becomes allowed
    //Without this the next tick would send the pre-correction position back and fight the server
    public void OnServerPosition()
    {
        Velocity = Vec3.Zero;
        CanSendPosition = true;
        ResetAck();
    }

    //Teleport places the player at a server position, clearing velocity and the pending send state
    public void Teleport(double x, double y, double z, float yRot, float xRot)
    {
        SetPos(new Vec3(x, y, z), yRot, xRot);
        OnServerPosition();
    }
}
