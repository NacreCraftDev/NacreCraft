using NacreCraft.Network.Chat;

namespace NacreCraft.Client;

//CloudStatus cloud rendering mode, maps to vanilla net.minecraft.client.CloudStatus
public enum CloudStatus
{
    Off,
    Fast,
    Fancy
}

public static class CloudStatusExtensions
{
    //GetSerializedName the legacy options.txt value, maps to vanilla getSerializedName
    public static string GetSerializedName(this CloudStatus status)
        => status switch
        {
            CloudStatus.Off => "false",
            CloudStatus.Fast => "fast",
            _ => "true"
        };

    //Caption the translated label shown in the options screen
    public static Component Caption(this CloudStatus status)
        => Component.Translatable(status switch
        {
            CloudStatus.Off => "options.off",
            CloudStatus.Fast => "options.clouds.fast",
            _ => "options.clouds.fancy"
        });
}
