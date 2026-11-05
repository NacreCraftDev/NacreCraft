namespace NacreCraft.Storage;

//LevelStorageException maps to vanilla net.minecraft.world.level.storage.LevelStorageException
//Thrown when the level folder cannot be listed or opened
//Degraded: the vanilla exception carries a chat Component, which lives in the Network layer that Storage cannot reference,
//so the plain message text is kept and exposed through GetMessageComponent
public class LevelStorageException : Exception
{
    private readonly string _messageComponent;

    public LevelStorageException(string message)
        : base(message)
    {
        _messageComponent = message;
    }

    public LevelStorageException(string message, Exception inner)
        : base(message, inner)
    {
        _messageComponent = message;
    }

    public string GetMessageComponent() => _messageComponent;
}
