namespace NacreCraft.Client.Blaze3d.Resource;

//ResourceDescriptor a blueprint that allocates a GPU resource and frees it again
//maps to vanilla com.mojang.blaze3d.resource.ResourceDescriptor
//A pool decides whether a cached resource can back a descriptor through CanUsePhysicalResource
public interface IResourceDescriptor
{
    //CanUsePhysicalResource whether an already allocated resource held for another descriptor fits this one
    //Vanilla defaults to equality, which a record descriptor gets for free
    bool CanUsePhysicalResource(IResourceDescriptor other) => Equals(other);
}

//ResourceDescriptor<T> the typed form, T is the resource being described
public interface IResourceDescriptor<T> : IResourceDescriptor
{
    //Allocate builds the resource from scratch
    T Allocate();

    //Prepare brings a reused resource into the state this descriptor expects, such as clearing a render target
    void Prepare(T resource);

    //Free releases the resource
    void Free(T resource);
}
