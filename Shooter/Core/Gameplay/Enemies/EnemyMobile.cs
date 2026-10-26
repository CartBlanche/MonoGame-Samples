using System.Text.Json;
using Microsoft.Xna.Framework;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>Port of AI/EnemyMobile.cs: the HoverBot's Patrol / Follow / Attack state machine and its animator parameters.</summary>
    public class EnemyMobile
    {
        public enum AIState { Patrol, Follow, Attack }

        public float AttackStopDistanceRatio = 0.5f;
        public AIState AiState { get; private set; }

        private readonly Enemy _e;

        public EnemyMobile(Enemy enemy, JsonElement fields)
        {
            _e = enemy;
            AttackStopDistanceRatio = JsonFields.Float(fields, "AttackStopDistanceRatio", AttackStopDistanceRatio);
            // Start(): subscribe, SetPathDestinationToClosestNode, begin patrolling
            _e.Attacked += OnAttack;
            _e.DetectedTarget += OnDetectedTarget;
            _e.LostTarget += OnLostTarget;
            _e.Damaged += OnDamaged;
            _e.SetPathDestinationToClosestNode();
            AiState = AIState.Patrol;
        }

        public void Update()
        {
            UpdateAiStateTransitions();
            UpdateCurrentAiState();
            // Animator MoveSpeed = the agent's velocity magnitude
            if (_e.View != null)
            {
                _e.View.SetFloat("MoveSpeed", _e.Agent.VelocityMagnitude);
            }
        }

        private void UpdateAiStateTransitions()
        {
            switch (AiState)
            {
                case AIState.Follow:
                    // attack when there is a line of sight to the target and it is in range
                    if (_e.Detection.IsSeeingTarget && _e.Detection.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        _e.SetNavDestination(_e.Position);
                    }
                    break;
                case AIState.Attack:
                    if (!_e.Detection.IsTargetInAttackRange)
                    {
                        AiState = AIState.Follow;
                    }
                    break;
            }
        }

        private void UpdateCurrentAiState()
        {
            switch (AiState)
            {
                case AIState.Patrol:
                    _e.UpdatePathDestination();
                    _e.SetNavDestination(_e.GetDestinationOnPath());
                    break;
                case AIState.Follow:
                {
                    Vector3 target = _e.Detection.KnownDetectedTarget.Position;
                    _e.SetNavDestination(target);
                    _e.OrientTowards(target);
                    _e.OrientWeaponsTowards(target);
                    break;
                }
                case AIState.Attack:
                {
                    Vector3 target = _e.Detection.KnownDetectedTarget.Position;
                    if (Vector3.Distance(target, _e.DetectionSourcePosition) >= AttackStopDistanceRatio * _e.Detection.AttackRange)
                    {
                        _e.SetNavDestination(target);
                    }
                    else
                    {
                        _e.SetNavDestination(_e.Position);
                    }
                    _e.OrientTowards(target);
                    _e.TryAttack(target);
                    break;
                }
            }
        }

        private void OnAttack()
        {
            if (_e.View != null) _e.View.SetTrigger("Attack");
        }

        private void OnDetectedTarget()
        {
            if (AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }
            // OnDetectVfx / OnDetectSfx: 3d
            if (_e.View != null) _e.View.SetBool("Alerted", true);
        }

        private void OnLostTarget()
        {
            if (AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
            }
            if (_e.View != null) _e.View.SetBool("Alerted", false);
        }

        private void OnDamaged()
        {
            // RandomHitSparks: 3d
            if (_e.View != null) _e.View.SetTrigger("OnDamaged");
        }
    }
}
