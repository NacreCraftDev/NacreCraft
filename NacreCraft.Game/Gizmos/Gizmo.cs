namespace NacreCraft.Game.Gizmos;

//Gizmo one debug shape that can emit its primitives, maps to vanilla net.minecraft.gizmos.Gizmo
public interface Gizmo
{
    void Emit(GizmoPrimitives primitives, float alphaMultiplier);
}
