using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands.Arguments;

//ArgumentSignatures signatures of the signed arguments of a command, maps to vanilla net.minecraft.commands.arguments.ArgumentSignatures
//The chat signing path builds it before sending the command; only the payload structure is ported
public sealed record ArgumentSignatures(IReadOnlyList<ArgumentSignatures.Entry> Entries)
{
    public const int MaxArgumentCount = 8;

    public static readonly ArgumentSignatures Empty = new(Array.Empty<Entry>());

    //SignCommand signs every argument value, dropping the ones the signer refuses, maps to vanilla signCommand
    public static ArgumentSignatures SignCommand<S>(SignableCommand<S> command,
        Func<string, MessageSignature?> signer)
    {
        var entries = new List<Entry>();
        foreach (var argument in command.Arguments)
        {
            var signature = signer(argument.Value);
            if (signature is not null) entries.Add(new Entry(argument.Name, signature));
        }
        return new ArgumentSignatures(entries);
    }

    //Entry one signed argument, maps to vanilla Entry
    public sealed record Entry(string Name, MessageSignature Signature);
}
