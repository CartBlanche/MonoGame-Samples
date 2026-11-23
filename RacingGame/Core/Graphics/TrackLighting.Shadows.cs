using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGame.Shaders;

namespace RacingGame.Graphics
{
    /// <summary>
    /// Shadow prototype: shadow lookup inside the lit shaders (Content/Shaders/TrackShadows.fxh)
    /// as an alternative to the classic screen-space receive, blur and composite passes.
    /// See Documentation/Shadow-Prototype-Plan.md. Classic stays the default.
    /// </summary>
    public sealed partial class TrackLighting
    {
        // RACINGGAME_SHADOWS=classic | integrated forces a mode (test hook), read once
        static readonly string shadowsOverride =
            Environment.GetEnvironmentVariable("RACINGGAME_SHADOWS");

        /// <summary>
        /// True when the shadows are the integrated ones (looked up by the lit shaders, soft and
        /// sky filled). That is the default whenever the lighting is on (Low or High); with
        /// Lighting Off the game keeps its original (classic) shadows exactly as before. The test
        /// variable RACINGGAME_SHADOWS=classic or integrated forces one mode. Follows the player's
        /// setting, so it can change at any time (the Options screen).
        /// </summary>
        public static bool IntegratedShadows
        {
            get
            {
                if (string.Equals(shadowsOverride, "integrated", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(shadowsOverride, "classic", StringComparison.OrdinalIgnoreCase))
                    return false;
                return RacingGame.Properties.GameSettings.Default.Lighting != RacingGame.Properties.LightingQuality.Off;
            }
        }

        /// <summary>
        /// RACINGGAME_CASTERS=near: integrated shadows with the old caster selection (static near-track list,
        /// solid leaf cards), for A/B tests. Default: frustum selected casters with cut-out leaves.
        /// </summary>
        public static readonly bool NearCastersOnly =
            string.Equals(Environment.GetEnvironmentVariable("RACINGGAME_CASTERS"),
                "near", StringComparison.OrdinalIgnoreCase);

        /// <summary>0 = classic overlay, 1 = integrated (prototype switch).</summary>
        public int ShadowMode { get { return IntegratedShadows ? 1 : 0; } }

        /// <summary>Depth bias (same as classic compareDepthBias), normal offset, strength.</summary>
        // Base depth bias (normalised depth); the shader adds a slope term on top.
        // Net bias including the 0.00065 the generate pass already adds to the stored depth
        // (ShadowMap.fx shadowMapDepthBias, shared with classic): that part is taken off again
        // in the shader parameter so this is the total. 0.00105 = the first integrated version.
        static readonly float ShadowDepthBias = EnvFloat("RACINGGAME_SHADOWBIAS", 0.0004f) - 0.00065f;
        /// <summary>Test switches (1 = on). Defaults are the feedback round 1 fixes.</summary>
        public static readonly bool ShadowFacingFix = EnvFloat("RACINGGAME_SHADOWFACING", 1.0f) > 0.5f;
        public static readonly bool ShadowTexelFix = EnvFloat("RACINGGAME_SHADOWTEXFIX", 1.0f) > 0.5f;
        /// <summary>Normal offset in shadow texels (scaled by the surface slope in the shader).</summary>
        float shadowNormalOffset = 1.5f;
        float shadowStrength = EnvFloat("RACINGGAME_SHADOWSTRENGTH", 1.0f);

        /// <summary>
        /// Apparent angular size of the sun in radians (penumbra = distance from the caster x this).
        /// The real sun is 0.0093; the game uses a larger value for readable soft edges.
        /// Test override: RACINGGAME_SUNSIZE.
        /// </summary>
        public static float SunAngularSize = EnvFloat("RACINGGAME_SUNSIZE", 0.04f);
        /// <summary>Filter radius range in shadow texels (clamp of the penumbra).</summary>
        static readonly float ShadowMinRadius = EnvFloat("RACINGGAME_SHADOWMINR", 1.0f);
        const float ShadowMaxRadius = 6.0f;
        /// <summary>
        /// Shadow quality: true = 8 blocker + 16 filter taps, false = 4 + 8 (for a Low setting).
        /// Test override: RACINGGAME_SHADOWQUALITY=low.
        /// </summary>
        public static bool ShadowHighQuality =
            !string.Equals(Environment.GetEnvironmentVariable("RACINGGAME_SHADOWQUALITY"),
                "low", StringComparison.OrdinalIgnoreCase);

        static float EnvFloat(string name, float def)
        {
            float v;
            return float.TryParse(Environment.GetEnvironmentVariable(name),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out v) ? v : def;
        }

        /// <summary>
        /// Test override of the sky fill (RACINGGAME_SHADOWFILL, one value for r,g,b), used to
        /// measure the plain "sun term removed" model (1.0).
        /// </summary>
        static readonly float shadowFillOverride = EnvFloat("RACINGGAME_SHADOWFILL", -1.0f);

        /// <summary>Cached TrackShadows.fxh parameters of one effect (null when optimised away).</summary>
        sealed class ShadowHandles
        {
            EffectParameter mode, sx, sy, sw, prm, map, fade, tint, pcssP, flagsP;

            public ShadowHandles(Effect effect)
            {
                mode = effect.Parameters["ShadowMode"];
                sx = effect.Parameters["ShadowX"];
                sy = effect.Parameters["ShadowY"];
                sw = effect.Parameters["ShadowW"];
                prm = effect.Parameters["ShadowParams"];
                map = effect.Parameters["ShadowMap"];
                fade = effect.Parameters["shadowDistanceFadeoutTexture"];
                tint = effect.Parameters["ShadowTint"];
                pcssP = effect.Parameters["ShadowPcss"];
                flagsP = effect.Parameters["ShadowFlags"];
            }

            public void Publish(float modeValue, Matrix shadowVP, Vector4 shadowParams,
                Texture2D shadowMap, Texture2D fadeMap, Vector4 shadowTint, Vector4 pcss)
            {
                if (mode != null) mode.SetValue(modeValue);
                if (modeValue < 0.5f)
                    return;
                // Row vector convention: output component k = dot(P4, column k)
                if (sx != null) sx.SetValue(new Vector4(shadowVP.M11, shadowVP.M21, shadowVP.M31, shadowVP.M41));
                if (sy != null) sy.SetValue(new Vector4(shadowVP.M12, shadowVP.M22, shadowVP.M32, shadowVP.M42));
                if (sw != null) sw.SetValue(new Vector4(shadowVP.M14, shadowVP.M24, shadowVP.M34, shadowVP.M44));
                if (prm != null) prm.SetValue(shadowParams);
                if (tint != null) tint.SetValue(shadowTint);
                if (pcssP != null) pcssP.SetValue(pcss);
                if (flagsP != null) flagsP.SetValue(new Vector4(ShadowFacingFix ? 1 : 0, ShadowTexelFix ? 1 : 0, 0, 0));
                if (map != null) map.SetValue(shadowMap);
                if (fade != null) fade.SetValue(fadeMap);
            }
        }

        ShadowHandles terrainShadow, roadShadow;
        Texture2D shadowFadeMap;
        bool shadowPublishedOn;

        /// <summary>
        /// Publish the shadow map to the converted effects (terrain and road shaders).
        /// Call once per frame after the shadow map was generated.
        /// </summary>
        /// <param name="shadowVP">world * lightView * lightProjection * texScaleBias</param>
        public void PublishShadows(Matrix shadowVP, Texture2D shadowMap, float texelSize)
        {
            if (shadowMap == null)
                return;
            if (terrainShadow == null)
            {
                var t = ShaderEffect.landscapeNormalMapping.Effect;
                var r = ShaderEffect.normalMapping.Effect;
                if (t == null || r == null)
                    return;
                terrainShadow = new ShadowHandles(t);
                roadShadow = new ShadowHandles(r);
                shadowFadeMap = new Texture("ShadowDistanceFadeoutMap").XnaTexture;
            }
            var p = new Vector4(texelSize, ShadowDepthBias, shadowNormalOffset, shadowStrength);
            // Environment fill: what the ambient light keeps in shadow (time of day preset;
            // 1 in tunnels, where the sun term is gone anyway). ShadowColor is classic only.
            Vector3 fill = shadowFillOverride >= 0 ? new Vector3(shadowFillOverride) : SkyFill;
            var tint = new Vector4(Vector3.Lerp(fill, Vector3.One, TunnelBlend), ShadowMapShader.ShadowCoverage);
            var pcss = new Vector4(SunAngularSize, ShadowHighQuality ? 1.0f : 0.0f,
                ShadowMinRadius, ShadowMaxRadius);
            terrainShadow.Publish(1, shadowVP, p, shadowMap, shadowFadeMap, tint, pcss);
            roadShadow.Publish(1, shadowVP, p, shadowMap, shadowFadeMap, tint, pcss);
            shadowPublishedOn = true;
        }

        /// <summary>Switch the shader shadow term off again (shadows disabled at runtime).</summary>
        public void PublishShadowsOff()
        {
            if (!shadowPublishedOn || terrainShadow == null)
                return;
            terrainShadow.Publish(0, Matrix.Identity, Vector4.Zero, null, null, Vector4.One, Vector4.Zero);
            roadShadow.Publish(0, Matrix.Identity, Vector4.Zero, null, null, Vector4.One, Vector4.Zero);
            shadowPublishedOn = false;
        }
    }
}
