using System.Security.Cryptography;
using System.Text;
using NacreCraft.Logging;

namespace NacreCraft.Game.Server.JsonRpc.Security;

//SecurityCheckResult authentication check result, maps to the internal class of the vanilla handler
//TokenSentInSecWebsocketProtocol true means the key is carried in the websocket subprotocol; the transport layer writes the negotiated header accordingly
public sealed record SecurityCheckResult(bool Allowed, string? Reason, bool TokenSentInSecWebsocketProtocol)
{
    public static SecurityCheckResult Ok(bool tokenSentInSecWebsocketProtocol = false) => new(true, null, tokenSentInSecWebsocketProtocol);

    public static SecurityCheckResult Denied(string reason) => new(false, reason, false);
}

//UnauthorizedResponse the 401 answer written before the connection closes, maps to vanilla sendUnauthorizedResponse
//ContentLength is the UTF8 byte length of Body and the caller sends BodyBytes as the entity
public sealed record UnauthorizedResponse(string Reason)
{
    public const int UnauthorizedStatusCode = 401;
    public const string JsonContentType = "application/json";
    public const string CloseConnection = "close";

    public string Body => "{\"error\":\"Unauthorized\",\"message\":\"" + Reason + "\"}";

    public byte[] BodyBytes => Encoding.UTF8.GetBytes(Body);

    public int ContentLength => BodyBytes.Length;
}

//JsonRpcAuthenticator per connection API key authentication, maps to vanilla net.minecraft.server.jsonrpc.security.AuthenticationHandler
//Vanilla is a sharable netty duplex handler that hangs the state on channel attributes, so the netty port is one instance per connection
//The connection passes only once a request produced a valid key, and taking the key from the subprotocol additionally needs an allowed Origin
//The transport layer writes the 401 answer and the negotiated subprotocol header, the decisions stay here
public sealed class JsonRpcAuthenticator
{
    //BearerPrefix the Bearer scheme prefix of the Authorization header
    public const string BearerPrefix = "Bearer ";
    //SubProtocolValue the websocket subprotocol name written back on a successful handshake
    public const string SubProtocolValue = "minecraft-v1";

    private const string SubProtocolHeaderPrefix = "minecraft-v1,";
    private const string AuthorizationHeaderName = "Authorization";
    private const string SubProtocolHeaderName = "Sec-WebSocket-Protocol";
    private const string OriginHeaderName = "Origin";
    private const int SwitchingProtocolsStatusCode = 101;

    private readonly SecurityConfig _securityConfig;
    private readonly HashSet<string> _allowedOrigins;
    private bool _authenticated;
    private bool _webSocketAllowed;

    //JsonRpcAuthenticator constructor; allowed origins are split by comma into a whitelist
    public JsonRpcAuthenticator(SecurityConfig securityConfig, string allowedOrigins)
    {
        _securityConfig = securityConfig;
        _allowedOrigins = new HashSet<string>(allowedOrigins.Split(','), StringComparer.Ordinal);
    }

    //Authenticated whether the connection cleared the check, maps to vanilla AUTHENTICATED_KEY
    public bool Authenticated => _authenticated;

    //OnRequest handles one http request, null means the request continues down the pipeline
    //A rejected request resets the connection state and returns the 401 answer the caller must write and then close
    public UnauthorizedResponse? OnRequest(IReadOnlyDictionary<string, string?> headers, string? clientIp = null)
    {
        var result = Check(headers);
        if (result.Allowed)
        {
            _authenticated = true;
            if (result.TokenSentInSecWebsocketProtocol) _webSocketAllowed = true;
            return null;
        }
        Log.Debug($"Authentication rejected for connection with ip {clientIp}: {result.Reason}");
        _authenticated = false;
        return new UnauthorizedResponse(result.Reason!);
    }

    //AcceptsNonRequest whether a message other than an http request may continue
    //An unauthenticated connection is dropped, maps to vanilla channelRead closing the channel
    public bool AcceptsNonRequest(string? clientIp = null)
    {
        if (_authenticated) return true;
        Log.Debug($"Dropping unauthenticated connection with ip {clientIp}");
        return false;
    }

    //ShouldWriteSubProtocol whether a switching protocols response must carry Sec-WebSocket-Protocol: minecraft-v1
    //Only the subprotocol token path sets the flag, a Bearer token in the Authorization header never negotiates it
    public bool ShouldWriteSubProtocol(int statusCode) => statusCode == SwitchingProtocolsStatusCode && _webSocketAllowed;

    //Check checks the request headers and returns whether to allow and the rejection reason
    //The header dictionary must be case-insensitive; Authorization and Sec-WebSocket-Protocol are alternatives, the latter needing the Origin whitelist first
    public SecurityCheckResult Check(IReadOnlyDictionary<string, string?> headers)
    {
        var token = ParseTokenInAuthorizationHeader(headers);
        if (token is not null)
        {
            return IsValidApiKey(token) ? SecurityCheckResult.Ok() : SecurityCheckResult.Denied("Invalid API key");
        }
        token = ParseTokenInSecWebsocketProtocolHeader(headers);
        if (token is not null)
        {
            if (!IsAllowedOriginHeader(headers)) return SecurityCheckResult.Denied("Origin Not Allowed");
            return IsValidApiKey(token) ? SecurityCheckResult.Ok(true) : SecurityCheckResult.Denied("Invalid API key");
        }
        return SecurityCheckResult.Denied("Missing API key");
    }

    //IsValidApiKey compares the key in constant time; passes only when equal length and byte identical
    public bool IsValidApiKey(string suppliedKey)
    {
        if (suppliedKey.Length == 0) return false;
        var supplied = Encoding.UTF8.GetBytes(suppliedKey);
        var configured = Encoding.UTF8.GetBytes(_securityConfig.SecretKey);
        return CryptographicOperations.FixedTimeEquals(supplied, configured);
    }

    //ParseTokenInAuthorizationHeader takes the key after the Bearer scheme; non-Bearer returns null
    private static string? ParseTokenInAuthorizationHeader(IReadOnlyDictionary<string, string?> headers)
    {
        if (!headers.TryGetValue(AuthorizationHeaderName, out var value) || value is null) return null;
        if (!value.StartsWith(BearerPrefix, StringComparison.Ordinal)) return null;
        return value[BearerPrefix.Length..].Trim();
    }

    //ParseTokenInSecWebsocketProtocolHeader takes the key after "minecraft-v1," in the websocket subprotocol header
    private static string? ParseTokenInSecWebsocketProtocolHeader(IReadOnlyDictionary<string, string?> headers)
    {
        if (!headers.TryGetValue(SubProtocolHeaderName, out var value) || value is null) return null;
        if (!value.StartsWith(SubProtocolHeaderPrefix, StringComparison.Ordinal)) return null;
        return value[SubProtocolHeaderPrefix.Length..].Trim();
    }

    //IsAllowedOriginHeader a missing or empty Origin header counts as not allowed, maps to vanilla isAllowedOriginHeader
    private bool IsAllowedOriginHeader(IReadOnlyDictionary<string, string?> headers)
    {
        if (!headers.TryGetValue(OriginHeaderName, out var origin) || string.IsNullOrEmpty(origin)) return false;
        return _allowedOrigins.Contains(origin);
    }
}
