using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Rendering;
using Shooter.Rendering.Particles;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// Spawns baked VFX prefabs (Content/Particles/&lt;Prefab&gt;.particles.json, Tools/ParticleBake) like Unity's Instantiate(prefab, position,
    /// rotation[, parent]): the prefab's root transform is replaced by the given one and every system keeps its pose relative to the root.
    /// The root is the shallowest system of the file (true for VFX_MuzzleFlashGreen and VFX_LazerSparksGreen, which have one system each).
    /// </summary>
    public sealed class VfxLibrary
    {
        private readonly ParticleManager _manager;
        private readonly Func<string, Material> _materialByName;
        private readonly Dictionary<string, List<ParticleSystemData.Placement>> _cache = new Dictionary<string, List<ParticleSystemData.Placement>>();

        public VfxLibrary(ParticleManager manager, Func<string, Material> materialByName)
        {
            _manager = manager;
            _materialByName = materialByName;
        }

        /// <summary>One spawned prefab instance: its systems and their pose relative to the prefab root.</summary>
        public sealed class Instance
        {
            public readonly List<ParticleSystemInstance> Systems = new List<ParticleSystemInstance>();
            public readonly List<Matrix> Relative = new List<Matrix>();
            public float Age;
            public float Lifetime;

            /// <summary>Put every system of this instance on the weapon layer (see <see cref="ParticleSystemInstance.WeaponLayer"/>).</summary>
            public void SetWeaponLayer(bool weaponLayer, bool drawOverWeapon = false)
            {
                foreach (ParticleSystemInstance s in Systems)
                {
                    s.WeaponLayer = weaponLayer;
                    s.DrawOverWeapon = drawOverWeapon;
                }
            }

            /// <summary>Move the prefab root (a parented instance follows its parent).</summary>
            public void SetRoot(Matrix root)
            {
                for (int i = 0; i < Systems.Count; i++) Systems[i].SetWorld(Relative[i] * root);
            }
        }

        private List<ParticleSystemData.Placement> Get(string prefab)
        {
            if (!_cache.TryGetValue(prefab, out List<ParticleSystemData.Placement> list))
            {
                using (Stream s = TitleContainer.OpenStream("Content/Particles/" + prefab + ".particles.json"))
                {
                    list = ParticleSystemData.Load(s);
                }
                _cache[prefab] = list;
            }
            return list;
        }

        /// <summary>Instantiate <paramref name="prefab"/> with its root at <paramref name="root"/>; destroyed after <paramref name="lifetime"/> seconds (by <see cref="Tick"/>).</summary>
        public Instance Spawn(string prefab, Matrix root, float lifetime)
        {
            List<ParticleSystemData.Placement> placements = Get(prefab);
            var inst = new Instance { Lifetime = lifetime };
            ParticleSystemData.Placement shallow = null;
            foreach (ParticleSystemData.Placement p in placements)
            {
                if (p.Active && (shallow == null || p.Path.Split('/').Length < shallow.Path.Split('/').Length)) shallow = p;
            }
            if (shallow == null) return inst;
            Matrix rootInverse = Matrix.Invert(shallow.World);
            foreach (ParticleSystemData.Placement p in placements)
            {
                if (!p.Active) continue;   // an inactive GameObject (VFX_ImpactRing, VFX_HealthGlow, ...) is not drawn
                Matrix rel = p.World * rootInverse;
                ParticleSystemInstance ps = _manager.Add(p.Data, _materialByName(p.Data.MaterialName), rel * root);
                inst.Systems.Add(ps);
                inst.Relative.Add(rel);
            }
            return inst;
        }

        /// <summary>The baked systems of a prefab with their authored (prefab space) transforms, for callers that place each system themselves.</summary>
        public IReadOnlyList<ParticleSystemData.Placement> Placements(string prefab) { return Get(prefab); }

        public ParticleSystemInstance AddSystem(ParticleSystemData.Placement placement, Matrix world)
        {
            return _manager.Add(placement.Data, _materialByName(placement.Data.MaterialName), world);
        }

        public void Remove(ParticleSystemInstance ps) { _manager.Remove(ps); }

        public void Destroy(Instance inst)
        {
            foreach (ParticleSystemInstance ps in inst.Systems) _manager.Remove(ps);
            inst.Systems.Clear();
        }
    }
}
