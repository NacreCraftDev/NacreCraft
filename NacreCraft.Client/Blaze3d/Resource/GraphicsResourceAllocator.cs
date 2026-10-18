namespace NacreCraft.Client.Blaze3d.Resource;

//GraphicsResourceAllocator hands out resources described by a descriptor and takes them back
//maps to vanilla com.mojang.blaze3d.resource.GraphicsResourceAllocator
public interface IGraphicsResourceAllocator
{
    T Acquire<T>(IResourceDescriptor<T> descriptor);

    void Release<T>(IResourceDescriptor<T> descriptor, T resource);
}

//GraphicsResourceAllocator factory for the shared implementations
public static class GraphicsResourceAllocator
{
    //Unpooled allocates and frees on every acquire, maps to the vanilla UNPOOLED reference allocator
    public static readonly IGraphicsResourceAllocator Unpooled = new UnpooledAllocator();

    private sealed class UnpooledAllocator : IGraphicsResourceAllocator
    {
        public T Acquire<T>(IResourceDescriptor<T> descriptor)
        {
            var resource = descriptor.Allocate();
            descriptor.Prepare(resource);
            return resource;
        }

        public void Release<T>(IResourceDescriptor<T> descriptor, T resource) => descriptor.Free(resource);
    }
}
