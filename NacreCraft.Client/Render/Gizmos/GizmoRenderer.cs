using NacreCraft.Game.Gizmos;

namespace NacreCraft.Client.Render.Gizmos;

//GizmoRenderer bridges collected NC gizmos into the RenderType batcher, a NacreCraft stand-in for
//net.minecraft.client.renderer.feature.GizmoFeatureRenderer until the submit-node feature pipeline is ported
public static class GizmoRenderer
{
    //Draw emits every live gizmo into a temporary primitive sink and records it into the open world pass
    public static void Draw(SimpleGizmoCollector collector, MultiBufferSource.BufferSource buffers)
    {
        var gizmos = collector.DrainGizmos();
        if (gizmos.Count == 0) return;

        var primitives = new DrawableGizmoPrimitives();
        var now = Environment.TickCount64;
        foreach (var instance in gizmos)
        {
            if (instance.ExpireTimeMillis > 0 && instance.ExpireTimeMillis < now) continue;
            instance.GizmoValue.Emit(primitives, instance.GetAlphaMultiplier(now));
        }

        primitives.Draw(buffers);
    }
}
