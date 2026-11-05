namespace NacreCraft.Network.Chat;

//ThrowingComponent an exception carrying a text component, maps to vanilla net.minecraft.network.chat.ThrowingComponent
public class ThrowingComponent : Exception
{
    public ThrowingComponent(Component component) : base(component.GetString())
        => Component = component;

    public ThrowingComponent(Component component, Exception cause) : base(component.GetString(), cause)
        => Component = component;

    public Component Component { get; }
}
