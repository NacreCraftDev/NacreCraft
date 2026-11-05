using NacreCraft.Codec;
using NacreCraft.Game.Data;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRpcApiSchema writes the OpenRPC discovery document, maps to vanilla net.minecraft.server.jsonrpc.dataprovider.JsonRpcApiSchema
public sealed class JsonRpcApiSchema : DataProvider
{
    private readonly string _path;

    public JsonRpcApiSchema(PackOutput packOutput)
        => _path = Path.Combine(packOutput.GetOutputFolder(PackOutput.Target.REPORTS), "json-rpc-api-schema.json");

    public Task Run(CachedOutput cache)
    {
        var discover = DiscoveryService.Discover(SchemaComponents.All());
        var json = DiscoveryService.DiscoverResponse.CODEC.EncodeStart(JsonOps.Instance, discover)
            .Result()
            .OrElse(null);
        return DataProvider.SaveStable(cache, json, _path);
    }

    public string GetName() => "Json RPC API schema";
}
