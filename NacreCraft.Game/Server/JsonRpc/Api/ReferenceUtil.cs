using NacreCraft.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//ReferenceUtil helpers for JSON schema references, maps to vanilla net.minecraft.server.jsonrpc.api.ReferenceUtil
public static class ReferenceUtil
{
    public static readonly Codec<Uri> REFERENCE_CODEC = Codecs.String.ComapFlatMap(
        text => Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out var uri)
            ? DataResult<Uri>.Success(uri)
            : DataResult<Uri>.Error(() => $"Invalid URI: {text}"),
        uri => uri.ToString());

    public static Uri CreateLocalReference(string typeId) => new($"#/components/schemas/{typeId}", UriKind.Relative);
}
