using NacreCraft.Client.Blaze3d.Resource;

namespace NacreCraft.Client.Blaze3d.Framegraph;

//FrameGraphBuilder declares the passes of one frame and runs them in dependency order
//maps to vanilla com.mojang.blaze3d.framegraph.FrameGraphBuilder
//A pass states what it reads and writes; the builder culls the passes nobody consumes, orders the rest
//topologically, and hands each internal resource to the allocator only for the span it is needed
public sealed class FrameGraphBuilder
{
    private readonly List<VirtualResource> _internalResources = new();
    private readonly List<VirtualResource> _externalResources = new();
    private readonly List<Pass> _passes = new();

    //AddPass registers a pass, passes run in the order their dependencies allow rather than the order they were added
    public IFramePass AddPass(string name)
    {
        var pass = new Pass(this, _passes.Count, name);
        _passes.Add(pass);
        return pass;
    }

    //ImportExternal hands a resource owned outside the graph to a pass, it is never allocated here
    public IResourceHandle<T> ImportExternal<T>(string name, T resource)
    {
        var holder = new ExternalResource<T>(name, resource);
        _externalResources.Add(holder);
        return holder.Handle;
    }

    //CreateInternal declares a resource the graph itself allocates, maps to vanilla createInternal
    public IResourceHandle<T> CreateInternal<T>(string name, IResourceDescriptor<T> descriptor)
        => CreateInternalResource(name, descriptor, null).Handle;

    //CreateInternalResource allocates an id for the resource so passes can reference it by index
    private InternalVirtualResource<T> CreateInternalResource<T>(string name, IResourceDescriptor<T> descriptor, Pass? createdBy)
    {
        var id = _internalResources.Count;
        var resource = new InternalVirtualResource<T>(id, name, createdBy, descriptor);
        _internalResources.Add(resource);
        return resource;
    }

    public void Execute(IGraphicsResourceAllocator allocator) => Execute(allocator, FrameGraphInspectors.None);

    public void Execute(IGraphicsResourceAllocator allocator, IFrameGraphInspector inspector)
    {
        var passesToKeep = IdentifyPassesToKeep();
        var passesInOrder = new List<Pass>(passesToKeep.Count);
        var visiting = new HashSet<int>();
        foreach (var pass in _passes)
        {
            ResolvePassOrder(pass, passesToKeep, visiting, passesInOrder);
        }
        AssignResourceLifetimes(passesInOrder);
        foreach (var pass in passesInOrder)
        {
            foreach (var resource in pass.ResourcesToAcquire)
            {
                inspector.AcquireResource(resource.Name);
                resource.Acquire(allocator);
            }
            inspector.BeforeExecutePass(pass.Name);
            pass.Task();
            inspector.AfterExecutePass(pass.Name);
            foreach (var id in pass.ResourcesToRelease)
            {
                var resource = (IInternalResourceLifecycle)_internalResources[id];
                inspector.ReleaseResource(resource.Name);
                resource.Release(allocator);
            }
        }
    }

    //IdentifyPassesToKeep culls passes nothing consumes, maps to vanilla identifyPassesToKeep
    //A pass stays when something outside the graph reads its output, or when it asked not to be culled
    private HashSet<int> IdentifyPassesToKeep()
    {
        var queue = new Queue<Pass>(_passes.Count);
        var passesToKeep = new HashSet<int>();
        foreach (var resource in _externalResources)
        {
            var pass = resource.HandleBase.CreatedBy;
            if (pass is null) continue;
            DiscoverAllRequiredPasses(pass, passesToKeep, queue);
        }
        foreach (var pass in _passes)
        {
            if (!pass.CullingDisabled) continue;
            DiscoverAllRequiredPasses(pass, passesToKeep, queue);
        }
        return passesToKeep;
    }

    //DiscoverAllRequiredPasses walks the requires graph outwards from a pass that has to run
    private void DiscoverAllRequiredPasses(Pass sourcePass, HashSet<int> visited, Queue<Pass> passesToTrace)
    {
        passesToTrace.Enqueue(sourcePass);
        while (passesToTrace.Count > 0)
        {
            var pass = passesToTrace.Dequeue();
            if (!visited.Add(pass.Id)) continue;
            foreach (var id in pass.RequiredPassIds)
            {
                passesToTrace.Enqueue(_passes[id]);
            }
        }
    }

