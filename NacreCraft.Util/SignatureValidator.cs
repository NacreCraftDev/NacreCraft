namespace NacreCraft.Util;

//SignatureValidator checks a signature over the bytes an updater produces
//maps to vanilla net.minecraft.util.SignatureValidator
public interface SignatureValidator
{
    bool Validate(SignatureUpdater updater, byte[] signature);
}
