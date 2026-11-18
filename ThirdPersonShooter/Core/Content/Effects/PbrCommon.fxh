// Shared by PbrLit.fx and PbrLitSkinned.fx: parameters, VSOut and the HDR pixel shader (linear output, NO tonemap; PostTonemap.fx does it).
// Textures are plain Color; albedo and emission are sRGB-encoded and decoded here (pow 2.2); normal/ORM are linear.
// ORM = R:AO G:roughness B:metallic. Normal maps are OpenGL style (Y+ up).
float4x4 World;
float4x4 ViewProj;
float3 CameraPos;

float4 AlbedoColor;
float3 EmissionColor;
float EmissionEnergy;
float Metallic;
float Roughness;
float Specular;
float NormalScale;
float4 UvTransform;      // xy scale, zw offset
float3 RoughMask;        // ORM channel selectors (texChannels)
float3 MetalMask;
float4 MatFlags;         // x: has AO in ORM (1/0)

float3 LightDir;         // fake key light (GI stand-in), direction light travels
float3 LightColor;
float3 AmbientColor;     // hemisphere ambient, sky (up-facing) colour, neutral-warm (derived, see LIGHTING-NOTES pass 2)
float2 AmbShape;         // x: ambient gain for wall/ceiling-facing normals, y: gain for up-facing (floors); lighting pass 3 (1,1 = unchanged)
float CavMin;            // cavity term floor (0.45 = pass 2)
float MetalDiff;         // pass 4: fraction of albedo that metals keep as diffuse (0 = pure metal)
float SpecMin;           // pass 4: min GGX alpha (0.03 = old)
float KeySpec;           // pass 4: spec scale of the fake key light
float3 AmbWarm;          // pass 5: warm saturated ambient on up-facing surfaces (floors); (0,0,0) = off
float3 AmbFlat;          // pass 4: flat neutral ambient (all normals)
float2 KeyNear;          // pass 4: x min key gain close to the camera, y distance (m) where the key reaches full gain (x=1: off)
float EnvGain;           // pass 4: gain of the fake reflection environment (1 = old)
float KeyFloor;          // pass 4: key light gain on up-facing surfaces (1 = old)
float AlbGain;           // pass 4: diffuse albedo gain (1 = old)
float3 AmbGround;        // hemisphere ambient, ground (down-facing) colour, dark
float FogStart;          // fog begins after this distance (m)
float3 EmisTune;         // x scale, y saturation (1 = unchanged), z max per-channel HDR
float3 EnvUp;            // fake reflection environment
float3 EnvHorizon;
float3 EnvDown;
float3 FogColor;
float FogDensity;
float HdrScale;          // 1 for float targets, <1 when the HDR target is plain Color
float4 LPos[8];          // xyz world position, w range
float4 LCol[8];          // rgb colour*energy, w cos(outer cone)  (points: -3)
float4 ShadowP;         // shadow map: x texel size (1/res), y depth bias (normalised), z normal offset (m per texel scale), w edge fade (1/ndc width)
float4x4 ShadowVP;       // world -> light clip (orthographic)
float3 ShadowDir;        // direction the shadow light travels
float ShadowAmb;
float4 ShadowK;          // x 1 = perspective (spot) map, 0 = orthographic; y 1/far (perspective depth = view distance / far); w PCF tap spacing multiplier
float3 ShadowOrigin;     // spot position (perspective map)
float ShadowSlot;        // index (0-7) of the point/spot light the shadow applies to, -1 = the key light only         // how strongly a key shadow also darkens the ambient (0..1)
float4 SpR[16];          // spot shadow maps: the 4 rows of each world -> light clip matrix (row-major, rows 4s..4s+3); a float4x4[4] array breaks the macOS GL program
float4 SpPos[4];         // xyz spot position, w index (0-7) of the light in LPos/LCol the map belongs to
float4 SpP[4];           // x world size of a texel per metre of distance, y 1/far, z strength (0 = map unused), w tile index
float SpotInv;           // 1 / tile size in texels
            // 1 = no key-light map this frame (spot maps only)
float4 LDirI[8];         // xyz direction light travels, w cos(inner cone) (points: -2)

