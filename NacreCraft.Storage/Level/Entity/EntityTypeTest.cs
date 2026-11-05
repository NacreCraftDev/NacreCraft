namespace NacreCraft.Storage;

//EntityTypeTest narrows a base entity type to a queryable subtype, maps to vanilla net.minecraft.world.level.entity.EntityTypeTest
//Vanilla takes a java.lang.Class; the C# port uses the generic parameter as the class token
public interface EntityTypeTest<B, T> where B : class where T : B
{
    T? TryCast(B entity);

    Type GetBaseClass();
}

public static class EntityTypeTests
{
    //ForClass matches subclasses too, maps to vanilla forClass
    public static EntityTypeTest<B, T> ForClass<B, T>() where B : class where T : B => new ClassTest<B, T>();

    //ForExactClass matches only the concrete type, maps to vanilla forExactClass
    public static EntityTypeTest<B, T> ForExactClass<B, T>() where B : class where T : B => new ExactClassTest<B, T>();

    private sealed class ClassTest<B, T> : EntityTypeTest<B, T> where B : class where T : B
    {
        public T? TryCast(B entity) => entity is T typed ? typed : default;

        public Type GetBaseClass() => typeof(T);
    }

    private sealed class ExactClassTest<B, T> : EntityTypeTest<B, T> where B : class where T : B
    {
        public T? TryCast(B entity) => entity.GetType() == typeof(T) ? (T)entity : default;

        public Type GetBaseClass() => typeof(T);
    }
}
