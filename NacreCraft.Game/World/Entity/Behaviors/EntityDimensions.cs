using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//EntityDimensions the size of an entity, maps to vanilla net.minecraft.world.entity.EntityDimensions
//Holds width, height, eye height, attachment points and whether scaling is allowed; the bounding box is built from the feet position
public sealed class EntityDimensions : IEquatable<EntityDimensions>
{
    private EntityDimensions(float width, float height, float eyeHeight, EntityAttachments attachments, bool fixedSize)
    {
        Width = width;
        Height = height;
        EyeHeight = eyeHeight;
        Attachments = attachments;
        FixedSize = fixedSize;
    }

    private EntityDimensions(float width, float height, bool fixedSize)
        : this(width, height, height * 0.85f, EntityAttachments.CreateDefault(width, height), fixedSize)
    {
    }

    public float Width { get; }
    public float Height { get; }
    public float EyeHeight { get; }
    public EntityAttachments Attachments { get; }
    public bool FixedSize { get; }

    //Vanilla-style accessors
    public float WidthValue => Width;
    public float HeightValue => Height;
    public float EyeHeightValue => EyeHeight;

    //MakeBoundingBox the box around a feet position, maps to vanilla makeBoundingBox
    public AABB MakeBoundingBox(Vec3 pos) => MakeBoundingBox(pos.X, pos.Y, pos.Z);

    public AABB MakeBoundingBox(double x, double y, double z)
    {
        var half = Width / 2.0;
        return new AABB(x - half, y, z - half, x + half, y + Height, z + half);
    }

    //Scale scales width/height and the attachments, returns itself when fixed or unscaled
    public EntityDimensions Scale(float scaleFactor) => Scale(scaleFactor, scaleFactor);

    public EntityDimensions Scale(float widthScaleFactor, float heightScaleFactor)
    {
        if (FixedSize || (widthScaleFactor == 1f && heightScaleFactor == 1f)) return this;
        return new EntityDimensions(Width * widthScaleFactor, Height * heightScaleFactor,
            EyeHeight * heightScaleFactor,
            Attachments.Scale(widthScaleFactor, heightScaleFactor, widthScaleFactor), false);
    }

    public static EntityDimensions Scalable(float width, float height) => new(width, height, false);

    public static EntityDimensions Fixed(float width, float height) => new(width, height, true);

    public EntityDimensions WithEyeHeight(float eyeHeight)
        => new(Width, Height, eyeHeight, Attachments, FixedSize);

    public EntityDimensions WithAttachments(EntityAttachments.Builder attachments)
        => new(Width, Height, EyeHeight, attachments.Build(Width, Height), FixedSize);

    public bool Equals(EntityDimensions? other)
        => other is not null && Width.Equals(other.Width) && Height.Equals(other.Height)
           && EyeHeight.Equals(other.EyeHeight) && FixedSize == other.FixedSize;

    public override bool Equals(object? obj) => Equals(obj as EntityDimensions);
    public override int GetHashCode() => HashCode.Combine(Width, Height, EyeHeight, FixedSize);
    public override string ToString() => $"EntityDimensions[{Width}, {Height}] ({EyeHeight})";
}
