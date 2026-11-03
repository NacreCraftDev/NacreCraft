using System.Text.Json;
using NacreCraft.Logging;
using NacreCraft.Network.Chat;
//NacreCraft.Client.Language is a namespace here, so the chat language table needs an alias to stay reachable
using ChatLanguage = NacreCraft.Network.Chat.Language;

namespace NacreCraft.Client.RealmsClient.Client;

//RealmsError a Realms service failure, maps to com.mojang.realmsclient.client.RealmsError
public interface RealmsError
{
    public static readonly Component NoMessage = Component.Translatable("mco.errorMessage.noDetails");

    int ErrorCode { get; }

    Component ErrorMessage { get; }

    string LogMessage { get; }

    //Parse reads the service payload, maps to vanilla RealmsError.parse
    public static RealmsError Parse(int httpCode, string? payload)
    {
        if (httpCode == 429) return CustomError.ServiceBusy;
        if (string.IsNullOrEmpty(payload)) return CustomError.NoPayload(httpCode);
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                var reason = ReadString(root, "reason");
                var message = ReadString(root, "errorMsg");
                var errorCode = root.TryGetProperty("errorCode", out var codeElement) && codeElement.TryGetInt32(out var parsed)
                    ? parsed
                    : -1;
                if (message is not null || reason is not null || errorCode != -1)
                    return new ErrorWithJsonPayload(httpCode, errorCode != -1 ? errorCode : httpCode, reason, message);
            }
        }
        catch (JsonException ex)
        {
            Log.Error($"Could not parse RealmsError: {ex.Message}");
        }
        return new ErrorWithRawPayload(httpCode, payload);
    }

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    //CustomError an error the client made up itself, maps to vanilla RealmsError.CustomError
    public sealed record CustomError(int HttpCode, Component? Payload) : RealmsError
    {
        public static readonly CustomError ServiceBusy =
            new(429, Component.Translatable("mco.errorMessage.serviceBusy"));

        public static readonly Component RetryMessage = Component.Translatable("mco.errorMessage.retry");

        private const string BodyTag = "<body>";
        private const string ClosingBodyTag = "</body>";

        public static CustomError UnknownCompatibilityResponse(string response)
            => new(500, Component.Translatable("mco.errorMessage.realmsService.unknownCompatibility", response));

        public static CustomError ConfigurationError()
            => new(500, Component.Translatable("mco.errorMessage.realmsService.configurationError"));

        public static CustomError ConnectivityError(System.Exception exception)
            => new(500, Component.Translatable("mco.errorMessage.realmsService.connectivity", exception.Message));

        public static CustomError Retry(int statusCode) => new(statusCode, RetryMessage);

        public static CustomError NoPayload(int statusCode) => new(statusCode, null);

        public static CustomError HtmlPayload(int statusCode, string payload)
        {
            var bodyStart = payload.IndexOf(BodyTag, StringComparison.Ordinal);
            var bodyEnd = payload.IndexOf(ClosingBodyTag, StringComparison.Ordinal);
            if (bodyStart >= 0 && bodyEnd > bodyStart)
                return new CustomError(statusCode, Component.Literal(payload[(bodyStart + BodyTag.Length)..bodyEnd].Trim()));
            Log.Error($"Got an error with an unreadable html body {payload}");
            return new CustomError(statusCode, null);
        }

        public int ErrorCode => HttpCode;

        public Component ErrorMessage => Payload ?? NoMessage;

        public string LogMessage => Payload is not null
            ? $"Realms service error ({HttpCode}) with message '{Payload.GetString()}'"
            : $"Realms service error ({HttpCode}) with no payload";
    }

    //ErrorWithJsonPayload a payload carrying reason/errorMsg/errorCode, maps to vanilla RealmsError.ErrorWithJsonPayload
    public sealed record ErrorWithJsonPayload(int HttpCode, int Code, string? Reason, string? Message) : RealmsError
    {
        public int ErrorCode => Code;

        public Component ErrorMessage
        {
            get
            {
                var codeKey = "mco.errorMessage." + Code;
                if (ChatLanguage.Instance.Has(codeKey)) return Component.Translatable(codeKey);
                if (Reason is not null)
                {
                    var reasonKey = "mco.errorReason." + Reason;
                    if (ChatLanguage.Instance.Has(reasonKey)) return Component.Translatable(reasonKey);
                }
                return Message is not null ? Component.Literal(Message) : NoMessage;
            }
        }

        public string LogMessage
            => $"Realms service error ({HttpCode}/{Code}/{Reason}) with message '{Message}'";
    }

    //ErrorWithRawPayload an unparseable payload shown verbatim, maps to vanilla RealmsError.ErrorWithRawPayload
    public sealed record ErrorWithRawPayload(int HttpCode, string Payload) : RealmsError
    {
        public int ErrorCode => HttpCode;

        public Component ErrorMessage => Component.Literal(Payload);

        public string LogMessage => $"Realms service error ({HttpCode}) with raw payload '{Payload}'";
    }

    //AuthenticationError an authentication failure, maps to vanilla RealmsError.AuthenticationError
    public sealed record AuthenticationError(string Message) : RealmsError
    {
        public const int ErrorCodeValue = 401;

        public int ErrorCode => ErrorCodeValue;

        public Component ErrorMessage => Component.Literal(Message);

        public string LogMessage => $"Realms authentication error with message '{Message}'";
    }
}
