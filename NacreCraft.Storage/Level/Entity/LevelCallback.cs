namespace NacreCraft.Storage;

//LevelCallback receives an entity's lifecycle from the section manager, maps to vanilla net.minecraft.world.level.entity.LevelCallback
public interface LevelCallback<T>
{
    void OnCreated(T entity);

    void OnDestroyed(T entity);

    void OnTickingStart(T entity);

    void OnTickingEnd(T entity);

    void OnTrackingStart(T entity);

    void OnTrackingEnd(T entity);

    void OnSectionChange(T entity);
}
