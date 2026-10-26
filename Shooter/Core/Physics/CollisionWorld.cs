using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Physics
{
    /// <summary>
    /// Static collision geometry: a world-space triangle soup in a uniform grid, queried with a capsule.
    /// Stand-in for Unity's PhysX scene for the player's CharacterController: every baked MeshCollider is a
    /// non-convex mesh, so triangles (two-sided) are the faithful representation; boxes and spheres are expanded
    /// to triangles. No MonoGame graphics dependency, so it can be built and tested headlessly.
    /// Not thread safe (queries share scratch buffers).
    /// </summary>
    public sealed class CollisionWorld
    {
        public struct Contact
        {
            public Vector3 Normal;   // points from the surface toward the capsule
            public float Depth;      // radius - distance between capsule axis and triangle
        }

        private readonly List<Vector3> _p0 = new List<Vector3>();
        private readonly List<Vector3> _p1 = new List<Vector3>();
        private readonly List<Vector3> _p2 = new List<Vector3>();
        private readonly List<Vector3> _n = new List<Vector3>();
        private readonly List<int> _owner = new List<int>();
        private int[] _aowner;
        private bool[] _ownerOff = new bool[0];
        private bool _anyOff;
        private Vector3[] _a0, _a1, _a2, _an;
        private int[] _stamp;
        private int _stampValue;
        private readonly List<int> _candidates = new List<int>();
        private Dictionary<long, List<int>> _grid;
        private float _cell = 4f;
        private bool _built;

        public int TriangleCount { get { return _p0.Count; } }
        public BoundingBox Bounds { get; private set; }

        /// <summary>Tag given to the triangles added from now on (-1 = level geometry); raycasts report it as <see cref="RayHit.Owner"/>.</summary>
        public int CurrentOwner = -1;

        /// <summary>Switches every triangle of a tagged owner on or off (a destroyed enemy's colliders): off ones are ignored by rays and capsules.</summary>
        public void SetOwnerActive(int owner, bool active)
        {
            if (owner < 0)
            {
                return;
            }
            if (owner >= _ownerOff.Length)
            {
                Array.Resize(ref _ownerOff, owner + 1);
            }
            _ownerOff[owner] = !active;
            _anyOff = false;
            for (int i = 0; i < _ownerOff.Length; i++)
            {
                _anyOff |= _ownerOff[i];
            }
        }

        private struct DynamicSphere
        {
            public int Owner;
            public Vector3 Center;
            public float Radius;
        }

        private readonly List<DynamicSphere> _spheres = new List<DynamicSphere>();

        /// <summary>
        /// A sphere that can move after Build (an enemy's hitbox): exact for rays and capsules, outside the triangle grid. Like a PhysX
        /// raycast, a ray that starts inside it does not hit it.
        /// </summary>
        public void AddDynamicSphere(int owner, Vector3 center, float radius)
        {
            _spheres.Add(new DynamicSphere { Owner = owner, Center = center, Radius = radius });
        }

        public void MoveDynamicSphere(int owner, Vector3 center)
        {
            for (int i = 0; i < _spheres.Count; i++)
            {
                if (_spheres[i].Owner == owner)
                {
                    DynamicSphere d = _spheres[i];
                    d.Center = center;
                    _spheres[i] = d;
                }
            }
        }

        private class DynamicBox
        {
            public int Owner;
            public Vector3 Center, Size;
            public readonly Vector3[] Tri = new Vector3[36];   // 12 triangles, world space
            public readonly Vector3[] Normal = new Vector3[12];
        }

        private readonly List<DynamicBox> _boxes = new List<DynamicBox>();

        /// <summary>
        /// An oriented box that can move after Build (an enemy hitbox hanging off an animated bone): the local center / size under a world
        /// matrix (scale included). Rays and capsules test its 12 triangles directly, outside the grid.
        /// </summary>
        public void AddDynamicBox(int owner, Matrix world, Vector3 center, Vector3 size)
        {
            var b = new DynamicBox { Owner = owner, Center = center, Size = size };
            _boxes.Add(b);
            Place(b, world);
        }

        /// <summary>Moves every dynamic box of an owner to a new world matrix (they share their GameObject's transform).</summary>
        public void MoveDynamicBoxes(int owner, Matrix world)
        {
            foreach (DynamicBox b in _boxes)
            {
                if (b.Owner == owner)
                {
                    Place(b, world);
                }
            }
        }

        private static void Place(DynamicBox b, Matrix world)
        {
            Vector3 h = b.Size * 0.5f;
            var c = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var local = new Vector3(b.Center.X + ((i & 1) == 0 ? -h.X : h.X), b.Center.Y + ((i & 2) == 0 ? -h.Y : h.Y), b.Center.Z + ((i & 4) == 0 ? -h.Z : h.Z));
                c[i] = Vector3.Transform(local, world);
            }
            int[,] faces = { { 0, 1, 3, 2 }, { 4, 6, 7, 5 }, { 0, 4, 5, 1 }, { 2, 3, 7, 6 }, { 0, 2, 6, 4 }, { 1, 5, 7, 3 } };
            int k = 0;
            for (int f = 0; f < 6; f++)
            {
                for (int q = 0; q < 2; q++)
                {
                    Vector3 p0 = c[faces[f, 0]], p1 = c[faces[f, q == 0 ? 1 : 2]], p2 = c[faces[f, q == 0 ? 2 : 3]];
                    b.Tri[k * 3] = p0; b.Tri[k * 3 + 1] = p1; b.Tri[k * 3 + 2] = p2;
                    Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
                    b.Normal[k] = n.LengthSquared() > 1e-20f ? Vector3.Normalize(n) : Vector3.Up;
                    k++;
                }
            }
        }

        private static bool RayTri(Vector3 o, Vector3 d, Vector3 v0, Vector3 v1, Vector3 v2, out float dist)
        {
            dist = 0f;
            Vector3 e1 = v1 - v0, e2 = v2 - v0;
            Vector3 p = Vector3.Cross(d, e2);
            float det = Vector3.Dot(e1, p);
            if (Math.Abs(det) < 1e-9f)
            {
                return false;
            }
            float inv = 1f / det;
            Vector3 tv = o - v0;
            float u = Vector3.Dot(tv, p) * inv;
            if (u < 0f || u > 1f)
            {
                return false;
            }
            Vector3 q = Vector3.Cross(tv, e1);
            float v = Vector3.Dot(d, q) * inv;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }
            dist = Vector3.Dot(e2, q) * inv;
            return dist >= 0f;
        }

        public bool IsOwnerActive(int owner)
        {
            return owner < 0 || owner >= _ownerOff.Length || !_ownerOff[owner];
        }

        private bool TriActive(int t)
        {
            int o = _aowner[t];
            if (o >= 0 && o == _queryIgnoreOwner)
            {
                return false;
            }
            if (!_anyOff)
            {
                return true;
            }
            return o < 0 || o >= _ownerOff.Length || !_ownerOff[o];
        }

        private int _queryIgnoreOwner = -2;

        /// <summary>Capsule overlap that ignores one owner's colliders (an enemy testing its own movement against everything but itself).</summary>
        public bool OverlapsIgnoring(Vector3 a, Vector3 b, float r, int ignoreOwner)
        {
            _queryIgnoreOwner = ignoreOwner;
            try
            {
                return DeepestContact(a, b, r, out _);
            }
            finally
            {
                _queryIgnoreOwner = -2;
            }
        }

        // ------------------------------------------------------------------ building

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            float len = n.Length();
            if (len < 1e-10f || float.IsNaN(len))
            {
                return;   // degenerate
            }
            _p0.Add(a); _p1.Add(b); _p2.Add(c); _n.Add(n / len); _owner.Add(CurrentOwner);
            _built = false;
        }

        /// <summary>Oriented box: local center/size transformed by the owner's world matrix (scale included).</summary>
        public void AddBox(Matrix world, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            var c = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var local = new Vector3(center.X + ((i & 1) == 0 ? -h.X : h.X), center.Y + ((i & 2) == 0 ? -h.Y : h.Y), center.Z + ((i & 4) == 0 ? -h.Z : h.Z));
                c[i] = Vector3.Transform(local, world);
            }
            int[,] faces = { { 0, 1, 3, 2 }, { 4, 6, 7, 5 }, { 0, 4, 5, 1 }, { 2, 3, 7, 6 }, { 0, 2, 6, 4 }, { 1, 5, 7, 3 } };
            for (int f = 0; f < 6; f++)
            {
                AddTriangle(c[faces[f, 0]], c[faces[f, 1]], c[faces[f, 2]]);
                AddTriangle(c[faces[f, 0]], c[faces[f, 2]], c[faces[f, 3]]);
            }
        }

        /// <summary>Sphere approximated by a twice-subdivided icosahedron (320 triangles, inscribed).</summary>
        public void AddSphere(Matrix world, Vector3 center, float radius)
        {
            float t = (1f + (float)Math.Sqrt(5.0)) * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Count; i++) { v[i] = Vector3.Normalize(v[i]); }
            int[] f0 = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                         3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            var tris = new List<int[]>();
            for (int i = 0; i < f0.Length; i += 3) { tris.Add(new[] { f0[i], f0[i + 1], f0[i + 2] }); }
            for (int level = 0; level < 2; level++)
            {
                var next = new List<int[]>();
                var mid = new Dictionary<long, int>();
                Func<int, int, int> midpoint = (a, b) =>
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (!mid.TryGetValue(key, out int idx))
                    {
                        v.Add(Vector3.Normalize((v[a] + v[b]) * 0.5f));
                        idx = v.Count - 1;
                        mid[key] = idx;
                    }
                    return idx;
                };
                foreach (int[] tri in tris)
                {
                    int ab = midpoint(tri[0], tri[1]), bc = midpoint(tri[1], tri[2]), ca = midpoint(tri[2], tri[0]);
                    next.Add(new[] { tri[0], ab, ca });
                    next.Add(new[] { tri[1], bc, ab });
                    next.Add(new[] { tri[2], ca, bc });
                    next.Add(new[] { ab, bc, ca });
                }
                tris = next;
            }
            Func<int, Vector3> pos = i => Vector3.Transform(center + v[i] * radius, world);
            foreach (int[] tri in tris)
            {
                AddTriangle(pos(tri[0]), pos(tri[1]), pos(tri[2]));
            }
        }

        public void Build(float cellSize = 4f)
        {
            _cell = cellSize;
            int count = _p0.Count;
            _a0 = _p0.ToArray(); _a1 = _p1.ToArray(); _a2 = _p2.ToArray(); _an = _n.ToArray(); _aowner = _owner.ToArray();
            _stamp = new int[count];
            _stampValue = 0;
            _grid = new Dictionary<long, List<int>>();
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (int i = 0; i < count; i++)
            {
                Vector3 lo = Vector3.Min(_a0[i], Vector3.Min(_a1[i], _a2[i]));
                Vector3 hi = Vector3.Max(_a0[i], Vector3.Max(_a1[i], _a2[i]));
                min = Vector3.Min(min, lo);
                max = Vector3.Max(max, hi);
                int x0 = Cell(lo.X), x1 = Cell(hi.X), y0 = Cell(lo.Y), y1 = Cell(hi.Y), z0 = Cell(lo.Z), z1 = Cell(hi.Z);
                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                        for (int z = z0; z <= z1; z++)
                        {
                            long key = Key(x, y, z);
                            if (!_grid.TryGetValue(key, out List<int> list))
                            {
                                list = new List<int>();
                                _grid[key] = list;
                            }
                            list.Add(i);
                        }
            }
            Bounds = count > 0 ? new BoundingBox(min, max) : new BoundingBox();
            _built = true;
        }

        private int Cell(float v)
        {
            return (int)Math.Floor(v / _cell);
        }

        private static long Key(int x, int y, int z)
        {
            return ((long)(x & 0x1FFFFF) << 42) | ((long)(y & 0x1FFFFF) << 21) | (long)(z & 0x1FFFFF);
        }

        // ------------------------------------------------------------------ queries

        private void Gather(Vector3 lo, Vector3 hi)
        {
            if (!_built)
            {
                Build(_cell);
            }
            _candidates.Clear();
            _stampValue++;
            int x0 = Cell(lo.X), x1 = Cell(hi.X), y0 = Cell(lo.Y), y1 = Cell(hi.Y), z0 = Cell(lo.Z), z1 = Cell(hi.Z);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    for (int z = z0; z <= z1; z++)
                    {
                        if (_grid.TryGetValue(Key(x, y, z), out List<int> list))
                        {
                            for (int k = 0; k < list.Count; k++)
                            {
                                int t = list[k];
                                if (_stamp[t] != _stampValue)
                                {
                                    _stamp[t] = _stampValue;
                                    _candidates.Add(t);
                                }
                            }
                        }
                    }
        }

        /// <summary>Appends the vertices (3 per triangle) of every triangle whose grid cells touch the box (diagnostics).</summary>
        public void TrianglesNear(BoundingBox box, List<Vector3> output)
        {
            Gather(box.Min, box.Max);
            foreach (int t in _candidates)
            {
                output.Add(_a0[t]); output.Add(_a1[t]); output.Add(_a2[t]);
            }
        }

        /// <summary>Deepest overlap of the capsule (axis a-b, radius r, treated as a solid) with any triangle.</summary>
        public bool DeepestContact(Vector3 a, Vector3 b, float r, out Contact contact)
        {
            contact = default(Contact);
            Vector3 lo = Vector3.Min(a, b) - new Vector3(r);
            Vector3 hi = Vector3.Max(a, b) + new Vector3(r);
            Gather(lo, hi);
            float best = 0f;
            bool found = false;
            for (int k = 0; k < _candidates.Count; k++)
            {
                int t = _candidates[k];
                if (TriangleContact(t, a, b, r, out Vector3 normal, out float depth) && depth > best)
                {
                    best = depth;
                    contact.Normal = normal;
                    contact.Depth = depth;
                    found = true;
                }
            }
            for (int i = 0; i < _spheres.Count; i++)
            {
                if (SphereContact(_spheres[i], a, b, r, out Vector3 sn, out float sd) && sd > best)
                {
                    best = sd;
                    contact.Normal = sn;
                    contact.Depth = sd;
                    found = true;
                }
            }
            for (int i = 0; i < _boxes.Count; i++)
            {
                DynamicBox bx = _boxes[i];
                if (!IsOwnerActive(bx.Owner) || bx.Owner == _queryIgnoreOwner)
                {
                    continue;
                }
                for (int k = 0; k < 12; k++)
                {
                    if (TriangleContactCore(bx.Tri[k * 3], bx.Tri[k * 3 + 1], bx.Tri[k * 3 + 2], bx.Normal[k], a, b, r, out Vector3 bnorm, out float bdepth) && bdepth > best)
                    {
                        best = bdepth;
                        contact.Normal = bnorm;
                        contact.Depth = bdepth;
                        found = true;
                    }
                }
            }
            return found;
        }

        /// <summary>Every overlapping triangle as a contact (one per triangle).</summary>
        public void Contacts(Vector3 a, Vector3 b, float r, List<Contact> output)
        {
            Gather(Vector3.Min(a, b) - new Vector3(r), Vector3.Max(a, b) + new Vector3(r));
            for (int k = 0; k < _candidates.Count; k++)
            {
                if (TriangleContact(_candidates[k], a, b, r, out Vector3 normal, out float depth) && depth > 0f)
                {
                    output.Add(new Contact { Normal = normal, Depth = depth });
                }
            }
            for (int i = 0; i < _spheres.Count; i++)
            {
                if (SphereContact(_spheres[i], a, b, r, out Vector3 sn, out float sd) && sd > 0f)
                {
                    output.Add(new Contact { Normal = sn, Depth = sd });
                }
            }
            for (int i = 0; i < _boxes.Count; i++)
            {
                DynamicBox bx = _boxes[i];
                if (!IsOwnerActive(bx.Owner) || bx.Owner == _queryIgnoreOwner)
                {
                    continue;
                }
                for (int k = 0; k < 12; k++)
                {
                    if (TriangleContactCore(bx.Tri[k * 3], bx.Tri[k * 3 + 1], bx.Tri[k * 3 + 2], bx.Normal[k], a, b, r, out Vector3 bnorm, out float bdepth) && bdepth > 0f)
                    {
                        output.Add(new Contact { Normal = bnorm, Depth = bdepth });
                    }
                }
            }
        }

        private bool SphereContact(DynamicSphere sp, Vector3 a, Vector3 b, float r, out Vector3 normal, out float depth)
        {
            normal = Vector3.Up;
            depth = 0f;
            if (!IsOwnerActive(sp.Owner) || sp.Owner == _queryIgnoreOwner)
            {
                return false;
            }
            Vector3 ab = b - a;
            float len2 = ab.LengthSquared();
            float t = len2 > 1e-12f ? MathHelper.Clamp(Vector3.Dot(sp.Center - a, ab) / len2, 0f, 1f) : 0f;
            Vector3 closest = a + ab * t;
            Vector3 d = closest - sp.Center;
            float dist = d.Length();
            depth = sp.Radius + r - dist;
            if (depth <= 0f)
            {
                return false;
            }
            normal = dist > 1e-6f ? d / dist : Vector3.Up;
            return true;
        }

        public struct RayHit
        {
            public float Distance;
            public Vector3 Point;
            public Vector3 Normal;   // faces the ray origin
            public int Owner;        // tag of the collider group that was hit (-1 = level geometry)
        }

        /// <summary>
        /// Closest two-sided triangle hit along a unit-length ray (grid walk + Moller-Trumbore). A hit at distance 0 means
        /// the origin touches the surface. Projectile "sphere casts" use this with their 1 cm radius ignored.
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 dir, float maxDistance, out RayHit hit, IList<int> ignoreOwners = null)
        {
            hit = default(RayHit);
            if (!_built)
            {
                Build(_cell);
            }
            float best = maxDistance;
            int bestTri = -1;
            _stampValue++;

            int cx = Cell(origin.X), cy = Cell(origin.Y), cz = Cell(origin.Z);
            int sx = dir.X > 0 ? 1 : -1, sy = dir.Y > 0 ? 1 : -1, sz = dir.Z > 0 ? 1 : -1;
            float tdx = dir.X != 0 ? _cell / Math.Abs(dir.X) : float.MaxValue;
            float tdy = dir.Y != 0 ? _cell / Math.Abs(dir.Y) : float.MaxValue;
            float tdz = dir.Z != 0 ? _cell / Math.Abs(dir.Z) : float.MaxValue;
            float tmx = dir.X != 0 ? ((dir.X > 0 ? (cx + 1) * _cell : cx * _cell) - origin.X) / dir.X : float.MaxValue;
            float tmy = dir.Y != 0 ? ((dir.Y > 0 ? (cy + 1) * _cell : cy * _cell) - origin.Y) / dir.Y : float.MaxValue;
            float tmz = dir.Z != 0 ? ((dir.Z > 0 ? (cz + 1) * _cell : cz * _cell) - origin.Z) / dir.Z : float.MaxValue;

            for (int iter = 0; iter < 8192; iter++)
            {
                if (_grid.TryGetValue(Key(cx, cy, cz), out List<int> list))
                {
                    for (int k = 0; k < list.Count; k++)
                    {
                        int t = list[k];
                        if (_stamp[t] == _stampValue)
                        {
                            continue;
                        }
                        _stamp[t] = _stampValue;
                        if (TriActive(t) && (ignoreOwners == null || !ignoreOwners.Contains(_aowner[t])) && RayTriangle(origin, dir, t, out float dist) && dist < best)
                        {
                            best = dist;
                            bestTri = t;
                        }
                    }
                }
                float exit = Math.Min(tmx, Math.Min(tmy, tmz));
                if (bestTri >= 0 && best <= exit)
                {
                    break;
                }
                if (exit > maxDistance)
                {
                    break;
                }
                if (tmx <= tmy && tmx <= tmz) { cx += sx; tmx += tdx; }
                else if (tmy <= tmz) { cy += sy; tmy += tdy; }
                else { cz += sz; tmz += tdz; }
            }

            int bestSphere = -1;
            for (int i = 0; i < _spheres.Count; i++)
            {
                DynamicSphere sp = _spheres[i];
                if (!IsOwnerActive(sp.Owner) || (ignoreOwners != null && ignoreOwners.Contains(sp.Owner)))
                {
                    continue;
                }
                Vector3 oc = origin - sp.Center;
                float b = Vector3.Dot(oc, dir), c = oc.LengthSquared() - sp.Radius * sp.Radius;
                if (c <= 0f)
                {
                    continue;   // starts inside
                }
                float disc = b * b - c;
                if (disc < 0f)
                {
                    continue;
                }
                float t = -b - (float)Math.Sqrt(disc);
                if (t >= 0f && t < best)
                {
                    best = t;
                    bestSphere = i;
                    bestTri = -1;
                }
            }
            int bestBox = -1, bestBoxTri = -1;
            for (int i = 0; i < _boxes.Count; i++)
            {
                DynamicBox bx = _boxes[i];
                if (!IsOwnerActive(bx.Owner) || (ignoreOwners != null && ignoreOwners.Contains(bx.Owner)))
                {
                    continue;
                }
                for (int k = 0; k < 12; k++)
                {
                    if (RayTri(origin, dir, bx.Tri[k * 3], bx.Tri[k * 3 + 1], bx.Tri[k * 3 + 2], out float bd) && bd < best)
                    {
                        best = bd;
                        bestBox = i;
                        bestBoxTri = k;
                        bestSphere = -1;
                        bestTri = -1;
                    }
                }
            }
            if (bestBox >= 0)
            {
                Vector3 bn = _boxes[bestBox].Normal[bestBoxTri];
                hit.Distance = best;
                hit.Point = origin + dir * best;
                hit.Normal = Vector3.Dot(bn, dir) < 0f ? bn : -bn;
                hit.Owner = _boxes[bestBox].Owner;
                return true;
            }
            if (bestSphere >= 0)
            {
                hit.Distance = best;
                hit.Point = origin + dir * best;
                hit.Normal = Vector3.Normalize(hit.Point - _spheres[bestSphere].Center);
                hit.Owner = _spheres[bestSphere].Owner;
                return true;
            }
            if (bestTri < 0)
            {
                return false;
            }
            hit.Distance = best;
            hit.Point = origin + dir * best;
            hit.Normal = Vector3.Dot(_an[bestTri], dir) < 0f ? _an[bestTri] : -_an[bestTri];
            hit.Owner = _aowner[bestTri];
            return true;
        }

        private bool RayTriangle(Vector3 o, Vector3 d, int t, out float dist)
        {
            dist = 0f;
            Vector3 e1 = _a1[t] - _a0[t], e2 = _a2[t] - _a0[t];
            Vector3 p = Vector3.Cross(d, e2);
            float det = Vector3.Dot(e1, p);
            if (Math.Abs(det) < 1e-9f)
            {
                return false;
            }
            float inv = 1f / det;
            Vector3 tv = o - _a0[t];
            float u = Vector3.Dot(tv, p) * inv;
            if (u < 0f || u > 1f)
            {
                return false;
            }
            Vector3 q = Vector3.Cross(tv, e1);
            float v = Vector3.Dot(d, q) * inv;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }
            dist = Vector3.Dot(e2, q) * inv;
            return dist >= 0f;
        }

        public bool Overlaps(Vector3 a, Vector3 b, float r)
        {
            return DeepestContact(a, b, r, out _);
        }

        /// <summary>
        /// Sweeps the capsule along a unit direction. Like PhysX, a capsule that already overlaps at the start
        /// is ignored (returns false). distance = how far it can travel before touching; normal = surface normal there.
        /// </summary>
        public bool CapsuleCast(Vector3 a, Vector3 b, float r, Vector3 dir, float maxDistance, out float distance, out Vector3 normal)
        {
            distance = 0f;
            normal = Vector3.Up;
            if (maxDistance <= 1e-6f || dir.LengthSquared() < 1e-12f || OverlapsAt(a, b, r, dir, 0f))
            {
                return false;
            }
            float step = r * 0.25f;
            float lastClear = 0f;
            float t = 0f;
            bool hit = false;
            while (t < maxDistance)
            {
                t = Math.Min(t + step, maxDistance);
                if (OverlapsAt(a, b, r, dir, t))
                {
                    hit = true;
                    break;
                }
                lastClear = t;
            }
            if (!hit)
            {
                return false;
            }
            float hi = t;
            for (int i = 0; i < 10; i++)
            {
                float mid = (lastClear + hi) * 0.5f;
                if (OverlapsAt(a, b, r, dir, mid)) { hi = mid; } else { lastClear = mid; }
            }
            distance = lastClear;
            Vector3 off = dir * hi;
            DeepestContact(a + off, b + off, r, out Contact c);
            normal = c.Depth > 0f ? c.Normal : -dir;
            return true;
        }

        private bool OverlapsAt(Vector3 a, Vector3 b, float r, Vector3 dir, float t)
        {
            Vector3 off = dir * t;
            return DeepestContact(a + off, b + off, r, out _);
        }

        // ------------------------------------------------------------------ capsule vs triangle

        private bool TriangleContact(int t, Vector3 a, Vector3 b, float r, out Vector3 normal, out float depth)
        {
            normal = Vector3.Up;
            depth = 0f;
            if (!TriActive(t))
            {
                return false;
            }
            return TriangleContactCore(_a0[t], _a1[t], _a2[t], _an[t], a, b, r, out normal, out depth);
        }

        private bool TriangleContactCore(Vector3 t0, Vector3 t1, Vector3 t2, Vector3 tn, Vector3 a, Vector3 b, float r, out Vector3 normal, out float depth)
        {
            normal = Vector3.Up;
            depth = 0f;

            // quick plane rejection: both axis end points farther than r on the same side
            float da = Vector3.Dot(a - t0, tn), db = Vector3.Dot(b - t0, tn);
            if ((da > r && db > r) || (da < -r && db < -r))
            {
                return false;
            }

            Vector3 onSeg, onTri;
            float distSq;
            if (da * db < 0f)
            {
                // the axis crosses the plane: does it cross inside the triangle?
                Vector3 p = a + (b - a) * (da / (da - db));
                if (PointInTriangle(p, t0, t1, t2, tn))
                {
                    // deep penetration: push out through the side of the axis midpoint
                    float side = Vector3.Dot((a + b) * 0.5f - t0, tn);
                    Vector3 n = side >= 0f ? tn : -tn;
                    float worst = Math.Min(Vector3.Dot(a - t0, n), Vector3.Dot(b - t0, n));
                    normal = n;
                    depth = r - Math.Min(0f, worst);
                    return true;
                }
            }

            distSq = float.MaxValue;
            onSeg = a; onTri = t0;
            Consider(a, ClosestPointOnTriangle(a, t0, t1, t2), ref distSq, ref onSeg, ref onTri);
            Consider(b, ClosestPointOnTriangle(b, t0, t1, t2), ref distSq, ref onSeg, ref onTri);
            SegEdge(a, b, t0, t1, ref distSq, ref onSeg, ref onTri);
            SegEdge(a, b, t1, t2, ref distSq, ref onSeg, ref onTri);
            SegEdge(a, b, t2, t0, ref distSq, ref onSeg, ref onTri);

            if (distSq >= r * r)
            {
                return false;
            }
            float dist = (float)Math.Sqrt(distSq);
            if (dist > 1e-5f)
            {
                normal = (onSeg - onTri) / dist;
            }
            else
            {
                normal = Vector3.Dot((a + b) * 0.5f - t0, tn) >= 0f ? tn : -tn;
            }
            depth = r - dist;
            return true;
        }

        private static void Consider(Vector3 onSegCandidate, Vector3 onTriCandidate, ref float best, ref Vector3 onSeg, ref Vector3 onTri)
        {
            float d = Vector3.DistanceSquared(onSegCandidate, onTriCandidate);
            if (d < best)
            {
                best = d;
                onSeg = onSegCandidate;
                onTri = onTriCandidate;
            }
        }

        private static void SegEdge(Vector3 a, Vector3 b, Vector3 e0, Vector3 e1, ref float best, ref Vector3 onSeg, ref Vector3 onTri)
        {
            ClosestSegmentSegment(a, b, e0, e1, out Vector3 c1, out Vector3 c2);
            Consider(c1, c2, ref best, ref onSeg, ref onTri);
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            return Vector3.Dot(Vector3.Cross(b - a, p - a), n) >= 0f
                && Vector3.Dot(Vector3.Cross(c - b, p - b), n) >= 0f
                && Vector3.Dot(Vector3.Cross(a - c, p - c), n) >= 0f;
        }

        // Ericson, Real-Time Collision Detection 5.1.5
        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + (c - b) * w;
            }
            float denom = 1f / (va + vb + vc);
            return a + ab * (vb * denom) + ac * (vc * denom);
        }

        // Ericson 5.1.9
        private static void ClosestSegmentSegment(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
        {
            const float eps = 1e-9f;
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s, t;
            if (a <= eps && e <= eps)
            {
                s = t = 0f;
            }
            else if (a <= eps)
            {
                s = 0f;
                t = MathHelper.Clamp(f / e, 0f, 1f);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= eps)
                {
                    t = 0f;
                    s = MathHelper.Clamp(-c / a, 0f, 1f);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > eps ? MathHelper.Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = MathHelper.Clamp(-c / a, 0f, 1f); }
                    else if (t > 1f) { t = 1f; s = MathHelper.Clamp((b - c) / a, 0f, 1f); }
                }
            }
            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
        }
    }
}
