using System;
using System.Numerics;
using Shooter.Core.Components;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Simple particle emitter for visual effects (impacts, sparks, smoke).
/// Similar to Unity's ParticleSystem but much simpler for Phase 2C.
/// </summary>
public class ParticleEmitter : EntityComponent
{
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector4 Color;
        public float Size;
        public float Lifetime;
        public float Age;
        public bool Active;
    }

    private Particle[] _particles = new Particle[50];
    private int _maxParticles = 50;
    private Random _random = new Random();

    // Emitter properties
    public int MaxParticles
    {
        get => _maxParticles;
        set
        {
            if (value != _maxParticles)
            {
                _maxParticles = value;
                _particles = new Particle[value];
            }
        }
    }

    public Vector3 EmissionPosition { get; set; }
    public Vector4 StartColor { get; set; } = new Vector4(1, 1, 1, 1);
    public Vector4 EndColor { get; set; } = new Vector4(1, 1, 1, 0);
    public float StartSize { get; set; } = 0.1f;
    public float EndSize { get; set; } = 0.05f;
    public float Lifetime { get; set; } = 0.5f;
    public float EmissionSpeed { get; set; } = 5f;
    public float Gravity { get; set; } = -9.8f;
    public bool OneShot { get; set; } = true; // Burst or continuous

    private bool _hasEmitted = false;

    /// <summary>
    /// Get active particles for rendering
    /// </summary>
    public Particle[] GetActiveParticles()
    {
        return _particles;
    }

    /// <summary>
    /// Emit a burst of particles
    /// </summary>
    public void Emit(int count, Vector3 position, Vector3 direction)
    {
        EmissionPosition = position;
        _hasEmitted = true;

        int emitted = 0;
        for (int i = 0; i < _particles.Length && emitted < count; i++)
        {
            if (!_particles[i].Active)
            {
                // Random cone spread around direction
                Vector3 randomDir = GetRandomDirection(direction, 30f); // 30 degree cone

                _particles[i].Position = position;
                _particles[i].Velocity = randomDir * EmissionSpeed;
                _particles[i].Color = StartColor;
                _particles[i].Size = StartSize;
                _particles[i].Lifetime = Lifetime;
                _particles[i].Age = 0f;
                _particles[i].Active = true;
                emitted++;
            }
        }
    }

    public override void Update(Core.Components.GameTime gameTime)
    {
        base.Update(gameTime);

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update all active particles
        for (int i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].Active)
                continue;

            _particles[i].Age += deltaTime;

            // Deactivate if lifetime exceeded
            if (_particles[i].Age >= _particles[i].Lifetime)
            {
                _particles[i].Active = false;
                continue;
            }

            // Update position with velocity and gravity
            _particles[i].Velocity += new Vector3(0, Gravity * deltaTime, 0);
            _particles[i].Position += _particles[i].Velocity * deltaTime;

            // Interpolate color and size based on age
            float t = _particles[i].Age / _particles[i].Lifetime;
            _particles[i].Color = Vector4.Lerp(StartColor, EndColor, t);
            _particles[i].Size = MathHelper.Lerp(StartSize, EndSize, t);
        }

        // Auto-destroy one-shot emitters after all particles die
        if (OneShot && _hasEmitted && AllParticlesDead())
        {
            if (Owner != null)
                Owner.Active = false; // Deactivate entity
        }
    }

    private bool AllParticlesDead()
    {
        foreach (var particle in _particles)
        {
            if (particle.Active)
                return false;
        }
        return true;
    }

    private Vector3 GetRandomDirection(Vector3 baseDirection, float coneAngleDegrees)
    {
        // Convert to radians
        float coneAngle = coneAngleDegrees * (float)(Math.PI / 180.0);

        // Random angles within cone
        float randomAngle = (float)(_random.NextDouble() * Math.PI * 2);
        float randomDistance = (float)(_random.NextDouble() * coneAngle);

        // Create perpendicular vectors
        Vector3 up = Vector3.UnitY;
        if (Math.Abs(Vector3.Dot(baseDirection, up)) > 0.99f)
            up = Vector3.UnitX;

        Vector3 right = Vector3.Normalize(Vector3.Cross(up, baseDirection));
        up = Vector3.Normalize(Vector3.Cross(baseDirection, right));

        // Apply spread
        float spreadX = MathF.Cos(randomAngle) * randomDistance;
        float spreadY = MathF.Sin(randomAngle) * randomDistance;

        Vector3 result = baseDirection + (right * spreadX) + (up * spreadY);
        return Vector3.Normalize(result);
    }
}

/// <summary>
/// Helper class for math utilities
/// </summary>
public static class MathHelper
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
}
