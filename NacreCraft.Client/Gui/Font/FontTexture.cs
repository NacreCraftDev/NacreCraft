using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Logging;
namespace NacreCraft.Client.Gui.Font;

//FontTexture dynamic glyph atlas, maps to vanilla FontTexture
//256×256 pixel atlas; a Node binary-tree allocator assigns space on demand to upload glyph pixels
//Returns null when it does not fit, and GlyphStitcher creates the next atlas
//colored=true uses an RGBA8 atlas, false uses an R8 atlas, corresponding to vanilla GpuFormat.RGBA8_UNORM/R8_UNORM
//The GpuTexture is created and injected by GlyphStitcher and released on Dispose; TextureSetup+GlyphRenderTypes are injected the same way
internal sealed class FontTexture : IDisposable
{
    public const int Size = 256;

    private readonly GpuTexture _texture;
    private readonly Node _root;
    private readonly bool _colored;
    private readonly TextureSetup _textureSetup;
    private readonly GlyphRenderTypes _renderTypes;

    public GpuTexture Texture => _texture;
    public bool Colored => _colored;
    public TextureSetup TextureSetup => _textureSetup;
    public GlyphRenderTypes RenderTypes => _renderTypes;

    public FontTexture(GpuTexture texture, bool colored, TextureSetup textureSetup, GlyphRenderTypes renderTypes)
    {
        _texture = texture;
        _colored = colored;
        _textureSetup = textureSetup;
        _renderTypes = renderTypes;
        _root = new Node(0, 0, Size, Size);
    }

    //Add stitches the glyph into the atlas and returns a BakedGlyph; null means it does not fit and a new atlas is needed
    //maps to vanilla FontTexture.add: checks isColored, then calls root.insert to find a slot and upload pixels
    public BakedGlyph? Add(IGlyphInfo info, IGlyphBitmap glyph)
    {
        if (glyph.IsColored != _colored) return null;
        var node = _root.Insert(glyph);
        if (node == null) return null;
        var pixels = glyph.GetPixels();
        //Queued rather than submitted: a submit-and-wait per glyph is what stalled startup and GUI rescaling
        //The render thread drains the queue in one submit before it records (see GpuTexture.FlushPendingRegionUploads)
        GpuTexture.QueueRegionUpload(_texture, node.X, node.Y, glyph.PixelWidth, glyph.PixelHeight, pixels);
        //UVs are inset by 0.01 pixels to avoid sampling out of bounds, maps to vanilla (x+0.01)/256.0f
        float u0 = (node.X + 0.01f) / Size;
        float u1 = ((node.X - 0.01f) + glyph.PixelWidth) / Size;
        float v0 = (node.Y + 0.01f) / Size;
        float v1 = ((node.Y - 0.01f) + glyph.PixelHeight) / Size;
        return new SheetBakedGlyph(info, u0, v0, u1, v1,
            glyph.Left, glyph.Right, glyph.Top, glyph.Bottom,
            _textureSetup, _renderTypes);
    }

    public void Dispose()
    {
        _texture.Dispose();
    }

    //Node binary-tree allocator, maps to vanilla FontTexture$Node
    //occupied marks whether a leaf is used; left/right children split horizontally or vertically based on the width/height delta
    private sealed class Node
    {
        internal readonly int X;
        internal readonly int Y;
        private readonly int _width;
        private readonly int _height;
        private Node? _left;
        private Node? _right;
        private bool _occupied;

        public Node(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            _width = width;
            _height = height;
        }

        //Insert recursively finds a slot and returns null when it does not fit
        //maps to vanilla Node.insert: recurse into children if split, otherwise check whether the size fits
        public Node? Insert(IGlyphBitmap glyph)
        {
            if (_left != null && _right != null)
            {
                var newNode = _left.Insert(glyph);
                return newNode ?? _right.Insert(glyph);
            }
            if (_occupied) return null;
            int glyphWidth = glyph.PixelWidth;
            int glyphHeight = glyph.PixelHeight;
            if (glyphWidth > _width || glyphHeight > _height) return null;
            if (glyphWidth == _width && glyphHeight == _height)
            {
                _occupied = true;
                return this;
            }
            int deltaWidth = _width - glyphWidth;
            int deltaHeight = _height - glyphHeight;
            //Split horizontally when the width delta exceeds the height delta, otherwise vertically, maps to the vanilla deltaWidth>deltaHeight check
            if (deltaWidth > deltaHeight)
            {
                _left = new Node(X, Y, glyphWidth, _height);
                _right = new Node(X + glyphWidth + 1, Y, _width - glyphWidth - 1, _height);
            }
            else
            {
                _left = new Node(X, Y, _width, glyphHeight);
                _right = new Node(X, Y + glyphHeight + 1, _width, _height - glyphHeight - 1);
            }
            return _left.Insert(glyph);
        }
    }
}
