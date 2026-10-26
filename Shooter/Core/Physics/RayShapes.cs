using System;
using Microsoft.Xna.Framework;

namespace Shooter.Physics
{
    /// <summary>Analytic ray tests for shapes that are not in the triangle grid (the player's capsule).</summary>
    public static class RayShapes
    {
        /// <summary>
        /// Nearest hit of a unit-length ray with a capsule (axis a-b, radius r) from outside, within maxDistance. A ray that starts
        /// inside does not hit (like a PhysX raycast). Casting a sphere of radius s is the same as passing r + s.
        /// </summary>
        public static bool RayCapsule(Vector3 origin, Vector3 dir, float maxDistance, Vector3 a, Vector3 b, float r, out float distance, out Vector3 normal)
        {
            distance = 0f;
            normal = Vector3.Up;
            Vector3 ab = b - a;
            float abLen2 = ab.LengthSquared();
            float best = float.MaxValue;
            Vector3 bestNormal = Vector3.Up;

            // start inside the capsule: no hit
            if (DistanceToSegmentSquared(origin, a, b) <= r * r)
            {
                return false;
            }

            // two end spheres
            foreach (Vector3 c in new[] { a, b })
            {
                if (RaySphere(origin, dir, c, r, out float t) && t < best)
                {
                    Vector3 p = origin + dir * t;
                    // only the part of the sphere outside the cylinder section counts, but the cylinder test below is nearer there anyway
                    best = t;
                    bestNormal = Vector3.Normalize(p - c);
                }
            }

            // the cylinder between them
            if (abLen2 > 1e-12f)
            {
                Vector3 axis = ab / (float)Math.Sqrt(abLen2);
                Vector3 oc = origin - a;
                Vector3 dPerp = dir - axis * Vector3.Dot(dir, axis);
                Vector3 oPerp = oc - axis * Vector3.Dot(oc, axis);
                float qa = dPerp.LengthSquared(), qb = Vector3.Dot(dPerp, oPerp), qc = oPerp.LengthSquared() - r * r;
                if (qa > 1e-12f)
                {
                    float disc = qb * qb - qa * qc;
                    if (disc >= 0f)
                    {
                        float t = (-qb - (float)Math.Sqrt(disc)) / qa;
                        if (t >= 0f && t < best)
                        {
                            Vector3 p = origin + dir * t;
                            float along = Vector3.Dot(p - a, axis);
                            if (along >= 0f && along * along <= abLen2)
                            {
                                best = t;
                                bestNormal = Vector3.Normalize(p - (a + axis * along));
                            }
                        }
                    }
                }
            }

            if (best > maxDistance || best == float.MaxValue)
            {
                return false;
            }
            distance = best;
            normal = bestNormal;
            return true;
        }

        public static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius, out float t)
        {
            t = 0f;
            Vector3 oc = origin - center;
            float b = Vector3.Dot(oc, dir), c = oc.LengthSquared() - radius * radius;
            if (c <= 0f)
            {
                return false;
            }
            float disc = b * b - c;
            if (disc < 0f)
            {
                return false;
            }
            t = -b - (float)Math.Sqrt(disc);
            return t >= 0f;
        }

        public static float DistanceToSegmentSquared(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.LengthSquared();
            float t = len2 > 1e-12f ? MathHelper.Clamp(Vector3.Dot(p - a, ab) / len2, 0f, 1f) : 0f;
            return Vector3.DistanceSquared(p, a + ab * t);
        }
    }
}
