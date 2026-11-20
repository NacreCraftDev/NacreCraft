#version 450

layout(set = 1, binding = 0) uniform sampler2D Sampler0;
layout(set = 1, binding = 1) uniform sampler2D Sampler1;
layout(set = 1, binding = 2) uniform sampler2D Sampler2;

layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragUv;
layout(location = 2) in vec2 fragLight;

layout(location = 0) out vec4 outColor;

void main() {
    vec4 tex = texture(Sampler0, fragUv);
    vec3 light = texture(Sampler2, fragLight).rgb;
    vec4 color = vec4(tex.rgb * fragColor.rgb * light, tex.a * fragColor.a);
#ifdef ALPHA_CUTOUT
    if (color.a < 0.1) discard;
#endif
    outColor = color;
}
