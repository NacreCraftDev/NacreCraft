using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
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
using NacreCraft.Logging;

namespace NacreCraft.Client.Gui.Render;

//GuiRenderer render phase main entry, maps to vanilla GuiRenderer
//The submission phase groups and batches by pipeline+texture+scissor in Prepare
//The render phase submits to the GPU in Draw, merging same-group elements into 1 DrawCall
//Stage 4.4a implements the Prepare batching logic, unit-testable; 4.4b implements the Upload/Draw submission
public sealed class GuiRenderer : IDisposable
{
    private readonly StagedVertexBuffer _vertexBuffer;
    private readonly List<Mesh> _meshes = new();
    //_pipRenderers PIP renderer registry dispatched by RenderStateClass, maps to vanilla pictureInPictureRenderers
    //Filled externally by RegisterPipRenderer; Prepare walks pipStates and calls the matching renderer.Prepare
    private readonly Dictionary<Type, IPictureInPictureRenderer> _pipRenderers = new();
    private bool _disposed;
    //_diagFrame counts submitted draws for the throttled draw logging
    private int _diagFrame;
    //_firstMeshIndexAfterBlur the BeforeBlur segment's mesh count; the AfterBlur segment starts here
    //-1 means no blur split, Prepare not called or the snapshot has no BlurBeforeThisStratum
    private int _firstMeshIndexAfterBlur = -1;
    //DrawCallCount drawIndexed/draw count submitted by DrawRange in the last frame, for perf acceptance
    //MeshCount batched mesh count VertexCount batched total vertex count
    public int DrawCallCount { get; private set; }
    public int MeshCount => _meshes.Count;
    public int VertexCount { get; private set; }

    public GuiRenderer()
    {
        _vertexBuffer = new StagedVertexBuffer();
    }

    //Meshes the batched mesh list for Draw to iterate and submit
    public IReadOnlyList<Mesh> Meshes => _meshes;

    //FirstMeshIndexAfterBlur the BeforeBlur/AfterBlur split point, read after Prepare
    //-1 no blur split 0 means BeforeBlur is empty >0 means BeforeBlur has meshes and AfterBlur starts here
    public int FirstMeshIndexAfterBlur => _firstMeshIndexAfterBlur;

    //RegisterPipRenderer registers a PIP renderer dispatched by RenderStateClass, maps to vanilla pictureInPictureRenderers.put
    //Prepare walks pipStates, looks up by state type and calls renderer.Prepare for offscreen rendering+blit
    public void RegisterPipRenderer<T>(PictureInPictureRenderer<T> renderer) where T : PictureInPictureRenderState
    {
        _pipRenderers[renderer.RenderStateClass] = renderer;
    }

    //PreparePip calls PIP renderer.Prepare on the Render thread for offscreen rendering+blit, adding the BlitRenderState to the snapshot
    //Must be called before Prepare so the BlitRenderState enters the snapshot for batching
    //P15 moves the call to OnRecordCommandBuffer; vkQueueSubmit must be serial on the Render thread to avoid queue contention
    public void PreparePip(GuiRenderState snapshot, int guiScale)
    {
        if (guiScale <= 0 || _pipRenderers.Count == 0) return;
        snapshot.ForEachPictureInPicture(pip =>
        {
            if (_pipRenderers.TryGetValue(pip.GetType(), out var renderer))
                renderer.Prepare(pip, snapshot, guiScale);
        });
    }

