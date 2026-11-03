using NacreCraft.Commands;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands.Synchronization;

//SuggestionProviders the command suggestion providers the command tree can name, maps to vanilla net.minecraft.commands.synchronization.SuggestionProviders
//The tree packet sends a provider name per node, so the client knows whether to ask the server or to build the list locally
//NC runs the whole command stack on one CommandSourceStack type, so the providers are not generic over the source
public static class SuggestionProviders
{
    private static readonly Dictionary<Identifier, SuggestionProvider<CommandSourceStack>> ProvidersByName = new();
    private static readonly Dictionary<SuggestionProvider<CommandSourceStack>, Identifier> NamesByProvider = new();

    //AskServerId the name of the provider that leaves the work to the server, maps to vanilla ID_ASK_SERVER
    public static readonly Identifier AskServerId = Identifier.WithDefaultNamespace("ask_server");

    //AskServer defers to the server; in 26.2 the source callback returns empty and the dispatcher answers the request, so this matches vanilla
    public static readonly SuggestionProvider<CommandSourceStack> AskServer =
        Register(AskServerId, static (_, _) => Suggestions.Empty());

    //AvailableSounds suggests every registered sound event, maps to vanilla AVAILABLE_SOUNDS
    public static readonly SuggestionProvider<CommandSourceStack> AvailableSounds = Register(
        Identifier.WithDefaultNamespace("available_sounds"),
        static (_, builder) => SuggestIdentifiers(BuiltInRegistries.SOUND_EVENT.KeySet, builder));

    //SummonableEntities suggests the entity types that can be created, maps to vanilla SUMMONABLE_ENTITIES
    //NC has no canSummon flag and no per-type description, so a type with a factory counts as summonable and carries no tooltip
    public static readonly SuggestionProvider<CommandSourceStack> SummonableEntities = Register(
        Identifier.WithDefaultNamespace("summonable_entities"),
        static (_, builder) => SuggestIdentifiers(SummonableEntityIds(), builder));

    //Register adds a provider under a name, maps to vanilla register
    //NC has no wrapper type around a delegate, so the registered delegate itself is what GetName recognizes
    public static SuggestionProvider<CommandSourceStack> Register(Identifier name, SuggestionProvider<CommandSourceStack> provider)
    {
        if (!ProvidersByName.TryAdd(name, provider))
            throw new ArgumentException($"A command suggestion provider is already registered with the name '{name}'");
        NamesByProvider[provider] = name;
        return provider;
    }

    //GetProvider looks a provider up by name, falling back to asking the server, maps to vanilla getProvider
    public static SuggestionProvider<CommandSourceStack> GetProvider(Identifier name)
        => ProvidersByName.GetValueOrDefault(name) ?? AskServer;

    //GetName names a provider for the command tree packet, unregistered providers fall back to ask_server, maps to vanilla getName
    public static Identifier GetName(SuggestionProvider<CommandSourceStack> provider)
        => NamesByProvider.GetValueOrDefault(provider, AskServerId);

    private static IEnumerable<Identifier> SummonableEntityIds()
        => BuiltInRegistries.ENTITY_TYPE.EntrySet
            .Where(entry => entry.Value.Factory is not null)
            .Select(entry => entry.Key.Identifier);

    //SuggestIdentifiers adds every identifier matching what the user typed, maps to vanilla suggestResource
    private static Task<Suggestions> SuggestIdentifiers(IEnumerable<Identifier> ids, SuggestionsBuilder builder)
    {
        foreach (var id in ids)
        {
            var text = id.ToString();
            if (text.StartsWith(builder.RemainingLowerCase, StringComparison.OrdinalIgnoreCase)) builder.Add(text);
        }
        return builder.BuildFuture();
    }
}
