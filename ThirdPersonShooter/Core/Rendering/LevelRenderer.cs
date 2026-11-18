using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>Draws the static level with PbrLit. Every effect parameter is set from C# on every call.</summary>
    public sealed class LevelRenderer
    {
        readonly Effect _fx;
        readonly Texture2D _white, _black, _flatNormal;
        public int DrawCalls, Triangles;
        static readonly bool _skipNull = System.Environment.GetEnvironmentVariable("TPS_DRAWNULL") == null /* default: skip. Material-less glb primitives are the "-colonly" collision proxies (props.glb: 35 prims); Godot imports them collision-only, drawing them as grey fallback z-fought the real props (the pale blotchy walls) */, _skipAlpha = System.Environment.GetEnvironmentVariable("TPS_SKIPALPHA") != null;

        static readonly float[] _dbgBox = System.Environment.GetEnvironmentVariable("TPS_DUMPBOX") is string bx ? Array.ConvertAll(bx.Split(','), t => float.Parse(t, System.Globalization.CultureInfo.InvariantCulture)) : null;
        static bool InDbgBox(DrawItem item, Matrix vp, GraphicsDevice gd)
        {
            if (_dbgBox == null) return true;
            var c = item.Mesh.BoundingSphere.Transform(item.World).Center; var q = Vector4.Transform(new Vector4(c, 1f), vp); if (q.W <= 0f) return false;
            float sx = (q.X / q.W * 0.5f + 0.5f) * gd.Viewport.Width, sy = (0.5f - q.Y / q.W * 0.5f) * gd.Viewport.Height;
            return MathF.Abs(sx - _dbgBox[0]) < _dbgBox[2] && MathF.Abs(sy - _dbgBox[1]) < _dbgBox[2];
        }
        static readonly bool _noCut = System.Environment.GetEnvironmentVariable("TPS_NOCUT") != null;
        static readonly string _dbgNode = System.Environment.GetEnvironmentVariable("TPS_SKIPNODE");
        static readonly string _dbgSkip = System.Environment.GetEnvironmentVariable("TPS_SKIPMAT");
        static readonly bool _dbgDump = System.Environment.GetEnvironmentVariable("TPS_DUMPWALL") != null;
        public readonly SceneLighting Lighting = new SceneLighting();
        /// <summary>Non-null while shadows are on for this frame (set by the game before Draw); selects the PbrLitShadow technique.</summary>
        public ShadowMap Shadow;
        /// <summary>True when the key map or any spot map is active this frame (shadow technique).</summary>
        public bool SpotOn => Lighting.Spots != null && Lighting.Spots.Enabled;
        public bool AnyShadow => Shadow != null || (Lighting.Spots != null && Lighting.Spots.Enabled);

        /// <summary>Depth-only pass of the opaque, non-alpha-cut level items inside the shadow frustum (alpha-cut grates and material-less proxies are skipped: no light leaking, no phantom shadows).</summary>
        public void DrawShadow(GraphicsDevice gd, LevelScene scene, ShadowMap sm)
        {
            var dfx = sm.Depth;
            foreach (var item in scene.Items)
            {
                if (!sm.Contains(item.Mesh.BoundingSphere.Transform(item.World))) continue;
                bool any = false;
                for (int p = 0; p < item.Mesh.MeshParts.Count; p++) { var m = item.PartMaterials[p]; if (m != null && !m.Skip && !m.Transparent && m.AlphaCut <= 0f) { any = true; break; } }
                if (!any) continue;
                dfx.Parameters["World"].SetValue(item.World);
                dfx.CurrentTechnique.Passes[0].Apply();
                for (int p = 0; p < item.Mesh.MeshParts.Count; p++)
                {
                    var m = item.PartMaterials[p];
                    if (m == null || m.Skip || m.Transparent || m.AlphaCut > 0f) continue;
                    var part = item.Mesh.MeshParts[p];
                    gd.SetVertexBuffer(part.VertexBuffer); gd.Indices = part.IndexBuffer;
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    sm.Casters++;
                }
            }
        }

        public LevelRenderer(GraphicsDevice gd, Effect fx)
        {
            _fx = fx;
            Lighting.ApplyLevelGrade();
            _white = Solid(gd, new Color(255, 255, 255, 255));
            _black = Solid(gd, new Color(0, 0, 0, 255));
            _flatNormal = Solid(gd, new Color(128, 128, 255, 255));
        }

        static Texture2D Solid(GraphicsDevice gd, Color c) { var t = new Texture2D(gd, 1, 1, false, SurfaceFormat.Color); t.SetData(new[] { c }); return t; }

        void P(string n, float v) => _fx.Parameters[n].SetValue(v);
        void P(string n, Vector3 v) => _fx.Parameters[n].SetValue(v);
        void P(string n, Vector4 v) => _fx.Parameters[n].SetValue(v);

        public void Draw(GraphicsDevice gd, LevelScene scene, Matrix view, Matrix proj, Vector3 camPos)
        {
            DrawCalls = 0; Triangles = 0;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullClockwise; // glTF front faces are CCW; XNA treats CW as front
            gd.BlendState = BlendState.Opaque;
            var aniso = new SamplerState { Filter = TextureFilter.Anisotropic, MaxAnisotropy = 8, AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap };
            for (int i = 0; i < 4; i++) gd.SamplerStates[i] = aniso;
            _fx.Parameters["ViewProj"].SetValue(view * proj);
            P("CameraPos", camPos);
            if (System.Environment.GetEnvironmentVariable("TPS_DBG") != null) { var seen = new System.Collections.Generic.HashSet<string>(); foreach (var it in scene.Items) for (int q = 0; q < it.Mesh.MeshParts.Count; q++) { var mm = it.PartMaterials[q]; string tag = mm == null ? "NULL" : (mm.Transparent ? "ALPHA" : null); if (tag != null && seen.Add(it.Model + ":" + tag + ":" + (mm?.Name ?? ""))) System.Console.WriteLine($"[dbg] {it.Model} part{q} {tag} {mm?.Name}"); } }
            if (_dbgDump) { Vector3 mn = new Vector3(1e9f), mx = new Vector3(-1e9f); foreach (var it in scene.Items) { var bs = it.Mesh.BoundingSphere.Transform(it.World); mn = Vector3.Min(mn, bs.Center - new Vector3(bs.Radius)); mx = Vector3.Max(mx, bs.Center + new Vector3(bs.Radius)); } System.Console.WriteLine($"[extent] min {mn} max {mx} farthestCorner {Math.Max(Vector3.Distance(camPos, mn), Vector3.Distance(camPos, mx))}"); }
            Lighting.Apply(_fx, camPos);
            _fx.CurrentTechnique = _fx.Techniques[Shadow != null ? "PbrLitShadow" : SpotOn ? "PbrLitSpot" : "PbrLit"]; if (Shadow != null) Shadow.Apply(_fx);
            for (int pass = 0; pass < 2; pass++)
            {
                bool alphaPass = pass == 1;
                gd.BlendState = alphaPass ? BlendState.NonPremultiplied : BlendState.Opaque;
                gd.DepthStencilState = alphaPass ? DepthStencilState.DepthRead : DepthStencilState.Default;
            var order = scene.Items;
            if (alphaPass) { order = new System.Collections.Generic.List<DrawItem>(scene.Items); order.Sort((a, b) => Vector3.DistanceSquared(b.World.Translation, camPos).CompareTo(Vector3.DistanceSquared(a.World.Translation, camPos))); }
            foreach (var item in order)
            {
                _fx.Parameters["World"].SetValue(item.World);
                for (int p = 0; p < item.Mesh.MeshParts.Count; p++)
                {
                    var m = item.PartMaterials[p] ?? PbrMaterial.Fallback;
                    if (m.Skip || m.Transparent != alphaPass) continue;
                    if (_dbgNode != null && ($"{item.Model}/{item.Mesh.ParentBone.Name}/{item.Mesh.MeshParts[p].Tag}/").Contains("/" + _dbgNode + "/")) continue;
                    if (_dbgSkip != null && (item.Model.Contains(_dbgSkip) || (m.Name ?? "").Contains(_dbgSkip))) continue;
                    if (_dbgDump && !alphaPass && Vector3.Distance(item.World.Translation, camPos) < 45f && InDbgBox(item, view * proj, gd)) System.Console.WriteLine($"[wall] {item.Model}/{item.Mesh.ParentBone.Name}/{item.Mesh.MeshParts[p].Tag} p{p} {m.Name} d={Vector3.Distance(item.World.Translation, camPos):F1} at {item.World.Translation}");
                    if (item.PartMaterials[p] == null && _skipNull) continue; if (alphaPass && _skipAlpha) continue;
                    _fx.Parameters["AlbedoTex"].SetValue(m.Albedo ?? _white);
                    _fx.Parameters["NormalTex"].SetValue(m.Normal ?? _flatNormal);
                    _fx.Parameters["OrmTex"].SetValue(m.Orm ?? _white);
                    _fx.Parameters["EmisTex"].SetValue(m.Emission ?? (m.EmissionColor != Vector3.Zero ? _white : _black));
                    P("AlbedoColor", m.Name == "tile_steel" ? new Vector4(m.AlbedoColor.X * Lighting.SteelTint.X, m.AlbedoColor.Y * Lighting.SteelTint.Y, m.AlbedoColor.Z * Lighting.SteelTint.Z, m.AlbedoColor.W) : m.AlbedoColor); P("EmissionColor", m.EmissionColor); P("EmissionEnergy", m.EmissionEnergy);
                    P("Metallic", m.Metallic); P("Roughness", m.Roughness); P("Specular", m.Specular); P("NormalScale", m.NormalScale);
                    P("UvTransform", m.UvTransform); P("RoughMask", m.RoughMask); P("MetalMask", m.MetalMask); P("MatFlags", new Vector4(m.HasAo ? 1 : 0, _noCut ? 0 : m.AlphaCut, 0, 0));
                    _fx.CurrentTechnique.Passes[0].Apply();
                    var part = item.Mesh.MeshParts[p];
                    gd.SetVertexBuffer(part.VertexBuffer);
                    gd.Indices = part.IndexBuffer;
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    DrawCalls++; Triangles += part.PrimitiveCount;
                }
            }
            }
        }
    }
}