texture AlbedoTex; sampler2D AlbedoSamp = sampler_state { Texture = <AlbedoTex>; AddressU = Wrap; AddressV = Wrap; Filter = Anisotropic; MaxAnisotropy = 8; };
texture NormalTex; sampler2D NormalSamp = sampler_state { Texture = <NormalTex>; AddressU = Wrap; AddressV = Wrap; Filter = Anisotropic; MaxAnisotropy = 8; };
texture OrmTex;    sampler2D OrmSamp    = sampler_state { Texture = <OrmTex>;    AddressU = Wrap; AddressV = Wrap; Filter = Anisotropic; MaxAnisotropy = 8; };
texture EmisTex;   sampler2D EmisSamp   = sampler_state { Texture = <EmisTex>;   AddressU = Wrap; AddressV = Wrap; Filter = Anisotropic; MaxAnisotropy = 8; };

#define SPOT_ON 1
#define SPOT_TAPS 4          // PCF: Vogel disc taps (the kernel size to compare: 9 = old 3x3 grid, 16, 25 = 5x5)
#define SPOT_RADIUS 2.5       // PCF disc radius in texels
#define SPOT_BIAS 0.03        // constant depth bias (m) on top of the slope term
#define SPOT_NOFF 1.0         // normal offset in texels (x (0.5 + 1.5 sin theta))
texture ShadowTex; sampler2D ShadowSamp = sampler_state { Texture = <ShadowTex>; AddressU = Clamp; AddressV = Clamp; Filter = Point; };
texture SpotTex; sampler2D SpotSamp = sampler_state { Texture = <SpotTex>; AddressU = Clamp; AddressV = Clamp; Filter = Point; };   // declared after ShadowTex: sampler slot 5

struct VSOut
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float3 WPos : TEXCOORD1;
    float3 N : TEXCOORD2;
    float4 T : TEXCOORD3;
};

float3 SrgbToLinear(float3 c) { return pow(max(c, 0.0001), 2.2); }

float DistGGX(float nh, float a) { float a2 = a * a; float d = nh * nh * (a2 - 1.0) + 1.0; return a2 / (3.14159265 * d * d + 1e-5); }
float VisSmith(float nv, float nl, float a) { float k = a * 0.5; return 1.0 / ((nv * (1.0 - k) + k) * (nl * (1.0 - k) + k) + 1e-5) * 0.25; }

float3 EnvColor(float3 r, float rough)
{
    float t = r.y;
    float3 c = t > 0 ? lerp(EnvHorizon, EnvUp, sqrt(t)) : lerp(EnvHorizon, EnvDown, sqrt(-t));
    float3 avg = (EnvUp + EnvHorizon * 2.0 + EnvDown) * 0.25;
    return lerp(c, avg, rough * rough);
}

// Shadow map lookup (R32F linear light-space depth, ortho): normal-offset receiver bias + constant bias, 3x3 PCF, fades out at the map edge.
// Returns 1 (lit) for surfaces facing away from the shadow light so ambient is only darkened where the key would have lit.
float ShadowFactor(float3 wpos, float3 Ng)
{
    float persp = ShadowK.x;
    float3 Ld = normalize(lerp(ShadowDir, wpos - ShadowOrigin, persp));
    float nls = dot(Ng, -Ld);
    float sinT = sqrt(saturate(1.0 - nls * nls));
    float3 p = wpos + Ng * (ShadowP.z * (0.5 + 1.5 * sinT));
    float4 lp = mul(float4(p, 1.0), ShadowVP);
    float3 sp = lp.xyz / lp.w;
    float2 uv = float2(sp.x * 0.5 + 0.5, 0.5 - sp.y * 0.5);
    float z = lerp(sp.z, lp.w * ShadowK.y, persp) - ShadowP.y;
    float s = 0.0;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
        s += (tex2D(ShadowSamp, uv + float2(x, y) * (ShadowP.x * ShadowK.w)).r >= z) ? 1.0 : 0.0;
    s /= 9.0;
    float2 e = abs(sp.xy);
    float edge = saturate((1.0 - max(e.x, e.y)) * ShadowP.w) * step(0.0, lp.w);
    edge *= lerp(step(0.0, sp.z) * step(sp.z, 1.0), 1.0, persp);
    return lerp(1.0, lerp(1.0, s, saturate(nls * 4.0)), edge);
}

