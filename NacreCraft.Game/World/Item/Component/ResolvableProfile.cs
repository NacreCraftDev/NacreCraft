using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Game.World.Items.Component;

//ResolvableProfile a profile reference that can be resolved to a game profile, maps to vanilla net.minecraft.world.item.component.ResolvableProfile
//NC has no skin/remote profile resolution, so the record just carries the name and uuid
public sealed record ResolvableProfile(string Name, Guid Id)
{
    public static ResolvableProfile Of(GameProfile profile) => new(profile.Name, profile.Id);

    public GameProfile ToGameProfile() => new(Id, Name);
}
