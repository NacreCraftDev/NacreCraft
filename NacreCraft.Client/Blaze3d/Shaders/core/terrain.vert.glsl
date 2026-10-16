#version 450

//terrain vertex shader 世界渲染区块 mesh 顶点变换
//顶点为 20 字节紧凑格式 位置/贴图/光照均已量化 见 ChunkVertexFormat
//set 0 MATRICES_PROJECTION 含 ViewProj push constant 携带区域世界原点
//顶点是 section 局部坐标 顶点里的 section 槽位给出区域内位移 shader 端相加得到世界坐标
//face shading 已 bake 进顶点 color 不需要 Lighting UBO 方向光照

layout(set = 0, binding = 0) uniform MatricesUbo {
    mat4 ViewProj;
} matrices;

layout(push_constant) uniform PushConstants {
    //xyz 为区域世界原点 regionX*128 regionY*64 regionZ*128 w 保留
    vec4 RegionOrigin;
} push;

layout(location = 0) in uvec2 a_Position;
layout(location = 1) in vec4  a_Color;
layout(location = 2) in uvec2 a_TexCoord;
layout(location = 3) in uvec4 a_LightAndData;

layout(location = 0) out vec2 fragUv;
layout(location = 1) out vec2 fragLightCoord;
layout(location = 2) out vec4 fragTint;

const uint  POSITION_BITS       = 20u;
const uint  POSITION_MAX_COORD  = 1u << POSITION_BITS;
const uint  TEXTURE_BITS        = 15u;
const uint  TEXTURE_MAX_COORD   = 1u << TEXTURE_BITS;
const uint  TEXTURE_MAX_VALUE   = TEXTURE_MAX_COORD - 1u;
const float VERTEX_SCALE        = 32.0 / float(POSITION_MAX_COORD);
const float VERTEX_OFFSET       = -8.0;

//三个坐标各占 10 位 hi/lo 两个 u32 交错存放
uvec3 deinterleavePosition(uvec2 data) {
    uvec3 hi = (uvec3(data.x) >> uvec3(0u, 10u, 20u)) & 0x3FFu;
    uvec3 lo = (uvec3(data.y) >> uvec3(0u, 10u, 20u)) & 0x3FFu;
    return (hi << 10u) | lo;
}

//section 槽位布局 (sx&7) | ((sz&7)<<3) | ((sy&3)<<6) 对齐 RenderRegion.SlotIndex
vec3 sectionOffsetOf(uint slot) {
    return vec3(
        float(slot & 7u),
        float((slot >> 6u) & 3u),
        float((slot >> 3u) & 7u)
    ) * 16.0;
}

void main() {
    vec3 localPos = deinterleavePosition(a_Position) * VERTEX_SCALE + VERTEX_OFFSET;
    //a_LightAndData.w 是 section 在区域内的槽位 移动方块传 0 由 push constant 直接给出基准原点
    vec3 worldPos = push.RegionOrigin.xyz + sectionOffsetOf(a_LightAndData.w) + localPos;
    gl_Position = matrices.ViewProj * vec4(worldPos, 1.0);

    //解包贴图坐标 低 15 位为量化值 最高位是量化时向贴图中心收缩的 bias 符号 用一格 epsilon 还原
    vec2 texCoord = vec2(a_TexCoord & TEXTURE_MAX_VALUE) / float(TEXTURE_MAX_COORD);
    vec2 texBias = mix(vec2(-1.0), vec2(1.0), bvec2(a_TexCoord >> TEXTURE_BITS));
    fragUv = texCoord + texBias / float(TEXTURE_MAX_COORD);

    //解包 light 低 8 位为方块光 高 8 位为天空光 取值 0-15 采样 lightmap 的格子中心
    fragLightCoord = (vec2(a_LightAndData.xy) + 0.5) / 16.0;

    fragTint = a_Color;
}
