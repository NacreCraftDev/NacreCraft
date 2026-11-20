using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Client.Render.Texture;

//StitcherEntry one sprite the stitcher can pack, maps to vanilla Stitcher.Entry
public interface StitcherEntry
{
    int Width { get; }

    int Height { get; }

    Identifier Name { get; }
}

//SpriteLoader receives the packed position of each sprite, maps to vanilla Stitcher.SpriteLoader
public interface SpriteLoader<in T> where T : StitcherEntry
{
    void Load(T entry, int x, int y, int padding);
}

//Stitcher generic bin-packing atlas layout, maps to vanilla net.minecraft.client.renderer.texture.Stitcher
//Sprites are sorted by descending height/width/name and placed into power-of-two regions; padding leaves room for mipmaps
public sealed class Stitcher<T> where T : class, StitcherEntry
{
    private readonly int _mipLevel;
    private readonly List<Holder> _texturesToBeStitched = new();
    private readonly List<Region> _storage = new();
    private int _storageX;
    private int _storageY;
    private readonly int _maxWidth;
    private readonly int _maxHeight;
    private readonly int _padding;

    public Stitcher(int maxWidth, int maxHeight, int mipLevel, int anisotropyBit)
    {
        _mipLevel = mipLevel;
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        _padding = 1 << mipLevel << Mth.Clamp(anisotropyBit - 1, 0, 4);
    }

    public int GetWidth() => _storageX;

    public int GetHeight() => _storageY;

    public void RegisterSprite(T entry)
    {
        _texturesToBeStitched.Add(new Holder(entry,
            SmallestFittingMinTexel(entry.Width + _padding * 2, _mipLevel),
            SmallestFittingMinTexel(entry.Height + _padding * 2, _mipLevel)));
    }

    public void Stitch()
    {
        var holders = new List<Holder>(_texturesToBeStitched);
        holders.Sort(static (a, b) =>
        {
            var byHeight = b.Height.CompareTo(a.Height);
            if (byHeight != 0) return byHeight;
            var byWidth = b.Width.CompareTo(a.Width);
            if (byWidth != 0) return byWidth;
            return string.CompareOrdinal(a.Entry.Name.ToString(), b.Entry.Name.ToString());
        });

        foreach (var holder in holders)
        {
            if (AddToStorage(holder)) continue;
            throw new StitcherException(holder.Entry, holders.Select(h => (StitcherEntry)h.Entry).ToList());
        }
    }

    public void GatherSprites(SpriteLoader<T> loader)
    {
        foreach (var region in _storage)
            region.Walk(loader, _padding);
    }

    private static int SmallestFittingMinTexel(int input, int maxMipLevel)
        => (input >> maxMipLevel) + ((input & ((1 << maxMipLevel) - 1)) == 0 ? 0 : 1) << maxMipLevel;

    private bool AddToStorage(Holder holder)
    {
        foreach (var region in _storage)
            if (region.Add(holder)) return true;
        return Expand(holder);
    }

    private bool Expand(Holder holder)
    {
        var xCurrentSize = Mth.SmallestEncompassingPowerOfTwo(_storageX);
        var yCurrentSize = Mth.SmallestEncompassingPowerOfTwo(_storageY);
        var xNewSize = Mth.SmallestEncompassingPowerOfTwo(_storageX + holder.Width);
        var yNewSize = Mth.SmallestEncompassingPowerOfTwo(_storageY + holder.Height);
        var xCanGrow = xNewSize <= _maxWidth;
        var yCanGrow = yNewSize <= _maxHeight;
        if (!xCanGrow && !yCanGrow) return false;

        var xWillGrow = xCanGrow && xCurrentSize != xNewSize;
        var yWillGrow = yCanGrow && yCurrentSize != yNewSize;
        bool growOnX;
        if (xWillGrow ^ yWillGrow) growOnX = xWillGrow;
        else growOnX = xCanGrow && xCurrentSize <= yCurrentSize;

        Region slot;
        if (growOnX)
        {
            if (_storageY == 0) _storageY = yNewSize;
            slot = new Region(_storageX, 0, xNewSize - _storageX, _storageY);
            _storageX = xNewSize;
        }
        else
        {
            slot = new Region(0, _storageY, _storageX, yNewSize - _storageY);
            _storageY = yNewSize;
        }

        slot.Add(holder);
        _storage.Add(slot);
        return true;
    }

    internal sealed record Holder(T Entry, int Width, int Height);

    //Region one packed rectangle, split recursively as sprites are added, maps to vanilla Stitcher.Region
    public sealed class Region
    {
        private readonly int _originX;
        private readonly int _originY;
        private readonly int _width;
        private readonly int _height;
        private List<Region>? _subSlots;
        private Holder? _holder;

        public Region(int originX, int originY, int width, int height)
        {
            _originX = originX;
            _originY = originY;
            _width = width;
            _height = height;
        }

        public int GetX() => _originX;

        public int GetY() => _originY;

        internal bool Add(Holder holder)
        {
            if (_holder is not null) return false;
            var textureWidth = holder.Width;
            var textureHeight = holder.Height;
            if (textureWidth > _width || textureHeight > _height) return false;
            if (textureWidth == _width && textureHeight == _height)
            {
                _holder = holder;
                return true;
            }

            if (_subSlots is null)
            {
                _subSlots = new List<Region>(1)
                {
                    new(_originX, _originY, textureWidth, textureHeight)
                };
                var spareWidth = _width - textureWidth;
                var spareHeight = _height - textureHeight;
                if (spareHeight > 0 && spareWidth > 0)
                {
                    var right = Math.Max(_height, spareWidth);
                    var bottom = Math.Max(_width, spareHeight);
                    if (right >= bottom)
                    {
                        _subSlots.Add(new Region(_originX, _originY + textureHeight, textureWidth, spareHeight));
                        _subSlots.Add(new Region(_originX + textureWidth, _originY, spareWidth, _height));
                    }
                    else
                    {
                        _subSlots.Add(new Region(_originX + textureWidth, _originY, spareWidth, textureHeight));
                        _subSlots.Add(new Region(_originX, _originY + textureHeight, _width, spareHeight));
                    }
                }
                else if (spareWidth == 0)
                {
                    _subSlots.Add(new Region(_originX, _originY + textureHeight, textureWidth, spareHeight));
                }
                else if (spareHeight == 0)
                {
                    _subSlots.Add(new Region(_originX + textureWidth, _originY, spareWidth, textureHeight));
                }
            }

            foreach (var subSlot in _subSlots)
                if (subSlot.Add(holder)) return true;
            return false;
        }

        public void Walk(SpriteLoader<T> output, int padding)
        {
            if (_holder is not null)
            {
                output.Load(_holder.Entry, GetX(), GetY(), padding);
            }
            else if (_subSlots is not null)
            {
                foreach (var subSlot in _subSlots) subSlot.Walk(output, padding);
            }
        }

        public override string ToString()
            => $"Slot{{originX={_originX}, originY={_originY}, width={_width}, height={_height}, texture={_holder}, subSlots={_subSlots}}}";
    }
}

