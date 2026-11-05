using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//LevelEntityGetter the read side of the entity index, maps to vanilla net.minecraft.world.level.entity.LevelEntityGetter
public interface LevelEntityGetter<T> where T : class
{
    T? Get(int id);

    T? Get(Guid id);

    IEnumerable<T> GetAll();

    void Get<U>(EntityTypeTest<T, U> type, AbortableIterationConsumer<U> consumer) where U : T;

    void Get(AABB box, Action<T> output);

    void Get<U>(EntityTypeTest<T, U> type, AABB box, AbortableIterationConsumer<U> consumer) where U : T;
}
