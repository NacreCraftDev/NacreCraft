using NacreCraft.Commands.Context;

namespace NacreCraft.Commands.Suggestion;

//SuggestionProvider delegate maps to vanilla com.mojang.brigadier.suggestion.SuggestionProvider
//An argument node holds this delegate to implement custom completions
public delegate Task<Suggestions> SuggestionProvider<S>(CommandContext<S> context, SuggestionsBuilder builder);
