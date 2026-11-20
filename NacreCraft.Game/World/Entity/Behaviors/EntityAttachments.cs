using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//EntityAttachment an attachment point kind, maps to vanilla net.minecraft.world.entity.EntityAttachment
//The fallback points are used when a dimension does not declare explicit points for the kind
public enum EntityAttachment
{
    PASSENGER,
    VEHICLE,
    NAME_TAG,
    WARDEN_CHEST,
}

//EntityAttachmentExtensions fallback point tables, maps to the vanilla Fallback interface constants
public static class EntityAttachmentExtensions
{
    public static IReadOnlyList<Vec3> CreateFallbackPoints(this EntityAttachment attachment, float width, float height)
        => attachment switch
        {
            EntityAttachment.PASSENGER => new[] { new Vec3(0, height, 0) },
            EntityAttachment.VEHICLE => new[] { Vec3.Zero },
            EntityAttachment.NAME_TAG => new[] { new Vec3(0, height, 0) },
            EntityAttachment.WARDEN_CHEST => new[] { new Vec3(0, height / 2.0, 0) },
            _ => Array.Empty<Vec3>(),
        };
}

//EntityAttachments the attachment points of one entity size, maps to vanilla net.minecraft.world.entity.EntityAttachments
public sealed class EntityAttachments
{
    private readonly IReadOnlyDictionary<EntityAttachment, IReadOnlyList<Vec3>> _attachments;

    private EntityAttachments(IReadOnlyDictionary<EntityAttachment, IReadOnlyList<Vec3>> attachments)
        => _attachments = attachments;

    //CreateDefault the fallback points for every attachment kind
    public static EntityAttachments CreateDefault(float width, float height) => NewBuilder().Build(width, height);

    public static Builder NewBuilder() => new();

    //Scale scales every point by the given factors, maps to vanilla scale
    public EntityAttachments Scale(float x, float y, float z)
    {
        var result = new Dictionary<EntityAttachment, IReadOnlyList<Vec3>>();
        foreach (var (attachment, points) in _attachments)
            result[attachment] = points.Select(point => new Vec3(point.X * x, point.Y * y, point.Z * z)).ToList();
        return new EntityAttachments(result);
    }

    //GetNullable the point at index rotated by the yaw, null when the index is out of range
    public Vec3? GetNullable(EntityAttachment attachment, int index, float yRot)
    {
        if (!_attachments.TryGetValue(attachment, out var points)) return null;
        if (index < 0 || index >= points.Count) return null;
        return TransformPoint(points[index], yRot);
    }

    //Get the point at index, throws when the index is out of range
    public Vec3 Get(EntityAttachment attachment, int index, float yRot)
        => GetNullable(attachment, index, yRot)
           ?? throw new InvalidOperationException($"Had no attachment point of type: {attachment} for index: {index}");

    //GetAverage the average of every point of a kind, throws when there are none
    public Vec3 GetAverage(EntityAttachment attachment)
    {
        if (!_attachments.TryGetValue(attachment, out var points) || points.Count == 0)
            throw new InvalidOperationException("No attachment points of type: PASSENGER");
        var sum = Vec3.Zero;
        foreach (var point in points) sum = sum.Add(point);
        return sum.Multiply(1.0 / points.Count);
    }

    //GetClamped the point at index clamped into range, maps to vanilla getClamped
    public Vec3 GetClamped(EntityAttachment attachment, int index, float yRot)
    {
        if (!_attachments.TryGetValue(attachment, out var points) || points.Count == 0)
            throw new InvalidOperationException($"Had no attachment points of type: {attachment}");
        return TransformPoint(points[Math.Clamp(index, 0, points.Count - 1)], yRot);
    }

    //TransformPoint rotates a point by the yaw, maps to vanilla transformPoint
    private static Vec3 TransformPoint(Vec3 point, float yRot)
    {
        var radians = -yRot * (Math.PI / 180.0);
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Vec3(point.X * cos + point.Z * sin, point.Y, -point.X * sin + point.Z * cos);
    }

    //Builder collects explicit points and falls back per kind on build
    public sealed class Builder
    {
        private readonly Dictionary<EntityAttachment, List<Vec3>> _points = new();

        public Builder Attach(EntityAttachment attachment, float x, float y, float z)
            => Attach(attachment, new Vec3(x, y, z));

        public Builder Attach(EntityAttachment attachment, Vec3 point)
        {
            if (!_points.TryGetValue(attachment, out var list)) _points[attachment] = list = new List<Vec3>(1);
            list.Add(point);
            return this;
        }

        public EntityAttachments Build(float width, float height)
        {
            var result = new Dictionary<EntityAttachment, IReadOnlyList<Vec3>>();
            foreach (var attachment in Enum.GetValues<EntityAttachment>())
                result[attachment] = _points.TryGetValue(attachment, out var points)
                    ? points.ToList()
                    : attachment.CreateFallbackPoints(width, height);
            return new EntityAttachments(result);
        }
    }
}