// Spot light shadow (perspective map s of the atlas): normal offset + slope bias scaled by the texel size at the receiver's distance, Vogel-disc PCF, fades at the frustum border and towards the far plane.
// Returns 1 (lit) for surfaces facing away from the light. The caller blends by the map's strength.
float SpotShadow(int s, float3 wpos, float3 Ng)
{
    float3 toL = wpos - SpPos[s].xyz;
    float d = max(length(toL), 0.01);
    float3 Ld = toL / d;
    float nls = dot(Ng, -Ld);
    float sinT = sqrt(saturate(1.0 - nls * nls));
    float texW = SpP[s].x * d;
    float3 p = wpos + Ng * (texW * SPOT_NOFF * (0.5 + 1.5 * sinT));
    float4 lp = p.x * SpR[s * 4] + p.y * SpR[s * 4 + 1] + p.z * SpR[s * 4 + 2] + SpR[s * 4 + 3];
    float iw = 1.0 / max(lp.w, 1e-3);
    float2 uv = float2(lp.x * iw * 0.5 + 0.5, 0.5 - lp.y * iw * 0.5);
    float z = (lp.w - (SPOT_BIAS + texW * (1.0 + 2.0 * sinT))) * SpP[s].y;
    float2 org = float2(fmod(SpP[s].w, 2.0), floor(SpP[s].w * 0.5)) * 0.5;
    float vis = 0.0;
    [unroll] for (int k = 0; k < SPOT_TAPS; k++)
    {
        float a = k * 2.39996323;
        float r = sqrt((k + 0.5) / SPOT_TAPS) * SPOT_RADIUS;
        float2 tuv = clamp(uv + float2(cos(a), sin(a)) * (r * SpotInv), 0.002, 0.998);
        vis += (tex2D(SpotSamp, org + tuv * 0.5).r >= z) ? 1.0 : 0.0;
    }
    vis /= SPOT_TAPS;
    float2 e = abs(float2(lp.x, lp.y) * iw);
    float edge = saturate((1.0 - max(e.x, e.y)) * 8.0) * step(0.0, lp.w);
    float farD = 1.0 / max(SpP[s].y, 1e-4);
    edge *= 1.0 - smoothstep(0.8 * farD, farD, d);
    return lerp(1.0, lerp(1.0, vis, saturate(nls * 4.0)), edge);
}

