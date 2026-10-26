using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Rendering.Particles
{
    /// <summary>
    /// Owns the active particle systems: updates them, culls and sorts them into the renderer's transparent pass (back to front with the
    /// other transparent geometry, by render queue) and draws them as camera-facing / stretched / horizontal / vertical billboards or as
    /// instanced cubes (render mode Mesh).
    /// </summary>
    public sealed class ParticleManager
    {
        private readonly GraphicsDevice _gd;
        private readonly Effect _fx;
        private readonly List<ParticleSystemInstance> _systems = new List<ParticleSystemInstance>();
        private readonly List<ParticleSystemInstance.RenderParticle> _scratch = new List<ParticleSystemInstance.RenderParticle>(512);
        private ParticleVertex[] _verts = new ParticleVertex[4 * 256];
        private short[] _indices = new short[0];

        private VertexBuffer _cubeVb;
        private IndexBuffer _cubeIb;
        private RenderMesh _cubeMesh;

        /// <summary>Material by Unity name, for a system's trail material (second renderer material); null = use the system's own material.</summary>
        public Func<string, Material> MaterialByName { get; set; }

        private readonly List<ParticleVertex> _trailVerts = new List<ParticleVertex>();
        private readonly List<short> _trailIndices = new List<short>();

        /// <summary>Remove non-looping systems automatically once they have finished and all particles died.</summary>
        public bool AutoRemoveFinished { get; set; }

        public IReadOnlyList<ParticleSystemInstance> Systems => _systems;

        internal ParticleManager(GraphicsDevice gd, Effect particleEffect)
        {
            _gd = gd;
            _fx = particleEffect;
            EnsureIndices(4096);
        }

        private void EnsureIndices(int quads)
        {
            if (_indices.Length >= quads * 6)
                return;
            _indices = new short[quads * 6];
            for (int q = 0; q < quads; q++)
            {
                int v = q * 4;
                _indices[q * 6 + 0] = (short)(v + 0);
                _indices[q * 6 + 1] = (short)(v + 1);
                _indices[q * 6 + 2] = (short)(v + 2);
                _indices[q * 6 + 3] = (short)(v + 0);
                _indices[q * 6 + 4] = (short)(v + 2);
                _indices[q * 6 + 5] = (short)(v + 3);
            }
        }

        // ------------------------------------------------------------------ registry

        public ParticleSystemInstance Add(ParticleSystemData data, Material material, Matrix world, int seed = 0)
        {
            var inst = new ParticleSystemInstance(data, material, world, seed);
            _systems.Add(inst);
            return inst;
        }

        public void Add(ParticleSystemInstance instance)
        {
            _systems.Add(instance);
        }

        public void Remove(ParticleSystemInstance instance)
        {
            _systems.Remove(instance);
        }

        public void Clear()
        {
            _systems.Clear();
        }

        public void Update(float dt)
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                ParticleSystemInstance s = _systems[i];
                s.Update(dt);
                if (AutoRemoveFinished && !s.KeepWhenFinished && !s.IsAlive && !s.Data.Looping && !s.IsPlaying)
                    _systems.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------ renderer integration

        /// <summary>Systems on the weapon layer go to <paramref name="weaponList"/> (culled and sorted against the weapon camera), the others to <paramref name="list"/>.</summary>
        internal void CollectVisible(Camera cam, Vector3 camPos, Vector3 camForward, List<SceneRenderer.Entry> list, SceneRenderer renderer,
            Camera weaponCam = null, Vector3 weaponPos = default(Vector3), Vector3 weaponForward = default(Vector3), List<SceneRenderer.Entry> weaponList = null)
        {
            if (_systems.Count == 0)
                return;
            var frustum = new BoundingFrustum(cam.View * cam.Projection);
            BoundingFrustum weaponFrustum = weaponCam != null && weaponList != null ? new BoundingFrustum(weaponCam.View * weaponCam.Projection) : null;
            foreach (ParticleSystemInstance s in _systems)
            {
                if (!s.Visible || (s.ParticleCount == 0 && !s.HasTrails) || s.Material == null || s.Data.RenderMode == ParticleRenderMode.None)
                    continue;
                bool weapon = s.WeaponLayer && weaponFrustum != null;
                if ((weapon ? weaponFrustum : frustum).Contains(s.Bounds) == ContainmentType.Disjoint)
                    continue;
                Vector3 center = (s.Bounds.Min + s.Bounds.Max) * 0.5f;
                int queue = s.Material.RenderQueue >= 0 ? s.Material.RenderQueue : (s.Material.Surface == SurfaceType.Transparent ? 3000 : 2000);
                (weapon ? weaponList : list).Add(new SceneRenderer.Entry
                {
                    Particle = s,
                    Depth = weapon ? Vector3.Dot(center - weaponPos, weaponForward) : Vector3.Dot(center - camPos, camForward),
                    Queue = queue + s.Data.RendererPriority * 0
                });
            }
        }

        private void EnsureVerts(int count)
        {
            if (_verts.Length < count)
                _verts = new ParticleVertex[Math.Max(count, _verts.Length * 2)];
        }

        /// <summary>Draws one system. Returns the number of particles drawn.</summary>
        internal int Draw(ParticleSystemInstance sys, Effect fx, Camera cam, Vector3 camPos, Matrix viewProj, Texture2D white, Texture2D black, Texture2D flatNormal, SceneRenderer renderer)
        {
            ParticleSystemData d = sys.Data;
            int n = sys.GetRenderParticles(_scratch);
            if (n == 0)
            {
                if (sys.HasTrails)
                    DrawTrails(sys, fx, camPos, viewProj, white, black, flatNormal, renderer);
                return 0;
            }
            Material m = sys.Material;

            renderer.BindParticleMaterial(m, false, white, black, flatNormal);
            renderer.ApplyParticleState(m);
            if (sys.WeaponLayer && sys.DrawOverWeapon)
                _gd.DepthStencilState = DepthStencilState.None;
            _gd.RasterizerState = m.Cull == CullKind.None ? RasterizerState.CullNone : (m.Cull == CullKind.Front ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise);

            Matrix inv = Matrix.Invert(cam.View);
            Vector3 right = inv.Right, up = inv.Up, fwd = inv.Forward;
            float tanHalf = 1f / cam.Projection.M22;

            if (d.RenderMode == ParticleRenderMode.Mesh)
            {
                DrawMeshParticles(sys, fx, n);
                return n;
            }

            if (n > 4096)
                n = 4096;
            EnsureVerts(n * 4);
            int v = 0;
            for (int i = 0; i < n; i++)
            {
                ParticleSystemInstance.RenderParticle p = _scratch[i];
                Vector3 axisX, axisY;
                float sx = p.Size.X, sy = d.Size3D ? p.Size.Y : p.Size.X;

                // viewport relative size clamp (renderer min/max particle size)
                float dist = Math.Max(0.01f, Vector3.Dot(p.Position - camPos, fwd));
                float viewportHeight = 2f * dist * tanHalf;
                float maxSize = d.MaxParticleSize * viewportHeight;
                if (d.MaxParticleSize > 0f && Math.Max(sx, sy) > maxSize)
                {
                    float k = maxSize / Math.Max(sx, sy);
                    sx *= k;
                    sy *= k;
                }

                switch (d.RenderMode)
                {
                    case ParticleRenderMode.Stretch:
                    {
                        float speed = p.Velocity.Length();
                        Vector3 dirV = speed > 1e-4f ? p.Velocity / speed : up;
                        Vector3 toCam = Vector3.Normalize(camPos - p.Position);
                        Vector3 side = Vector3.Cross(dirV, toCam);
                        side = side.LengthSquared() > 1e-8f ? Vector3.Normalize(side) : right;
                        float length = sy * d.LengthScale + speed * d.VelocityScale;
                        if (length < 0f)
                        {
                            // a negative velocity scale (the explosion's action lines) stretches the quad backwards: flip the axis instead of the winding
                            dirV = -dirV;
                            length = -length;
                            side = -side;
                        }
                        axisX = side * (sx * 0.5f);
                        axisY = dirV * (length * 0.5f);
                        break;
                    }
                    case ParticleRenderMode.HorizontalBillboard:
                    {
                        float c = (float)Math.Cos(p.Rotation.Z), s = (float)Math.Sin(p.Rotation.Z);
                        Vector3 bx = new Vector3(c, 0f, s);
                        Vector3 by = new Vector3(-s, 0f, c) * -1f; // quad lies in XZ, its "up" points to -Z (facing +Y)
                        axisX = bx * (sx * 0.5f);
                        axisY = by * (sy * 0.5f);
                        break;
                    }
                    case ParticleRenderMode.VerticalBillboard:
                    {
                        Vector3 toCam = camPos - p.Position;
                        toCam.Y = 0f;
                        Vector3 sideV = Vector3.Cross(Vector3.UnitY, toCam);
                        sideV = sideV.LengthSquared() > 1e-8f ? Vector3.Normalize(sideV) : right;
                        float c = (float)Math.Cos(p.Rotation.Z), s = (float)Math.Sin(p.Rotation.Z);
                        axisX = (sideV * c + Vector3.UnitY * s) * (sx * 0.5f);
                        axisY = (-sideV * s + Vector3.UnitY * c) * (sy * 0.5f);
                        break;
                    }
                    default:
                    {
                        float c = (float)Math.Cos(p.Rotation.Z), s = (float)Math.Sin(p.Rotation.Z);
                        axisX = (right * c + up * s) * (sx * 0.5f);
                        axisY = (-right * s + up * c) * (sy * 0.5f);
                        break;
                    }
                }

                Vector2 u0 = p.FrameUv, u1 = p.FrameUv + p.FrameSize;
                if (d.UvFlipU) { float t = u0.X; u0.X = u1.X; u1.X = t; }
                if (d.UvFlipV) { float t = u0.Y; u0.Y = u1.Y; u1.Y = t; }

                Vector3 pc = p.Position;
                if (d.Pivot != Vector3.Zero)
                {
                    // renderer Pivot: the quad is shifted by pivot x size along its own axes (the enemies' alert mark floats above its emitter: y 0.57)
                    pc += axisX * (2f * d.Pivot.X) + axisY * (2f * d.Pivot.Y);
                    if (d.Pivot.Z != 0f)
                        pc += Vector3.Normalize(camPos - p.Position) * (Math.Max(sx, sy) * d.Pivot.Z);
                }
                _verts[v++] = new ParticleVertex { Position = pc - axisX + axisY, Color = p.Color, UV = new Vector2(u0.X, u0.Y) };
                _verts[v++] = new ParticleVertex { Position = pc + axisX + axisY, Color = p.Color, UV = new Vector2(u1.X, u0.Y) };
                _verts[v++] = new ParticleVertex { Position = pc + axisX - axisY, Color = p.Color, UV = new Vector2(u1.X, u1.Y) };
                _verts[v++] = new ParticleVertex { Position = pc - axisX - axisY, Color = p.Color, UV = new Vector2(u0.X, u1.Y) };
            }

            fx.CurrentTechnique = fx.Techniques["ParticlesBillboard"];
            fx.CurrentTechnique.Passes[0].Apply();
            _gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _verts, 0, n * 4, _indices, 0, n * 2);
            if (sys.HasTrails)
                DrawTrails(sys, fx, camPos, viewProj, white, black, flatNormal, renderer);
            return n;
        }

        /// <summary>The Trails module: ribbons behind the particles, drawn with the renderer's trail material.</summary>
        private void DrawTrails(ParticleSystemInstance sys, Effect fx, Vector3 camPos, Matrix viewProj, Texture2D white, Texture2D black, Texture2D flatNormal, SceneRenderer renderer)
        {
            _trailVerts.Clear();
            _trailIndices.Clear();
            sys.BuildTrailGeometry(camPos, _trailVerts, _trailIndices);
            if (_trailIndices.Count == 0)
                return;
            if (sys.TrailMaterial == null)
                sys.TrailMaterial = (MaterialByName != null && sys.Data.TrailMaterialName != null ? MaterialByName(sys.Data.TrailMaterialName) : null) ?? sys.Material;
            Material tm = sys.TrailMaterial;
            renderer.BindParticleMaterial(tm, false, white, black, flatNormal);
            renderer.ApplyParticleState(tm);
            _gd.RasterizerState = RasterizerState.CullNone;
            fx.CurrentTechnique = fx.Techniques["ParticlesBillboard"];
            fx.CurrentTechnique.Passes[0].Apply();
            _gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _trailVerts.ToArray(), 0, _trailVerts.Count, _trailIndices.ToArray(), 0, _trailIndices.Count / 3);
        }

        // ------------------------------------------------------------------ mesh particles (built-in cube)

        private void EnsureCube()
        {
            if (_cubeVb != null)
                return;
            var verts = new List<StaticMeshVertex>();
            var idx = new List<short>();
            Vector3[] normals = { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ };
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = normals[f];
                Vector3 t = Math.Abs(n.Y) > 0.9f ? Vector3.UnitX : (Math.Abs(n.X) > 0.9f ? -Vector3.UnitZ * Math.Sign(n.X) : Vector3.UnitX * Math.Sign(n.Z));
                Vector3 b = Vector3.Cross(n, t);
                int baseIndex = verts.Count;
                Vector2[] uv = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                Vector2[] c = { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
                for (int k = 0; k < 4; k++)
                    verts.Add(new StaticMeshVertex { Position = (n + t * c[k].X + b * c[k].Y) * 0.5f, Normal = n, UV = uv[k], Tangent = t, Binormal = -b });
                // winding: clockwise seen from outside (MonoGame front face)
                idx.AddRange(new short[] { (short)baseIndex, (short)(baseIndex + 1), (short)(baseIndex + 2), (short)baseIndex, (short)(baseIndex + 2), (short)(baseIndex + 3) });
            }
            _cubeVb = new VertexBuffer(_gd, StaticMeshVertex.Declaration, verts.Count, BufferUsage.WriteOnly);
            _cubeVb.SetData(verts.ToArray());
            _cubeIb = new IndexBuffer(_gd, IndexElementSize.SixteenBits, idx.Count, BufferUsage.WriteOnly);
            _cubeIb.SetData(idx.ToArray());
        }

        private void DrawMeshParticles(ParticleSystemInstance sys, Effect fx, int n)
        {
            EnsureCube();
            fx.CurrentTechnique = fx.Techniques["ParticlesMesh"];
            _gd.SetVertexBuffer(_cubeVb);
            _gd.Indices = _cubeIb;
            for (int i = 0; i < n; i++)
            {
                ParticleSystemInstance.RenderParticle p = _scratch[i];
                Matrix world = Matrix.CreateScale(p.Size) * Matrix.CreateFromYawPitchRoll(p.Rotation.Y, p.Rotation.X, p.Rotation.Z) * Matrix.CreateTranslation(p.Position);
                fx.Parameters["World"].SetValue(world);
                // particle colour modulates through BaseColor for mesh particles (vertex colour is white on the mesh path)
                Vector4 bc = sys.Material.BaseColor;
                if (sys.Material.ColorsAreGamma)
                    bc = SceneRenderer.GammaToLinear(bc);
                fx.Parameters["BaseColor"].SetValue(bc * p.Color);
                fx.CurrentTechnique.Passes[0].Apply();
                _gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }
        }
    }
}
