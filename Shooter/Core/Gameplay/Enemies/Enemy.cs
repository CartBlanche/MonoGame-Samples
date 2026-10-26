using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Weapons;
using Shooter.Physics;
using Shooter.Rendering;
using Shooter.Scene;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>What the scene draws for an enemy: an animated model whose Animator parameters and eye colour the AI drives.</summary>
    public interface IEnemyView
    {
        void SetFloat(string name, float value);
        void SetBool(string name, bool value);
        void SetTrigger(string name);
        /// <summary>EnemyController eye colour: false = DefaultEyeColor, true = AttackEyeColor.</summary>
        void SetEyeColor(bool attack);
        /// <summary>EnemyController.Update: the on-hit flash writes the body's _EmissionColor (linear HDR) every frame.</summary>
        void SetBodyEmission(Vector3 linearEmission);
        /// <summary>Advances the animator (then runs the late-update hook) and places the model.</summary>
        void Update(float dt, Matrix modelWorld);

        // skeleton access for enemies whose weapon, aim point and hitbox hang off animated bones (the Turret)
        bool HasBone(string bone);
        /// <summary>World matrix (scale included) of a bone in the current pose, or, with <paramref name="bindPose"/>, in the bind pose.</summary>
        bool TryGetBoneWorld(string bone, bool bindPose, out Matrix world);
        /// <summary>Sets a bone's world rotation (MonoGame) after the animation, like assigning Transform.rotation in LateUpdate.</summary>
        void SetBoneWorldRotation(string bone, Quaternion worldRotation);
        /// <summary>Called inside <see cref="Update"/> after the animation was evaluated and before the pose is used (Unity's LateUpdate).</summary>
        void SetLateUpdate(Action lateUpdate);
    }

    /// <summary>Everything an enemy needs from the world it lives in.</summary>
    public class EnemyContext
    {
        public CollisionWorld World;
        public ProjectileManager Projectiles;          // enemy shots
        public ProjectileSettings ProjectileSettings;  // Projectile_Hoverbot
        public Func<Actor> PlayerActor;
        public Func<bool> GameIsEnding;                // GameFlowManager.GameIsEnding (null = never)
        public IEnemyView View;
    }

    /// <summary>
    /// Port of AI/EnemyController.cs. 3a: Health + Actor + registration, damage reaction and death. 3b (HoverBot): detection, patrol path,
    /// orientation, attack, eye colour, and the agent / weapon / mobile state machine. Hit flash, death VFX / loot and sounds are 3d.
    /// </summary>
    public class Enemy
    {
        public readonly EnemyDto Data;
        public readonly int Index;               // index into SceneFile.Enemies
        public readonly Health Health;
        public readonly Actor Actor;
        public readonly List<int> HitTags = new List<int>();   // collision tags of this enemy's hitboxes
        public float LastTimeDamaged = float.NegativeInfinity;
        public bool IsDestroyed { get; private set; }

        // EnemyController fields (scene values)
        public readonly float SelfDestructYHeight, DeathDuration, DropRate, FlashOnHitDuration, PathReachingRadius, OrientationSpeed;

        public event Action Damaged;             // onDamaged: the player hurt this enemy
        public event Action<Enemy> Destroyed;    // after UnregisterEnemy: the scene removes the render item and colliders
        public event Action DetectedTarget;      // onDetectedTarget / onLostTarget come from the DetectionModule
        public event Action LostTarget;
        public event Action Attacked;            // onAttack: a shot was fired
        /// <summary>Health.OnDie, first thing (Instantiate(DeathVfx, DeathVfxSpawnPoint)): the enemy and the DeathVfxSpawnPoint position.</summary>
        public event Action<Enemy, Vector3> Dying;
        public event Action OnDamagedAny;        // Health.OnDamaged for scripts that react to every hit, whatever its source (EnemyTurret)

        /// <summary>Time.time for the enemy scripts.</summary>
        public float Time { get { return _manager.Time; } }

        // behaviour (HoverBot only for now)
        public NavAgent Agent { get; private set; }
        public DetectionModule Detection { get; private set; }
        public EnemyMobile Mobile { get; private set; }
        public EnemyWeapon Weapon { get; private set; }
        public EnemyTurret Turret { get; private set; }
        /// <summary>Hitboxes that hang off an animated bone: collision tag and the bone they follow (set by the director).</summary>
        public readonly List<KeyValuePair<int, string>> HitboxBones = new List<KeyValuePair<int, string>>();
        public IEnemyView View { get; private set; }
        public Vector3[] PatrolNodes { get; private set; }
        public int PathDestinationNodeIndex { get; private set; }

        private readonly EnemyManager _manager;
        private readonly ActorsManager _actors;
        private readonly EnemyContext _ctx;
        private Vector3 _position;
        private float _yaw;
        private float _dt = 1f / 60f;
        private readonly Vector3 _modelOffset;
        private readonly string _aimNode;
        private readonly Rendering.Particles.Gradient _hitGradient;   // OnHitBodyGradient

        /// <summary>Root transform position, MonoGame space.</summary>
        public Vector3 Position
        {
            get { return Agent != null ? Agent.Position : _position; }
            set { _position = value; if (Agent != null) Agent.Position = value; }
        }

        /// <summary>Unity yaw in degrees (0 = Unity +Z, positive = to the right).</summary>
        public float YawDegrees
        {
            get { return Agent != null ? Agent.YawDegrees : _yaw; }
            set { _yaw = value; if (Agent != null) Agent.YawDegrees = value; }
        }

        public bool IsDetectingTarget { get; private set; }

        /// <summary>Horizontal forward of the root in MonoGame space.</summary>
        public Vector3 Forward
        {
            get
            {
                float r = MathHelper.ToRadians(YawDegrees);
                return new Vector3((float)Math.Sin(r), 0f, -(float)Math.Cos(r));
            }
        }

        /// <summary>A point given in the root's local space (MonoGame, +Z backwards) to world space.</summary>
        public Vector3 ToWorld(Vector3 rootLocal)
        {
            return Vector3.Transform(rootLocal, Matrix.CreateRotationY(-MathHelper.ToRadians(YawDegrees))) + Position;
        }

        /// <summary>The root GameObject's world matrix: the baked matrix for a stationary enemy (the Turret), yaw and position for the HoverBot.</summary>
        public Matrix RootMatrix
        {
            get
            {
                if (Data.Kind == "Turret")
                {
                    return SceneLoader.ToMatrix(Data.M);
                }
                return Matrix.CreateRotationY(-MathHelper.ToRadians(YawDegrees)) * Matrix.CreateTranslation(Position);
            }
        }

        /// <summary>
        /// World matrix of a child GameObject by name: its baked local matrix composed up the hierarchy. A node under a bone of the model takes
        /// the bone's animated pose from the view (or its bind pose, which is what a scene holds before the Animator's first update).
        /// </summary>
        public Matrix NodeWorld(string name, bool bindPose = false)
        {
            if (View != null && View.TryGetBoneWorld(name, bindPose, out Matrix bone))
            {
                return bone;
            }
            if (Data.Hierarchy == null || !Data.Hierarchy.TryGetValue(name, out NodeDto node))
            {
                return RootMatrix;
            }
            Matrix local = SceneLoader.ToMatrix(node.Local);
            return local * (node.Parent == null ? RootMatrix : NodeWorld(node.Parent, bindPose));
        }

        public Vector3 DetectionSourcePosition { get { return NodeWorld("WeaponRoot").Translation; } }
        public Vector3 AimPointPosition { get { return NodeWorld(_aimNode).Translation; } }

        /// <summary>EnemyController.DeathVfxSpawnPoint: the HoverBot's HitBox (its aim point), the Turret's pivot bone (read from the raw prefabs).</summary>
        public Vector3 DeathVfxPosition
        {
            get
            {
                if (Data.Kind == "Turret" && View != null && View.TryGetBoneWorld("Turret_Pivot_C_Jnt", false, out Matrix pivot))
                {
                    return pivot.Translation;
                }
                return AimPointPosition;
            }
        }

        /// <summary>The model's world matrix: child offset, then the root's yaw and position (the Turret's model sits at its root).</summary>
        public Matrix ModelWorld
        {
            get
            {
                if (Data.Kind == "Turret")
                {
                    return SceneLoader.ToMatrix(Data.M);
                }
                return Matrix.CreateTranslation(_modelOffset) * Matrix.CreateRotationY(-MathHelper.ToRadians(YawDegrees)) * Matrix.CreateTranslation(Position);
            }
        }

        public Enemy(EnemyDto data, int index, EnemyManager manager, ActorsManager actors, EnemyContext ctx = null, PatrolPathDto path = null)
        {
            Data = data;
            Index = index;
            _manager = manager;
            _actors = actors;
            _ctx = ctx ?? new EnemyContext();
            _position = new Vector3(data.M[12], data.M[13], data.M[14]);

            JsonElement ec = default(JsonElement), hc = default(JsonElement), ac = default(JsonElement);
            if (data.Components != null)
            {
                data.Components.TryGetValue("EnemyController", out ec);
                data.Components.TryGetValue("Health", out hc);
                data.Components.TryGetValue("Actor", out ac);
            }
            SelfDestructYHeight = JsonFields.Float(ec, "SelfDestructYHeight", -20f);
            DeathDuration = JsonFields.Float(ec, "DeathDuration", 0f);
            DropRate = JsonFields.Float(ec, "DropRate", 1f);
            FlashOnHitDuration = JsonFields.Float(ec, "FlashOnHitDuration", 0.5f);
            if (ec.ValueKind == JsonValueKind.Object && ec.TryGetProperty("OnHitBodyGradient", out JsonElement hg))
            {
                _hitGradient = Rendering.Particles.Gradient.FromJson(hg);
            }
            PathReachingRadius = JsonFields.Float(ec, "PathReachingRadius", 2f);
            OrientationSpeed = JsonFields.Float(ec, "OrientationSpeed", 10f);

            Health = new Health(JsonFields.Float(hc, "MaxHealth", data.MaxHealth), JsonFields.Float(hc, "CriticalHealthRatio", 0.3f));
            Actor = new Actor { Affiliation = JsonFields.Int(ac, "Affiliation", 0), Name = data.Name };
            Actor.GetPosition = () => Position;
            Actor.GetAimPoint = () => AimPointPosition;

            // Actor.AimPoint and DetectionSourcePoint, resolved from the raw prefabs: HoverBot HitBox / WeaponRoot, Turret Turret_Head_C_Jnt / WeaponRoot
            _modelOffset = Node("BasicRobot");
            _aimNode = data.Kind == "Turret" ? "Turret_Head_C_Jnt" : "HitBox";

            // Start(): register as enemy and actor, subscribe to damage and death
            _manager.RegisterEnemy(this);
            _actors.Register(Actor);
            Health.OnDamaged += OnDamaged;
            Health.OnDie += OnDie;

            if (data.Kind == "HoverBot" && data.Parts != null && data.Parts.ContainsKey("DetectionModule"))
            {
                View = _ctx.View;
                BuildHoverBot(path);
            }
            else if (data.Kind == "Turret" && data.Parts != null && data.Parts.ContainsKey("DetectionModule"))
            {
                View = _ctx.View;
                BuildTurret();
            }
        }

        private Vector3 Node(string name)
        {
            return Data.Nodes != null && Data.Nodes.TryGetValue(name, out float[] p) ? new Vector3(p[0], p[1], p[2]) : Vector3.Zero;
        }

        private void BuildHoverBot(PatrolPathDto path)
        {
            JsonElement agent = Data.Parts.TryGetValue("NavMeshAgent", out JsonElement a) ? a : default(JsonElement);
            Agent = new NavAgent
            {
                Position = _position,
                YawDegrees = _yaw,
                Speed = JsonFields.Float(agent, "m_Speed", 3.5f),
                Acceleration = JsonFields.Float(agent, "m_Acceleration", 8f),
                AngularSpeed = JsonFields.Float(agent, "m_AngularSpeed", 120f),
                StoppingDistance = JsonFields.Float(agent, "m_StoppingDistance", 0f),
                Radius = JsonFields.Float(agent, "m_Radius", 0.5f),
                Height = JsonFields.Float(agent, "m_Height", 2f),
                AutoBraking = JsonFields.Bool(agent, "m_AutoBraking", true)
            };
            // NavigationModule overrides the agent values when present (not on the HoverBot, which has none)
            if (Data.Parts.TryGetValue("NavigationModule", out JsonElement nav))
            {
                Agent.Speed = JsonFields.Float(nav, "MoveSpeed", Agent.Speed);
                Agent.AngularSpeed = JsonFields.Float(nav, "AngularSpeed", Agent.AngularSpeed);
                Agent.Acceleration = JsonFields.Float(nav, "Acceleration", Agent.Acceleration);
            }

            Detection = new DetectionModule(Data.Parts["DetectionModule"]);
            Detection.DetectedTarget += OnDetectedTarget;
            Detection.LostTarget += OnLostTarget;

            if (path != null && path.Points.Count > 0)
            {
                PatrolNodes = new Vector3[path.Points.Count];
                for (int i = 0; i < PatrolNodes.Length; i++)
                {
                    PatrolNodes[i] = new Vector3(path.Points[i][0], path.Points[i][1], path.Points[i][2]);
                }
            }

            BuildWeapon();
            if (Weapon != null)
            {
                Weapon.Forward = -Forward;   // Weapon_EyeLazers hangs under WeaponRoot, which is turned 180 degrees
            }

            JsonElement mobile = Data.Components != null && Data.Components.TryGetValue("EnemyMobile", out JsonElement m) ? m : default(JsonElement);
            Mobile = new EnemyMobile(this, mobile);   // Start(): closest patrol node, patrolling
        }

        private void BuildTurret()
        {
            // the Turret has a NavMeshAgent component but EnemyTurret never gives it a destination: it stays where it is
            Detection = new DetectionModule(Data.Parts["DetectionModule"]);
            Detection.DetectedTarget += OnDetectedTarget;
            Detection.LostTarget += OnLostTarget;
            BuildWeapon();
            JsonElement t = Data.Components != null && Data.Components.TryGetValue("EnemyTurret", out JsonElement tc) ? tc : default(JsonElement);
            Turret = new EnemyTurret(this, t);
        }

        /// <summary>The WeaponController under the enemy: the weapon GameObject is the parent of GunRoot; the muzzle's local z (scaled by the weapon's world scale) is where shots start along the weapon's forward.</summary>
        private void BuildWeapon()
        {
            if (!Data.Parts.TryGetValue("WeaponController", out JsonElement wc) || Data.Hierarchy == null || !Data.Hierarchy.TryGetValue("GunRoot", out NodeDto gunRoot) || gunRoot.Parent == null)
            {
                return;
            }
            string weaponNode = gunRoot.Parent;
            NodeDto muzzle = Data.Hierarchy["GunMuzzle"];
            float unityLocalZ = -SceneLoader.ToMatrix(muzzle.Local).M43;   // MonoGame local z is the Unity one mirrored
            float scale = NodeWorld(weaponNode).Right.Length();
            Weapon = new EnemyWeapon(WeaponSettings.From(wc), () => NodeWorld(weaponNode), () => NodeWorld("GunRoot"), unityLocalZ * scale, () => _manager.Time);
        }

        // ------------------------------------------------------------------ EnemyController.Update

        public void Update(float dt)
        {
            if (IsDestroyed)
            {
                return;
            }
            _dt = dt;
            // EnsureIsWithinLevelBounds: Destroy(gameObject), which does not go through OnDie
            if (Position.Y < SelfDestructYHeight)
            {
                Destroy();
                return;
            }
            if (Detection == null)
            {
                return;
            }

            Detection.HandleTargetDetection(this, _actors, _ctx.World, _manager.Time);
            // body flash on hit (OnHitBodyGradient): 3d
            Mobile?.Update();
            Turret?.Update(dt);
            Weapon?.Update(dt);

            if (Agent == null)
            {
                // stationary enemy: the animator runs (and EnemyTurret's late update turns the pivot), then the hitboxes follow their bones
                UpdateBodyFlash();
                View?.Update(dt, ModelWorld);
                FollowBones();
                return;
            }

            // NavMeshAgent update (the engine's, after the scripts)
            Agent.Update(dt, _ctx.World, HitTags.Count > 0 ? HitTags[0] : -2);
            if (_ctx.World != null && HitTags.Count > 0)
            {
                _ctx.World.MoveDynamicSphere(HitTags[0], AimPointPosition);   // the hitbox sphere follows the root
            }
            UpdateBodyFlash();
            View?.Update(dt, ModelWorld);
        }

        /// <summary>EnemyController.Update: body emission = OnHitBodyGradient((Time - LastTimeDamaged) / FlashOnHitDuration); Unity treats the colour as gamma -> linear.</summary>
        private void UpdateBodyFlash()
        {
            if (_hitGradient != null && View != null)
            {
                Vector4 c = _hitGradient.Evaluate((_manager.Time - LastTimeDamaged) / FlashOnHitDuration);
                View.SetBodyEmission(SceneRenderer.GammaToLinear(new Vector3(c.X, c.Y, c.Z)));
            }
        }

        /// <summary>Moves the hitboxes that hang off bones to the bones' current pose.</summary>
        public void FollowBones()
        {
            if (_ctx.World == null || View == null)
            {
                return;
            }
            foreach (KeyValuePair<int, string> hb in HitboxBones)
            {
                if (View.TryGetBoneWorld(hb.Value, false, out Matrix m))
                {
                    _ctx.World.MoveDynamicBoxes(hb.Key, m);
                }
            }
        }

        // ------------------------------------------------------------------ detection events

        private void OnDetectedTarget()
        {
            IsDetectingTarget = true;
            if (DetectedTarget != null) DetectedTarget();
            View?.SetEyeColor(true);   // AttackEyeColor
        }

        private void OnLostTarget()
        {
            IsDetectingTarget = false;
            if (LostTarget != null) LostTarget();
            View?.SetEyeColor(false);  // DefaultEyeColor
        }

        // ------------------------------------------------------------------ path

        private bool IsPathValid() { return PatrolNodes != null && PatrolNodes.Length > 0; }

        public void ResetPathDestination() { PathDestinationNodeIndex = 0; }

        public void SetPathDestinationToClosestNode()
        {
            if (IsPathValid())
            {
                int closest = 0;
                for (int i = 0; i < PatrolNodes.Length; i++)
                {
                    if (Vector3.Distance(Position, PatrolNodes[i]) < Vector3.Distance(Position, PatrolNodes[closest]))
                    {
                        closest = i;
                    }
                }
                PathDestinationNodeIndex = closest;
            }
            else
            {
                PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            return IsPathValid() ? PatrolNodes[PathDestinationNodeIndex] : Position;
        }

        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathValid() && (Position - GetDestinationOnPath()).Length() <= PathReachingRadius)
            {
                PathDestinationNodeIndex += inverseOrder ? -1 : 1;
                if (PathDestinationNodeIndex < 0) PathDestinationNodeIndex += PatrolNodes.Length;
                if (PathDestinationNodeIndex >= PatrolNodes.Length) PathDestinationNodeIndex -= PatrolNodes.Length;
            }
        }

        public void SetNavDestination(Vector3 destination)
        {
            if (Agent != null) Agent.SetDestination(destination);
        }

        // ------------------------------------------------------------------ orientation and attack

        /// <summary>Slerp of the yaw towards the look position (horizontal), at OrientationSpeed per second.</summary>
        public void OrientTowards(Vector3 lookPosition)
        {
            float dx = lookPosition.X - Position.X, dz = lookPosition.Z - Position.Z;
            if (dx * dx + dz * dz > 1e-12f)
            {
                float target = NavAgent.YawOf(dx, dz);
                float delta = MathHelper.ToDegrees(MathHelper.WrapAngle(MathHelper.ToRadians(target - YawDegrees)));
                YawDegrees = YawDegrees + delta * MathHelper.Clamp(_dt * OrientationSpeed, 0f, 1f);
            }
        }

        public void OrientWeaponsTowards(Vector3 lookPosition)
        {
            Weapon?.OrientTowards(this, lookPosition);
        }

        public bool TryAttack(Vector3 targetPosition)
        {
            if (_ctx.GameIsEnding != null && _ctx.GameIsEnding())
            {
                return false;   // EnemyController.TryAtack: nothing is fired (or aimed) while the game is ending
            }
            if (Weapon == null)
            {
                return false;
            }
            OrientWeaponsTowards(targetPosition);
            if (!Weapon.HandleShootInputs(false, true, false))
            {
                return false;
            }
            if (_ctx.Projectiles != null && _ctx.ProjectileSettings != null)
            {
                Vector3 muzzle = Weapon.MuzzleWorld(this);
                var p = new ProjectileStandard(_ctx.ProjectileSettings, _ctx.World, muzzle, Weapon.Forward, Vector3.Zero)
                {
                    Owner = this,
                    IgnoreOwners = HitTags
                };
                Actor player = _ctx.PlayerActor != null ? _ctx.PlayerActor() : null;
                if (player != null && player.GetCapsule != null)
                {
                    p.ExtraTarget = (o, d, max, radius) =>
                    {
                        Capsule c = player.GetCapsule();
                        bool hit = RayShapes.RayCapsule(o, d, max, c.A, c.B, c.Radius + radius, out float t, out Vector3 n);
                        return (hit, t, n);
                    };
                }
                _ctx.Projectiles.SpawnEnemy(p, _dt);
            }
            if (Attacked != null)
            {
                Attacked();
            }
            return true;
        }

        // ------------------------------------------------------------------ health

        private void OnDamaged(float damage, object damageSource)
        {
            if (OnDamagedAny != null)
            {
                OnDamagedAny();
            }
            // only damage that did not come from an enemy counts as the player's attack
            if (damageSource != null && !(damageSource is Enemy))
            {
                if (damageSource is Actor source)
                {
                    Detection?.OnDamaged(source, _manager.Time);   // pursue the source
                }
                LastTimeDamaged = _manager.Time;
                if (Damaged != null)
                {
                    Damaged();
                }
                // 3d: DamageTick sound (once per frame)
            }
        }

        private void OnDie()
        {
            if (Dying != null)
            {
                Dying(this, DeathVfxPosition);
            }
            _manager.UnregisterEnemy(this);
            Destroy();   // Destroy(gameObject, DeathDuration): DeathDuration is 0 in the scene
        }

        private void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }
            IsDestroyed = true;
            _actors.Unregister(Actor);
            if (Destroyed != null)
            {
                Destroyed(this);
            }
        }
    }
}
