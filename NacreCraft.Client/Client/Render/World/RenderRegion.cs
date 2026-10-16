using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render;
using NacreCraft.Primitives;

namespace NacreCraft.Client.Render.World;

//ArenaSlice a byte range inside a GPU buffer
public readonly record struct ArenaSlice(GpuBuffer Buffer, long Offset, long Length)
{
    public bool IsValid => Buffer is not null;
}

//RenderRegionPos render region coordinates, a region spans 8x4x8 sections (128x64x128 blocks)
public readonly struct RenderRegionPos : IEquatable<RenderRegionPos>
{
    public const int ShiftX = 3;
    public const int ShiftY = 2;
    public const int ShiftZ = 3;

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public RenderRegionPos(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    //Of the region containing a section
    public static RenderRegionPos Of(SectionPos pos)
        => new(pos.X >> ShiftX, pos.Y >> ShiftY, pos.Z >> ShiftZ);

    //AsLong packs the coordinates for use as a dictionary key
    public long AsLong()
        => (((long)X & 0x7FFFF) << 37) | (((long)Y & 0x3FFFF) << 19) | ((long)Z & 0x7FFFF);

    public bool Equals(RenderRegionPos other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is RenderRegionPos other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public static bool operator ==(RenderRegionPos left, RenderRegionPos right) => left.Equals(right);
    public static bool operator !=(RenderRegionPos left, RenderRegionPos right) => !left.Equals(right);
    public override string ToString() => $"[{X}, {Y}, {Z}]";
}

//RenderRegion one render region: the 8x4x8 section slots it owns plus the packed buffer its sections upload into
//A section stages its bytes keyed by its own position, so a region binds one vertex buffer and one index buffer for
//all of its sections, which is what makes a batched draw possible
//Heap blocks are reclaimed on their own once every allocation inside them has been released, which is what replaced
//the old "rebuild the whole region once its arena grew past a threshold" rule
public sealed class RenderRegion : IDisposable
{
    public const int SectionWidth = 8;
    public const int SectionHeight = 4;
    public const int SectionDepth = 8;
    public const int SectionCount = SectionWidth * SectionHeight * SectionDepth;

    //HeapSize bytes of one GPU buffer the packed buffer opens on demand
    private const long HeapSize = 4 * 1024 * 1024;
    //Alignment every section range starts at, four byte aligned so vertex strides and int indices both fit
    private const int Alignment = 4;
    //StagingBatchSize bytes the staging area holds before it has to be flushed
    private const int StagingBatchSize = 1 << 20;

    private readonly RenderSection?[] _sections = new RenderSection?[SectionCount];
    private readonly GpuDevice? _device;
    private readonly StagingBuffer _staging;
    private readonly UberGpuBuffer<long> _vertices;
    private readonly UberGpuBuffer<long> _indices;
    private bool _disposed;

    public RenderRegionPos Pos { get; }

    //LiveSectionCount sections currently attached
    public int LiveSectionCount { get; private set; }

    public RenderRegion(RenderRegionPos pos, GpuBufferPool pool)
    {
        Pos = pos;
        _device = pool.Device;
        _staging = StagingBuffer.Create($"region {pos} staging", StagingBatchSize);
        _vertices = new UberGpuBuffer<long>($"region {pos} vertices",
            GpuBuffer.UsageVertex | GpuBuffer.UsageCopyDst, HeapSize, Alignment, _staging);
        _indices = new UberGpuBuffer<long>($"region {pos} indices",
            GpuBuffer.UsageIndex | GpuBuffer.UsageCopyDst, HeapSize, Alignment, _staging);
    }

    //SlotIndex the region-local slot of a section, 8 bits wide so it fits the sectionIndex vertex field
    public static int SlotIndex(int sectionX, int sectionY, int sectionZ)
        => (sectionX & (SectionWidth - 1))
            | ((sectionZ & (SectionDepth - 1)) << 3)
            | ((sectionY & (SectionHeight - 1)) << 6);

    public RenderSection? GetSection(int slot) => _sections[slot];

    public void SetSection(int slot, RenderSection? section)
    {
        var previous = _sections[slot];
        if (previous is not null && section is null) LiveSectionCount--;
        else if (previous is null && section is not null) LiveSectionCount++;
        _sections[slot] = section;
    }

    //Sections attached to the region, in slot order
    public IEnumerable<RenderSection> Sections
    {
        get
        {
            foreach (var section in _sections)
                if (section is not null) yield return section;
        }
    }

    //UploadVertexData stages a section's vertices and puts them on the GPU, returning the byte range they landed in
    public ArenaSlice UploadVertexData(long key, ReadOnlySpan<byte> data) => Upload(_vertices, key, data);

    //UploadIndexData same for a section's indices
    public ArenaSlice UploadIndexData(long key, ReadOnlySpan<byte> data) => Upload(_indices, key, data);

    //Release drops whatever the key owns, so its heap blocks can be reclaimed once they empty out
    public void Release(long key)
    {
        _vertices.RemoveAllocation(key);
        _indices.RemoveAllocation(key);
    }

    private ArenaSlice Upload(UberGpuBuffer<long> buffer, long key, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return default;
        var device = _device ?? throw new InvalidOperationException("Uploading section data needs a GPU device");
        //Staging is bounded, so a full batch is flushed and the append retried
        if (!buffer.AddAllocation(key, null, data))
        {
            Flush(device);
            if (!buffer.AddAllocation(key, null, data))
                throw new InvalidOperationException($"Section data of {data.Length} bytes does not fit the staging batch");
        }
        Flush(device);
        var allocation = buffer.GetAllocation(key)
            ?? throw new InvalidOperationException("The allocation is missing right after staging it");
        return new ArenaSlice(buffer.GetGpuBuffer(allocation), allocation.OffsetFromHeap, data.Length);
    }

    //Flush hands everything staged to the device through a one time command, the same path texture uploads take
    //WriteToBuffer records a buffer copy rather than writing straight through, so the command has to run and complete
    //before the bytes are really on the GPU
    private void Flush(GpuDevice device)
    {
        using var encoder = device.CreateCommandEncoder();
        using (var uploader = _staging.StartUploading(encoder))
        {
            _vertices.UploadStagedAllocations(device, uploader);
            _indices.UploadStagedAllocations(device, uploader);
        }
        encoder.Submit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _vertices.Dispose();
        _indices.Dispose();
        _staging.Dispose();
        _disposed = true;
    }
}
