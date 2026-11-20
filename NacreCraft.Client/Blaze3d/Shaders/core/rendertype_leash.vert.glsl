#version 450

//RenderTypes leash shader, vanilla POSITION_COLOR_LIGHTMAP format
layout(set = 0, binding = 0) uniform DynamicTransforms {
    mat4 ModelViewMat;
    vec4 ColorModulator;
    vec3 ModelOffset;
    mat4 TextureMat;
};

layout(set = 0, binding = 1) uniform Projection {
    mat4 ProjMat;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in ivec2 UV2;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec2 fragLight;

void main() {
    vec3 pos = Position + ModelOffset;
    gl_Position = ProjMat * ModelViewMat * vec4(pos, 1.0);
    fragColor = Color * ColorModulator;
    fragLight = (vec2(UV2) + 0.5) / 16.0;
}
