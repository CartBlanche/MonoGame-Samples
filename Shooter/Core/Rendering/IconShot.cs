using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Gameplay.Effects;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Rendering
{
    /// <summary>
    /// --icon: renders the Blaster viewmodel alone into a transparent square PNG (app icon master). The renderer has no alpha output, so the
    /// weapon is rendered on a black and on a white background (the "sky" is a white texture, post effects that vary per frame are off) together
    /// with the two empty backgrounds; per pixel alpha = 1 - (weapon on white - weapon on black) / (empty white - empty black) and the colour is
    /// recovered from the black render. The result is cropped to its alpha bounds and scaled to fit the square with a margin.
    /// </summary>
    public static class IconShot
    {
        public struct Options
        {
            public int Size;              // output edge in pixels (1024)
            public float Yaw, Pitch, Roll;   // degrees: camera direction around the weapon (0 yaw = from its right side), elevation, roll about the view axis
            public float Margin;          // fraction of the square left empty on each side
            public float Flash;           // age in seconds of a VFX_MuzzleFlashGreen at the muzzle (0 = none); its light is added on top of the matte
            public float Blast;           // metres in front of the muzzle where a Blaster bolt is drawn (0 = none)
        }

        private const float BoltLength = 1.0f, BoltWidthScale = 1.0f;

        public static Options Defaults { get { return new Options { Size = 1024, Yaw = 150f, Pitch = 14f, Roll = 0f, Margin = 0.06f, Blast = 0f, Flash = 0.05f }; } }

        public static void Render(GraphicsDevice gd, LoadedScene scene, ISceneRenderer renderer, GameTime gameTime, VfxLibrary vfx, ParticleManager particles, string file, Options o)
        {
            const int Oversample = 2;
            int big = o.Size * Oversample;
            var items = new List<RenderItem>();
            scene.BuildWeaponItems(Matrix.Identity, items);
            float[] mz = scene.Data.Weapon.MuzzleInRoot;
            Vector3 muzzleWorld = Vector3.Transform(new Vector3(mz[0], mz[1], mz[2]), scene.WeaponRootInCamera);
            if (o.Blast > 0f)
            {
                // a bolt just leaving the muzzle (the weapon looks down -Z in its own space)
                float[] m = scene.Data.Weapon.MuzzleInRoot;
                Vector3 muzzle = Vector3.Transform(new Vector3(m[0], m[1], m[2]), scene.WeaponRootInCamera);
                // the real bolt is a 12 m streak that only reads in flight: for the icon it is shortened to BoltLength and made a little fatter
                var bolt = new List<RenderItem>();
                scene.BuildProjectileItems(Vector3.Zero, Vector3.Forward, bolt);
                var pts = new List<Vector3>();
                foreach (RenderItem it in bolt) foreach (Vector3 c in it.Mesh.Bounds.GetCorners()) pts.Add(Vector3.Transform(c, it.World));
                BoundingBox bb = BoundingBox.CreateFromPoints(pts);
                float kz = BoltLength / (bb.Max.Z - bb.Min.Z), kxy = BoltWidthScale;
                float tailZ = muzzle.Z - o.Blast;   // the bolt's back end (largest z) sits Blast metres in front of the muzzle
                Matrix place = Matrix.CreateScale(kxy, kxy, kz) * Matrix.CreateTranslation(muzzle.X, muzzle.Y, tailZ - bb.Max.Z * kz);
                foreach (RenderItem it in bolt)
                {
                    RenderItem moved = it;
                    moved.World = it.World * place;
                    items.Add(moved);
                }
            }

            // bounds of the weapon (corners of every mesh's box in world space)
            var corners = new List<Vector3>();
            foreach (RenderItem it in items)
            {
                BoundingBox b = it.Mesh.Bounds;
                foreach (Vector3 c in b.GetCorners()) corners.Add(Vector3.Transform(c, it.World));
            }
            if (o.Flash > 0f) corners.Add(muzzleWorld + Vector3.Forward * 0.7f);   // room for the flash's glow in front of the muzzle
            var box = BoundingBox.CreateFromPoints(corners);
            Vector3 center = (box.Min + box.Max) * 0.5f;
            float radius = Vector3.Distance(box.Min, box.Max) * 0.5f;

            // camera: yaw 0 looks from the weapon's right (+X) towards -X (the muzzle, -Z, points to the right of the picture); yaw turns it towards the muzzle
            float yaw = MathHelper.ToRadians(o.Yaw), pitch = MathHelper.ToRadians(o.Pitch);
            Vector3 dir = new Vector3((float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)Math.Sin(pitch), (float)(-Math.Sin(yaw) * Math.Cos(pitch)));
            float distance = radius * 6f;
            Vector3 eye = center + dir * distance;
            Vector3 forward = Vector3.Normalize(center - eye);
            Vector3 up = Vector3.Transform(Vector3.Up, Matrix.CreateFromAxisAngle(forward, MathHelper.ToRadians(o.Roll)));
            Matrix world = Matrix.CreateWorld(eye, forward, up);

            // centre the weapon in the frame and pick a field of view that holds all corners (the result is cropped afterwards)
            Matrix view = Matrix.Invert(world);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 p in corners)
            {
                Vector3 v = Vector3.Transform(p, view);
                float tx = v.X / -v.Z, ty = v.Y / -v.Z;
                minX = Math.Min(minX, tx); maxX = Math.Max(maxX, tx);
                minY = Math.Min(minY, ty); maxY = Math.Max(maxY, ty);
            }
            eye += world.Right * (((minX + maxX) * 0.5f) * distance) + world.Up * (((minY + maxY) * 0.5f) * distance);   // slide the camera in its own plane to centre the weapon
            world = Matrix.CreateWorld(eye, forward, up);
            float half = Math.Max(maxX - minX, maxY - minY) * 0.5f * 1.1f;
            float fov = 2f * (float)Math.Atan(half);

            Camera cam = scene.Camera, wcam = scene.WeaponCamera;
            foreach (Camera c in new[] { cam, wcam })
            {
                c.FieldOfViewY = fov;
                c.NearPlane = Math.Max(0.01f, distance - radius * 2f);
                c.FarPlane = distance + radius * 4f;
                SceneLoader.SetCamera(c, world, 1f);
            }

            // steady post chain: nothing that changes between the four renders
            PostProcessSettings post = scene.Post;
            post.BloomIntensity = 0f; post.VignetteIntensity = 0f; post.FilmGrainIntensity = 0f; post.MotionBlurIntensity = 0f; post.Fxaa = false;
            SceneLighting lighting = scene.Lighting;
            // the level's sun is orange and comes from one side: light the visible side with a neutral key from above-left of the camera instead
            lighting.SunColor = new Vector3(1f, 0.96f, 0.9f);
            lighting.SunIntensity = 0.9f;
            lighting.SunDirection = Vector3.Normalize(forward + world.Down * 0.7f + world.Right * 0.45f);
            var white = new Texture2D(gd, 1, 1);
            white.SetData(new[] { Color.White });
            Texture2D realSky = lighting.SkyboxTexture;
            float realExposure = lighting.SkyExposure;
            Vector4 realTint = lighting.SkyTint;

            var rt = new RenderTarget2D(gd, big, big, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            Func<bool, bool, Color[]> capture = (weapon, whiteBackground) =>
            {
                lighting.SkyboxTexture = whiteBackground ? white : null;
                lighting.SkyExposure = 8f;
                lighting.SkyTint = new Vector4(1f, 1f, 1f, 0.5f);
                renderer.Overlay = null;
                gd.SetRenderTarget(rt);
                var list = weapon ? items : new List<RenderItem>();
                renderer.Render(gameTime, list, lighting, cam, wcam, post);
                renderer.Render(gameTime, list, lighting, cam, wcam, post);
                gd.SetRenderTarget(null);
                var data = new Color[big * big];
                rt.GetData(data);
                return data;
            };
            Color[] bgBlack = capture(false, false), bgWhite = capture(false, true), wBlack = capture(true, false), wWhite = capture(true, true);
            Color[] flashBlack = null;
            VfxLibrary.Instance flash = null;
            if (o.Flash > 0f && vfx != null && particles != null)
            {
                // the flash is additive light with no coverage: render it on black next to the plain weapon and add the difference
                flash = vfx.Spawn("VFX_MuzzleFlashGreen", Matrix.CreateTranslation(muzzleWorld + Vector3.Forward * 0.12f), 2f);
                flash.SetWeaponLayer(true);
                int steps = (int)Math.Round(o.Flash * 60f);
                for (int i = 0; i <= steps; i++) particles.Update(1f / 60f);
                flashBlack = capture(true, false);
                vfx.Destroy(flash);
            }
            lighting.SkyboxTexture = realSky; lighting.SkyExposure = realExposure; lighting.SkyTint = realTint;
            rt.Dispose();
            white.Dispose();

            // matte: premultiplied float RGBA
            var pm = new float[big * big * 4];
            int left = big, top = big, right = -1, bottom = -1;
            for (int i = 0; i < big * big; i++)
            {
                float a = 0f, n = 0f;
                float[] bb = { bgBlack[i].R / 255f, bgBlack[i].G / 255f, bgBlack[i].B / 255f };
                float[] bw = { bgWhite[i].R / 255f, bgWhite[i].G / 255f, bgWhite[i].B / 255f };
                float[] ob = { wBlack[i].R / 255f, wBlack[i].G / 255f, wBlack[i].B / 255f };
                float[] ow = { wWhite[i].R / 255f, wWhite[i].G / 255f, wWhite[i].B / 255f };
                for (int c = 0; c < 3; c++)
                {
                    float span = bw[c] - bb[c];
                    if (span > 0.2f) { a += 1f - (ow[c] - ob[c]) / span; n += 1f; }
                }
                a = n > 0f ? MathHelper.Clamp(a / n, 0f, 1f) : 1f;
                if (a < 0.004f) a = 0f;
                // weapon on black: colour * alpha + (1 - alpha) * black background = premultiplied colour (plus the background's own black level)
                for (int c = 0; c < 3; c++)
                {
                    pm[i * 4 + c] = a > 0f ? Math.Max(0f, Math.Min(a, ob[c] - (1f - a) * bb[c])) : 0f;
                }
                if (flashBlack != null)
                {
                    float fmax = 0f;
                    float[] fb = { flashBlack[i].R / 255f, flashBlack[i].G / 255f, flashBlack[i].B / 255f };
                    for (int c = 0; c < 3; c++)
                    {
                        float add = Math.Max(0f, fb[c] - ob[c]);
                        fmax = Math.Max(fmax, add);
                        pm[i * 4 + c] += add;   // premultiplied: the glow's light over whatever is behind
                    }
                    a = Math.Max(a, fmax);      // coverage at least as strong as the brightest light it adds
                    for (int c = 0; c < 3; c++) pm[i * 4 + c] = Math.Min(pm[i * 4 + c], a);
                }
                pm[i * 4 + 3] = a;
                if (a > 0.02f)
                {
                    int x = i % big, y = i / big;
                    left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                }
            }
            if (right < 0) throw new InvalidOperationException("icon: the weapon left no pixels");
            Console.WriteLine("  icon: weapon covers " + (right - left + 1) + " x " + (bottom - top + 1) + " of " + big + " x " + big + " (fov " + MathHelper.ToDegrees(fov).ToString("0.0") + ")");

            // crop to the alpha bounds, fit into the square with the margin, area-average on premultiplied values
            int cw = right - left + 1, ch = bottom - top + 1;
            float fit = o.Size * (1f - 2f * o.Margin) / Math.Max(cw, ch);
            float ow2 = cw * fit, oh2 = ch * fit;
            float ox = (o.Size - ow2) * 0.5f, oy = (o.Size - oh2) * 0.5f;
            var outBytes = new byte[o.Size * o.Size * 4];
            float inv = 1f / fit;
            const int Samples = 4;
            for (int y = 0; y < o.Size; y++)
            {
                for (int x = 0; x < o.Size; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0; int count = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float fx = (x + (sx + 0.5f) / Samples - ox) * inv + left, fy = (y + (sy + 0.5f) / Samples - oy) * inv + top;
                            count++;
                            if (fx < left || fy < top || fx >= right + 1 || fy >= bottom + 1) continue;
                            int ix = Math.Min(big - 1, (int)fx), iy = Math.Min(big - 1, (int)fy);
                            int k = (iy * big + ix) * 4;
                            r += pm[k]; g += pm[k + 1]; b += pm[k + 2]; a += pm[k + 3];
                        }
                    }
                    r /= count; g /= count; b /= count; a /= count;
                    int d = (y * o.Size + x) * 4;
                    if (a > 0.0005f)
                    {
                        outBytes[d] = (byte)MathHelper.Clamp((float)Math.Round(r / a * 255f), 0f, 255f);
                        outBytes[d + 1] = (byte)MathHelper.Clamp((float)Math.Round(g / a * 255f), 0f, 255f);
                        outBytes[d + 2] = (byte)MathHelper.Clamp((float)Math.Round(b / a * 255f), 0f, 255f);
                        outBytes[d + 3] = (byte)MathHelper.Clamp((float)Math.Round(a * 255f), 0f, 255f);
                    }
                }
            }
            var tex = new Texture2D(gd, o.Size, o.Size, false, SurfaceFormat.Color);
            tex.SetData(outBytes);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file)));
            using (FileStream fs = File.Create(file))
            {
                tex.SaveAsPng(fs, o.Size, o.Size);
            }
            tex.Dispose();
            Console.WriteLine("  wrote " + file);
        }
    }
}
