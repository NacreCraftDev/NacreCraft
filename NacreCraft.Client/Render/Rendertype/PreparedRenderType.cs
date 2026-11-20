using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Render.Rendertype;

//PreparedRenderType a render type's resolved GPU state for one draw, maps to vanilla net.minecraft.client.renderer.rendertype.PreparedRenderType
//Holds the pipeline, output target, dynamic transform slice and bound textures; drawing opens an immediate render pass
public sealed record PreparedRenderType(
    RenderPipeline Pipeline,
    OutputTarget OutputTarget,
    GpuBufferSlice DynamicTransforms,
    ScissorState ScissorState,
    IReadOnlyList<PreparedRenderType.Texture> Textures)
{
    public void DrawFromBuffer(ExecuteInfo info)
        => DrawFromBuffer(info.VertexBuffer, info.IndexBuffer, info.IndexType, info.BaseVertex, info.FirstIndex, info.IndexCount, info.VertexCount);

    //Draw records into an already open pass, used when the caller owns the target pass (the NC world pass)
    //DrawFromBuffer instead opens its own pass aimed at the RenderType's OutputTarget
    public void Draw(RenderPass renderPass, ExecuteInfo info)
    {
        renderPass.SetPipeline(Pipeline);
        if (ScissorState.Enabled)
            renderPass.EnableScissor(ScissorState.X, ScissorState.Y, ScissorState.Width, ScissorState.Height);
        RenderSystem.BindDefaultUniforms(renderPass);
        renderPass.SetUniform("DynamicTransforms", DynamicTransforms);
        renderPass.SetVertexBuffer(0, info.VertexBuffer.Slice());
        foreach (var texture in Textures)
            renderPass.BindTexture(texture.Name, texture.TextureView, texture.Sampler);

        if (info.IndexBuffer is not null)
        {
            renderPass.SetIndexBuffer(info.IndexBuffer, info.IndexType);
            renderPass.DrawIndexed(info.IndexCount, 1, info.FirstIndex, info.BaseVertex, 0);
        }
        else
        {
            renderPass.Draw(info.VertexCount, 1, info.BaseVertex, 0);
        }
    }

    public void DrawFromBuffer(GpuBuffer vertexBuffer, GpuBuffer? indexBuffer, IndexType indexType, int baseVertex,
        int firstIndex, int indexCount, int vertexCount)
    {
        var renderTarget = OutputTarget.GetRenderTarget();
        var colorTexture = RenderSystem.OutputColorTextureOverride
                           ?? renderTarget.ColorTextureView
                           ?? throw new InvalidOperationException($"Render target {renderTarget.Label} has no color attachment");
        var depthTexture = renderTarget.UseDepth
            ? RenderSystem.OutputDepthTextureOverride ?? renderTarget.DepthTextureView
            : null;

        using var renderPass = RenderSystem.GetDevice().CreateCommandEncoder()
            .CreateRenderPass(() => $"Immediate draw with {Pipeline}", colorTexture, null, depthTexture, null);
        renderPass.SetPipeline(Pipeline);
        if (ScissorState.Enabled)
            renderPass.EnableScissor(ScissorState.X, ScissorState.Y, ScissorState.Width, ScissorState.Height);
        RenderSystem.BindDefaultUniforms(renderPass);
        renderPass.SetUniform("DynamicTransforms", DynamicTransforms);
        renderPass.SetVertexBuffer(0, vertexBuffer.Slice());
        foreach (var texture in Textures)
            renderPass.BindTexture(texture.Name, texture.TextureView, texture.Sampler);

        if (indexBuffer is not null)
        {
            renderPass.SetIndexBuffer(indexBuffer, indexType);
            renderPass.DrawIndexed(indexCount, 1, firstIndex, baseVertex, 0);
        }
        else
        {
            renderPass.Draw(vertexCount, 1, baseVertex, 0);
        }
    }

    //Texture one texture binding, maps to vanilla PreparedRenderType.Texture
    public sealed record Texture(string Name, GpuTextureView TextureView, GpuSampler Sampler);
}
