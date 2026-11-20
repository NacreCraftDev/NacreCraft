#version 450

//RenderTypes glint shader, vanilla POSITION_TEX format
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
layout(location = 1) in vec2 UV0;

layout(location = 0) out vec2 fragUv;

void main() {
    vec3 pos = Position + ModelOffset;
    gl_Position = ProjMat * ModelViewMat * vec4(pos, 1.0);
    fragUv = (TextureMat * vec4(UV0, 0.0, 1.0)).xy;
}
