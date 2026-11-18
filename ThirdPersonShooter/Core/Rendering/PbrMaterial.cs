using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>Runtime form of a Materials.json StandardMaterial3D entry (textures resolved, colours in linear).</summary>
    public sealed class PbrMaterial
    {
        public Texture2D Albedo, Normal, Orm, Emission; // null = use the renderer's neutral dummy
        public Vector4 AlbedoColor = Vector4.One;
        public Vector3 EmissionColor = Vector3.Zero;
        public float EmissionEnergy = 1, Metallic = 0, Roughness = 1, Specular = 0.5f, NormalScale = 1;
        public Vector4 UvTransform = new Vector4(1, 1, 0, 0);
        public float AlphaCut; // lighting pass 5: Godot transparency 2 (alpha scissor) threshold, 0 = off
        public bool HasAo, Transparent, Skip; // Transparent: alpha/blended materials are skipped until the alpha pass exists
        public Vector3 RoughMask = new Vector3(0, 1, 0), MetalMask = new Vector3(0, 0, 1);
        public string Name = "";

        static float Lin(float c) => MathF.Pow(MathF.Max(c, 0f), 2.2f);

        public static PbrMaterial Fallback { get; } = new PbrMaterial { AlbedoColor = new Vector4(0.3f, 0.3f, 0.3f, 1), Name = "fallback" };

        public static PbrMaterial FromJson(JsonElement m, Func<string, Texture2D> tex)
        {
            var r = new PbrMaterial { Name = m.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "" };
            string S(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            float F(string k, float d) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : d;
            bool B(string k, bool d) => m.TryGetProperty(k, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : d;
            r.Skip = S("type") != "StandardMaterial3D"; r.Transparent = F("transparency", 0) == 1; if (F("transparency", 0) == 2) r.AlphaCut = F("alphaScissor", 0.5f); // alpha-blend and ShaderMaterial (light shafts etc.) wait for their own passes
            if (S("albedo") is string a) r.Albedo = tex(a);
            if (B("normalEnabled", true) && S("normal") is string n) r.Normal = tex(n);
            if (S("orm") is string o) r.Orm = tex(o);
            r.HasAo = r.Orm != null && B("aoEnabled", false);
            if (m.TryGetProperty("albedoColor", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() >= 3)
                r.AlbedoColor = new Vector4(Lin(c[0].GetSingle()), Lin(c[1].GetSingle()), Lin(c[2].GetSingle()), c.GetArrayLength() > 3 ? c[3].GetSingle() : 1f);
            r.Metallic = F("metallic", 0); r.Roughness = F("roughness", 1); r.Specular = F("specular", 0.5f); r.NormalScale = F("normalScale", 1);
            if (m.TryGetProperty("texChannels", out var tc))
            {
                Vector3 Ch(string k, int d) { int c = tc.TryGetProperty(k, out var v) ? v.GetInt32() : d; return new Vector3(c == 0 ? 1 : 0, c == 1 ? 1 : 0, c == 2 ? 1 : 0); }
                r.RoughMask = Ch("roughness", 1); r.MetalMask = Ch("metallic", 2);
            }
            if (r.Orm == null) { } // no ORM: the dummy is white, so Roughness/Metallic act as constants (G=R=B=1)
            if (B("emissionEnabled", false))
            {
                r.EmissionEnergy = F("emissionEnergy", 1);
                if (S("emission") is string e) r.Emission = tex(e);
                var ec = Vector3.One;
                if (m.TryGetProperty("emissionColor", out var col) && col.ValueKind == JsonValueKind.Array && col.GetArrayLength() >= 3)
                    ec = new Vector3(Lin(col[0].GetSingle()), Lin(col[1].GetSingle()), Lin(col[2].GetSingle()));
                else if (r.Emission == null) ec = Vector3.Zero;
                r.EmissionColor = ec;
            }
            if (m.TryGetProperty("uv1Scale", out var sc) && sc.ValueKind == JsonValueKind.Array && m.TryGetProperty("uv1Offset", out var of) && of.ValueKind == JsonValueKind.Array)
                r.UvTransform = new Vector4(sc[0].GetSingle(), sc[1].GetSingle(), of[0].GetSingle(), of[1].GetSingle());
            return r;
        }
    }
}
