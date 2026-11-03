namespace NacreCraft.Resources;

//ResourceManagerReloadListener a listener that only needs the finished resource manager
//maps to vanilla net.minecraft.server.packs.resources.ResourceManagerReloadListener
//Vanilla waits on the preparation barrier and then runs on the reload executor; NC has no split phases, so Reload forwards directly
public interface ResourceManagerReloadListener : PreparableReloadListener
{
    void OnResourceManagerReload(ResourceManager manager);

    void PreparableReloadListener.Reload(ResourceManager rm, ReloadContext ctx) => OnResourceManagerReload(rm);
}
