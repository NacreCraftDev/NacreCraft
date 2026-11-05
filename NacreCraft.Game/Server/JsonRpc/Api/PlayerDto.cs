using NacreCraft.Codec;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc;

//PlayerDto a player reference in RPC payloads, maps to vanilla net.minecraft.server.jsonrpc.api.PlayerDto
public sealed record PlayerDto(Optional<Guid> Id, Optional<string> Name)
{
    private static readonly Codec<Guid> UuidCodec = Codecs.String.ComapFlatMap(
        text => Guid.TryParse(text, out var id)
            ? DataResult<Guid>.Success(id)
            : DataResult<Guid>.Error(() => $"Invalid UUID: {text}"),
        id => id.ToString());

    public static readonly MapCodec<PlayerDto> CODEC = RecordCodecBuilder.Of2(
        UuidCodec.OptionalFieldOf("id").ForGetter((PlayerDto dto) => dto.Id),
        Codecs.String.OptionalFieldOf("name").ForGetter((PlayerDto dto) => dto.Name),
        (id, name) => new PlayerDto(id, name));

    public static PlayerDto From(GameProfile profile)
        => new(Optional<Guid>.Of(profile.Id), Optional<string>.Of(profile.Name));

    public static PlayerDto From(NameAndId nameAndId)
        => new(Guid.TryParse(nameAndId.Id, out var id) ? Optional<Guid>.Of(id) : Optional<Guid>.Empty(),
            Optional<string>.Of(nameAndId.Name));

    public static PlayerDto From(ServerPlayer player)
        => new(Optional<Guid>.Of(player.Profile.Id), Optional<string>.Of(player.Profile.Name));
}
