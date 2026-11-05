namespace NacreCraft.Util;

//Signer produces a signature over the bytes an updater provides
//maps to vanilla net.minecraft.util.Signer
public interface Signer
{
    byte[] Sign(SignatureUpdater updater);
}
