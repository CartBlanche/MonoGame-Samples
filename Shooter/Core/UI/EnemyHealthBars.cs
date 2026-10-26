using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Gameplay.Enemies;
using Shooter.Rendering;
using Shooter.Rendering.Particles;

namespace Shooter.UI
{
    /// <summary>
    /// Port of UI/WorldspaceHealthBar.cs plus the world-space canvas under each enemy prefab (HealthBarPivot at root + (0, 2.101, 0),
    /// Canvas 1 x 0.1 m at pivot-local z -0.123 (HoverBot) / -0.117 (Turret), ImageBackground = TEX_Black_16x16 stretched, ImageFill =
    /// red TEX_White_16x16 inset 0.01 on every side, Filled / Horizontal / origin Right). The pivot LookAts the camera, so the canvas is
    /// seen from its back: the right-origin fill grows from the viewer's left. HideFullHealthBar is on in both prefabs.
    /// The bars are two transparent sprite quads per enemy (SpriteUnlit, depth tested, no depth write, like a world-space UGUI canvas).
    /// </summary>
    public class EnemyHealthBars
    {
        public const float PivotHeight = 2.101f;
        public const float CanvasWidth = 1f, CanvasHeight = 0.1f, FillInset = 0.01f;
        public const float HoverBotCanvasZ = -0.123f, TurretCanvasZ = -0.117f;

        private readonly RenderMesh _backMesh, _fillMesh;

        public EnemyHealthBars(GraphicsDevice device)
        {
            var v = new StaticMeshVertex[4];
            Vector2[] uv = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
            Vector2[] c = { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, 0.5f) };
            for (int i = 0; i < 4; i++)
                v[i] = new StaticMeshVertex { Position = new Vector3(c[i], 0f), Normal = Vector3.UnitZ, UV = uv[i], Tangent = Vector3.UnitX, Binormal = Vector3.UnitY };
            var vb = new VertexBuffer(device, StaticMeshVertex.Declaration, 4, BufferUsage.WriteOnly);
            vb.SetData(v);
            var ib = new IndexBuffer(device, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
            ib.SetData(new short[] { 0, 1, 2, 0, 2, 3 });

            // UGUI draws children in hierarchy order: a fixed queue keeps the fill above the background (sorting by depth flips when the fill is shifted sideways)
            _backMesh = Quad("HealthBarBackground", vb, ib, new Vector4(0f, 0f, 0f, 1f), 3000);
            _fillMesh = Quad("HealthBarFill", vb, ib, new Vector4(1f, 0f, 0f, 1f), 3001);
        }

        private static RenderMesh Quad(string name, VertexBuffer vb, IndexBuffer ib, Vector4 color, int renderQueue)
        {
            var mat = new Material
            {
                Name = name,
                Kind = MaterialKind.SpriteUnlit,
                Surface = SurfaceType.Transparent,
                Blend = BlendKind.Alpha,
                Cull = CullKind.None,
                ZWrite = false,
                RenderQueue = renderQueue,
                BaseColor = color,
                BaseMapIsSrgb = false,   // no texture: the solid sprite is the tint
            };
            var mesh = new RenderMesh { Name = name, Bounds = new BoundingBox(new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)) };
            mesh.Parts.Add(new MeshPart { VertexBuffer = vb, IndexBuffer = ib, PrimitiveCount = 2, Material = mat });
            return mesh;
        }

        /// <summary>Appends the bars of every living, damaged enemy for a camera at <paramref name="cameraPosition"/>.</summary>
        public void Build(IEnumerable<Enemy> enemies, Vector3 cameraPosition, List<RenderItem> output)
        {
            foreach (Enemy e in enemies)
            {
                if (e == null || e.IsDestroyed)
                    continue;
                Vector3 pivot = e.RootMatrix.Translation + Vector3.Up * PivotHeight;
                Add(pivot, e.Data.Kind == "Turret" ? TurretCanvasZ : HoverBotCanvasZ, e.Health.CurrentHealth / e.Health.MaxHealth, cameraPosition, output);
            }
        }

        /// <summary>One bar (<paramref name="ratio"/> = CurrentHealth / MaxHealth); nothing is added at full health (HideFullHealthBar).</summary>
        public void Add(Vector3 pivot, float canvasZ, float ratio, Vector3 cameraPosition, List<RenderItem> output)
        {
            if (ratio == 1f)
                return;
            ratio = MathHelper.Clamp(ratio, 0f, 1f);

            // Transform.LookAt(camera): the pivot's +Z (Unity forward) points at the camera, world up
            Vector3 toCam = cameraPosition - pivot;
            if (toCam.LengthSquared() < 1e-8f)
                return;
            Vector3 n = Vector3.Normalize(toCam);
            Vector3 right = Vector3.Cross(Vector3.Up, n);
            if (right.LengthSquared() < 1e-8f)
                right = Vector3.Right;
            right = Vector3.Normalize(right);
            Vector3 up = Vector3.Cross(n, right);

            // canvas-local z is along the pivot's forward (toward the camera): a negative z sits behind the pivot
            Vector3 center = pivot + n * canvasZ;

            output.Add(Item(_backMesh, Basis(right * CanvasWidth, up * CanvasHeight, n, center)));

            // fill rect: canvas inset by FillInset, drawn from the viewer's left
            float fullW = CanvasWidth - 2f * FillInset, h = CanvasHeight - 2f * FillInset;
            float w = fullW * ratio;
            if (w <= 0f)
                return;
            Vector3 fillCenter = center + right * (-fullW * 0.5f + w * 0.5f) + n * 0.001f;   // above the background (UGUI draws children in order)
            output.Add(Item(_fillMesh, Basis(right * w, up * h, n, fillCenter)));
        }

        private static Matrix Basis(Vector3 x, Vector3 y, Vector3 z, Vector3 t)
        {
            return new Matrix(x.X, x.Y, x.Z, 0f, y.X, y.Y, y.Z, 0f, z.X, z.Y, z.Z, 0f, t.X, t.Y, t.Z, 1f);
        }

        private static RenderItem Item(RenderMesh mesh, Matrix world)
        {
            return new RenderItem { Mesh = mesh, World = world, CastShadows = false, ReceiveShadows = false, Layer = RenderLayer.Default };
        }
    }
}
