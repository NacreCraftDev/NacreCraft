namespace NacreCraft.Client.Blaze3d.Shaders;

//UniformType uniform binding kind, maps to vanilla com.mojang.blaze3d.shaders.UniformType
//Only a uniform buffer or a texel buffer, the concrete layout comes from the shader
public enum UniformType
{
    UniformBuffer,
    TexelBuffer
}
