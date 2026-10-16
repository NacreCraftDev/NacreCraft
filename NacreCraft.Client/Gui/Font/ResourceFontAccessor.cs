using NacreCraft.Registry;
using ClientPackType = NacreCraft.Resources.PackType;
using ClientResourceManager = NacreCraft.Resources.ResourceManager;

namespace NacreCraft.Client.Gui.Font;

//ResourceFontAccessor IFontResourceAccessor backed by the client ResourceManager
//identifier format is namespace:path, e.g. minecraft:font/include/space.json
//Going through the resource manager is what lets a resource pack override a font, which a direct assets path cannot do
public sealed class ResourceFontAccessor : IFontResourceAccessor
{
    private readonly ClientResourceManager _resources;

    public ResourceFontAccessor(ClientResourceManager resources) => _resources = resources;

    public Stream? OpenResource(string identifier)
    {
        var location = Identifier.TryParse(identifier);
        if (location is null) return null;
        return _resources.GetResource(ClientPackType.ClientResources, location.Value)?.Open();
    }
}
