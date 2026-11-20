using System.Numerics;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.Rendertype;

namespace NacreCraft.Client.Render.Entity;

//DebugQuadRenderer draws one textured world-space quad through RenderTypes.ItemCutout
//It exists for the NC_DEBUG_ENTITIES diagnostic to exercise the RenderType entity batching and sampler path
internal sealed class DebugQuadRenderer : EntityRenderer
{
    public override EntityModel Model { get; } = new EmptyModel();

    public override RenderType RenderType(EntityRenderState state) => RenderTypes.ItemCutout(RenderTypes.BlockAtlasLocation);

    public override void Render(PoseStack poseStack, EntityVertexBuilder builder, EntityRenderState state)
    {
        builder.AddQuad(
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            unchecked((int)0xFFFF4040), 0f, 0f, 1f, 1f, 0, 0xF000F0, Vector3.UnitZ);
    }

    private sealed class EmptyModel : EntityModel
    {
        public EmptyModel() : base(new ModelPart(new List<ModelPart.Cube>(), new Dictionary<string, ModelPart>()))
        {
        }
    }
}
