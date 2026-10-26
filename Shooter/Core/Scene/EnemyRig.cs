using System;
using Microsoft.Xna.Framework;
using Shooter.Animation;
using Shooter.Gameplay.Enemies;
using Shooter.Rendering;
using RMaterial = Shooter.Rendering.Material;

namespace Shooter.Scene
{
    /// <summary>
    /// The animated model of one enemy: an Animator (<see cref="AnimatorController"/> over the character's clips) writing the skin palette
    /// the render item points at, the eye emission colour (EnemyController's MaterialPropertyBlock) and the model's world matrix.
    /// </summary>
    public sealed class EnemyRig : IEnemyView
    {
        private readonly LoadedScene _scene;
        private readonly int _enemyIndex;
        private readonly AnimatorController _animator;
        private readonly AnimationPlayer _player;
        private readonly Matrix[] _palette;
        private readonly RMaterial _eye, _body;
        private readonly Vector3 _defaultEye, _attackEye;   // linear HDR emission

        private Matrix _modelWorld = Matrix.Identity;
        private Action _lateUpdate;

        public AnimatorController Animator { get { return _animator; } }

        /// <summary>The model's world matrix before the first Update (the static pose).</summary>
        public void SetInitialModelWorld(Matrix world) { _modelWorld = world; }

        public bool HasBone(string bone) { return _player.Model.GetBoneIndex(bone) >= 0; }

        public bool TryGetBoneWorld(string bone, bool bindPose, out Matrix world)
        {
            int i = _player.Model.GetBoneIndex(bone);
            if (i < 0)
            {
                world = Matrix.Identity;
                return false;
            }
            world = (bindPose ? _player.EvaluateBindGlobals()[i] : _player.GlobalPose[i]) * _modelWorld;
            return true;
        }

        public void SetBoneWorldRotation(string bone, Quaternion worldRotation)
        {
            int i = _player.Model.GetBoneIndex(bone);
            if (i < 0)
            {
                return;
            }
            // world = model-space rotation, then the model's own world rotation (row vectors: Concatenate(a, b) = a then b)
            Quaternion modelRot = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(NormalizedRotation(_modelWorld)));
            _player.SetBoneModelRotation(i, Quaternion.Concatenate(Quaternion.Normalize(worldRotation), Quaternion.Inverse(modelRot)));
        }

        public void SetLateUpdate(Action lateUpdate) { _lateUpdate = lateUpdate; }

        private static Matrix NormalizedRotation(Matrix m)
        {
            Vector3 x = new Vector3(m.M11, m.M12, m.M13), y = new Vector3(m.M21, m.M22, m.M23), z = new Vector3(m.M31, m.M32, m.M33);
            x.Normalize(); y.Normalize(); z.Normalize();
            return new Matrix(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1);
        }

        public EnemyRig(LoadedScene scene, int enemyIndex, AnimatorController animator, AnimationPlayer player, Matrix[] palette, RMaterial eye, Vector3 defaultEyeGamma, Vector3 attackEyeGamma, RMaterial body = null)
        {
            _scene = scene;
            _enemyIndex = enemyIndex;
            _animator = animator;
            _player = player;
            _palette = palette;
            _eye = eye;
            _body = body;
            _defaultEye = SceneRenderer.GammaToLinear(defaultEyeGamma);
            _attackEye = SceneRenderer.GammaToLinear(attackEyeGamma);
        }

        /// <summary>Scene reload: back to the default eye colour and idle parameters.</summary>
        public void ResetState()
        {
            SetEyeColor(false);
            SetBodyEmission(Vector3.Zero);
            // parameters a controller does not have are ignored by the controller
            _animator.SetFloat("MoveSpeed", 0f);
            _animator.SetBool("Alerted", false);
            _animator.SetBool("IsActive", false);
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("OnDamaged");
        }

        public void SetFloat(string name, float value) { _animator.SetFloat(name, value); }
        public void SetBool(string name, bool value) { _animator.SetBool(name, value); }
        public void SetTrigger(string name) { _animator.SetTrigger(name); }

        public void SetEyeColor(bool attack)
        {
            if (_eye != null)
            {
                _eye.EmissionColor = attack ? _attackEye : _defaultEye;
            }
        }

        /// <summary>Body _EmissionColor of the on-hit flash (linear HDR); the material is shared by all enemies of the kind (one HoverBot, one Turret).</summary>
        public Vector3 BodyEmission { get; private set; }

        public void SetBodyEmission(Vector3 linearEmission)
        {
            BodyEmission = linearEmission;
            if (_body != null)
            {
                _body.EmissionColor = linearEmission;
            }
        }

        public void Update(float dt, Matrix modelWorld)
        {
            _modelWorld = modelWorld;
            _animator.Update(dt);
            _player.Evaluate();
            if (_lateUpdate != null)
            {
                _lateUpdate();   // LateUpdate scripts (the Turret's pivot) edit the pose after the animation
            }
            _player.CopySkinMatricesTo(_palette);
            _scene.SetEnemyWorld(_enemyIndex, modelWorld);
        }
    }
}
