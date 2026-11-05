namespace NacreCraft.Util;

//SignatureUpdater feeds bytes into a signer or verifier
//maps to vanilla net.minecraft.util.SignatureUpdater
public interface SignatureUpdater
{
    //Of adapts a lambda to the updater interface
    static SignatureUpdater Of(Action<Output> update) => new DelegateSignatureUpdater(update);

    void Update(Output output);

    //Output receives the bytes to sign or verify
    public interface Output
    {
        void Update(byte[] bytes);
    }
}

//DelegateSignatureUpdater adapter around a lambda
internal sealed class DelegateSignatureUpdater(Action<SignatureUpdater.Output> update) : SignatureUpdater
{
    public void Update(SignatureUpdater.Output output) => update(output);
}
