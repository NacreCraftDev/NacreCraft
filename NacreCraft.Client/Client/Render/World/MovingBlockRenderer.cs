using System.Numerics;
using System.Runtime.InteropServices;
using NacreCraft.Client.Level;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;

namespace NacreCraft.Client.Render.World;

//MovingBlockRenderer client rendering pass for moving blocks, maps to the vanilla client's rendering of PistonMovingBlockEntity
//The chunk mesh is baked statically; a displacement like a piston advancing half a block each tick cannot be baked in, so this opens a dedicated per-frame rebuilt pass
//Each block is baked section-local with its own section origin, because the 20-byte format cannot carry world coordinates;
//Draw pushes that origin right before the block's draws, reusing the terrain pipelines and their push constant range
public sealed class MovingBlockRenderer : IDisposable
{
    private const int LayerCount = 3;
    private const int SectionMask = ~15;

    private readonly ClientLevel _level;
    private readonly ChunkMeshBuilder _builder;
    private readonly GpuBufferPool _bufferPool;
    private readonly List<MovingBlockMesh> _meshes = new();
    private bool _disposed;

    public MovingBlockRenderer(ClientLevel level, ChunkMeshBuilder builder, GpuBufferPool bufferPool)
    {
        _level = level;
        _builder = builder;
        _bufferPool = bufferPool;
    }

    //MovingBlockCount last frame's number of rendered moving blocks, for debugging
    public int MovingBlockCount { get; private set; }

    //LastDrawCallCount batches actually submitted last frame, one per layer per block
    public int LastDrawCallCount { get; private set; }

    //Prepare bakes each moving block to the section-local frame for the current tick
    //Progress is derived from ticks elapsed since receiving the state packet: half a block per tick, done in two ticks, consistent with vanilla PistonMovingBlockEntity.tick
    public void Prepare()
    {
        MovingBlockCount = 0;
        ReturnAllBuffers();
        _meshes.Clear();
        var pose = new PoseStack();
        foreach (var moving in _level.MovingBlocks)
        {
            var progress = moving.Progress;
            //Same conversion as vanilla getXOff/getYOff/getZOff: extending goes from -1 to 0, retracting from 0 to 1
            var extended = moving.Extending ? progress - 1f : 1f - progress;
            var originX = moving.Pos.X & SectionMask;
            var originY = moving.Pos.Y & SectionMask;
            var originZ = moving.Pos.Z & SectionMask;

            var buffers = new ChunkBuildBuffers();
            pose.PushPose();
            pose.Scale(1f / 16f, 1f / 16f, 1f / 16f);
            //Subtracting the origin keeps the vertex section-local, matching what the shader expects from the push constant
            pose.Translate(
                moving.Pos.X + moving.Direction.StepX * extended - originX,
                moving.Pos.Y + moving.Direction.StepY * extended - originY,
                moving.Pos.Z + moving.Direction.StepZ * extended - originZ);
            _builder.BuildBlock(moving.RenderState, moving.Pos, pose, buffers);
            pose.PopPose();

            if (buffers.TotalVertexCount == 0) continue;
            _meshes.Add(new MovingBlockMesh
            {
                Mesh = SectionMesh.FromChunkBuildBuffers(buffers),
                Origin = new Vector4(originX, originY, originZ, 0f)
            });
            MovingBlockCount++;
        }
    }

    //Upload uploads each block's baked vertices/indices per layer; empty layers do not borrow buffers to avoid an empty DrawCall
    public void Upload(GpuDevice device)
    {
        foreach (var block in _meshes)
        {
            if (block.Uploaded) continue;
            for (var i = 0; i < LayerCount; i++)
            {
                var layer = (RenderLayer)i;
                var vertices = block.Mesh.GetVertices(layer);
                var indices = block.Mesh.GetIndices(layer);
                block.IndexCounts[i] = indices.Length;
                if (indices.Length == 0) continue;
                var vertexBuffer = _bufferPool.GetBuffer(vertices.Length, GpuBuffer.UsageVertex | GpuBuffer.UsageCopyDst);
                vertexBuffer.Upload(vertices.ToArray());
                var indexBuffer = _bufferPool.GetBuffer(indices.Length * sizeof(int), GpuBuffer.UsageIndex | GpuBuffer.UsageCopyDst);
                indexBuffer.Upload(indices.ToArray());
                block.VertexBuffers[i] = vertexBuffer;
                block.IndexBuffers[i] = indexBuffer;
            }
            block.Uploaded = true;
        }
    }

    //Draw submits in Solid→Cutout→Translucent order, skipping empty layers and pushing each block's section origin
    public void Draw(RenderPass pass, Action<RenderPass> bindGlobals)
    {
        LastDrawCallCount = 0;
        foreach (var block in _meshes)
        {
            for (var i = 0; i < LayerCount; i++)
            {
                if (block.IndexCounts[i] == 0) continue;
                if (block.VertexBuffers[i] is not { } vertexBuffer || block.IndexBuffers[i] is not { } indexBuffer) continue;
                var pipeline = (RenderLayer)i switch
                {
                    RenderLayer.Solid => WorldRenderPipelines.SOLID_TERRAIN,
                    RenderLayer.Cutout => WorldRenderPipelines.CUTOUT_TERRAIN,
                    _ => WorldRenderPipelines.TRANSLUCENT_TERRAIN
                };
                pass.SetPipeline(pipeline);
                bindGlobals(pass);
                pass.DisableScissor();
                pass.SetPushConstant(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref block.Origin, 1)));
                pass.SetVertexBuffer(0, vertexBuffer.Slice());
                pass.SetIndexBuffer(indexBuffer, NacreCraft.Client.Blaze3d.IndexType.Int);
                pass.DrawIndexed(block.IndexCounts[i], 1, 0, 0, 0);
                LastDrawCallCount++;
            }
        }
    }

    //ReturnAllBuffers returns every block's GPU buffers; called on rebuild and on dispose
    private void ReturnAllBuffers()
    {
        foreach (var block in _meshes) block.ReturnBuffers(_bufferPool);
    }

    public void Dispose()
    {
        if (_disposed) return;
        ReturnAllBuffers();
        _meshes.Clear();
        _disposed = true;
    }

    //MovingBlockMesh one baked block: its mesh, the section origin pushed before drawing and the uploaded buffers
    private sealed class MovingBlockMesh
    {
        public required SectionMesh Mesh { get; init; }
        //Origin kept as a field so the draw path can take a ref for the push constant
        public Vector4 Origin;
        public GpuBuffer?[] VertexBuffers { get; } = new GpuBuffer?[LayerCount];
        public GpuBuffer?[] IndexBuffers { get; } = new GpuBuffer?[LayerCount];
        public int[] IndexCounts { get; } = new int[LayerCount];
        public bool Uploaded { get; set; }

        public void ReturnBuffers(GpuBufferPool pool)
        {
            for (var i = 0; i < LayerCount; i++)
            {
                if (VertexBuffers[i] is { } vertexBuffer)
                {
                    pool.ReturnBuffer(vertexBuffer);
                    VertexBuffers[i] = null;
                }
                if (IndexBuffers[i] is { } indexBuffer)
                {
                    pool.ReturnBuffer(indexBuffer);
                    IndexBuffers[i] = null;
                }
                IndexCounts[i] = 0;
            }
            Uploaded = false;
        }
    }
}
