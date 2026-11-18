using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>Skinned character (player, red robot): model + materials (Materials.json glbMaterialMap["player"]) + AnimationPlayer, drawn with PbrLitSkinned.</summary>
    public sealed class PlayerRenderer
    {
        public readonly SkinnedModel Model;
        public AnimationPlayer Anim; // replaced by the player's PlayerAnimator.Pose in play mode
        public Matrix World = Matrix.Identity;
        readonly Effect _fx;
        readonly PbrMaterial[] _mats;
        readonly Texture2D _white, _black, _flatNormal;
        readonly Matrix[] _palette = new Matrix[72];
        public int Triangles;

        public PlayerRenderer(GraphicsDevice gd, ContentManager content, string contentRoot, string modelName = "player")
        {
            Model = content.Load<SkinnedModel>("Models/" + modelName);
            Anim = new AnimationPlayer(Model);
            _fx = content.Load<Effect>("Effects/PbrLitSkinned");
            _white = Solid(gd, Color.White); _black = Solid(gd, Color.Black); _flatNormal = Solid(gd, new Color(128, 128, 255, 255));
            using var matDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "Materials.json")));
            var mats = matDoc.RootElement.GetProperty("materials"); var map = matDoc.RootElement.GetProperty("glbMaterialMap");
            var mc = new Dictionary<string, PbrMaterial>(); var tc = new Dictionary<string, Texture2D>();
            _mats = new PbrMaterial[Model.Parts.Count];
            for (int i = 0; i < _mats.Length; i++) _mats[i] = LevelScene.ResolveMaterial(map, mats, modelName, Model.Parts[i].Material, mc, tc, content);
        }

        static Texture2D Solid(GraphicsDevice gd, Color c) { var t = new Texture2D(gd, 1, 1); t.SetData(new[] { c }); return t; }
        void P(string n, float v) => _fx.Parameters[n].SetValue(v);
        void P(string n, Vector3 v) => _fx.Parameters[n].SetValue(v);
        void P(string n, Vector4 v) => _fx.Parameters[n].SetValue(v);

        public void Draw(GraphicsDevice gd, LevelRenderer env, Matrix view, Matrix proj, Vector3 camPos) { Shadow = env.Shadow; ShadowTech = env.AnyShadow; Draw(gd, env.Lighting, view, proj, camPos); Shadow = null; ShadowTech = false; }
        public ShadowMap Shadow; public bool ShadowTech;

        /// <summary>Depth-only pass of the skinned model into the shadow map (call between ShadowMap.Begin and End with the skinned technique selected).</summary>
        public void DrawShadow(GraphicsDevice gd, ShadowMap sm)
        {
            var dfx = sm.Depth; dfx.Parameters["World"].SetValue(World);
            for (int p = 0; p < Model.Parts.Count; p++)
            {
                var part = Model.Parts[p]; var m = _mats[p];
                if (m != null && (m.Skip || m.Transparent)) continue;
                for (int i = 0; i < part.Palette.Length; i++) _palette[i] = Anim.Skin[part.Palette[i]];
                dfx.Parameters["Bones"].SetValue(_palette);
                dfx.CurrentTechnique.Passes[0].Apply();
                gd.SetVertexBuffer(part.Vertices); gd.Indices = part.Indices;
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, part.PrimitiveCount);
                sm.Casters++;
            }
        }

        /// <summary>Draw lit by an arbitrary light set (the menu backdrop has its own).</summary>
        public void Draw(GraphicsDevice gd, SceneLighting lighting, Matrix view, Matrix proj, Vector3 camPos)
        {
            Triangles = 0;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullClockwise; // glTF CCW fronts, see _SHARED correction
            gd.BlendState = BlendState.Opaque;
            var aniso = new SamplerState { Filter = TextureFilter.Anisotropic, MaxAnisotropy = 8, AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap };
            for (int i = 0; i < 4; i++) gd.SamplerStates[i] = aniso;
            _fx.Parameters["ViewProj"].SetValue(view * proj);
            _fx.Parameters["World"].SetValue(World);
            P("CameraPos", camPos);
            lighting.Apply(_fx, World.Translation);
            _fx.CurrentTechnique = _fx.Techniques[Shadow != null ? "PbrLitSkinnedShadow" : ShadowTech ? "PbrLitSkinnedSpot" : "PbrLitSkinned"]; if (Shadow != null) Shadow.Apply(_fx);
            var ptint = lighting.PlayerTint; _fx.Parameters["KeyNear"]?.SetValue(Vector2.One); // the player is lit like in pass 3 (no near-camera key falloff)
            for (int p = 0; p < Model.Parts.Count; p++)
            {
                var part = Model.Parts[p]; var m = _mats[p] ?? PbrMaterial.Fallback;
                for (int i = 0; i < part.Palette.Length; i++) _palette[i] = Anim.Skin[part.Palette[i]];
                _fx.Parameters["Bones"].SetValue(_palette);
                _fx.Parameters["AlbedoTex"].SetValue(m.Albedo ?? _white);
                _fx.Parameters["NormalTex"].SetValue(m.Normal ?? _flatNormal);
                _fx.Parameters["OrmTex"].SetValue(m.Orm ?? _white);
                _fx.Parameters["EmisTex"].SetValue(m.Emission ?? (m.EmissionColor != Vector3.Zero ? _white : _black));
                P("AlbedoColor", new Vector4(m.AlbedoColor.X * ptint.X, m.AlbedoColor.Y * ptint.Y, m.AlbedoColor.Z * ptint.Z, m.AlbedoColor.W)); P("EmissionColor", m.EmissionColor); P("EmissionEnergy", m.EmissionEnergy);
                P("Metallic", m.Metallic); P("Roughness", m.Roughness); P("Specular", m.Specular); P("NormalScale", m.NormalScale);
                P("UvTransform", m.UvTransform); P("RoughMask", m.RoughMask); P("MetalMask", m.MetalMask); P("MatFlags", new Vector4(m.HasAo ? 1 : 0, 0, 0, 0));
                _fx.CurrentTechnique.Passes[0].Apply();
                gd.SetVertexBuffer(part.Vertices); gd.Indices = part.Indices;
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, part.PrimitiveCount);
                Triangles += part.PrimitiveCount;
            }
        }
    }
}
