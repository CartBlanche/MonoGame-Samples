using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGame.Graphics
{
    /// <summary>
    /// Single owner of all light state (see Documentation/Lighting-Plan.md).
    /// WS0: only the Day preset exists and every hook returns its input unchanged,
    /// so the rendered output is identical to the pre-TrackLighting game.
    /// Later workstreams add time of day, fog, headlights, lamps and tunnels
    /// (in partial files: TrackLighting.TimeOfDay.cs, .Headlights.cs, .Lamps.cs).
    /// </summary>
    public sealed partial class TrackLighting
    {
        /// <summary>
        /// Evaluated light settings for the current time of day. Day is today's look.
        /// Every field is neutral at its "off" value (see plan section 4.2): tints are
        /// 1, fog density is 0, light count is 0. MonoGame on GL does not apply .fx
        /// initial values (every parameter starts at 0), so these are always set from C#.
        /// </summary>
        public struct Preset
        {
            /// <summary>Multiplied into the material ambient colour (rgb).</summary>
            public Vector3 AmbientTint;
            /// <summary>Multiplied into the material diffuse colour (rgb).</summary>
            public Vector3 DiffuseTint;
            /// <summary>Multiplied into the material specular colour (rgb).</summary>
            public Vector3 SpecularTint;
            /// <summary>Hemisphere ambient multipliers (sky above, ground below); (1,1,1) both = off.</summary>
            public Vector3 AmbSky, AmbGround;
            /// <summary>Fog: x = density (0 = off), y = start distance.</summary>
            public Vector2 Fog;
            public Vector3 FogColor;

            /// <summary>Today's look.</summary>
            public static Preset Day
            {
                get
                {
                    Preset p = new Preset();
                    p.AmbientTint = p.DiffuseTint = p.SpecularTint = Vector3.One;
                    p.AmbSky = p.AmbGround = Vector3.One;
                    p.Fog = Vector2.Zero;
                    p.FogColor = Vector3.One;
                    return p;
                }
            }
        }

        /// <summary>Current evaluated preset (WS0: always Day).</summary>
        public Preset Current = Preset.Day;

        /// <summary>
        /// 0 = legacy per-vertex directional path (today), 1 = per-pixel lights (WS2).
        /// </summary>
        public float LightingMode = 0.0f;

        /// <summary>
        /// 0 outside a tunnel, 1 deep inside, eased over time. Written by WS4 (tunnels),
        /// read by WS3 (headlights switch on inside tunnels). Always 0 until WS4 lands.
        /// </summary>
        public float TunnelBlend = 0.0f;

        /// <summary>Number of active lights (0..MaxLights). WS0: always 0.</summary>
        public int LightCount = 0;

        /// <summary>Light budget (decision D4): 2 headlights + 2 nearest lamps/tunnel lights.</summary>
        public const int MaxLights = 4;

        // Hooks used by the ShaderEffect / Model choke points -----------------

        /// <summary>Material ambient colour as it must be uploaded to a shader.</summary>
        public Color Ambient(Color material)
        {
            return Scale(material, Current.AmbientTint);
        }

        /// <summary>Material diffuse colour as it must be uploaded to a shader.</summary>
        public Color Diffuse(Color material)
        {
            return Scale(material, Current.DiffuseTint);
        }

        /// <summary>Material specular colour as it must be uploaded to a shader.</summary>
        public Color Specular(Color material)
        {
            return Scale(material, Current.SpecularTint);
        }

        /// <summary>Same as <see cref="Ambient"/> but for the Vector4 upload paths.</summary>
        public Vector4 Ambient(Vector4 material)
        {
            return Scale(material, Current.AmbientTint);
        }

        /// <summary>Same as <see cref="Diffuse"/> but for the Vector4 upload paths.</summary>
        public Vector4 Diffuse(Vector4 material)
        {
            return Scale(material, Current.DiffuseTint);
        }

        /// <summary>Same as <see cref="Specular"/> but for the Vector4 upload paths.</summary>
        public Vector4 Specular(Vector4 material)
        {
            return Scale(material, Current.SpecularTint);
        }

        /// <summary>Sun (directional light) direction as it must be uploaded to a shader.</summary>
        public Vector3 SunDirection(Vector3 gameLightDirection)
        {
            return gameLightDirection;
        }

        /// <summary>Colour the sky cube is multiplied with.</summary>
        public Color Sky(Color daySky)
        {
            return TintSky(daySky);
        }

        static readonly Vector3 One = Vector3.One;

        static Color Scale(Color c, Vector3 tint)
        {
            // Exactly the input when the tint is 1 (keeps Day bit-identical).
            if (tint == One)
                return c;
            return new Color(
                (byte)MathHelper.Clamp(c.R * tint.X, 0, 255),
                (byte)MathHelper.Clamp(c.G * tint.Y, 0, 255),
                (byte)MathHelper.Clamp(c.B * tint.Z, 0, 255),
                c.A);
        }

        static Vector4 Scale(Vector4 v, Vector3 tint)
        {
            if (tint == One)
                return v;
            return new Vector4(v.X * tint.X, v.Y * tint.Y, v.Z * tint.Z, v.W);
        }

        // Shader parameters --------------------------------------------------

        /// <summary>
        /// Cached handles of the lighting parameters of one effect (declared in
        /// Content/Shaders/TrackLighting.fxh; unused ones are optimised away by the
        /// shader compiler, so each handle can be null).
        /// </summary>
        public sealed class EffectHandles
        {
            EffectParameter lightingMode, lightCount, fogParams, fogColor, ambSky, ambGround;
            // Named float4 parameters (not arrays, GLSL std140 caveat).
            EffectParameter[] lPos = new EffectParameter[MaxLights],
                lCol = new EffectParameter[MaxLights],
                lDir = new EffectParameter[MaxLights];

            public EffectHandles(Effect effect)
            {
                lightingMode = effect.Parameters["LightingMode"];
                lightCount = effect.Parameters["LightCount"];
                fogParams = effect.Parameters["FogParams"];
                fogColor = effect.Parameters["FogColor"];
                ambSky = effect.Parameters["AmbSky"];
                ambGround = effect.Parameters["AmbGround"];
                for (int i = 0; i < MaxLights; i++)
                {
                    lPos[i] = effect.Parameters["LPos" + i];
                    lCol[i] = effect.Parameters["LCol" + i];
                    lDir[i] = effect.Parameters["LDir" + i];
                }
            }

            /// <summary>True if the effect uses at least one lighting parameter.</summary>
            public bool Any
            {
                get
                {
                    return lightingMode != null || lightCount != null ||
                        fogParams != null || fogColor != null ||
                        ambSky != null || ambGround != null || lPos[0] != null;
                }
            }

            public void Apply(TrackLighting l)
            {
                l.UpdateLightsOncePerFrame();
                if (lightingMode != null)
                    lightingMode.SetValue(l.LightingMode);
                if (lightCount != null)
                    lightCount.SetValue((float)l.LightCount);
                if (fogParams != null)
                    fogParams.SetValue(new Vector4(
                        l.Current.Fog.X, l.Current.Fog.Y, 0, 0));
                if (fogColor != null)
                    fogColor.SetValue(l.Current.FogColor);
                if (ambSky != null)
                    ambSky.SetValue(l.Current.AmbSky);
                if (ambGround != null)
                    ambGround.SetValue(l.Current.AmbGround);
                for (int i = 0; i < MaxLights; i++)
                {
                    if (lPos[i] != null)
                        lPos[i].SetValue(l.LPos[i]);
                    if (lCol[i] != null)
                        lCol[i].SetValue(l.LCol[i]);
                    if (lDir[i] != null)
                        lDir[i].SetValue(l.LDir[i]);
                }
            }
        }
    }
}
