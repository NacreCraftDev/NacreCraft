#version 450

layout(set = 1, binding = 0) uniform sampler2D Sampler0;
layout(set = 1, binding = 1) uniform sampler2D Sampler1;

layout(location = 0) out vec4 outColor;

void main() {
    vec2 uv = gl_FragCoord.xy / 256.0;
    vec4 sky = texture(Sampler0, uv);
    vec4 portal = texture(Sampler1, uv * 1.7);
    outColor = vec4(sky.rgb + portal.rgb * 0.5, 1.0);
}
