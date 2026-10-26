// URP FinalPost: FXAA 3.11 (quality preset 12, URP constants: subpixel 0.65, relative threshold 0.15, absolute threshold 0.03,
// luma = dot(rgb, 0.299/0.587/0.114) of the sampled values), film grain (ApplyGrain) and the linear -> sRGB encode.
// Slots: 0 SourceTex (linear LDR, LinearClamp), 1 GrainTex (LinearWrap), 2 HudTex (premultiplied linear UI layer, LinearClamp).
// Unity blends the UI over the finished frame in linear space, so the HUD layer is composited after FXAA / grain, right before the encode.

#include "PostCommon.fxh"

float4 SourceSize;       // xy size, zw 1/size
float4 GrainTiling;      // xy scale, zw offset
float2 GrainParams;      // x intensity (volume intensity * 4), y response
float4 FinalFlags = float4(1, 1, 1, 0); // x fxaa, y grain, z srgb encode, w hud layer

DECLARE_TEXTURE(SourceTex, 0);
DECLARE_TEXTURE(GrainTex, 1);
DECLARE_TEXTURE(HudTex, 2);

float4 Fetch(float2 uv)
{
    return tex2Dlod(SourceTexSampler, float4(uv, 0, 0));
}

float FxaaLuma(float4 rgba)
{
    return dot(rgba.xyz, float3(0.299, 0.587, 0.114));
}

float4 FetchOff(float2 uv, float2 off)
{
    return Fetch(uv + off * SourceSize.zw);
}

static const float kSubpixelBlendAmount = 0.65;
static const float kRelativeContrastThreshold = 0.15;
static const float kAbsoluteContrastThreshold = 0.03;

