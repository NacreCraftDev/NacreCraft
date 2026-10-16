#version 450

layout(set = 2, binding = 0) uniform sampler2D Sampler0;
layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragUv;
layout(location = 0) out vec4 outColor;

void main() {
    vec4 tex = texture(Sampler0, fragUv);
#ifdef IS_GRAYSCALE
    //A grayscale atlas carries the coverage in the red channel, replicated across the color like vanilla does;
    //an RGBA atlas such as the bitmap font keeps its own alpha and is multiplied in as is
    tex = tex.rrrr;
#endif
    vec4 color = tex * fragColor;
    if (color.a < 0.1) {
        discard;
    }
    outColor = color;
}
