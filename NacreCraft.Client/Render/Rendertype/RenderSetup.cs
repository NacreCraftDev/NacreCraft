using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Rendertype;

//RenderSetup immutable render-type configuration, maps to vanilla net.minecraft.client.renderer.rendertype.RenderSetup
//Holds the pipeline plus the textures, lightmap/overlay flags and output target that decide how a draw is bound
public sealed class RenderSetup
{
    internal readonly RenderPipeline Pipeline;
    internal readonly IReadOnlyDictionary<string, TextureBinding> Textures;
    internal readonly TextureTransform TextureTransform;
    internal readonly OutputTarget OutputTarget;
    internal readonly OutlineProperty Outline;
    internal readonly bool UseLightmap;
    internal readonly bool UseOverlay;
    internal readonly bool AffectsCrumbling;
    internal readonly bool SortOnUpload;
    internal readonly LayeringTransform LayeringTransform;

    private RenderSetup(RenderPipeline pipeline, IReadOnlyDictionary<string, TextureBinding> textures, bool useLightmap,
        bool useOverlay, LayeringTransform layeringTransform, OutputTarget outputTarget, TextureTransform textureTransform,
        OutlineProperty outline, bool affectsCrumbling, bool sortOnUpload)
    {
        Pipeline = pipeline;
        Textures = textures;
        OutputTarget = outputTarget;
        TextureTransform = textureTransform;
        UseLightmap = useLightmap;
        UseOverlay = useOverlay;
        Outline = outline;
        LayeringTransform = layeringTransform;
        AffectsCrumbling = affectsCrumbling;
        SortOnUpload = sortOnUpload;
    }

    public override string ToString()
        => $"RenderSetup[layeringTransform={LayeringTransform}, textureTransform={TextureTransform}, textures={Textures.Count}, " +
           $"outlineProperty={Outline}, useLightmap={UseLightmap}, useOverlay={UseOverlay}]";

    public static RenderSetupBuilder Builder(RenderPipeline pipeline) => new(pipeline);

    //PrepareTextures resolves every configured texture plus the optional overlay/lightmap samplers, maps to vanilla prepareTextures
    public List<PreparedRenderType.Texture> PrepareTextures(TextureManager textureManager, SamplerCache samplerCache,
        GpuTextureView? overlayTexture, GpuTextureView? lightmapTexture)
    {
        if (Textures.Count == 0 && !UseOverlay && !UseLightmap)
            return new List<PreparedRenderType.Texture>();

        var textures = new List<PreparedRenderType.Texture>(Textures.Count + 2);
        if (UseOverlay)
        {
            if (overlayTexture is null)
                throw new InvalidOperationException("Render setup uses the overlay texture but none was supplied");
            textures.Add(new PreparedRenderType.Texture("Sampler1", overlayTexture, samplerCache.GetClampToEdge(FilterMode.Linear)));
        }

        if (UseLightmap)
        {
            if (lightmapTexture is null)
                throw new InvalidOperationException("Render setup uses the lightmap but none was supplied");
            textures.Add(new PreparedRenderType.Texture("Sampler2", lightmapTexture, samplerCache.GetClampToEdge(FilterMode.Linear)));
        }

        foreach (var (name, binding) in Textures)
        {
            var texture = textureManager.GetTexture(binding.Location);
            var samplerOverride = binding.Sampler();
            textures.Add(new PreparedRenderType.Texture(name, texture.GetTextureView(),
                samplerOverride ?? texture.GetSampler()));
        }

        return textures;
    }

    //OutlineProperty how a render type participates in the entity outline pass, maps to vanilla RenderSetup.OutlineProperty
    public enum OutlineProperty
    {
        None,
        IsOutline,
        AffectsOutline
    }

    //TextureBinding one configured texture plus an optional sampler override supplier
    internal sealed record TextureBinding(Identifier Location, Func<GpuSampler?> Sampler);

    //RenderSetupBuilder fluent builder, maps to vanilla RenderSetup.RenderSetupBuilder
    public sealed class RenderSetupBuilder
    {
        private readonly RenderPipeline _pipeline;
        private readonly Dictionary<string, TextureBinding> _textures = new();
        private bool _useLightmap;
        private bool _useOverlay;
        private LayeringTransform _layeringTransform = LayeringTransform.NO_LAYERING;
        private OutputTarget _outputTarget = OutputTarget.MAIN_TARGET;
        private TextureTransform _textureTransform = TextureTransform.DEFAULT_TEXTURING;
        private bool _affectsCrumbling;
        private bool _sortOnUpload;
        private OutlineProperty _outlineProperty = OutlineProperty.None;

        internal RenderSetupBuilder(RenderPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public RenderSetupBuilder WithTexture(string name, Identifier texture)
        {
            _textures[name] = new TextureBinding(texture, static () => null);
            return this;
        }

        public RenderSetupBuilder WithTexture(string name, Identifier texture, Func<GpuSampler?>? sampler)
        {
            _textures[name] = new TextureBinding(texture, Memoize(sampler));
            return this;
        }

        public RenderSetupBuilder UseLightmap()
        {
            _useLightmap = true;
            return this;
        }

        public RenderSetupBuilder UseOverlay()
        {
            _useOverlay = true;
            return this;
        }

        public RenderSetupBuilder AffectsCrumbling()
        {
            _affectsCrumbling = true;
            return this;
        }

        public RenderSetupBuilder SortOnUpload()
        {
            _sortOnUpload = true;
            return this;
        }

        public RenderSetupBuilder SetLayeringTransform(LayeringTransform layeringTransform)
        {
            _layeringTransform = layeringTransform;
            return this;
        }

        public RenderSetupBuilder SetOutputTarget(OutputTarget outputTarget)
        {
            _outputTarget = outputTarget;
            return this;
        }

        public RenderSetupBuilder SetTextureTransform(TextureTransform textureTransform)
        {
            _textureTransform = textureTransform;
            return this;
        }

        public RenderSetupBuilder SetOutline(OutlineProperty outlineProperty)
        {
            _outlineProperty = outlineProperty;
            return this;
        }

        public RenderSetup CreateRenderSetup()
            => new(_pipeline, _textures, _useLightmap, _useOverlay, _layeringTransform, _outputTarget, _textureTransform,
                _outlineProperty, _affectsCrumbling, _sortOnUpload);

        private static Func<GpuSampler?> Memoize(Func<GpuSampler?>? supplier)
        {
            if (supplier is null) return static () => null;
            GpuSampler? cached = null;
            var resolved = false;
            return () =>
            {
                if (resolved) return cached;
                cached = supplier();
                resolved = true;
                return cached;
            };
        }
    }
}
