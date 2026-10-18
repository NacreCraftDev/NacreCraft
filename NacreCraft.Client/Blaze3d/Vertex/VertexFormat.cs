using NacreCraft.Client.Blaze3d;

namespace NacreCraft.Client.Blaze3d.Vertex;

//VertexFormatElement one vertex attribute: its name, byte offset inside the vertex and its data format
//maps to vanilla com.mojang.blaze3d.vertex.VertexFormatElement
public readonly record struct VertexFormatElement(string Name, int Offset, GpuFormat Format);

//VertexFormat a vertex layout, maps to vanilla com.mojang.blaze3d.vertex.VertexFormat
//Attributes are looked up by name, matching the semantic names DefaultVertexFormat declares
//VertexSize accumulates the block size of every attribute; the backends bind attributes at these offsets
public sealed class VertexFormat
{
    //MaxVertexElements the attribute ceiling, maps to vanilla VertexFormat.MAX_VERTEX_ELEMENTS
    public const int MaxVertexElements = 16;

    private readonly List<VertexFormatElement> _elements;
    private readonly Dictionary<string, VertexFormatElement> _elementsByName;

    public IReadOnlyList<VertexFormatElement> Elements => _elements;
    //VertexSize bytes of one vertex, maps to vanilla getVertexSize
    public int VertexSize { get; }
    //StepRate instancing step rate, 0 for per-vertex data, maps to vanilla getStepRate
    public int StepRate { get; }

    private VertexFormat(List<VertexFormatElement> elements, Dictionary<string, VertexFormatElement> elementsByName, int vertexSize, int stepRate)
    {
        _elements = elements;
        _elementsByName = elementsByName;
        VertexSize = vertexSize;
        StepRate = stepRate;
    }

    //Create starts a builder; stepRate 0 means per-vertex data
    public static Builder Create(int stepRate = 0) => new(stepRate);

    //GetElement looks up an attribute by name, maps to vanilla getElement
    public VertexFormatElement? GetElement(string name)
        => _elementsByName.TryGetValue(name, out var element) ? element : null;

    //Contains whether the format declares the attribute, maps to vanilla contains
    public bool Contains(string name) => _elementsByName.ContainsKey(name);

    public sealed class Builder
    {
        private readonly int _stepRate;
        private readonly List<VertexFormatElement> _elements = new();
        private readonly Dictionary<string, VertexFormatElement> _elementsByName = new();
        private int _offset;

        internal Builder(int stepRate) => _stepRate = stepRate;

        //AddAttribute appends an attribute, maps to vanilla Builder.addAttribute
        //The offset advances by the format's block size; the first declaration of a name wins, as vanilla does with putIfAbsent
        public Builder AddAttribute(string name, GpuFormat format)
        {
            var offset = Align(_offset, format.ByteAlignment());
            var element = new VertexFormatElement(name, offset, format);
            _elements.Add(element);
            _elementsByName.TryAdd(name, element);
            _offset = offset + format.BlockSize();
            return this;
        }

        public VertexFormat Build()
        {
            if (_elements.Count > MaxVertexElements)
                throw new InvalidOperationException($"A vertex format may not declare more than {MaxVertexElements} elements, got {_elements.Count}");
            //The vertex size must stay 4 byte aligned, which is what the backends require for the binding stride
            return new VertexFormat(_elements, _elementsByName, Align(_offset, 4), _stepRate);
        }

        private static int Align(int value, int alignment)
            => alignment <= 1 ? value : (value + alignment - 1) / alignment * alignment;
    }
}

//DefaultVertexFormat vertex format presets, maps to vanilla DefaultVertexFormat
//Only the presets the client currently renders with are declared; add more as pipelines need them
public static class DefaultVertexFormat
{
    //Semantic names, matching the vanilla attribute names the pipelines bind against
    public const string PositionSemanticName = "Position";
    public const string ColorSemanticName = "Color";
    public const string Uv0SemanticName = "UV0";
    public const string Uv1SemanticName = "UV1";
    public const string Uv2SemanticName = "UV2";
    public const string NormalSemanticName = "Normal";
    public const string LineWidthSemanticName = "LineWidth";

