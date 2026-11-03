namespace NacreCraft.Resources;

//CloseableResourceManager a resource manager that owns its packs, maps to vanilla net.minecraft.server.packs.resources.CloseableResourceManager
//Vanilla extends ResourceManager with AutoCloseable; NC's ResourceManager is the pack-type facade, so this interface extends ResourceProvider
public interface CloseableResourceManager : ResourceProvider
{
    //Close releases every pack the manager owns, maps to vanilla CloseableResourceManager.close
    void Close();
}
