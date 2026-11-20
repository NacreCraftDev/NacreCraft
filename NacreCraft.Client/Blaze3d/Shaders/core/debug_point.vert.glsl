#version 450

//RenderTypes debug point shader, vanilla POSITION_COLOR_LINE_WIDTH format
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
layout(location = 2) in float LineWidth;

layout(location = 0) out vec4 fragColor;

void main() {
    vec3 pos = Position + ModelOffset;
    gl_Position = ProjMat * ModelViewMat * vec4(pos, 1.0);
    gl_PointSize = max(1.0, LineWidth);
    fragColor = Color * ColorModulator;
}
