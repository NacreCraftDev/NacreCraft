using NacreCraft.Game;
using NacreCraft.Resources;
using ChatLanguage = NacreCraft.Network.Chat.Language;

namespace NacreCraft.Client.Language;

//LanguageManager tracks the selected language and rebuilds the table on reload, maps to vanilla net.minecraft.client.resources.language.LanguageManager
//Vanilla notifies a Consumer<ClientLanguage>; NacreCraft's table type is ChatLanguage, so the callback carries that instead
//The available list comes from the language section of pack.mcmeta; en_us is the fallback base of every stack
public sealed class LanguageManager
{
    public static readonly LanguageInfo DefaultLanguage = new("US", "English", false);

    private readonly Action<ChatLanguage>? _reloadCallback;
    private IReadOnlyDictionary<string, LanguageInfo> _languages = new Dictionary<string, LanguageInfo>(StringComparer.Ordinal)
    {
        [ChatLanguage.Default] = DefaultLanguage
    };
    private string _currentCode;

    public LanguageManager(string languageCode, Action<ChatLanguage>? reloadCallback = null)
    {
        _currentCode = languageCode;
        _reloadCallback = reloadCallback;
    }

    //Languages the languages declared by the currently loaded packs
    public IReadOnlyDictionary<string, LanguageInfo> Languages => _languages;

    //DefaultRightToLeft direction of the selected language, taken from its metadata
    public bool DefaultRightToLeft { get; private set; }

    //Selected current language code, maps to vanilla getSelected/setSelected
    public string Selected
    {
        get => _currentCode;
        set => _currentCode = value;
    }

    public string GetSelected() => _currentCode;

    public void SetSelected(string code) => _currentCode = code;

    //GetLanguages sorted by code, maps to vanilla getLanguages returning a TreeMap
    public SortedDictionary<string, LanguageInfo> GetLanguages()
    {
        var sorted = new SortedDictionary<string, LanguageInfo>(StringComparer.Ordinal);
        foreach (var (code, info) in _languages) sorted[code] = info;
        return sorted;
    }

    public LanguageInfo? GetLanguage(string code)
        => _languages.TryGetValue(code, out var info) ? info : null;

    //Reload extracts the pack language list, rebuilds the table and replaces the global instance
    //Vanilla also passes the direction to ClientLanguage.loadFrom; here the table builder owns the stack and the flag stays on the manager
    public void Reload(ResourceManager manager)
    {
        _languages = LanguageMetadataSection.ReadAll(manager).Languages;
        DefaultRightToLeft = DefaultLanguage.Bidirectional;
        if (!string.Equals(_currentCode, ChatLanguage.Default, StringComparison.Ordinal)
            && _languages.TryGetValue(_currentCode, out var current))
            DefaultRightToLeft = current.Bidirectional;

        var language = LanguageTable.Build(manager, _currentCode);
        ChatLanguage.Inject(language);
        _reloadCallback?.Invoke(language);
    }
}
