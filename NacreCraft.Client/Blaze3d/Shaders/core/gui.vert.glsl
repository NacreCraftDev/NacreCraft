#version 450

//Globals 全局 uniform 块 set 0
layout(set = 0, binding = 0) uniform Globals {
    mat4 ScreenSize;
};

//DynamicTransforms 与 Projection 使用原版块名与顺序 set 1 binding 0/1
layout(set = 1, binding = 0) uniform DynamicTransforms {
    mat4 ModelViewMat;
    vec4 ColorModulator;
    vec3 ModelOffset;
    mat4 TextureMat;
};

layout(set = 1, binding = 1) uniform Projection {
    mat4 ProjMat;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fragColor;

void main() {
    vec3 pos = Position + ModelOffset;
    gl_Position = ProjMat * ModelViewMat * vec4(pos, 1.0);
    fragColor = Color * ColorModulator;
}
