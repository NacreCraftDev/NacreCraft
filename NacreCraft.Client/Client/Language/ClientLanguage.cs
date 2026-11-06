using NacreCraft.Resources;

namespace NacreCraft.Client.Language;

//ClientLanguage client language table, maps to vanilla net.minecraft.client.resources.language.ClientLanguage
//Attached to the reload chain as a resource reload listener; each reload reassembles the language table for the current language code and replaces the global instance
//The selection and pack metadata live in LanguageManager, this type keeps the listener hook the client already wires up
public sealed class ClientLanguage : PreparableReloadListener
{
    private readonly LanguageManager _manager;

    //LanguageCode current language code, maps to vanilla Minecraft.options.languageCode; to switch languages change this and trigger a reload
    public string LanguageCode
    {
        get => _manager.Selected;
        set => _manager.Selected = value;
    }

    //AvailableLanguages languages declared by resource packs; en_us is present before the first reload
    public IReadOnlyDictionary<string, LanguageInfo> AvailableLanguages => _manager.Languages;

    //DefaultRightToLeft whether the selected language is written right to left
    public bool DefaultRightToLeft => _manager.DefaultRightToLeft;

    public ClientLanguage(string languageCode) => _manager = new LanguageManager(languageCode);

    //Reload assembles and replaces the global instance for the current language code; when the code has no matching file only en_us remains as fallback
    public void Reload(ResourceManager rm, ReloadContext ctx) => _manager.Reload(rm);
}
