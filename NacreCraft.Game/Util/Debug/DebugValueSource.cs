using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugValueSource debug value provider, maps to vanilla net.minecraft.util.debug.DebugValueSource
//Chunks, block entities and entities implement it to feed values into the level synchronizers
public interface DebugValueSource
{
    void RegisterDebugValues(NacreCraft.Storage.ServerLevel level, Registration registration);

    //ValueGetter reads the current value, null when there is nothing to report
    public interface ValueGetter<T>
    {
        T? Get();
    }

    //Registration collects the values a source exposes
    public interface Registration
    {
        void Register<T>(DebugSubscription<T> subscription, ValueGetter<T> getter);
    }
}
