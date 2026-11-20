#version 450

layout(set = 1, binding = 0) uniform sampler2D Sampler2;

layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragLight;

layout(location = 0) out vec4 outColor;

void main() {
    vec3 light = texture(Sampler2, fragLight).rgb;
    outColor = vec4(fragColor.rgb * light, fragColor.a);
}
