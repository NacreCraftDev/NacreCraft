using System.Numerics;
using NacreCraft.Client.Blaze3d.Buffers;

namespace NacreCraft.Client.Render;

//DynamicUniforms owns the per-frame transform and chunk-section uniform rings, maps to vanilla net.minecraft.client.renderer.DynamicUniforms
//Every render type writes a DynamicTransforms block here before drawing; the render pass binds the returned slice by offset
public sealed class DynamicUniforms : IDisposable
{
    private static readonly Vector4 White = Vector4.One;
    private static readonly Vector3 NoOffset = Vector3.Zero;
    private static readonly Matrix4x4 IdentityTextureTransform = Matrix4x4.Identity;

    public const int TRANSFORM_UBO_SIZE = 64 + 16 + 16 + 64;
    public const int CHUNK_SECTION_UBO_SIZE = 64 + 4 + 8 + 16;

    private const int InitialCapacity = 2;

    private readonly DynamicUniformStorage<Transform> _transforms = new("Dynamic Transforms UBO", TRANSFORM_UBO_SIZE, InitialCapacity);
    private readonly DynamicUniformStorage<ChunkSectionInfo> _chunkSections = new("Chunk Sections UBO", CHUNK_SECTION_UBO_SIZE, InitialCapacity);
    private bool _disposed;

    //Reset starts a new frame, maps to vanilla reset
    public void Reset()
    {
        _transforms.EndFrame();
        _chunkSections.EndFrame();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transforms.Dispose();
        _chunkSections.Dispose();
        _disposed = true;
    }

    public GpuBufferSlice WriteTransform(Matrix4x4 modelView)
        => WriteTransform(new Transform(modelView, White, NoOffset, IdentityTextureTransform));

    public GpuBufferSlice WriteTransform(Matrix4x4 modelView, Vector4 colorModulator)
        => WriteTransform(new Transform(modelView, colorModulator, NoOffset, IdentityTextureTransform));

    public GpuBufferSlice WriteTransform(Matrix4x4 modelView, Matrix4x4 textureMatrix)
        => WriteTransform(new Transform(modelView, White, NoOffset, textureMatrix));

    public GpuBufferSlice WriteTransform(Matrix4x4 modelView, Vector4 colorModulator, Vector3 modelOffset, Matrix4x4 textureMatrix)
        => WriteTransform(new Transform(modelView, colorModulator, modelOffset, textureMatrix));

    public GpuBufferSlice WriteTransform(Transform uniform) => _transforms.WriteUniform(uniform);

    public GpuBufferSlice[] WriteTransforms(params Transform[] transforms) => _transforms.WriteUniforms(transforms);

    public GpuBufferSlice[] WriteChunkSections(params ChunkSectionInfo[] infos) => _chunkSections.WriteUniforms(infos);

    //Transform one DynamicTransforms block, maps to vanilla DynamicUniforms.Transform
    public sealed record Transform(Matrix4x4 ModelView, Vector4 ColorModulator, Vector3 ModelOffset, Matrix4x4 TextureMatrix)
        : DynamicUniformStorage<Transform>.IDynamicUniform
    {
        public void Write(byte[] buffer)
            => Std140Builder.IntoBuffer(buffer)
                .PutMat4f(ModelView)
                .PutVec4(ColorModulator)
                .PutVec3(ModelOffset)
                .PutMat4f(TextureMatrix);
    }

    //ChunkSectionInfo one chunk-section uniform block, maps to vanilla DynamicUniforms.ChunkSectionInfo
    public sealed record ChunkSectionInfo(Matrix4x4 ModelView, int X, int Y, int Z, float Visibility, int TextureAtlasWidth, int TextureAtlasHeight)
        : DynamicUniformStorage<ChunkSectionInfo>.IDynamicUniform
    {
        public void Write(byte[] buffer)
            => Std140Builder.IntoBuffer(buffer)
                .PutMat4f(ModelView)
                .PutFloat(Visibility)
                .PutIVec2(TextureAtlasWidth, TextureAtlasHeight)
                .PutIVec3(X, Y, Z);
    }
}
