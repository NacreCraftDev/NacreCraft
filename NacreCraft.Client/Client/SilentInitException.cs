namespace NacreCraft.Client;

//SilentInitException aborts client initialization without printing the usual crash banner, maps to vanilla net.minecraft.client.main.SilentInitException
public sealed class SilentInitException : Exception
{
    public SilentInitException(string message) : base(message)
    {
    }

    public SilentInitException(string message, Exception cause) : base(message, cause)
    {
    }
}
