using NacreCraft.Commands.Context;

namespace NacreCraft.Commands;

//RedirectModifier delegate maps to vanilla com.mojang.brigadier.RedirectModifier
//Returns multiple sources from a CommandContext for fork expansion
public delegate ICollection<S> RedirectModifier<S>(CommandContext<S> context);