float3 Fxaa(float2 posM, float4 rgbyM)
{
    float2 rcp2 = SourceSize.zw;
    float lumaM = FxaaLuma(rgbyM);
    float lumaS = FxaaLuma(FetchOff(posM, float2(0, 1)));
    float lumaE = FxaaLuma(FetchOff(posM, float2(1, 0)));
    float lumaN = FxaaLuma(FetchOff(posM, float2(0, -1)));
    float lumaW = FxaaLuma(FetchOff(posM, float2(-1, 0)));
    float maxSM = max(lumaS, lumaM);
    float minSM = min(lumaS, lumaM);
    float maxESM = max(lumaE, maxSM);
    float minESM = min(lumaE, minSM);
    float maxWN = max(lumaN, lumaW);
    float minWN = min(lumaN, lumaW);
    float rangeMax = max(maxWN, maxESM);
    float rangeMin = min(minWN, minESM);
    float rangeMaxScaled = rangeMax * kRelativeContrastThreshold;
    float range = rangeMax - rangeMin;
    float rangeMaxClamped = max(kAbsoluteContrastThreshold, rangeMaxScaled);
    if (range < rangeMaxClamped)
        return rgbyM.rgb;

    float lumaNW = FxaaLuma(FetchOff(posM, float2(-1, -1)));
    float lumaSE = FxaaLuma(FetchOff(posM, float2(1, 1)));
    float lumaNE = FxaaLuma(FetchOff(posM, float2(1, -1)));
    float lumaSW = FxaaLuma(FetchOff(posM, float2(-1, 1)));

    float lumaNS = lumaN + lumaS;
    float lumaWE = lumaW + lumaE;
    float subpixRcpRange = 1.0 / range;
    float subpixNSWE = lumaNS + lumaWE;
    float edgeHorz1 = (-2.0 * lumaM) + lumaNS;
    float edgeVert1 = (-2.0 * lumaM) + lumaWE;
    float lumaNESE = lumaNE + lumaSE;
    float lumaNWNE = lumaNW + lumaNE;
    float edgeHorz2 = (-2.0 * lumaE) + lumaNESE;
    float edgeVert2 = (-2.0 * lumaN) + lumaNWNE;
    float lumaNWSW = lumaNW + lumaSW;
    float lumaSWSE = lumaSW + lumaSE;
    float edgeHorz4 = (abs(edgeHorz1) * 2.0) + abs(edgeHorz2);
    float edgeVert4 = (abs(edgeVert1) * 2.0) + abs(edgeVert2);
    float edgeHorz3 = (-2.0 * lumaW) + lumaNWSW;
    float edgeVert3 = (-2.0 * lumaS) + lumaSWSE;
    float edgeHorz = abs(edgeHorz3) + edgeHorz4;
    float edgeVert = abs(edgeVert3) + edgeVert4;
    float subpixNWSWNESE = lumaNWSW + lumaNESE;
    float lengthSign = rcp2.x;
    bool horzSpan = edgeHorz >= edgeVert;
    float subpixA = subpixNSWE * 2.0 + subpixNWSWNESE;
    if (!horzSpan) lumaN = lumaW;
    if (!horzSpan) lumaS = lumaE;
    if (horzSpan) lengthSign = rcp2.y;
    float subpixB = (subpixA * (1.0 / 12.0)) - lumaM;
    float gradientN = lumaN - lumaM;
    float gradientS = lumaS - lumaM;
    float lumaNN = lumaN + lumaM;
    float lumaSS = lumaS + lumaM;
    bool pairN = abs(gradientN) >= abs(gradientS);
    float gradient = max(abs(gradientN), abs(gradientS));
    if (pairN) lengthSign = -lengthSign;
    float subpixC = saturate(abs(subpixB) * subpixRcpRange);

    float2 posB = posM;
    float2 offNP;
    offNP.x = (!horzSpan) ? 0.0 : rcp2.x;
    offNP.y = (horzSpan) ? 0.0 : rcp2.y;
    if (!horzSpan) posB.x += lengthSign * 0.5;
    if (horzSpan) posB.y += lengthSign * 0.5;

    float2 posN = posB - offNP * 1.0;
    float2 posP = posB + offNP * 1.0;
    float subpixD = ((-2.0) * subpixC) + 3.0;
    float lumaEndN = FxaaLuma(Fetch(posN));
    float subpixE = subpixC * subpixC;
    float lumaEndP = FxaaLuma(Fetch(posP));
    if (!pairN) lumaNN = lumaSS;
    float gradientScaled = gradient * 1.0 / 4.0;
    float lumaMM = lumaM - lumaNN * 0.5;
    float subpixF = subpixD * subpixE;
    bool lumaMLTZero = lumaMM < 0.0;
    lumaEndN -= lumaNN * 0.5;
    lumaEndP -= lumaNN * 0.5;
    bool doneN = abs(lumaEndN) >= gradientScaled;
    bool doneP = abs(lumaEndP) >= gradientScaled;
    if (!doneN) posN -= offNP * 1.5;
    bool doneNP = (!doneN) || (!doneP);
    if (!doneP) posP += offNP * 1.5;

    // quality preset 12: steps 1.0, 1.5, 2.0, 4.0, 12.0
    [unroll] for (int s = 0; s < 3; s++)
    {
        float stepLen = (s == 0) ? 2.0 : ((s == 1) ? 4.0 : 12.0);
        if (doneNP)
        {
            if (!doneN) lumaEndN = FxaaLuma(Fetch(posN));
            if (!doneP) lumaEndP = FxaaLuma(Fetch(posP));
            if (!doneN) lumaEndN = lumaEndN - lumaNN * 0.5;
            if (!doneP) lumaEndP = lumaEndP - lumaNN * 0.5;
            doneN = abs(lumaEndN) >= gradientScaled;
            doneP = abs(lumaEndP) >= gradientScaled;
            if (!doneN) posN -= offNP * stepLen;
            doneNP = (!doneN) || (!doneP);
            if (!doneP) posP += offNP * stepLen;
        }
    }
    // final (5th) fetch of the loop above is only taken when doneNP remains set after the 4.0 step; the 12.0 step is the last.

    float dstN = posM.x - posN.x;
    float dstP = posP.x - posM.x;
    if (!horzSpan) dstN = posM.y - posN.y;
    if (!horzSpan) dstP = posP.y - posM.y;
    bool goodSpanN = (lumaEndN < 0.0) != lumaMLTZero;
    float spanLength = (dstP + dstN);
    bool goodSpanP = (lumaEndP < 0.0) != lumaMLTZero;
    float spanLengthRcp = 1.0 / spanLength;
    bool directionN = dstN < dstP;
    float dst = min(dstN, dstP);
    bool goodSpan = directionN ? goodSpanN : goodSpanP;
    float subpixG = subpixF * subpixF;
    float pixelOffset = (dst * (-spanLengthRcp)) + 0.5;
    float subpixH = subpixG * kSubpixelBlendAmount;
    float pixelOffsetGood = goodSpan ? pixelOffset : 0.0;
    float pixelOffsetSubpix = max(pixelOffsetGood, subpixH);
    if (!horzSpan) posM.x += pixelOffsetSubpix * lengthSign;
    if (horzSpan) posM.y += pixelOffsetSubpix * lengthSign;
    return Fetch(posM).rgb;
}

float4 PSFinal(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float4 color = Fetch(uv);

    if (FinalFlags.x > 0.5)
        color.rgb = Fxaa(uv, color);

    if (FinalFlags.y > 0.5)
    {
        float grain = tex2Dlod(GrainTexSampler, float4(uv * GrainTiling.xy + GrainTiling.zw, 0, 0)).r;
        grain = (grain - 0.5) * 2.0;
        float lum = Luma(color.rgb);
        lum = 1.0 - sqrt(max(lum, 0));
        lum = lerp(1.0, lum, GrainParams.y);
        color.rgb = color.rgb + color.rgb * grain * GrainParams.x * lum;
    }

    if (FinalFlags.w > 0.5)
    {
        float4 hud = tex2Dlod(HudTexSampler, float4(uv, 0, 0));
        color.rgb = saturate(color.rgb) * (1.0 - hud.a) + hud.rgb;
    }

    float3 outc = saturate(color.rgb);
    if (FinalFlags.z > 0.5)
        outc = LinearToSRGB3(outc);
    return float4(outc, 1);
}

TECHNIQUE(FinalPost, PostVS, PSFinal);
