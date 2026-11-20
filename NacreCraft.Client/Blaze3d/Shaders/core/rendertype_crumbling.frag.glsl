#version 450

layout(set = 1, binding = 0) uniform sampler2D Sampler0;

layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragUv;

layout(location = 0) out vec4 outColor;

void main() {
    vec4 tex = texture(Sampler0, fragUv);
    vec4 color = tex * fragColor;
    if (color.a < 0.1) discard;
    outColor = color;
}
