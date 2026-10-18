using NacreCraft.Client.Blaze3d.Resource;

namespace NacreCraft.Client.Blaze3d.Framegraph;

//FramePass one unit of work in a frame graph, maps to vanilla com.mojang.blaze3d.framegraph.FramePass
//A pass declares what it reads and writes, and the builder uses those declarations to order the passes
public interface IFramePass
{
    //CreatesInternal declares a resource this pass owns and produces
    IResourceHandle<T> CreatesInternal<T>(string name, IResourceDescriptor<T> descriptor);

    //Reads declares that this pass consumes the given resource
    void Reads<T>(IResourceHandle<T> handle);

    //ReadsAndWrites declares that this pass consumes and replaces the given resource, returning the new version
    IResourceHandle<T> ReadsAndWrites<T>(IResourceHandle<T> handle);

    //Requires orders this pass after another one even when no resource passes between them
    void Requires(IFramePass pass);

    //DisableCulling keeps this pass alive even when nothing outside the graph consumes its output
    void DisableCulling();

    //Executes sets the work this pass runs
    void Executes(System.Action task);
}

//IFrameGraphInspector observes a frame graph run, maps to vanilla FrameGraphBuilder.Inspector
//Every hook defaults to nothing so an implementation only overrides what it cares about
public interface IFrameGraphInspector
{
    void AcquireResource(string name)
    {
    }

    void ReleaseResource(string name)
    {
    }

    void BeforeExecutePass(string name)
    {
    }

    void AfterExecutePass(string name)
    {
    }
}

//FrameGraphInspectors factory for the shared inspectors
public static class FrameGraphInspectors
{
    //None the no-op inspector used when nobody is observing
    public static readonly IFrameGraphInspector None = new NoopInspector();

    private sealed class NoopInspector : IFrameGraphInspector
    {
    }
}
