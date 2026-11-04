using NacreCraft.Codec;

namespace NacreCraft.Network.Chat.Contents;

//Keybind contents, maps to vanilla net.minecraft.network.chat.contents.KeybindContents
//Name is the key name, translated at runtime into localized text by KeybindResolver
public sealed record KeybindContents(string Name) : ComponentContents
{
    //_nameResolver cached resolution of the key name, maps to vanilla nameResolver
    private Func<Component>? _nameResolver;

    //NestedComponent resolves the key name into the component it displays
    private Component NestedComponent() => (_nameResolver ??= KeybindResolver.Resolve(Name))();

    public MapCodec<ComponentContents> Codec() => throw new NotImplementedException();

    public Optional<T> Visit<T>(FormattedText.ContentConsumer<T> output)
        => NestedComponent().Visit(output);

    public Optional<T> Visit<T>(FormattedText.StyledContentConsumer<T> output, Style currentStyle)
        => NestedComponent().Visit(output, currentStyle);

    public override string ToString() => $"keybind{{{Name}}}";
}
