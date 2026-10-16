namespace NacreCraft.Resources;

//IRepositorySource a place packs are discovered from, maps to vanilla net.minecraft.server.packs.repository.RepositorySource
//The built-in packs, the user pack folder and server downloads each get their own implementation
public interface IRepositorySource
{
    //LoadPacks reports every pack this source can offer, the consumer decides what to keep
    void LoadPacks(Action<Pack> consumer);
}
