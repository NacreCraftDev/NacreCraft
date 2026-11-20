namespace NacreCraft.Game.World.Entity;

//InsideBlockEffectApplier collects inside-block effects for one tick and runs them in a stable order, maps to vanilla net.minecraft.world.entity.InsideBlockEffectApplier
//Effects queued before/after a type run around the type's own effect; the collector flushes in enum declaration order
public interface InsideBlockEffectApplier
{
    static readonly InsideBlockEffectApplier Noop = new NoopApplier();

    void Apply(InsideBlockEffectType type);

    void RunBefore(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect);

    void RunAfter(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect);

    //StepBasedCollector the per-tick collector, maps to vanilla StepBasedCollector
    public sealed class StepBasedCollector : InsideBlockEffectApplier
    {
        private static readonly InsideBlockEffectType[] ApplyOrder = Enum.GetValues<InsideBlockEffectType>();

        private readonly HashSet<InsideBlockEffectType> _effectsInStep = new();
        private readonly Dictionary<InsideBlockEffectType, List<Action<NacreCraft.Registry.Entity>>> _beforeEffectsInStep = NewLists();
        private readonly Dictionary<InsideBlockEffectType, List<Action<NacreCraft.Registry.Entity>>> _afterEffectsInStep = NewLists();
        private readonly List<Action<NacreCraft.Registry.Entity>> _finalEffects = new();
        private int _lastStep = -1;

        //AdvanceStep flushes the previous step's effects when the step number changes
        public void AdvanceStep(int step)
        {
            if (_lastStep == step) return;
            _lastStep = step;
            FlushStep();
        }

        //ApplyAndClear runs the collected effects and resets the collector, stopping when the entity dies
        public void ApplyAndClear(NacreCraft.Registry.Entity entity)
        {
            FlushStep();
            foreach (var effect in _finalEffects)
            {
                if (entity.IsRemoved || entity is LivingEntity { IsDeadOrDying: true }) break;
                effect(entity);
            }
            _finalEffects.Clear();
            _lastStep = -1;
        }

        private void FlushStep()
        {
            foreach (var type in ApplyOrder)
            {
                var before = _beforeEffectsInStep[type];
                _finalEffects.AddRange(before);
                before.Clear();
                if (_effectsInStep.Remove(type)) _finalEffects.Add(entity => type.ApplyEffect(entity));
                var after = _afterEffectsInStep[type];
                _finalEffects.AddRange(after);
                after.Clear();
            }
        }

        public void Apply(InsideBlockEffectType type) => _effectsInStep.Add(type);

        public void RunBefore(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect)
            => _beforeEffectsInStep[type].Add(effect);

        public void RunAfter(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect)
            => _afterEffectsInStep[type].Add(effect);

        private static Dictionary<InsideBlockEffectType, List<Action<NacreCraft.Registry.Entity>>> NewLists()
        {
            var map = new Dictionary<InsideBlockEffectType, List<Action<NacreCraft.Registry.Entity>>>();
            foreach (var type in ApplyOrder) map[type] = new List<Action<NacreCraft.Registry.Entity>>();
            return map;
        }
    }

    private sealed class NoopApplier : InsideBlockEffectApplier
    {
        public void Apply(InsideBlockEffectType type)
        {
        }

        public void RunBefore(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect)
        {
        }

        public void RunAfter(InsideBlockEffectType type, Action<NacreCraft.Registry.Entity> effect)
        {
        }
    }
}
