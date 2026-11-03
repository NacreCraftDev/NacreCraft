using System.Security.Cryptography.X509Certificates;
using NacreCraft.Logging;

namespace NacreCraft.Game.Server.JsonRpc.Security;

//JsonRpcSslContextProvider loads the management server certificate, maps to vanilla net.minecraft.server.jsonrpc.security.JsonRpcSslContextProvider
//Vanilla builds a netty SslContext from the Pkcs12 keystore through a key manager and a trust manager over the same store
//The trust manager only feeds client certificate validation, which the vanilla pipeline leaves at ClientAuth.NONE, so only the server certificate is loaded here
public static class JsonRpcSslContextProvider
{
    private const string PasswordEnvVariableKey = "MINECRAFT_MANAGEMENT_TLS_KEYSTORE_PASSWORD";
    private const string PasswordAppContextKey = "management.tls.keystore.password";
    private const string KeystoreFormat = "PKCS12";

    //CreateFrom loads the server certificate and its private key from a Pkcs12 keystore
    //An empty path or a missing file throws an argument exception; password priority: environment variable > AppContext > server.properties
    public static X509Certificate2 CreateFrom(string keystorePath, string keystorePasswordFromServerProperties)
    {
        if (string.IsNullOrEmpty(keystorePath))
        {
            throw new ArgumentException("TLS is enabled but keystore is not configured");
        }
        if (!File.Exists(keystorePath) || Directory.Exists(keystorePath))
        {
            throw new ArgumentException($"Supplied keystore is not a file or does not exist: '{keystorePath}'");
        }
        var password = GetKeystorePassword(keystorePasswordFromServerProperties);
        return LoadKeystoreFromPath(keystorePath, password);
    }

    //GetKeystorePassword takes the keystore password by priority; falls back to the server.properties value when none is set
    private static string? GetKeystorePassword(string keystorePasswordFromServerProperties)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(PasswordEnvVariableKey);
        if (fromEnvironment is not null) return fromEnvironment;
        if (AppContext.GetData(PasswordAppContextKey) is string fromAppContext) return fromAppContext;
        return keystorePasswordFromServerProperties;
    }

    //LoadKeystoreFromPath opens the Pkcs12 store and keeps the key in memory, maps to vanilla loadKeystoreFromPath
    //An empty password means the store has no password, matching the empty char array vanilla hands to KeyStore.load
    private static X509Certificate2 LoadKeystoreFromPath(string path, string? password)
    {
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, string.IsNullOrEmpty(password) ? null : password,
            X509KeyStorageFlags.EphemeralKeySet);
        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new ArgumentException($"Supplied {KeystoreFormat} keystore contains no private key: '{path}'");
        }
        return certificate;
    }

    //PrintInstructions prints the steps needed to enable TLS
    public static void PrintInstructions()
    {
        Log.Info("To use TLS for the management server, please follow these steps:");
        Log.Info("1. Set the server property 'management-server-tls-enabled' to 'true' to enable TLS");
        Log.Info("2. Create a keystore file of type PKCS12 containing your server certificate and private key");
        Log.Info("3. Set the server property 'management-server-tls-keystore' to the path of your keystore file");
        Log.Info("4. Set the keystore password via the environment variable 'MINECRAFT_MANAGEMENT_TLS_KEYSTORE_PASSWORD', or system property 'management.tls.keystore.password', or server property 'management-server-tls-keystore-password'");
        Log.Info("5. Restart the server to apply the changes.");
    }
}
