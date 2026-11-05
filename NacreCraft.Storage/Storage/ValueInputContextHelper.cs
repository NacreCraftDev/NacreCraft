using NacreCraft.Codec;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//ValueInputContextHelper maps to vanilla net.minecraft.world.level.storage.ValueInputContextHelper
//Bundles the registry lookup, the ops used for parsing and the shared empty ValueInput
//Degraded: RegistryAccess has no createSerializationContext, so ops are stored as given instead of being wrapped with the lookup
public sealed class ValueInputContextHelper
{
    private readonly RegistryAccess _lookup;
    private readonly DynamicOps<Tag> _ops;
    private readonly ValueInput _empty;

    public ValueInputContextHelper(RegistryAccess lookup, DynamicOps<Tag> ops)
    {
        _lookup = lookup;
        _ops = ops;
        _empty = new EmptyValueInput(lookup);
    }

    public DynamicOps<Tag> Ops() => _ops;

    public RegistryAccess Lookup() => _lookup;

    public ValueInput Empty() => _empty;

    //EmptyValueInput answers every read with the caller's default and has no children, maps to the anonymous empty input
    private sealed class EmptyValueInput : ValueInput
    {
        private readonly RegistryAccess _lookup;

        public EmptyValueInput(RegistryAccess lookup) => _lookup = lookup;

        public T? Read<T>(string name, Codec<T> codec) => default;

        public ValueInput? Child(string name) => null;

        public ValueInput ChildOrEmpty(string name) => this;

        public IReadOnlyList<ValueInput>? ChildrenList(string name) => null;

        public IReadOnlyList<ValueInput> ChildrenListOrEmpty(string name) => Array.Empty<ValueInput>();

        public IReadOnlyList<T>? List<T>(string name, Codec<T> codec) => null;

        public IReadOnlyList<T> ListOrEmpty<T>(string name, Codec<T> codec) => Array.Empty<T>();

        public bool GetBooleanOr(string name, bool defaultValue) => defaultValue;

        public byte GetByteOr(string name, byte defaultValue) => defaultValue;

        public int GetShortOr(string name, short defaultValue) => defaultValue;

        public int? GetInt(string name) => null;

        public int GetIntOr(string name, int defaultValue) => defaultValue;

        public long GetLongOr(string name, long defaultValue) => defaultValue;

        public long? GetLong(string name) => null;

        public float GetFloatOr(string name, float defaultValue) => defaultValue;

        public double GetDoubleOr(string name, double defaultValue) => defaultValue;

        public string? GetString(string name) => null;

        public string GetStringOr(string name, string defaultValue) => defaultValue;

        public int[]? GetIntArray(string name) => null;

        public RegistryAccess Lookup() => _lookup;

        public bool IsEmpty() => true;
    }
}
