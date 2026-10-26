using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Rendering;
using RMaterial = Shooter.Rendering.Material;

namespace Shooter.Scene.Diagnostics
{
    /// <summary>
    /// Minimal BasicEffect-based ISceneRenderer used for scene inspection when (or while) the real SceneRenderer is
    /// not available: one directional light + flat ambient, base map/color only, no shadows, no skybox, no post-processing.
    /// Everything is drawn in gamma space, so colours will not match Unity's linear-space output exactly.
    /// </summary>
    public sealed class BasicSceneRenderer : ISceneRenderer
    {
        private readonly GraphicsDevice _device;
        private readonly BasicEffect _effect;
        private readonly List<RenderItem> _opaque = new List<RenderItem>();
        private readonly List<RenderItem> _transparent = new List<RenderItem>();
        private readonly List<RenderItem> _weapon = new List<RenderItem>();

        public BasicSceneRenderer(GraphicsDevice device)
        {
            _device = device;
            _effect = new BasicEffect(device)
            {
                PreferPerPixelLighting = true,
                LightingEnabled = true,
                TextureEnabled = false,
                VertexColorEnabled = false
            };
        }

        public Texture2D Overlay { get; set; }   // not supported by the diagnostics renderer

        public void Render(GameTime gameTime, IReadOnlyList<RenderItem> items, SceneLighting lighting, Camera camera, Camera weaponCamera, PostProcessSettings post)
        {
            _opaque.Clear();
            _transparent.Clear();
            _weapon.Clear();
            foreach (RenderItem item in items)
            {
                if (item.Layer == RenderLayer.Weapon)
                {
                    _weapon.Add(item);
                }
                else
                {
                    _opaque.Add(item);
                }
            }

            _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(49, 77, 121), 1f, 0);

            _effect.AmbientLightColor = Vector3.Clamp(lighting.AmbientColor * lighting.AmbientIntensity * 0.6f, Vector3.Zero, Vector3.One);
            _effect.DirectionalLight0.Enabled = true;
            _effect.DirectionalLight0.Direction = Vector3.Normalize(lighting.SunDirection);
            _effect.DirectionalLight0.DiffuseColor = Vector3.Clamp(lighting.SunColor * lighting.SunIntensity * 2f, Vector3.Zero, Vector3.One);
            _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
            _effect.DirectionalLight1.Enabled = false;
            _effect.DirectionalLight2.Enabled = false;

            DrawList(_opaque, camera);
            if (_weapon.Count > 0)
            {
                _device.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
                DrawList(_weapon, weaponCamera);
            }
        }

        private void DrawList(List<RenderItem> list, Camera camera)
        {
            _effect.View = camera.View;
            _effect.Projection = camera.Projection;
            _device.RasterizerState = RasterizerState.CullCounterClockwise;
            _device.SamplerStates[0] = SamplerState.LinearWrap;

            // Opaque pass, then transparent parts (no depth write).
            for (int pass = 0; pass < 2; pass++)
            {
                _device.BlendState = pass == 0 ? BlendState.Opaque : BlendState.AlphaBlend;
                _device.DepthStencilState = pass == 0 ? DepthStencilState.Default : DepthStencilState.DepthRead;
                foreach (RenderItem item in list)
                {
                    _effect.World = item.World;
                    foreach (MeshPart part in item.Mesh.Parts)
                    {
                        RMaterial m = part.Material;
                        bool transparent = m != null && m.Surface == SurfaceType.Transparent;
                        if (transparent != (pass == 1))
                        {
                            continue;
                        }
                        Vector4 c = m != null ? m.BaseColor : Vector4.One;
                        _effect.DiffuseColor = new Vector3(c.X, c.Y, c.Z);
                        _effect.Alpha = transparent ? c.W : 1f;
                        Texture2D tex = m != null ? m.BaseMap : null;
                        _effect.TextureEnabled = tex != null;
                        if (tex != null)
                        {
                            _effect.Texture = tex;
                        }
                        _effect.CurrentTechnique.Passes[0].Apply();
                        _device.SetVertexBuffer(part.VertexBuffer);
                        _device.Indices = part.IndexBuffer;
                        _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.BaseVertex, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
            _device.BlendState = BlendState.Opaque;
            _device.DepthStencilState = DepthStencilState.Default;
        }
    }
}
