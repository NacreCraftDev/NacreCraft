using NacreCraft.Client.RealmsClient.Client;

namespace NacreCraft.Client.RealmsClient.Exception;

//RetryCallException asks the caller to retry after a delay, maps to com.mojang.realmsclient.exception.RetryCallException
public sealed class RetryCallException : RealmsServiceException
{
    public const int DefaultDelay = 5;

    public int DelaySeconds { get; }

    public RetryCallException(int delaySeconds, int statusCode)
        : base(RealmsError.CustomError.Retry(statusCode))
        => DelaySeconds = delaySeconds < 0 || delaySeconds > 120 ? DefaultDelay : delaySeconds;
}
