using System.Globalization;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server;
using NacreCraft.Nbt;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Commands.Data;

//StorageDataAccessor command storage data access, maps to vanilla net.minecraft.server.commands.data.StorageDataAccessor
//The target is the key-value storage commands use themselves, unrelated to blocks/entities, often used to pass values between commands
public sealed class StorageDataAccessor : IDataAccessor
{
    private readonly CommandStorage _storage;
    private readonly Identifier _id;

    public StorageDataAccessor(CommandStorage storage, Identifier id)
    {
        _storage = storage;
        _id = id;
    }

    public void SetData(CompoundTag tag) => _storage.Set(_id, tag);

    public CompoundTag GetData() => _storage.Get(_id);

    public string ModifiedSuccess => $"Modified storage {_id}";

    //PrintSuccess matches the vanilla message commands.data.storage.query: "Storage <id> has the following contents: <snbt>"
    public string PrintSuccess(Tag data)
        => $"Storage {_id} has the following contents: {NbtUtils.ToSnbt(data)}";

    public string PrintSuccess(NbtPath path, double scale, int value)
        => $"{path} in storage {_id} after scale factor of "
            + $"{scale.ToString("0.00", CultureInfo.InvariantCulture)} is {value}";
}
