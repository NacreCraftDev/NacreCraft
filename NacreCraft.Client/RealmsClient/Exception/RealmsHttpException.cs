namespace NacreCraft.Client.RealmsClient.Exception;

//RealmsHttpException a transport level failure talking to the Realms service
//maps to com.mojang.realmsclient.exception.RealmsHttpException, which vanilla derives from RuntimeException
public class RealmsHttpException(string message, System.Exception innerException)
    : System.Exception(message, innerException);
