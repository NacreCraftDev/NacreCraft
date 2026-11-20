using NacreCraft.Registry;

namespace NacreCraft.Game.Commands.Arguments;

//IRegistryKeyedArgument an argument type bound to one registry, maps to the registryKey field of vanilla ResourceOrTagArgument/ResourceOrTagKeyArgument
//The command tree serializer writes this key so the client can restore the argument's registry
public interface IRegistryKeyedArgument
{
    Identifier RegistryIdentifier { get; }
}