float4 Shade(VSOut i, bool useKey, bool useSpot)
{
    float4 albT = tex2D(AlbedoSamp, i.UV);
    if (MatFlags.y > 0.0) clip(albT.a - MatFlags.y); // pass 5: alpha scissor (floor grates, container4)
    float3 albedo = SrgbToLinear(albT.rgb) * AlbedoColor.rgb;
    float alpha = AlbedoColor.a;
    float3 orm = tex2D(OrmSamp, i.UV).rgb;
    float ao = lerp(1.0, orm.r, MatFlags.x);
    float rough = saturate(dot(orm, RoughMask) * Roughness);
    float metal = saturate(dot(orm, MetalMask) * Metallic);
    float3 emis = SrgbToLinear(tex2D(EmisSamp, i.UV).rgb) * EmissionColor * EmissionEnergy;
    { float el = dot(emis, float3(0.2126, 0.7152, 0.0722)); emis = min(lerp(el.xxx, emis, EmisTune.y) * EmisTune.x, EmisTune.z); }

    float3 N = normalize(i.N);
    float3 Ng = N;
    float3 T0 = i.T.xyz - N * dot(i.T.xyz, N);
    float3 T = dot(T0, T0) > 1e-8 ? normalize(T0) : normalize(cross(N, abs(N.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0)));
    float3 B = cross(N, T) * i.T.w;
    float3 nm = tex2D(NormalSamp, i.UV).xyz * 2.0 - 1.0;
    nm.xy *= NormalScale;
    N = normalize(nm.x * T + nm.y * B + nm.z * N);

    float3 toCam = CameraPos - i.WPos;
    float dist = length(toCam);
    float3 V = toCam / max(dist, 1e-4);
    float nv = saturate(dot(N, V) + 1e-4);
    float a = max(rough * rough, SpecMin);
    float3 f0 = lerp(0.08 * Specular * 2.0, albedo, metal);
    float3 diffuseCol = albedo * (1.0 - metal * (1.0 - MetalDiff)) * AlbGain;

    float kn = lerp(KeyNear.x, 1.0, saturate((dist - 2.0) / max(KeyNear.y - 2.0, 0.001))); // pass 4: key + flat ambient fade close to the camera
    float sk = 1.0;
    if (useKey) sk = ShadowFactor(i.WPos, Ng);
    float4 spv = float4(1, 1, 1, 1);
    if (useSpot)
    {
        spv.x = lerp(1.0, SpotShadow(0, i.WPos, Ng), SpP[0].z);
        spv.y = lerp(1.0, SpotShadow(1, i.WPos, Ng), SpP[1].z);
        spv.z = lerp(1.0, SpotShadow(2, i.WPos, Ng), SpP[2].z);
        spv.w = lerp(1.0, SpotShadow(3, i.WPos, Ng), SpP[3].z);
    }
    float skKey = lerp(sk, 1.0, step(-0.5, ShadowSlot));   // the key light is only shadowed when no local light owns the map
    // fake key light + 8 point/spot lights
    float3 direct = 0;
    {
        float3 L = -LightDir; float3 H = normalize(L + V);
        float nl = saturate(dot(N, L)), nh = saturate(dot(N, H)), vh = saturate(dot(V, H));
        float3 F = f0 + (1.0 - f0) * pow(1.0 - vh, 5.0);
        direct += (diffuseCol * (1.0 - F) / 3.14159265 + DistGGX(nh, a) * VisSmith(nv, nl, a) * F * KeySpec) * LightColor * (nl * lerp(1.0, KeyFloor, saturate(N.y)) * kn * skKey);
    }
    [unroll]
    for (int k = 0; k < 8; k++)
    {
        float3 toL = LPos[k].xyz - i.WPos;
        float d2 = max(dot(toL, toL), 0.25);
        float d = sqrt(d2);
        float3 L = toL / d;
        float w = saturate(1.0 - pow(d / LPos[k].w, 4.0));
        float cone = smoothstep(LCol[k].w, LDirI[k].w, dot(-L, LDirI[k].xyz));
        float att = w * w * cone / d2;
        float3 H = normalize(L + V);
        float nl = saturate(dot(N, L)), nh = saturate(dot(N, H)), vh = saturate(dot(V, H));
        float3 F = f0 + (1.0 - f0) * pow(1.0 - vh, 5.0);
        float lsh = lerp(1.0, sk, step(abs(k - ShadowSlot), 0.5));
        if (useSpot) { [unroll] for (int m = 0; m < 4; m++) lsh *= (SpP[m].z > 0.003 && abs(k - SpPos[m].w) < 0.5) ? spv[m] : 1.0; }
        direct += (diffuseCol * (1.0 - F) / 3.14159265 + DistGGX(nh, a) * VisSmith(nv, nl, a) * F) * LCol[k].rgb * (nl * att * lsh);
    }

    float3 fEnv = f0 + (max(1.0 - rough, f0) - f0) * pow(1.0 - nv, 5.0);
    float3 R = reflect(-V, N);
    float hemi = saturate(N.y * 0.5 + 0.5);
    float cav = lerp(CavMin, 1.0, pow(saturate(nm.z / max(length(nm), 1e-4)), 6.0)); // normal-map cavity: bevels/grooves get less ambient
    ao *= cav;
    float3 hAmb = lerp(AmbGround, AmbientColor, hemi * hemi) * lerp(AmbShape.x, AmbShape.y, pow(saturate(N.y), 2.0));
    float3 ambient = ((hAmb + AmbWarm * pow(saturate(N.y), 2.0) + AmbFlat * kn * lerp(1.0, 0.2, saturate(N.y))) * diffuseCol * (1.0 - fEnv * 0.5) + EnvColor(R, rough) * fEnv * EnvGain) * ao * lerp(1.0, skKey, ShadowAmb);

    float3 hdr = direct + ambient + emis;
    float fog = 1.0 - exp(-FogDensity * max(dist - FogStart, 0.0));
    hdr = lerp(hdr, FogColor, fog);
    return float4(hdr * HdrScale, alpha);
}

float4 PS(VSOut i) : COLOR0 { return Shade(i, false, false); }
float4 PSShadow(VSOut i) : COLOR0 { return Shade(i, true, false); }     // key-light map
float4 PSSpot(VSOut i) : COLOR0 { return Shade(i, false, true); }       // level spot-light maps (separate program: both in one overflowed the GL fragment program on macOS)
