using NacreCraft.Game.Server.Network;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Server.Network;

//ServerTextFilter base for local chat text filters, maps to vanilla net.minecraft.server.network.ServerTextFilter
//NC does not call the Mojang/Azure HTTP filtering services; subclasses implement FilterText locally and still expose the
//same PlayerContext/TextFilter surface used by chat handling
public abstract class ServerTextFilter : IDisposable
{
    protected ServerTextFilter(ServerTextFilter.IgnoreStrategy ignoreStrategy) => Ignore = ignoreStrategy;

    protected ServerTextFilter.IgnoreStrategy Ignore { get; }

    //CreateContext binds the filter to a player profile, maps to vanilla createContext
    public TextFilter CreateContext(GameProfile profile, string language = "en_us") => new PlayerContext(this, profile, language);

    //FilterText applies the local filtering rules, maps to vanilla filterText
    protected abstract FilteredText FilterText(string message, ServerTextFilter.IgnoreStrategy ignoreStrategy);

    //ParseMask marks the given character indices as filtered, maps to vanilla parseMask
    protected static FilterMask ParseMask(string message, IReadOnlyCollection<int> removedChars, ServerTextFilter.IgnoreStrategy strategy)
    {
        if (strategy.ShouldIgnore(message, removedChars.Count)) return FilterMask.FullyFiltered;
        var mask = new FilterMask(message.Length);
        foreach (var index in removedChars)
        {
            if (index >= 0 && index < message.Length) mask.SetFiltered(index);
        }
        return mask;
    }

    public virtual void Dispose()
    {
    }

    public interface IgnoreStrategy
    {
        bool ShouldIgnore(string message, int removedCharCount);

        public static readonly IgnoreStrategy NeverIgnore = new DelegateIgnoreStrategy((_, _) => false);
        public static readonly IgnoreStrategy IgnoreFullyFiltered =
            new DelegateIgnoreStrategy((message, removed) => message.Length == removed);
    }

    private sealed class DelegateIgnoreStrategy(Func<string, int, bool> shouldIgnore) : IgnoreStrategy
    {
        public bool ShouldIgnore(string message, int removedCharCount) => shouldIgnore(message, removedCharCount);
    }

    private sealed class PlayerContext(ServerTextFilter filter, GameProfile profile, string language) : TextFilter
    {
        public Task<FilteredText> ProcessStreamMessage(string message)
            => Task.FromResult(filter.FilterText(message, filter.Ignore));

        public Task<IReadOnlyList<FilteredText>> ProcessMessageBundle(IReadOnlyList<string> messages)
        {
            var results = messages.Select(message => filter.FilterText(message, filter.Ignore)).ToList();
            return Task.FromResult<IReadOnlyList<FilteredText>>(results);
        }

        public override string ToString() => $"{profile.Name}@{language}";
    }
}

//LegacyTextFilter a local banned-word filter, maps to vanilla net.minecraft.server.network.LegacyTextFilter
//The HTTP v1/chat service is not called; the configured banned words are matched case-insensitively
public sealed class LegacyTextFilter : ServerTextFilter
{
    private readonly string[] _bannedWords;

    public LegacyTextFilter(IEnumerable<string> bannedWords) : base(IgnoreStrategy.NeverIgnore)
        => _bannedWords = bannedWords.Where(word => !string.IsNullOrEmpty(word)).ToArray();

    public static LegacyTextFilter Create(IEnumerable<string> bannedWords) => new(bannedWords);

    protected override FilteredText FilterText(string message, ServerTextFilter.IgnoreStrategy ignoreStrategy)
    {
        var removed = new List<int>();
        foreach (var word in _bannedWords)
        {
            var start = 0;
            while (start < message.Length)
            {
                var index = message.IndexOf(word, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
                for (var i = 0; i < word.Length; i++) removed.Add(index + i);
                start = index + word.Length;
            }
        }
        return new FilteredText(message, ParseMask(message, removed, ignoreStrategy));
    }
}

//PlayerSafetyServiceTextFilter a local redaction filter, maps to vanilla net.minecraft.server.network.PlayerSafetyServiceTextFilter
//The Azure Player Safety service is not called; the configured words are redacted locally
public sealed class PlayerSafetyServiceTextFilter : ServerTextFilter
{
    private readonly string[] _redactedWords;

    public PlayerSafetyServiceTextFilter(IEnumerable<string> redactedWords) : base(IgnoreStrategy.IgnoreFullyFiltered)
        => _redactedWords = redactedWords.Where(word => !string.IsNullOrEmpty(word)).ToArray();

    protected override FilteredText FilterText(string message, ServerTextFilter.IgnoreStrategy ignoreStrategy)
    {
        var removed = new List<int>();
        foreach (var word in _redactedWords)
        {
            var start = 0;
            while (start < message.Length)
            {
                var index = message.IndexOf(word, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
                for (var i = 0; i < word.Length; i++) removed.Add(index + i);
                start = index + word.Length;
            }
        }
        return new FilteredText(message, ParseMask(message, removed, ignoreStrategy));
    }
}
