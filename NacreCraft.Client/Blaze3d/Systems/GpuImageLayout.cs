namespace NacreCraft.Client.Blaze3d.Systems;

//GpuImageLayout backend-agnostic image layout, a NacreCraft extension driving CommandEncoder.TransitionImageLayout
//The Vulkan backend maps these to ImageLayout values
public enum GpuImageLayout
{
    ColorAttachment,
    ShaderReadOnly,
    TransferDst,
    TransferSrc
}
