namespace NacreCraft.Client.Blaze3d.Resource;

//ResourceHandle a deferred reference to a resource the frame graph owns
//maps to vanilla com.mojang.blaze3d.resource.ResourceHandle
public interface IResourceHandle<out T>
{
    T Get();
}

//ResourceHandle factory for the shared handles
public static class ResourceHandle
{
    //Invalid a handle with no underlying resource, dereferencing it is a programming error
    public static IResourceHandle<T> Invalid<T>() => InvalidResourceHandle<T>.Instance;

    private sealed class InvalidResourceHandle<T> : IResourceHandle<T>
    {
        public static readonly InvalidResourceHandle<T> Instance = new();

        public T Get() => throw new InvalidOperationException("Cannot dereference handle with no underlying resource");
    }
}
