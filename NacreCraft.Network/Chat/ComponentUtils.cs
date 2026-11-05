using NacreCraft.Commands;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Network.Chat.Contents;

namespace NacreCraft.Network.Chat;

//ComponentUtils component helpers, maps to vanilla net.minecraft.network.chat.ComponentUtils
//Resolver helpers flatten and re-style a component tree; contents-level resolution of score/selector/object is not ported yet,
//so Resolve only rebuilds the tree and resolves hover text
public static class ComponentUtils
{
    public const string DefaultSeparatorText = ", ";

    public static readonly Component DefaultSeparator = Component.Literal(", ").WithColor(ChatFormatting.Gray);

    public static readonly Component DefaultNoStyleSeparator = Component.Literal(", ");

    public static MutableComponent MergeStyles(MutableComponent component, Style style)
    {
        if (style.IsEmpty) return component;
        var inner = component.Style;
        if (inner.IsEmpty) return component.SetStyle(style);
        return inner.Equals(style) ? component : component.SetStyle(inner.ApplyTo(style));
    }

    public static Component MergeStyles(Component component, Style style)
    {
        if (style.IsEmpty) return component;
        var inner = component.Style;
        if (inner.IsEmpty) return component.Copy().SetStyle(style);
        return inner.Equals(style) ? component : component.Copy().SetStyle(inner.ApplyTo(style));
    }

    //Resolve rebuilds the component tree under the resolution context, maps to vanilla resolve
    public static MutableComponent Resolve(ResolutionContext context, Component component) => Resolve(context, component, 0);

    public static MutableComponent Resolve(ResolutionContext context, Component component, int recursionDepth)
    {
        if (recursionDepth > context.DepthLimit)
            return context.DepthLimitBehavior == ResolutionContext.LimitBehavior.DiscardRemaining
                ? CommonComponents.Ellipsis.Copy()
                : component.Copy();

        var result = component.PlainCopy();
        foreach (var sibling in component.Siblings)
            result.Append(Resolve(context, sibling, recursionDepth + 1));
        return result.SetStyle(ResolveStyle(context, component.Style, recursionDepth));
    }

    //ResolveStyle resolves the hover text of a style, maps to vanilla resolveStyle
    private static Style ResolveStyle(ResolutionContext context, Style style, int recursionDepth)
        => style.HoverEvent is HoverEvent.ShowText showText
            ? style.WithHoverEvent(new HoverEvent.ShowText(Resolve(context, showText.Value, recursionDepth + 1)))
            : style;

    public static Component FormatList(IEnumerable<string> values)
        => FormatList(values, value => (Component)Component.Literal(value).WithColor(ChatFormatting.Green));

    public static Component FormatAndSortList<T>(IEnumerable<T> values, Func<T, Component> formatter)
        where T : IComparable<T>
    {
        var list = values.ToList();
        if (list.Count == 0) return CommonComponents.Empty;
        if (list.Count == 1) return formatter(list[0]);
        list.Sort();
        return FormatList(list, formatter);
    }

    public static Component FormatList<T>(IEnumerable<T> values, Func<T, Component> formatter)
        => FormatList(values, DefaultSeparator, formatter);

    public static MutableComponent FormatList<T>(IEnumerable<T> values, Component separator, Func<T, Component> formatter)
    {
        var list = values.ToList();
        if (list.Count == 0) return Component.Empty();
        if (list.Count == 1) return formatter(list[0]).Copy();
        var result = Component.Empty();
        var first = true;
        foreach (var value in list)
        {
            if (!first) result.Append(separator);
            result.Append(formatter(value));
            first = false;
        }
        return result;
    }

    public static Component FormatList(IEnumerable<Component> values, Component separator)
        => FormatList(values, separator, value => value);

    public static MutableComponent WrapInSquareBrackets(Component inner)
        => Component.Translatable("chat.square_brackets", inner);

    public static Component FromMessage(IMessage message)
        => message as Component ?? Component.Literal(message.GetString());

    public static bool IsTranslationResolvable(Component? component)
    {
        if (component?.Contents is not TranslatableContents translatable) return true;
        return translatable.Fallback is not null || Language.Instance.Has(translatable.Key);
    }

    public static MutableComponent CopyOnClickText(string text)
        => WrapInSquareBrackets(Component.Literal(text).WithStyle(style => style
            .WithColor(ChatFormatting.Green)
            .WithClickEvent(new ClickEvent.CopyToClipboard(text))
            .WithHoverEvent(new HoverEvent.ShowText(Component.Translatable("chat.copy.click")))
            .WithInsertion(text)));
}