    //POSITION_COLOR GUI solid color, maps to vanilla POSITION_COLOR
    public static readonly VertexFormat POSITION_COLOR = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, GpuFormat.Rgb32Float)
        .AddAttribute(ColorSemanticName, GpuFormat.Rgba8Unorm)
        .Build();

    //POSITION_TEX_COLOR textured GUI, maps to vanilla POSITION_TEX_COLOR
    public static readonly VertexFormat POSITION_TEX_COLOR = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, GpuFormat.Rgb32Float)
        .AddAttribute(Uv0SemanticName, GpuFormat.Rg32Float)
        .AddAttribute(ColorSemanticName, GpuFormat.Rgba8Unorm)
        .Build();

    //POSITION_TEX untextured GUI, maps to vanilla POSITION_TEX
    public static readonly VertexFormat POSITION_TEX = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, GpuFormat.Rgb32Float)
        .AddAttribute(Uv0SemanticName, GpuFormat.Rg32Float)
        .Build();

    //POSITION_COLOR_UV_LIGHT_NORMAL 3D item vertex format, a simplified vanilla version without overlay
    //VertexConsumer3D outputs a 10-float vertex; color/light are stored as float bit patterns and the shader casts them back
    public static readonly VertexFormat POSITION_COLOR_UV_LIGHT_NORMAL = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, GpuFormat.Rgb32Float)
        .AddAttribute(ColorSemanticName, GpuFormat.R32Float)
        .AddAttribute(Uv0SemanticName, GpuFormat.Rg32Float)
        .AddAttribute(Uv2SemanticName, GpuFormat.R32Float)
        .AddAttribute(NormalSemanticName, GpuFormat.Rgb32Float)
        .Build();

    //POSITION_COLOR_TEX_OVERLAY_LIGHT_NORMAL entity vertex format, maps to the vanilla entity format
    //Adds an overlay attribute over blocks, used for the red damage flash; stride 44 bytes
    public static readonly VertexFormat POSITION_COLOR_TEX_OVERLAY_LIGHT_NORMAL = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, GpuFormat.Rgb32Float)
        .AddAttribute(ColorSemanticName, GpuFormat.R32Float)
        .AddAttribute(Uv0SemanticName, GpuFormat.Rg32Float)
        .AddAttribute(Uv1SemanticName, GpuFormat.R32Float)
        .AddAttribute(Uv2SemanticName, GpuFormat.R32Float)
        .AddAttribute(NormalSemanticName, GpuFormat.Rgb32Float)
        .Build();

    //The attribute formats below are the vanilla ones, maps to the vanilla private format constants
    //Colour is a packed RGBA8 and light is an RG16 pair here; the item and entity presets above pack those into
    //R32 floats instead because their shaders decode the values from a raw bit pattern
    private static readonly GpuFormat PositionFormat = GpuFormat.Rgb32Float;
    private static readonly GpuFormat ColorFormat = GpuFormat.Rgba8Unorm;
    private static readonly GpuFormat Uv0Format = GpuFormat.Rg32Float;
    private static readonly GpuFormat Uv1Format = GpuFormat.Rg16Sint;
    private static readonly GpuFormat Uv2Format = GpuFormat.Rg16Sint;
    private static readonly GpuFormat NormalFormat = GpuFormat.Rgba8Snorm;
    private static readonly GpuFormat LineWidthFormat = GpuFormat.R32Float;

    //BLOCK terrain vertices, maps to vanilla BLOCK
    public static readonly VertexFormat BLOCK = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .Build();

    //ENTITY entity models, maps to vanilla ENTITY
    public static readonly VertexFormat ENTITY = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(Uv1SemanticName, Uv1Format)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .AddAttribute(NormalSemanticName, NormalFormat)
        .Build();

    //PARTICLE particles, maps to vanilla PARTICLE
    public static readonly VertexFormat PARTICLE = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .Build();

    //POSITION bare positions, maps to vanilla POSITION
    public static readonly VertexFormat POSITION = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .Build();

    //POSITION_COLOR_NORMAL, maps to vanilla POSITION_COLOR_NORMAL
    public static readonly VertexFormat POSITION_COLOR_NORMAL = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(NormalSemanticName, NormalFormat)
        .Build();

    //POSITION_COLOR_LIGHTMAP lit geometry without a texture, maps to vanilla POSITION_COLOR_LIGHTMAP
    public static readonly VertexFormat POSITION_COLOR_LIGHTMAP = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .Build();

    //POSITION_COLOR_TEX_LIGHTMAP, maps to vanilla POSITION_COLOR_TEX_LIGHTMAP
    public static readonly VertexFormat POSITION_COLOR_TEX_LIGHTMAP = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .Build();

    //POSITION_TEX_LIGHTMAP_COLOR, maps to vanilla POSITION_TEX_LIGHTMAP_COLOR
    public static readonly VertexFormat POSITION_TEX_LIGHTMAP_COLOR = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(Uv2SemanticName, Uv2Format)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .Build();

    //POSITION_TEX_COLOR_NORMAL, maps to vanilla POSITION_TEX_COLOR_NORMAL
    public static readonly VertexFormat POSITION_TEX_COLOR_NORMAL = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(Uv0SemanticName, Uv0Format)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(NormalSemanticName, NormalFormat)
        .Build();

    //POSITION_COLOR_LINE_WIDTH lines carrying a width, maps to vanilla POSITION_COLOR_LINE_WIDTH
    public static readonly VertexFormat POSITION_COLOR_LINE_WIDTH = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(LineWidthSemanticName, LineWidthFormat)
        .Build();

    //POSITION_COLOR_NORMAL_LINE_WIDTH, maps to vanilla POSITION_COLOR_NORMAL_LINE_WIDTH
    public static readonly VertexFormat POSITION_COLOR_NORMAL_LINE_WIDTH = VertexFormat.Create()
        .AddAttribute(PositionSemanticName, PositionFormat)
        .AddAttribute(ColorSemanticName, ColorFormat)
        .AddAttribute(NormalSemanticName, NormalFormat)
        .AddAttribute(LineWidthSemanticName, LineWidthFormat)
        .Build();
}
