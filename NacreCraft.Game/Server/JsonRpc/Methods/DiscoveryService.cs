using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc;

//DiscoveryService builds the OpenRPC discovery document, maps to vanilla net.minecraft.server.jsonrpc.methods.DiscoveryService
public static class DiscoveryService
{
    public static DiscoverResponse Discover(IReadOnlyList<SchemaComponent> schemaRegistry)
    {
        SchemaPresets.EnsureInitialized();
        var methods = new List<NamedMethodInfo>();
        foreach (var id in BuiltInRegistries.INCOMING_RPC_METHOD.KeySet)
            if (BuiltInRegistries.INCOMING_RPC_METHOD.GetValue(id) is IIncomingRpcMethod incoming
                && incoming.Attributes.Discoverable)
                methods.Add(incoming.Info().Named(id));
        foreach (var id in BuiltInRegistries.OUTGOING_RPC_METHOD.KeySet)
            if (BuiltInRegistries.OUTGOING_RPC_METHOD.GetValue(id) is IOutgoingRpcMethod outgoing
                && outgoing.Attributes.Discoverable)
                methods.Add(outgoing.Info().Named(id));
        var schemas = schemaRegistry.ToDictionary(component => component.Name, component => component.Schema);
        return new DiscoverResponse("1.3.2", new DiscoverInfo("Minecraft Server JSON-RPC", "3.0.0"),
            methods, new DiscoverComponents(schemas));
    }

    public sealed record DiscoverInfo(string Title, string Version)
    {
        public static readonly Codec<DiscoverInfo> Codec = RecordCodecBuilder.Of2(
            Codecs.String.FieldOf("title").ForGetter((DiscoverInfo info) => info.Title),
            Codecs.String.FieldOf("version").ForGetter((DiscoverInfo info) => info.Version),
            (title, version) => new DiscoverInfo(title, version));
    }

    public sealed record DiscoverResponse(string JsonRpcProtocolVersion, DiscoverInfo DiscoverInfo,
        IReadOnlyList<NamedMethodInfo> Methods, DiscoverComponents Components)
    {
        public static readonly MapCodec<DiscoverResponse> CODEC = RecordCodecBuilder.Of4(
            Codecs.String.FieldOf("openrpc").ForGetter((DiscoverResponse response) => response.JsonRpcProtocolVersion),
            DiscoverInfo.Codec.FieldOf("info").ForGetter((DiscoverResponse response) => response.DiscoverInfo),
            NamedMethodInfo.Codec.ListOf().FieldOf("methods").ForGetter((DiscoverResponse response) => response.Methods),
            DiscoverComponents.Codec.FieldOf("components").ForGetter((DiscoverResponse response) => response.Components),
            (protocol, info, methods, components) => new DiscoverResponse(protocol, info, methods, components));
    }

    public sealed record DiscoverComponents(IReadOnlyDictionary<string, SchemaInfo> Schemas)
    {
        public static readonly Codec<DiscoverComponents> Codec =
            Codecs.UnboundedMap(Codecs.String, SchemaInfo.Codec).ComapFlatMap(
                map => DataResult<DiscoverComponents>.Success(new DiscoverComponents(map)),
                components => components.Schemas.ToDictionary(pair => pair.Key, pair => pair.Value));
    }
}
