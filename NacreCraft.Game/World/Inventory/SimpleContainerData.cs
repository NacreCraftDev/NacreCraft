namespace NacreCraft.Game.World.Inventory;

//SimpleContainerData array backed data slots, maps to vanilla net.minecraft.world.inventory.SimpleContainerData
//Block entities that keep their numeric state in an array use it directly instead of implementing ContainerData
public sealed class SimpleContainerData(int count) : ContainerData
{
    private readonly int[] _values = new int[count];

    public int Count => _values.Length;

    public int Get(int index) => _values[index];

    public void Set(int index, int value) => _values[index] = value;
}
