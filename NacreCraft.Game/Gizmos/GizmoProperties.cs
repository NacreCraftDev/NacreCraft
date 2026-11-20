namespace NacreCraft.Game.Gizmos;

//GizmoProperties per-gizmo display properties, maps to vanilla net.minecraft.gizmos.GizmoProperties
public interface GizmoProperties
{
    GizmoProperties SetAlwaysOnTop();

    GizmoProperties PersistForMillis(int milliseconds);

    GizmoProperties FadeOut();
}
