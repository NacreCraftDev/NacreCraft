using NacreCraft.Network.Chat;

namespace NacreCraft.Client;

//PrioritizeChunkUpdates chunk update scheduling preference, maps to vanilla net.minecraft.client.PrioritizeChunkUpdates
public enum PrioritizeChunkUpdates
{
    None,
    PlayerAffected,
    Nearby
}

public static class PrioritizeChunkUpdatesExtensions
{
    //Id the numeric id persisted in options, maps to vanilla id
    public static int Id(this PrioritizeChunkUpdates value)
        => value switch
        {
            PrioritizeChunkUpdates.None => 0,
            PrioritizeChunkUpdates.PlayerAffected => 1,
            _ => 2
        };

    //Caption the translated label shown in the options screen
    public static Component Caption(this PrioritizeChunkUpdates value)
        => Component.Translatable(value switch
        {
            PrioritizeChunkUpdates.None => "options.prioritizeChunkUpdates.none",
            PrioritizeChunkUpdates.PlayerAffected => "options.prioritizeChunkUpdates.byPlayer",
            _ => "options.prioritizeChunkUpdates.nearby"
        });
}
