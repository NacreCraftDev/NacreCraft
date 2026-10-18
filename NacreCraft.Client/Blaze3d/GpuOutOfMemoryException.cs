namespace NacreCraft.Client.Blaze3d;

//GpuOutOfMemoryException the GPU refused an allocation
//maps to vanilla com.mojang.blaze3d.GpuOutOfMemoryException
public sealed class GpuOutOfMemoryException : Exception
{
    public GpuOutOfMemoryException(string message) : base(message) { }
}
