using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Gameplay.Pickups;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// What pickups look and sound like: the mesh follows the bobbing / spinning root, the prefab's own glow particles (Pickup_Health: VFX_Health,
    /// Loot_Health: also VFX_PickupSparkles) are children of that root, and Pickup.PlayPickupFeedback plays PickupSfx (2D one-shot, spatial blend 0)
    /// and instantiates PickupVfxPrefab at the pickup's position with no rotation. Loot dropped later gets the same mesh and particles.
    /// </summary>
    public sealed class PickupEffects : IDisposable
    {
        private const float PickupVfxLifetime = 5f;   // VFX_HealthPickup: lengthInSec 5, looping, no stop action (Unity leaves it running; its burst is over after ~3.5 s)

        private readonly LoadedScene _scene;
        private readonly PickupManager _pickups;
        private readonly VfxLibrary _vfx;
        private readonly ParticleManager _particles;
        private readonly SfxPlayer _sfx;
        private readonly Dictionary<Pickup, List<ParticleSystemInstance>> _idle = new Dictionary<Pickup, List<ParticleSystemInstance>>();
        private readonly Dictionary<Pickup, List<Matrix>> _idleRelative = new Dictionary<Pickup, List<Matrix>>();
        private readonly List<VfxLibrary.Instance> _live = new List<VfxLibrary.Instance>();

        public int LiveEffects { get { return _live.Count; } }

        public PickupEffects(LoadedScene scene, PickupManager pickups, VfxLibrary vfx, ParticleManager particles, SfxPlayer sfx)
        {
            _scene = scene;
            _pickups = pickups;
            _vfx = vfx;
            _particles = particles;
            _sfx = sfx;
            // a reload: loot items of the last run go, the baked pickup meshes come back
            if (scene.DynamicItems.Count > scene.BakedDynamicCount)
            {
                scene.DynamicItems.RemoveRange(scene.BakedDynamicCount, scene.DynamicItems.Count - scene.BakedDynamicCount);
            }
            foreach (DynamicItem d in scene.DynamicItems)
            {
                d.Active = true;
            }
            int n = 0;
            foreach (Pickup p in pickups.All)
            {
                p.DynamicIndex = n < scene.PickupItemIndex.Count ? scene.PickupItemIndex[n] : -1;
                p.MeshRelative = n < scene.PickupMeshRelatives.Count ? scene.PickupMeshRelatives[n] : scene.PickupMeshRelative;
                n++;
                if (p.Kind == "Health")
                {
                    AddParticles(p, "Pickup_Health");   // Pickup_Jetpack has no particle systems
                }
                Apply(p);
            }
            pickups.Spawned += OnSpawned;
            pickups.Picked += OnPicked;
        }

        public void Dispose()
        {
            _pickups.Spawned -= OnSpawned;
            _pickups.Picked -= OnPicked;
            foreach (List<ParticleSystemInstance> list in _idle.Values)
            {
                foreach (ParticleSystemInstance ps in list) _particles.Remove(ps);
            }
            _idle.Clear();
            _idleRelative.Clear();
            foreach (VfxLibrary.Instance i in _live) _vfx.Destroy(i);
            _live.Clear();
        }

        private void AddParticles(Pickup p, string prefab)
        {
            var list = new List<ParticleSystemInstance>();
            var rel = new List<Matrix>();
            if (_vfx != null)
            {
                foreach (ParticleSystemData.Placement pl in _vfx.Placements(prefab))
                {
                    if (!pl.Active) continue;   // VFX_HealthGlow is an inactive GameObject in both prefabs
                    list.Add(_vfx.AddSystem(pl, pl.World * p.Root));   // prefab-space world: root at the origin, scale 2 included
                    rel.Add(pl.World);
                }
            }
            _idle[p] = list;
            _idleRelative[p] = rel;
        }

        private void OnSpawned(Pickup p)
        {
            if (p.DynamicIndex >= 0 || _scene.PickupTemplate == null)
            {
                return;
            }
            // loot: same mesh as Pickup_Health, particles from Loot_Health (VFX_Health, glow and sparkles)
            var item = new DynamicItem { Item = _scene.PickupTemplate.Value };
            _scene.DynamicItems.Add(item);
            p.DynamicIndex = _scene.DynamicItems.Count - 1;
            p.MeshRelative = _scene.PickupMeshRelative;
            AddParticles(p, "Loot_Health");
            Apply(p);
        }

        private void OnPicked(Pickup p)
        {
            if (p.DynamicIndex >= 0 && p.DynamicIndex < _scene.DynamicItems.Count)
            {
                _scene.DynamicItems[p.DynamicIndex].Active = false;
            }
            if (_idle.TryGetValue(p, out List<ParticleSystemInstance> list))
            {
                foreach (ParticleSystemInstance ps in list) _particles.Remove(ps);
                _idle.Remove(p);
                _idleRelative.Remove(p);
            }
            // Pickup.PlayPickupFeedback
            if (_sfx != null)
            {
                _sfx.PlayOneShot(p.SfxName, 1f);
            }
            if (_vfx != null && !string.IsNullOrEmpty(p.VfxPrefab))
            {
                _live.Add(_vfx.Spawn(p.VfxPrefab, Matrix.CreateTranslation(p.Position), PickupVfxLifetime));
            }
        }

        private void Apply(Pickup p)
        {
            Matrix root = p.Root;
            if (p.DynamicIndex >= 0 && p.DynamicIndex < _scene.DynamicItems.Count)
            {
                _scene.DynamicItems[p.DynamicIndex].SetWorld(p.MeshRelative * root);
            }
            if (_idle.TryGetValue(p, out List<ParticleSystemInstance> list))
            {
                List<Matrix> rel = _idleRelative[p];
                for (int i = 0; i < list.Count; i++) list[i].SetWorld(rel[i] * root);
            }
        }

        /// <summary>After <see cref="PickupManager.Update"/>: move the meshes and particles, age the one-shot effects.</summary>
        public void Update(float dt)
        {
            foreach (Pickup p in _pickups.All)
            {
                if (!p.Destroyed)
                {
                    Apply(p);
                }
            }
            for (int k = _live.Count - 1; k >= 0; k--)
            {
                VfxLibrary.Instance i = _live[k];
                i.Age += dt;
                if (i.Age >= i.Lifetime)
                {
                    _vfx.Destroy(i);
                    _live.RemoveAt(k);
                }
            }
        }
    }
}