    //ResolvePassOrder appends the pass after every pass it depends on, maps to vanilla resolvePassOrder
    //A pass still being visited when it comes up again means the graph has a cycle
    private void ResolvePassOrder(Pass pass, HashSet<int> passesToFind, HashSet<int> visiting, List<Pass> output)
    {
        if (visiting.Contains(pass.Id))
        {
            var involved = string.Join(", ", visiting.Select(id => _passes[id].Name));
            throw new InvalidOperationException($"Frame graph cycle detected between {involved}");
        }
        if (!passesToFind.Contains(pass.Id)) return;
        visiting.Add(pass.Id);
        passesToFind.Remove(pass.Id);
        foreach (var id in pass.RequiredPassIds)
        {
            ResolvePassOrder(_passes[id], passesToFind, visiting, output);
        }
        foreach (var handle in pass.WritesFrom)
        {
            foreach (var id in handle.ReadBy)
            {
                if (id == pass.Id) continue;
                ResolvePassOrder(_passes[id], passesToFind, visiting, output);
            }
        }
        output.Add(pass);
        visiting.Remove(pass.Id);
    }

    //AssignResourceLifetimes gives each internal resource one acquire point and one release point
    //maps to vanilla assignResourceLifetimes: a resource is acquired before the first pass that needs it
    //and released right after the last one, so the allocator can hand the memory back in between
    private void AssignResourceLifetimes(IReadOnlyCollection<Pass> passesInOrder)
    {
        var lastPassByResource = new Pass?[_internalResources.Count];
        foreach (var pass in passesInOrder)
        {
            foreach (var id in pass.RequiredResourceIds)
            {
                var resource = _internalResources[id];
                var lastPass = lastPassByResource[id];
                lastPassByResource[id] = pass;
                if (lastPass is null)
                {
                    pass.ResourcesToAcquire.Add((IInternalResourceLifecycle)resource);
                }
                else
                {
                    lastPass.ResourcesToRelease.Remove(id);
                }
                pass.ResourcesToRelease.Add(id);
            }
        }
    }

    //Pass one unit of work, maps to the vanilla nested Pass
    private sealed class Pass : IFramePass
    {
        private readonly FrameGraphBuilder _builder;

        public int Id { get; }

        public string Name { get; }

        public SortedSet<int> RequiredResourceIds { get; } = new();

        public SortedSet<int> RequiredPassIds { get; } = new();

        public List<HandleBase> WritesFrom { get; } = new();

        public List<IInternalResourceLifecycle> ResourcesToAcquire { get; } = new();

        public SortedSet<int> ResourcesToRelease { get; } = new();

        public System.Action Task { get; private set; } = () => { };

        public bool CullingDisabled { get; private set; }

        public Pass(FrameGraphBuilder builder, int id, string name)
        {
            _builder = builder;
            Id = id;
            Name = name;
        }

        public IResourceHandle<T> CreatesInternal<T>(string name, IResourceDescriptor<T> descriptor)
        {
            var resource = _builder.CreateInternalResource(name, descriptor, this);
            RequiredResourceIds.Add(resource.Id);
            return resource.Handle;
        }

        public void Reads<T>(IResourceHandle<T> handle) => ReadsCore((HandleBase)handle);

        public IResourceHandle<T> ReadsAndWrites<T>(IResourceHandle<T> handle)
        {
            var typed = (Handle<T>)handle;
            WritesFrom.Add(typed);
            ReadsCore(typed);
            return typed.WriteAndAlias(this);
        }

        public void Requires(IFramePass pass) => RequiredPassIds.Add(((Pass)pass).Id);

        public void DisableCulling() => CullingDisabled = true;

        public void Executes(System.Action task) => Task = task;

        //ReadsCore records the dependency and the resource, then notes this pass reads the handle
        private void ReadsCore(HandleBase handle)
        {
            if (handle.ResourceId >= 0) RequiredResourceIds.Add(handle.ResourceId);
            if (handle.CreatedBy is not null) RequiredPassIds.Add(handle.CreatedBy.Id);
            handle.ReadBy.Add(Id);
        }

        public override string ToString() => Name;
    }

    //IResourceHolder<T> the minimum a handle needs from whatever it points at
    private interface IResourceHolder<T>
    {
        Handle<T> Handle { get; set; }

