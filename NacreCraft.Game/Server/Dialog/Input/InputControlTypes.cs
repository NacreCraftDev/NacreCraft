using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//InputControlTypes registers the built-in input controls, maps to vanilla net.minecraft.server.dialog.input.InputControlTypes
public static class InputControlTypes
{
    public static MapCodec<RegistryInputControl> Bootstrap(Registry<MapCodec<RegistryInputControl>> registry)
    {
        Registry<MapCodec<RegistryInputControl>>.Register(registry,
            Identifier.WithDefaultNamespace("boolean"), BooleanInput.ControlCodec);
        Registry<MapCodec<RegistryInputControl>>.Register(registry,
            Identifier.WithDefaultNamespace("number_range"), NumberRangeInput.ControlCodec);
        Registry<MapCodec<RegistryInputControl>>.Register(registry,
            Identifier.WithDefaultNamespace("single_option"), SingleOptionInput.ControlCodec);
        return Registry<MapCodec<RegistryInputControl>>.Register(registry,
            Identifier.WithDefaultNamespace("text"), TextInput.ControlCodec);
    }
}
