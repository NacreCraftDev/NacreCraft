namespace NacreCraft.Game.Gizmos;

//GizmoCollector receives gizmos created during one collection scope, maps to vanilla net.minecraft.gizmos.GizmoCollector
public interface GizmoCollector
{
    static readonly GizmoProperties Ignored = new IgnoredProperties();

    static readonly GizmoCollector Noop = new NoopCollector();

    GizmoProperties Add(Gizmo gizmo);

    private sealed class IgnoredProperties : GizmoProperties
    {
        public GizmoProperties SetAlwaysOnTop() => this;

        public GizmoProperties PersistForMillis(int milliseconds) => this;

        public GizmoProperties FadeOut() => this;
    }

    private sealed class NoopCollector : GizmoCollector
    {
        public GizmoProperties Add(Gizmo gizmo) => Ignored;
    }
}