        T Get();
    }

    //IInternalResourceLifecycle lets a pass drive the allocator without knowing the resource type
    private interface IInternalResourceLifecycle
    {
        string Name { get; }

        void Acquire(IGraphicsResourceAllocator allocator);

        void Release(IGraphicsResourceAllocator allocator);
    }

    //VirtualResource a resource the graph knows about, internal ones are allocated and external ones are handed in
    private abstract class VirtualResource
    {
        public string Name { get; }

        public abstract HandleBase HandleBase { get; }

        protected VirtualResource(string name) => Name = name;

        public override string ToString() => Name;
    }

    //InternalVirtualResource a resource the graph allocates through the allocator for the span it is needed
    private sealed class InternalVirtualResource<T> : VirtualResource, IResourceHolder<T>, IInternalResourceLifecycle
    {
        private readonly IResourceDescriptor<T> _descriptor;
        private T? _physicalResource;

        public int Id { get; }

        public Handle<T> Handle { get; set; }

        public override HandleBase HandleBase => Handle;

        public InternalVirtualResource(int id, string name, Pass? createdBy, IResourceDescriptor<T> descriptor) : base(name)
        {
            Id = id;
            _descriptor = descriptor;
            //Every resource starts at version 0; a pass that writes it advances the handle
            Handle = new Handle<T>(this, 0, createdBy);
        }

        public T Get() => _physicalResource
                          ?? throw new InvalidOperationException($"Resource {Name} is not currently available");

        public void Acquire(IGraphicsResourceAllocator allocator)
        {
            if (_physicalResource is not null)
                throw new InvalidOperationException("Tried to acquire physical resource, but it was already assigned");
            _physicalResource = allocator.Acquire(_descriptor);
        }

        public void Release(IGraphicsResourceAllocator allocator)
        {
            if (_physicalResource is null)
                throw new InvalidOperationException("Tried to release physical resource that was not allocated");
            allocator.Release(_descriptor, _physicalResource);
            _physicalResource = default;
        }
    }

    //ExternalResource a resource owned outside the graph, never allocated or freed here
    private sealed class ExternalResource<T> : VirtualResource, IResourceHolder<T>
    {
        private readonly T _resource;

        public Handle<T> Handle { get; set; }

        public override HandleBase HandleBase => Handle;

        public ExternalResource(string name, T resource) : base(name)
        {
            _resource = resource;
            Handle = new Handle<T>(this, 0, null);
        }

        public T Get() => _resource;
    }

    //HandleBase the parts of a handle the graph tracks regardless of the resource type
    private abstract class HandleBase
    {
        public Pass? CreatedBy { get; }

        public SortedSet<int> ReadBy { get; } = new();

        //ResourceId the internal resource index, or -1 when the handle points at an external resource
        public abstract int ResourceId { get; }

        protected HandleBase(Pass? createdBy) => CreatedBy = createdBy;
    }

    //Handle<T> a versioned reference to a resource, maps to the vanilla nested Handle
    //Writing a resource retires the current handle and aliases the next one onto the same holder
    private sealed class Handle<T> : HandleBase, IResourceHandle<T>
    {
        private readonly IResourceHolder<T> _holder;

        public int Version { get; }

        public Handle<T>? AliasedBy { get; private set; }

        public override int ResourceId => _holder is InternalVirtualResource<T> internalResource ? internalResource.Id : -1;

        public Handle(IResourceHolder<T> holder, int version, Pass? createdBy) : base(createdBy)
        {
            _holder = holder;
            Version = version;
        }

        public T Get() => _holder.Get();

        //WriteAndAlias moves the holder onto a fresh handle, maps to vanilla writeAndAlias
        public Handle<T> WriteAndAlias(Pass pass)
        {
            if (!ReferenceEquals(_holder.Handle, this))
                throw new InvalidOperationException($"Handle {this} is no longer valid, as its contents were moved into {AliasedBy}");
            var next = new Handle<T>(_holder, Version + 1, pass);
            _holder.Handle = next;
            AliasedBy = next;
            return next;
        }

        public override string ToString()
            => CreatedBy is null ? $"{_holder}#{Version}" : $"{_holder}#{Version} (from {CreatedBy})";
    }
}
