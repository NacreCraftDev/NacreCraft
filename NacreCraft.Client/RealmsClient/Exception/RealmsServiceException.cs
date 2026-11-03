using NacreCraft.Client.RealmsClient.Client;

namespace NacreCraft.Client.RealmsClient.Exception;

//RealmsServiceException a failed Realms service call, maps to com.mojang.realmsclient.exception.RealmsServiceException
public class RealmsServiceException(RealmsError error) : System.Exception
{
    public RealmsError RealmsError { get; } = error;

    public override string Message => RealmsError.LogMessage;
}
