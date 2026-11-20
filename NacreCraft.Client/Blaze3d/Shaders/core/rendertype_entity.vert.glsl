#version 450

//RenderTypes entity shader, vanilla ENTITY vertex format
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
layout(location = 2) in vec2 UV0;
layout(location = 3) in ivec2 UV1;
layout(location = 4) in ivec2 UV2;
layout(location = 5) in vec4 Normal;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec2 fragUv;
layout(location = 2) out vec2 fragLight;
layout(location = 3) out vec2 fragOverlay;

void main() {
    vec3 pos = Position + ModelOffset;
    gl_Position = ProjMat * ModelViewMat * vec4(pos, 1.0);
    fragColor = Color * ColorModulator;
    fragUv = (TextureMat * vec4(UV0, 0.0, 1.0)).xy;
    fragLight = (vec2(UV2) + 0.5) / 16.0;
    fragOverlay = (vec2(UV1) + 0.5) / 16.0;
}
