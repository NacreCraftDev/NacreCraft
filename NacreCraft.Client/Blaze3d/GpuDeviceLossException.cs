namespace NacreCraft.Client.Blaze3d;

//GpuDeviceLossException the GPU device was lost and its resources went with it
//maps to vanilla com.mojang.blaze3d.GpuDeviceLossException
public sealed class GpuDeviceLossException : Exception
{
    public GpuDeviceLossException(string message) : base(message) { }
}
