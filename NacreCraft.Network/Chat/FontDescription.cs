namespace NacreCraft.Network.Chat;

using NacreCraft.Registry;

//Font description, maps to vanilla net.minecraft.network.chat.FontDescription
//Describes the font resource or atlas sprite used when rendering text
public interface FontDescription
{
    //Default font, maps to vanilla DEFAULT
    public static readonly Resource Default = new(Identifier.WithDefaultNamespace("default"));

    //Resource type maps to vanilla FontDescription.Resource, a font description in resource path form
    public sealed class Resource(Identifier id) : FontDescription
    {
        public Identifier Id { get; } = id;

        public override bool Equals(object? obj) => obj is Resource other && Id.Equals(other.Id);

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => Id.ToString();
    }

    //AtlasSprite type maps to vanilla FontDescription.AtlasSprite, a font description in atlas sprite form
    public sealed class AtlasSprite(Identifier atlasId, Identifier spriteId) : FontDescription
    {
        public Identifier AtlasId { get; } = atlasId;
        public Identifier SpriteId { get; } = spriteId;

        public override bool Equals(object? obj)
        {
            return obj is AtlasSprite other && AtlasId.Equals(other.AtlasId) && SpriteId.Equals(other.SpriteId);
        }

        public override int GetHashCode() => HashCode.Combine(AtlasId, SpriteId);

        public override string ToString() => $"{AtlasId}:{SpriteId}";
    }

    //PlayerSprite type maps to vanilla FontDescription.PlayerSprite, a player head rendered as a glyph
    //NC's Network layer cannot reference the Game layer's ResolvableProfile, so the profile is carried as id+name
    public sealed class PlayerSprite(Guid id, string name, bool hat) : FontDescription
    {
        public Guid Id { get; } = id;
        public string Name { get; } = name;
        public bool Hat { get; } = hat;

        public override bool Equals(object? obj)
        {
            return obj is PlayerSprite other && Id.Equals(other.Id) && Name == other.Name && Hat == other.Hat;
        }

        public override int GetHashCode() => HashCode.Combine(Id, Name, Hat);

        public override string ToString() => $"{Name}:{Id}{(Hat ? ":hat" : string.Empty)}";
    }
}
