namespace NacreCraft.Network.Chat.Contents;

//TranslatableFormatException thrown when a translation template cannot be expanded
//maps to vanilla net.minecraft.network.chat.contents.TranslatableFormatException
public sealed class TranslatableFormatException : ArgumentException
{
    public TranslatableFormatException(TranslatableContents component, string message)
        : base($"Error parsing: {component}: {message}")
    {
    }

    public TranslatableFormatException(TranslatableContents component, int index)
        : base($"Invalid index {index} requested for {component}")
    {
    }

    public TranslatableFormatException(TranslatableContents component, Exception cause)
        : base($"Error while parsing: {component}", cause)
    {
    }
}
