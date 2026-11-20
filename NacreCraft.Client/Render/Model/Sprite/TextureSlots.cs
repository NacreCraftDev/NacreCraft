using System.Text.Json;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Model.Sprite;

//TextureSlots the resolved #slot → material map of a model, maps to vanilla net.minecraft.client.resources.model.sprite.TextureSlots
public sealed class TextureSlots
{
    public static readonly TextureSlots Empty = new(new Dictionary<string, Material>());

    private readonly IReadOnlyDictionary<string, Material> _resolvedValues;

    private TextureSlots(IReadOnlyDictionary<string, Material> resolvedValues) => _resolvedValues = resolvedValues;

    public Material? GetMaterial(string reference)
    {
        if (reference.Length > 0 && reference[0] == '#') reference = reference[1..];
        return _resolvedValues.TryGetValue(reference, out var material) ? material : null;
    }

    //ParseTextureMap reads a model's "textures" object into slot data
    public static Data ParseTextureMap(JsonElement texturesObject)
    {
        var builder = new Data.Builder();
        foreach (var entry in texturesObject.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String)
            {
                var value = entry.Value.GetString() ?? string.Empty;
                if (value.StartsWith('#')) builder.AddReference(entry.Name, value[1..]);
                else builder.AddTexture(entry.Name, new Material(Identifier.Parse(value)));
            }
            else if (entry.Value.ValueKind == JsonValueKind.Object)
            {
                var sprite = entry.Value.TryGetProperty("sprite", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                var force = entry.Value.TryGetProperty("force_translucent", out var f) && f.GetBoolean();
                builder.AddTexture(entry.Name, new Material(Identifier.Parse(sprite), force));
            }
        }

        return builder.Build();
    }

    public sealed record Data(IReadOnlyDictionary<string, SlotContents> Values)
    {
        public static readonly Data Empty = new(new Dictionary<string, SlotContents>());

        public sealed class Builder
        {
            private readonly Dictionary<string, SlotContents> _textureMap = new();

            public Builder AddReference(string slot, string reference)
            {
                _textureMap[slot] = new Reference(reference);
                return this;
            }

            public Builder AddTexture(string slot, Material material)
            {
                _textureMap[slot] = new Value(material);
                return this;
            }

            public Data Build() => _textureMap.Count == 0 ? Empty : new Data(_textureMap);
        }
    }

    public abstract record SlotContents;
    public sealed record Value(Material Material) : SlotContents;
    public sealed record Reference(string Target) : SlotContents;

    //Resolver merges slot data and resolves references, maps to vanilla TextureSlots.Resolver
    public sealed class Resolver
    {
        private readonly List<Data> _entries = new();

        public Resolver AddLast(Data data)
        {
            _entries.Add(data);
            return this;
        }

        public Resolver AddFirst(Data data)
        {
            _entries.Insert(0, data);
            return this;
        }

        public TextureSlots Resolve(ModelDebugName debugName)
        {
            if (_entries.Count == 0) return Empty;
            var resolved = new Dictionary<string, Material>();
            var unresolved = new Dictionary<string, Reference>();
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                foreach (var (slot, contents) in _entries[i].Values)
                {
                    if (contents is Value value)
                    {
                        unresolved.Remove(slot);
                        resolved[slot] = value.Material;
                    }
                    else if (contents is Reference reference)
                    {
                        resolved.Remove(slot);
                        unresolved[slot] = reference;
                    }
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var (slot, reference) in unresolved.ToArray())
                {
                    if (!resolved.TryGetValue(reference.Target, out var material)) continue;
                    resolved[slot] = material;
                    unresolved.Remove(slot);
                    changed = true;
                }
            }

            return new TextureSlots(resolved);
        }
    }
}