    //Prepare the render phase groups and batches by pipeline+texture+scissor
    //Same-group elements are written into one Draw and submitted in a single drawIndexed
    //Repeated calls are safe: EndFrame resets the previous frame then re-batches
    //snapshot is passed by the caller, built and deep-copied on Tick; the Render thread only reads it
    //For a blur split it batches BeforeBlur elements then AfterBlur elements, with FirstMeshIndexAfterBlur marking the split
    //Without BlurBeforeThisStratum, BeforeBlur iterates everything and AfterBlur is empty, equivalent to no split
    //PIP prepare is already done earlier by OnRecordCommandBuffer calling PreparePip, so Prepare only batches and no longer calls PIP Submit
    public void Prepare(GuiRenderState snapshot, int guiScale = 0)
    {
        _meshes.Clear();
        _firstMeshIndexAfterBlur = -1;
        _vertexBuffer.EndFrame();

        //The element order is logged periodically: batch order follows first appearance, so a stray background
        //drawn late is only visible here
        var diagOrder = 0;
        var diagLog = Log.DebugEnabled && _prepareCount++ % 120 == 0;
        //The glyph path derives its pen position independently of the widget layout, so text placed off screen is
        //only visible as the accumulated text bounds
        var textCount = 0;
        var textMinX = float.MaxValue;
        var textMinY = float.MaxValue;
        var textMaxX = float.MinValue;
        var textMaxY = float.MinValue;
        Action<GuiElementRenderState> append = element =>
        {
            if (diagLog && diagOrder < 6)
                Log.Debug($"gui element {diagOrder} pipeline={element.Pipeline.Location} bounds={element.Bounds.X},{element.Bounds.Y},{element.Bounds.Width}x{element.Bounds.Height}");
            if (element.Pipeline.Location.EndsWith("/text"))
            {
                var b = element.Bounds;
                textCount++;
                textMinX = MathF.Min(textMinX, b.X);
                textMinY = MathF.Min(textMinY, b.Y);
                textMaxX = MathF.Max(textMaxX, b.X + b.Width);
                textMaxY = MathF.Max(textMaxY, b.Y + b.Height);
            }
            diagOrder++;
            var mesh = FindMesh(element.Pipeline, element.TextureSetup, element.ScissorArea);
            if (mesh == null)
            {
                var format = element.Pipeline.VertexFormatPerBuffer.Count > 0
                    ? element.Pipeline.VertexFormatPerBuffer[0]
                    : null;
                if (format == null)
                    throw new InvalidOperationException($"pipeline {element.Pipeline.Location} lacks a VertexFormat, cannot AppendDraw");
                var draw = _vertexBuffer.AppendDraw(format, element.Pipeline.PrimitiveTopology);
                var builder = _vertexBuffer.GetVertexBuilder(draw);
                mesh = new Mesh(element.Pipeline, element.TextureSetup, element.ScissorArea, draw, builder);
                _meshes.Add(mesh);
            }
            element.BuildVertices(mesh.VertexBuilder);
        };
        snapshot.ForEachElement(append, TraverseRange.BeforeBlur);
        if (snapshot.HasBlurSplit)
            _firstMeshIndexAfterBlur = _meshes.Count;
        snapshot.ForEachElement(append, TraverseRange.AfterBlur);
        if (diagLog)
        {
            Log.Debug($"gui prepared elements={diagOrder} meshes={_meshes.Count}");
            if (textCount > 0)
                Log.Debug($"gui text elements={textCount} bounds={textMinX},{textMinY}..{textMaxX},{textMaxY}");
        }

        foreach (var mesh in _meshes)
            _vertexBuffer.EndDraw(mesh.Draw);
        DrawCallCount = 0;
        VertexCount = 0;
        foreach (var mesh in _meshes)
            VertexCount += mesh.Draw.VertexCount;
    }

    //Upload stitches vertices into the vertex buffer and indices into the index buffer, then uploads to the GPU
    //Must be called after Prepare and before Draw; the buffer is reused across frames, host-visible and rewritten each frame
    public void Upload(GpuDevice device)
    {
        _vertexBuffer.Upload(device);
        DumpFirstDrawOnce();
    }

    //DumpFirstDrawOnce prints the first frame's batch headers and leading vertex bytes, so a blank GUI can be told
    //apart from a broken vertex layout without a debugger
    private static bool s_dumpedFirstDraw;
    private int _prepareCount;
    private void DumpFirstDrawOnce()
    {
        if (s_dumpedFirstDraw || _meshes.Count == 0) return;
        s_dumpedFirstDraw = true;
        for (int i = 0; i < _meshes.Count && i < 6; i++)
        {
            var draw = _meshes[i].Draw;
            var bytes = _vertexBuffer.GetVertexBytes(draw);
            var head = bytes.Length > 128 ? bytes[..128] : bytes;
            Log.Debug($"gui first batch {i} pipeline={_meshes[i].Pipeline.Location} vertices={draw.VertexCount} stride={draw.VertexFormat.VertexSize} bytes={bytes.Length} head={Convert.ToHexString(head)}");
        }
    }

    //Draw render phase submits everything, equivalent to DrawRange(0, _meshes.Count)
    //With a blur split the caller uses DrawRange to submit BeforeBlur/AfterBlur separately with blur in between
    public void Draw(RenderPass pass, Action<RenderPass> bindGlobals, Func<TextureSetup, (GpuTextureView? View, GpuSampler? Sampler)> textureResolver)
        => DrawRange(pass, bindGlobals, textureResolver, 0, _meshes.Count);

