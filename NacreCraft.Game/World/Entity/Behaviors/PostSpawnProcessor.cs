namespace NacreCraft.Game.World.Entity;

//PostSpawnProcessor a hook applied after an entity spawns, maps to vanilla net.minecraft.world.entity.PostSpawnProcessor
//An abstract class instead of an interface because the vanilla andThen/nop helpers build chained processor instances
public abstract class PostSpawnProcessor<T> where T : NacreCraft.Registry.Entity
{
    public abstract void Apply(T target);

    //AndThen runs this processor then the next one, maps to vanilla andThen
    public PostSpawnProcessor<T> AndThen(PostSpawnProcessor<T> after) => new Chained(this, after);

    //Nop a processor that does nothing, maps to vanilla nop
    public static PostSpawnProcessor<T> Nop() => new NopProcessor();

    private sealed class NopProcessor : PostSpawnProcessor<T>
    {
        public override void Apply(T target)
        {
        }
    }

    private sealed class Chained(PostSpawnProcessor<T> first, PostSpawnProcessor<T> second) : PostSpawnProcessor<T>
    {
        public override void Apply(T target)
        {
            first.Apply(target);
            second.Apply(target);
        }
    }
}
