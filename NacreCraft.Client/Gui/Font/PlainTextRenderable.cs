using System.Numerics;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Gui.Font;

//PlainTextRenderable an axis-aligned textured glyph, maps to vanilla net.minecraft.client.gui.font.PlainTextRenderable
public interface PlainTextRenderable : TextRenderable.Styled
{
    const float DefaultWidth = 8.0f;
    const float DefaultHeight = 8.0f;
    const float DefaultAscent = 8.0f;

    void Render(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords, bool flat)
    {
        var frontDepth = 0.0f;
        if (ShadowColor() != 0)
        {
            RenderSprite(pose, buffer, packedLightCoords, ShadowOffset(), ShadowOffset(), 0.0f, ShadowColor());
            if (!flat) frontDepth += 0.03f;
        }

        RenderSprite(pose, buffer, packedLightCoords, 0.0f, 0.0f, frontDepth, Color());
    }

    void RenderSprite(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords,
        float offsetX, float offsetY, float z, int color);

    float X();

    float Y();

    int Color();

    int ShadowColor();

    float ShadowOffset();

    Style Style { get; }

    float Width() => DefaultWidth;

    float Height() => DefaultHeight;

    float Ascent() => DefaultAscent;

    float Left() => X();

    float Right() => Left() + Width();

    float Top() => Y() + 7.0f - Ascent();

    float Bottom() => Top() + Height();
}
