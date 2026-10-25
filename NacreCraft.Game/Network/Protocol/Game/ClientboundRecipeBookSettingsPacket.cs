namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundRecipeBookSettingsPacket recipe book settings packet, maps to vanilla ClientboundRecipeBookSettingsPacket
//Field: BookSettings(RecipeBookSettingsData)
public sealed record ClientboundRecipeBookSettingsPacket(object BookSettings) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundRecipeBookSettingsPacket> StreamCodec { get; } = new RecipeBookSettingsCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundRecipeBookSettings;

    public void Handle(ClientGamePacketListener handler) => handler.HandleRecipeBookSettings(this);

    private sealed class RecipeBookSettingsCodec : StreamCodec<FriendlyByteBuf, ClientboundRecipeBookSettingsPacket>
    {
        //Vanilla writes one open/filtering pair per book type in crafting, furnace, blast furnace, smoker order
        public ClientboundRecipeBookSettingsPacket Decode(FriendlyByteBuf buf)
            => new(new RecipeBookSettingsData(
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean(),
                buf.ReadBoolean()));

        public void Encode(FriendlyByteBuf buf, ClientboundRecipeBookSettingsPacket value)
        {
            var settings = value.BookSettings as RecipeBookSettingsData ?? RecipeBookSettingsData.Default;
            buf.WriteBoolean(settings.CraftingOpen);
            buf.WriteBoolean(settings.CraftingFiltering);
            buf.WriteBoolean(settings.FurnaceOpen);
            buf.WriteBoolean(settings.FurnaceFiltering);
            buf.WriteBoolean(settings.BlastFurnaceOpen);
            buf.WriteBoolean(settings.BlastFurnaceFiltering);
            buf.WriteBoolean(settings.SmokerOpen);
            buf.WriteBoolean(settings.SmokerFiltering);
        }
    }
}

//RecipeBookSettingsData open/filtering flags for each recipe book type
public sealed record RecipeBookSettingsData(
    bool CraftingOpen,
    bool CraftingFiltering,
    bool FurnaceOpen,
    bool FurnaceFiltering,
    bool BlastFurnaceOpen,
    bool BlastFurnaceFiltering,
    bool SmokerOpen,
    bool SmokerFiltering)
{
    public static readonly RecipeBookSettingsData Default = new(false, false, false, false, false, false, false, false);
}
