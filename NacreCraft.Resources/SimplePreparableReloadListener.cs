namespace NacreCraft.Resources;

//SimplePreparableReloadListener a listener split into a prepare step and an apply step
//maps to vanilla net.minecraft.server.packs.resources.SimplePreparableReloadListener
//Vanilla runs prepare on the worker pool and apply on the main thread; NC runs both back to back on the caller's thread
public abstract class SimplePreparableReloadListener<T> : PreparableReloadListener
{
    public void Reload(ResourceManager rm, ReloadContext ctx)
    {
        var preparations = Prepare(rm, ctx);
        Apply(preparations, rm, ctx);
    }

    //Prepare parses the resources and returns the data the apply step needs, maps to vanilla prepare
    protected abstract T Prepare(ResourceManager manager, ReloadContext ctx);

    //Apply installs the prepared data, maps to vanilla apply
    protected abstract void Apply(T preparations, ResourceManager manager, ReloadContext ctx);
}
