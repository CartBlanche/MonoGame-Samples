using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Gameplay.Combat
{
    /// <summary>Port of Game/Actor.cs: team membership for AI target selection. AimPoint (a Transform in Unity) is added with the AI in 3b.</summary>
    public class Actor
    {
        public int Affiliation;
        public string Name;
        /// <summary>Actor.transform.position (the root: feet for the player, the agent root for enemies).</summary>
        public Func<Vector3> GetPosition = () => Vector3.Zero;
        /// <summary>Actor.AimPoint.position: where other actors aim and cast their line-of-sight ray.</summary>
        public Func<Vector3> GetAimPoint = () => Vector3.Zero;
        /// <summary>The collider the line-of-sight ray has to reach (the player's CharacterController capsule): axis end points and radius; null = not targetable by ray.</summary>
        public Func<Capsule> GetCapsule;
    }

    public struct Capsule
    {
        public Vector3 A, B;   // axis end points (hemisphere centres)
        public float Radius;
    }

    /// <summary>Port of Game/Managers/ActorsManager.cs.</summary>
    public class ActorsManager
    {
        public List<Actor> Actors { get; private set; }
        public object Player { get; private set; }

        public ActorsManager()
        {
            Actors = new List<Actor>();
        }

        public void SetPlayer(object player) { Player = player; }

        public void Register(Actor a)
        {
            if (!Actors.Contains(a))
            {
                Actors.Add(a);
            }
        }

        public void Unregister(Actor a) { Actors.Remove(a); }
    }
}
