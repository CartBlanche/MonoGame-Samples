using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Rendering;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// The visible half of OverheatBehavior and WeaponFuelCellHandler:
    ///   OverheatBehavior.Update: when the ammo ratio changes, the weapon material's _EmissionColor (a property block)
    ///   becomes OverheatGradient.Evaluate(1 - ratio) (see EmissionFor) and the steam emission rate SteamVfxEmissionRateMax * (1 - ratio).
    ///   WeaponFuelCellHandler.Update: every fuel cell slides from FuelCellUsedPosition to FuelCellUnusedPosition as the ammo ratio passes
    ///   its slice (cell i covers [i / n, (i + 1) / n]) unless SimultaneousFuelCellsUsage.
    /// The weapon's own particle systems (Weapon_Blaster prefab) are added by <see cref="AttachParticles"/>: the steam and a VFX_GunCylinder
    /// parented to each container, so the cylinders follow their fuel cell.
    /// </summary>
    public sealed class OverheatVisuals
    {
        private readonly LoadedScene _scene;
        private readonly Material _gem;
        private readonly Rendering.Particles.Gradient _gradient;
        private readonly float _steamRateMax;
        private readonly bool _simultaneous;
        private readonly Vector3 _usedPosition, _unusedPosition;   // Unity local space
        private readonly int[] _cellPiece;                          // piece index of each fuel cell, in handler order
        private float _lastRatio;                                   // m_LastAmmoRatio starts at 0, so the first Update always applies the colour

        private ParticleSystemInstance _steam;
        private readonly List<ParticleSystemInstance> _cylinders = new List<ParticleSystemInstance>();
        private readonly List<ParticleSystemData.Placement> _cylinderPlacements = new List<ParticleSystemData.Placement>();
        private readonly List<int> _cylinderCell = new List<int>();
        private ParticleSystemData.Placement _steamPlacement;

        public Vector3 CurrentEmission { get; private set; }

        public OverheatVisuals(SceneFile file, LoadedScene scene, Func<string, Material> materialByName)
        {
            _scene = scene;
            Dictionary<string, JsonElement> c = file.Weapon.Components;
            _gradient = Rendering.Particles.Gradient.White;
            if (c != null && c.TryGetValue("OverheatBehavior", out JsonElement oh))
            {
                if (oh.TryGetProperty("OverheatGradient", out JsonElement g)) _gradient = Rendering.Particles.Gradient.FromJson(g);
                _steamRateMax = JsonFields.Float(oh, "SteamVfxEmissionRateMax", 8f);
                string mat = JsonFields.AssetFileName(oh, "OverheatingMaterial");
                if (mat != null) _gem = materialByName(mat);
            }
            _usedPosition = Vector3.Zero;
            _unusedPosition = new Vector3(0f, -0.1f, 0f);   // script default
            if (c != null && c.TryGetValue("WeaponFuelCellHandler", out JsonElement fc))
            {
                _simultaneous = JsonFields.Bool(fc, "SimultaneousFuelCellsUsage", false);
                _usedPosition = JsonFields.Vector3(fc, "FuelCellUsedPosition", _usedPosition);
                _unusedPosition = JsonFields.Vector3(fc, "FuelCellUnusedPosition", _unusedPosition);
            }
            _cellPiece = new int[file.Weapon.FuelCellPieces.Count];
            for (int i = 0; i < _cellPiece.Length; i++)
            {
                _cellPiece[i] = scene.WeaponPieceNames.IndexOf(file.Weapon.FuelCellPieces[i]);
            }
            scene.WeaponPieceOffsets = new Vector3[scene.WeaponPieces.Count];
        }

        /// <summary>Spawn the weapon prefab's particle systems (steam rate starts at 0 like OverheatBehavior.Awake).</summary>
        public void AttachParticles(VfxLibrary vfx)
        {
            _lastRatio = 0f;   // a freshly instantiated weapon: the next Update applies colour and steam rate again
            foreach (ParticleSystemData.Placement p in vfx.Placements("Weapon_Blaster"))
            {
                int cell = -1;
                for (int i = 0; i < _cellPiece.Length && cell < 0; i++)
                {
                    if (_cellPiece[i] >= 0 && p.Path.Contains("/" + _scene.WeaponPieceNames[_cellPiece[i]] + "/")) cell = i;
                }
                ParticleSystemInstance inst = vfx.AddSystem(p, p.World);
                inst.WeaponLayer = true;   // children of the weapon prefab: the weapon layer, drawn by the weapon camera
                if (p.Data.Name == "VFX_OverheatPrimary")
                {
                    _steam = inst;
                    _steamPlacement = p;
                    inst.RateOverTimeMultiplier = 0f;
                }
                else
                {
                    _cylinders.Add(inst);
                    _cylinderPlacements.Add(p);
                    _cylinderCell.Add(cell);
                }
            }
        }

        public void DetachParticles(ParticleManager manager)
        {
            if (_steam != null) manager.Remove(_steam);
            foreach (ParticleSystemInstance i in _cylinders) manager.Remove(i);
            _steam = null;
            _cylinders.Clear();
            _cylinderPlacements.Clear();
            _cylinderCell.Clear();
        }

        /// <summary>The overheat gradient colour for an ammo ratio (used as authored: converting it gamma -> linear like the enemy eye colour overshoots the reference, see PORTING-PLAN).</summary>
        public static Vector3 EmissionFor(Rendering.Particles.Gradient gradient, float ratio)
        {
            Vector4 g = gradient.Evaluate(1f - ratio);
            return new Vector3(g.X, g.Y, g.Z);
        }

        public Vector3 EmissionAt(float ratio) { return EmissionFor(_gradient, ratio); }

        /// <summary>Fuel cell offsets (weapon-root space) for an ammo ratio.</summary>
        public Vector3 CellOffset(int cell, float ratio)
        {
            float n = _cellPiece.Length;
            float value = _simultaneous ? ratio : MathHelper.Clamp((ratio - cell / n) / (((cell + 1) / n) - (cell / n)), 0f, 1f);
            Vector3 unity = Vector3.Lerp(_usedPosition, _unusedPosition, value);
            // Unity-local -> MonoGame-local flips z; the baked container matrix is the socket's world matrix (the container's own local pose is identity)
            Matrix container = _scene.WeaponPieces[_cellPiece[cell]].World;
            Vector3 local = new Vector3(unity.X, unity.Y, -unity.Z);
            return Vector3.TransformNormal(local, container);
        }

        /// <summary>OverheatBehavior.Update + WeaponFuelCellHandler.Update for one frame. <paramref name="weaponRoot"/> = viewmodel root in world space.</summary>
        public void Update(float ammoRatio, Matrix weaponRoot)
        {
            if (ammoRatio != _lastRatio)
            {
                if (_gem != null)
                {
                    CurrentEmission = EmissionAt(ammoRatio);
                    _gem.EmissionColor = CurrentEmission;
                }
                if (_steam != null) _steam.RateOverTimeMultiplier = _steamRateMax * (1f - ammoRatio);
                _lastRatio = ammoRatio;
            }
            for (int i = 0; i < _cellPiece.Length; i++)
            {
                if (_cellPiece[i] >= 0) _scene.WeaponPieceOffsets[_cellPiece[i]] = CellOffset(i, ammoRatio);
            }
            if (_steam != null) _steam.SetWorld(_steamPlacement.World * weaponRoot);
            for (int k = 0; k < _cylinders.Count; k++)
            {
                Vector3 offset = _cylinderCell[k] >= 0 ? _scene.WeaponPieceOffsets[_cellPiece[_cylinderCell[k]]] : Vector3.Zero;
                _cylinders[k].SetWorld(_cylinderPlacements[k].World * Matrix.CreateTranslation(offset) * weaponRoot);
            }
        }
    }
}
