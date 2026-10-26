using System;
using Microsoft.Xna.Framework;

namespace Shooter.Rendering
{
    /// <summary>
    /// Single-cascade directional shadow fitting, equivalent to Unity "StableFit": the cascade is the bounding sphere of the camera
    /// frustum slice [near, shadowDistance]; the sphere centre is snapped to shadow-map texels so shadows do not shimmer when the
    /// camera rotates, and the orthographic extent is the (rotation invariant) sphere diameter.
    /// </summary>
    internal static class ShadowMapper
    {
        public struct Result
        {
            public Matrix View;
            public Matrix Projection;
            public Matrix ViewProjection;
            public BoundingFrustum Frustum;
            public float WorldTexelSize;   // world units per shadow-map texel
            public float SphereRadius;
            public Vector3 SphereCenter;
        }

        /// <param name="casterMinAlongLight">Smallest value of dot(point, lightDirection) over all caster bounds (use float.MaxValue when unknown); used to extend the near plane.</param>
        public static Result Fit(Camera camera, Vector3 lightDirection, float shadowDistance, int resolution, float casterMinAlongLight)
        {
            Matrix invView = Matrix.Invert(camera.View);
            Vector3 camPos = invView.Translation;
            Vector3 camForward = invView.Forward; // camera-to-world matrix: -Z column

            float near = camera.NearPlane;
            float far = Math.Min(shadowDistance, camera.FarPlane);
            float tanHalfV = (float)Math.Tan(camera.FieldOfViewY * 0.5f);
            float aspect = camera.AspectRatio;
            float k = (float)Math.Sqrt(1.0 + aspect * aspect) * tanHalfV;

            float center, radius;
            if (k * k >= (far - near) / (far + near))
            {
                center = far;
                radius = far * k;
            }
            else
            {
                center = 0.5f * (far + near) * (1.0f + k * k);
                radius = 0.5f * (float)Math.Sqrt((far - near) * (far - near) + 2.0f * (far * far + near * near) * k * k + (far + near) * (far + near) * k * k * k * k);
            }
            // Unity rounds the radius up to avoid tiny frame-to-frame variations
            radius = (float)Math.Ceiling(radius * 16f) / 16f;

            Vector3 sphereCenter = camPos + camForward * center;

            Vector3 dir = Vector3.Normalize(lightDirection);
            Vector3 up = Math.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
            Matrix rotOnly = Matrix.CreateLookAt(Vector3.Zero, dir, up);

            float texel = 2f * radius / resolution;
            Vector3 centerLs = Vector3.Transform(sphereCenter, rotOnly);
            centerLs.X = (float)Math.Floor(centerLs.X / texel) * texel;
            centerLs.Y = (float)Math.Floor(centerLs.Y / texel) * texel;
            Matrix invRot = Matrix.Invert(rotOnly);
            sphereCenter = Vector3.Transform(centerLs, invRot);

            // casters may sit "above" the sphere along the light direction
            float back = radius;
            if (casterMinAlongLight < float.MaxValue * 0.5f)
                back = Math.Max(radius, Vector3.Dot(sphereCenter, dir) - casterMinAlongLight + 1f);
            Vector3 eye = sphereCenter - dir * back;
            Matrix view = Matrix.CreateLookAt(eye, sphereCenter, up);
            Matrix proj = Matrix.CreateOrthographicOffCenter(-radius, radius, -radius, radius, 0f, back + radius);

            Result r;
            r.View = view;
            r.Projection = proj;
            r.ViewProjection = view * proj;
            r.Frustum = new BoundingFrustum(r.ViewProjection);
            r.WorldTexelSize = texel;
            r.SphereRadius = radius;
            r.SphereCenter = sphereCenter;
            return r;
        }

    }
}