    //DrawRange submits the meshes in _meshes[start..end) for segmented blur execution
    //The pipeline is bound declaratively, then bindGlobals writes the global uniforms for that pipeline layout
    //Each mesh's color texture is bound through the last BindGroupLayout's sampler name
    //Must be called after Upload with the render pass open; QUADS uses DrawIndexed with baseVertex encoded in the indices, others use non-indexed Draw
    public void DrawRange(RenderPass pass, Action<RenderPass> bindGlobals, Func<TextureSetup, (GpuTextureView? View, GpuSampler? Sampler)> textureResolver, int start, int end)
    {
        //A batch whose texture resolves to nothing samples an unwritten descriptor and draws invisible pixels, so
        //the resolution result is traced next to the batch summary
        var diagBind = Log.DebugEnabled && _diagFrame % 120 == 0;
        //The batch summary is logged for the whole range at once, so every batch of a blank frame shows up together
        if (Log.DebugEnabled && _diagFrame++ % 120 == 0)
        {
            for (int i = start; i < end; i++)
            {
                var batch = _meshes[i];
                var batchInfo = _vertexBuffer.GetExecuteInfo(batch.Draw);
                Log.Debug($"gui batch {i} pipeline={batch.Pipeline.Location} scissor={batch.ScissorArea.Width}x{batch.ScissorArea.Height} vertices={batch.Draw.VertexCount} indices={batchInfo.IndexCount} indexed={batchInfo.IndexBuffer != null}");
            }
        }
        for (int i = start; i < end; i++)
        {
            var mesh = _meshes[i];
            pass.SetPipeline(mesh.Pipeline);
            bindGlobals(pass);
            DrawCallCount++;

            //The color texture binds through the sampler name declared by the pipeline's last BindGroupLayout
            var bindGroup = mesh.Pipeline.BindGroupLayouts[^1];
            if (bindGroup.Samplers.Count > 0)
            {
                var (view, sampler) = textureResolver(mesh.TextureSetup);
                if (diagBind && i < 8)
                    Log.Debug($"gui batch {i} texture view={view is not null} sampler={sampler is not null} setup={mesh.TextureSetup?.GetHashCode() ?? 0}");
                if (view != null && sampler != null)
                    pass.BindTexture(bindGroup.Samplers[0], view, sampler);
            }

            //scissor switched dynamically; an empty rectangle disables clipping for full-screen rendering
            if (mesh.ScissorArea.IsEmpty)
                pass.DisableScissor();
            else
                pass.EnableScissor(mesh.ScissorArea.X, mesh.ScissorArea.Y, mesh.ScissorArea.Width, mesh.ScissorArea.Height);

            var info = _vertexBuffer.GetExecuteInfo(mesh.Draw);
            //The binding offset is the Draw's own byte range: draws are packed by their own stride, so one frame may
            //mix POSITION_COLOR and POSITION_TEX_COLOR draws
            var byteLength = (long)mesh.Draw.VertexCount * mesh.Draw.VertexFormat.VertexSize;
            pass.SetVertexBuffer(0, info.VertexBuffer.Slice(mesh.Draw.VertexStartByte, byteLength));
            if (info.IndexBuffer != null && info.IndexCount > 0)
            {
                //QUADS indices are relative to this Draw's first vertex, matching the binding offset above
                pass.SetIndexBuffer(info.IndexBuffer, NacreCraft.Client.Blaze3d.IndexType.Int);
                pass.DrawIndexed(info.IndexCount, 1, info.FirstIndex, 0, 0);
            }
            else
            {
                pass.Draw(mesh.Draw.VertexCount, 1, 0, 0);
            }
        }
    }

    //GetExecuteInfo returns the mesh's Draw execution info, passed to IRenderPass in the submission phase
    public ExecuteInfo GetExecuteInfo(Mesh mesh) => _vertexBuffer.GetExecuteInfo(mesh.Draw);

    //EndFrame resets the staging area while keeping GPU buffers for cross-frame reuse
    public void EndFrame() => _vertexBuffer.EndFrame();

    //FindMesh returns the mesh that may take the element, which is only the newest one
    //Reusing an earlier mesh would move the element's draw back to where that mesh was created, so a button
    //background drawn after a label would land before the label and cover it; vanilla issues one draw per element
    //and this keeps the same ordering while still merging runs of identical state
    private Mesh? FindMesh(RenderPipeline pipeline, TextureSetup texture, ScreenRectangle scissor)
    {
        if (_meshes.Count == 0) return null;
        var newest = _meshes[^1];
        return newest.Matches(pipeline, texture, scissor) ? newest : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _vertexBuffer.Dispose();
        _disposed = true;
    }
}

//Mesh render state group for one batch; elements with the same pipeline+texture+scissor are merged
//Draw records vertex/index metadata; ExecuteDraw submits with pipeline/texture/scissor
public sealed class Mesh
{
    public RenderPipeline Pipeline { get; }
    public TextureSetup TextureSetup { get; }
    public ScreenRectangle ScissorArea { get; }
    public Draw Draw { get; }
    public VertexConsumer VertexBuilder { get; }

    internal Mesh(RenderPipeline pipeline, TextureSetup textureSetup, ScreenRectangle scissorArea, Draw draw, VertexConsumer vertexBuilder)
    {
        Pipeline = pipeline;
        TextureSetup = textureSetup;
        ScissorArea = scissorArea;
        Draw = draw;
        VertexBuilder = vertexBuilder;
    }

    //Matches whether batching is possible: same pipeline (reference)+texture (texture reference)+scissor (value)
    public bool Matches(RenderPipeline pipeline, TextureSetup texture, ScreenRectangle scissor)
        => ReferenceEquals(Pipeline, pipeline)
            && Equals(TextureSetup, texture)
            && ScissorArea == scissor;
}
