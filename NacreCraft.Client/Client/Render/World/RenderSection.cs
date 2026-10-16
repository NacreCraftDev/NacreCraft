using System.Threading;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Primitives;

namespace NacreCraft.Client.Render.World;

//RenderSectionState section render state machine
//Empty not compiled, Queued in the compile queue, Compiling compiling in the background, Compiled done compiling awaiting upload, Uploaded uploaded and drawable
//Dirty uploaded but needs recompiling; the old buffer is still valid, Draw is skipped until recompiling finishes
public enum RenderSectionState : byte
{
    Empty,
    Queued,
    Compiling,
    Compiled,
    Uploaded,
    Dirty
}

//RenderSection single-section render data holder + state machine
//_mesh is published atomically with Interlocked.Exchange; the compile thread writes and the Render thread reads
//The vertex and index data live in the owning RenderRegion's arenas, so the section only keeps byte ranges
//BoundingBox built from Pos<<4 for Frustum culling
public sealed class RenderSection
{
    public SectionPos Pos { get; }
    public AABB BoundingBox { get; }

    private volatile RenderSectionState _state;
    public RenderSectionState State => _state;

    private SectionMesh? _mesh;
    public SectionMesh? Mesh => _mesh;

    //Region the section belongs to; assigned when it is first marked dirty
    public RenderRegion? Region { get; set; }

    //Slot of the section inside its region, 0-255
    public int Slot { get; set; }

    //RegionKey sort key that clusters sections of one region together, so Draw pushes a region origin once
    public long RegionKey => Region?.Pos.AsLong() ?? long.MaxValue;

    //CameraDistanceSq squared distance from the camera, written by Draw and read when ordering the translucent pass
    public float CameraDistanceSq { get; set; }

    //VertexRange/IndexRange the section's byte ranges inside the region arenas, valid while State==Uploaded
    public ArenaSlice VertexRange { get; private set; }
    public ArenaSlice IndexRange { get; private set; }
    public bool HasUploadedRanges { get; private set; }

    //Slices per-layer offsets inside the section's own ranges, used by dispatcher.GetSectionSlice
    //null means the layer has no vertices; skipped at Draw
    public UploadedSlice?[] Slices { get; } = new UploadedSlice?[3];

    //IsRemoved chunk unload flag set to true by UnloadSection; compile/upload threads check and skip to prevent races
    private volatile bool _removed;
    public bool IsRemoved => _removed;

    public RenderSection(SectionPos pos)
    {
        Pos = pos;
        var minX = pos.X << 4;
        var minY = pos.Y << 4;
        var minZ = pos.Z << 4;
        BoundingBox = new AABB(minX, minY, minZ, minX + 16, minY + 16, minZ + 16);
    }

    //MarkRemoved marks as unloaded; the compile thread checks before PublishMesh to keep a removed section from entering the upload queue
    public void MarkRemoved() => _removed = true;

    //TryMarkDirty marks dirty and enqueues; CAS on state prevents duplicates
    //Empty/Compiled → Queued, no uploaded ranges, enqueue directly
    //Uploaded → Dirty, the old ranges are still valid, Draw is skipped until recompiling finishes
    //Queued/Compiling/Dirty return false; already queued or compiling, skip
    public bool TryMarkDirty()
    {
        while (true)
        {
            var prev = _state;
            if (prev == RenderSectionState.Queued
                || prev == RenderSectionState.Compiling
                || prev == RenderSectionState.Dirty)
                return false;
            var target = prev == RenderSectionState.Uploaded
                ? RenderSectionState.Dirty
                : RenderSectionState.Queued;
            if (Interlocked.CompareExchange(ref _state, target, prev) == prev)
                return true;
        }
    }

    //TryBeginCompile called by a worker after Take; Queued/Dirty → Compiling
    public bool TryBeginCompile()
    {
        while (true)
        {
            var prev = _state;
            if (prev != RenderSectionState.Queued && prev != RenderSectionState.Dirty)
                return false;
            if (Interlocked.CompareExchange(ref _state, RenderSectionState.Compiling, prev) == prev)
                return true;
        }
    }

    //PublishMesh called by the compile thread; atomically replaces mesh and transitions to Compiled for the Render thread to Upload
    public void PublishMesh(SectionMesh mesh)
    {
        Interlocked.Exchange(ref _mesh, mesh);
        Interlocked.Exchange(ref _state, RenderSectionState.Compiled);
    }

    //SetUploaded records the region arena ranges after the upload and transitions to Uploaded
    public void SetUploaded(ArenaSlice vertexRange, ArenaSlice indexRange)
    {
        VertexRange = vertexRange;
        IndexRange = indexRange;
        HasUploadedRanges = true;
        _state = RenderSectionState.Uploaded;
    }

    //ClearUploaded drops the ranges, used when the owning region resets its arenas
    public void ClearUploaded()
    {
        VertexRange = default;
        IndexRange = default;
        HasUploadedRanges = false;
        Array.Fill(Slices, null);
    }
}

//UploadedSlice per-layer offsets inside the section's own arena ranges
//BaseVertex the layer's first vertex index inside the region vertex arena; FirstIndex index start; IndexCount index count
public readonly record struct UploadedSlice(int BaseVertex, int FirstIndex, int IndexCount);
