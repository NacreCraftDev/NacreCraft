using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundUpdateAdvancementsPacket advancements update packet, maps to vanilla ClientboundUpdateAdvancementsPacket
//Fields: Reset(boolean), Added(List<AdvancementData>), Removed(List<Identifier>)
//Progress(Map<Identifier, List<CriterionProgressData>>), ShowAdvancements(boolean)
public sealed record ClientboundUpdateAdvancementsPacket(bool Reset, object Added, object Removed, object Progress, bool ShowAdvancements) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundUpdateAdvancementsPacket> StreamCodec { get; } = new UpdateAdvancementsCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundUpdateAdvancements;

    public void Handle(ClientGamePacketListener handler) => handler.HandleUpdateAdvancementsPacket(this);

    private sealed class UpdateAdvancementsCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundUpdateAdvancementsPacket>
    {
        public ClientboundUpdateAdvancementsPacket Decode(RegistryFriendlyByteBuf buf)
        {
            var reset = buf.ReadBoolean();
            var addedCount = buf.ReadVarInt();
            var added = new List<AdvancementData>(addedCount);
            for (var i = 0; i < addedCount; i++)
            {
                var id = buf.ReadIdentifier();
                added.Add(ReadAdvancement(id, buf));
            }
            var removedCount = buf.ReadVarInt();
            var removed = new List<Identifier>(removedCount);
            for (var i = 0; i < removedCount; i++) removed.Add(buf.ReadIdentifier());
            var progressCount = buf.ReadVarInt();
            var progress = new Dictionary<Identifier, List<CriterionProgressData>>(progressCount);
            for (var i = 0; i < progressCount; i++)
            {
                var id = buf.ReadIdentifier();
                progress[id] = ReadProgress(buf);
            }
            var showAdvancements = buf.ReadBoolean();
            return new ClientboundUpdateAdvancementsPacket(reset, added, removed, progress, showAdvancements);
        }

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundUpdateAdvancementsPacket value)
        {
            buf.WriteBoolean(value.Reset);
            var added = value.Added as IReadOnlyList<AdvancementData> ?? Array.Empty<AdvancementData>();
            buf.WriteVarInt(added.Count);
            foreach (var advancement in added)
            {
                buf.WriteIdentifier(advancement.Id);
                WriteAdvancement(buf, advancement);
            }
            var removed = value.Removed as IReadOnlyList<Identifier> ?? Array.Empty<Identifier>();
            buf.WriteVarInt(removed.Count);
            foreach (var id in removed) buf.WriteIdentifier(id);
            var progress = value.Progress as IReadOnlyDictionary<Identifier, List<CriterionProgressData>>
                ?? new Dictionary<Identifier, List<CriterionProgressData>>();
            buf.WriteVarInt(progress.Count);
            foreach (var pair in progress)
            {
                buf.WriteIdentifier(pair.Key);
                WriteProgress(buf, pair.Value);
            }
            buf.WriteBoolean(value.ShowAdvancements);
        }

        //Advancement: optional parent, optional display, requirements, telemetry flag
        private static AdvancementData ReadAdvancement(Identifier id, RegistryFriendlyByteBuf buf)
        {
            Identifier? parent = buf.ReadBoolean() ? buf.ReadIdentifier() : null;
            var display = (AdvancementDisplayData?)null;
            if (buf.ReadBoolean()) display = ReadDisplay(buf);
            var requirementCount = buf.ReadVarInt();
            var requirements = new List<List<string>>(requirementCount);
            for (var i = 0; i < requirementCount; i++)
            {
                var groupCount = buf.ReadVarInt();
                var group = new List<string>(groupCount);
                for (var j = 0; j < groupCount; j++) group.Add(buf.ReadString());
                requirements.Add(group);
            }
            var sendsTelemetryEvent = buf.ReadBoolean();
            return new AdvancementData(id, parent, display, requirements, sendsTelemetryEvent);
        }

        private static void WriteAdvancement(RegistryFriendlyByteBuf buf, AdvancementData advancement)
        {
            buf.WriteBoolean(advancement.Parent is not null);
            if (advancement.Parent is Identifier parent) buf.WriteIdentifier(parent);
            buf.WriteBoolean(advancement.Display is not null);
            if (advancement.Display is AdvancementDisplayData display) WriteDisplay(buf, display);
            buf.WriteVarInt(advancement.Requirements.Count);
            foreach (var group in advancement.Requirements)
            {
                buf.WriteVarInt(group.Count);
                foreach (var criterion in group) buf.WriteString(criterion);
            }
            buf.WriteBoolean(advancement.SendsTelemetryEvent);
        }

        //DisplayInfo: title, description, icon template, frame enum, flags int, optional background, then the canvas position
        private static AdvancementDisplayData ReadDisplay(RegistryFriendlyByteBuf buf)
        {
            var title = ComponentSerialization.StreamCodec.Decode(buf);
            var description = ComponentSerialization.StreamCodec.Decode(buf);
            var icon = ItemStackTemplate.StreamCodec.Decode(buf);
            var frame = buf.ReadVarInt();
            var flags = buf.ReadInt();
            Identifier? background = (flags & 1) != 0 ? buf.ReadIdentifier() : null;
            var showToast = (flags & 2) != 0;
            var hidden = (flags & 4) != 0;
            var x = buf.ReadFloat();
            var y = buf.ReadFloat();
            return new AdvancementDisplayData(title, description, icon, frame, showToast, hidden, background, x, y);
        }

        private static void WriteDisplay(RegistryFriendlyByteBuf buf, AdvancementDisplayData display)
        {
            ComponentSerialization.StreamCodec.Encode(buf, display.Title);
            ComponentSerialization.StreamCodec.Encode(buf, display.Description);
            ItemStackTemplate.StreamCodec.Encode(buf, display.Icon);
            buf.WriteVarInt(display.Frame);
            var flags = 0;
            if (display.Background is not null) flags |= 1;
            if (display.ShowToast) flags |= 2;
            if (display.Hidden) flags |= 4;
            buf.WriteInt(flags);
            if (display.Background is Identifier background) buf.WriteIdentifier(background);
            buf.WriteFloat(display.X);
            buf.WriteFloat(display.Y);
        }

        //AdvancementProgress: criterion name to an optional obtained timestamp
        private static List<CriterionProgressData> ReadProgress(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var criteria = new List<CriterionProgressData>(count);
            for (var i = 0; i < count; i++)
            {
                var criterion = buf.ReadString();
                long? obtained = buf.ReadBoolean() ? buf.ReadLong() : null;
                criteria.Add(new CriterionProgressData(criterion, obtained));
            }
            return criteria;
        }

        private static void WriteProgress(FriendlyByteBuf buf, IReadOnlyList<CriterionProgressData> criteria)
        {
            buf.WriteVarInt(criteria.Count);
            foreach (var criterion in criteria)
            {
                buf.WriteString(criterion.Criterion);
                buf.WriteBoolean(criterion.ObtainedMillis.HasValue);
                if (criterion.ObtainedMillis is long obtained) buf.WriteLong(obtained);
            }
        }
    }
}

//AdvancementData one advancement definition
public sealed record AdvancementData(
    Identifier Id,
    Identifier? Parent,
    AdvancementDisplayData? Display,
    List<List<string>> Requirements,
    bool SendsTelemetryEvent);

//AdvancementDisplayData the advancement tab entry; Frame is the AdvancementType ordinal and X/Y the canvas position
public sealed record AdvancementDisplayData(
    Component Title,
    Component Description,
    ItemStackTemplate Icon,
    int Frame,
    bool ShowToast,
    bool Hidden,
    Identifier? Background,
    float X,
    float Y);

//CriterionProgressData one criterion and its obtained timestamp in epoch millis, null when still locked
public sealed record CriterionProgressData(string Criterion, long? ObtainedMillis);
