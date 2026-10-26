using System;
using System.Collections.Generic;
using System.Text.Json;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>
    /// Inspector values of PlayerCharacterController / PlayerInputHandler / CharacterController. Defaults are the
    /// script's own defaults; <see cref="FromScene"/> overrides them with the baked values (e.g. MaxSpeedOnGround 13,
    /// SprintSpeedModifier 1.5, GroundCheckDistance 1 differ from the script defaults).
    /// </summary>
    public class PlayerSettings
    {
        public float GravityDownForce = 20f;
        public float GroundCheckDistance = 0.05f;
        public float MaxSpeedOnGround = 10f;
        public float MovementSharpnessOnGround = 15f;
        public float MaxSpeedCrouchedRatio = 0.5f;
        public float MaxSpeedInAir = 10f;
        public float AccelerationSpeedInAir = 25f;
        public float SprintSpeedModifier = 2f;
        public float KillHeight = -50f;
        public float RotationSpeed = 200f;
        public float AimingRotationMultiplier = 0.4f;
        public float JumpForce = 9f;
        public float FootstepSfxFrequency = 1f, FootstepSfxFrequencyWhileSprinting = 1f;   // steps per metre travelled
        public float CameraHeightRatio = 0.9f;
        public float CapsuleHeightStanding = 1.8f;
        public float CapsuleHeightCrouching = 0.9f;
        public float CrouchingSharpness = 10f;
        public bool RecievesFallDamage;
        public float MinSpeedForFallDamage = 10f;
        public float MaxSpeedForFallDamage = 30f;
        public float FallDamageAtMinSpeed = 10f;
        public float FallDamageAtMaxSpeed = 50f;

        public float LookSensitivity = 1f;
        public bool InvertYAxis;
        public bool InvertXAxis;

        // CharacterController component
        public float Radius = 0.35f;
        public float SlopeLimit = 45f;
        public float StepOffset = 0.3f;
        public float SkinWidth = 0.08f;

        // spawn
        public Microsoft.Xna.Framework.Vector3 StartPosition;
        public float StartYawDegrees;

        public PlayerSettings Clone() { return (PlayerSettings)MemberwiseClone(); }

        public static PlayerSettings FromScene(SceneFile scene)
        {
            PlayerDto p = scene.Player;
            var s = new PlayerSettings();
            s.Radius = p.CharacterRadius > 0f ? p.CharacterRadius : s.Radius;
            s.SlopeLimit = p.SlopeLimit;
            s.StepOffset = p.StepOffset;
            s.SkinWidth = p.SkinWidth > 0f ? p.SkinWidth : s.SkinWidth;
            s.StartPosition = new Microsoft.Xna.Framework.Vector3(p.Position[0], p.Position[1], p.Position[2]);
            s.StartYawDegrees = p.YawDegrees;

            Dictionary<string, JsonElement> c = p.Components;
            if (c != null)
            {
                JsonElement pc;
                if (c.TryGetValue("PlayerCharacterController", out pc))
                {
                    s.GravityDownForce = F(pc, "GravityDownForce", s.GravityDownForce);
                    s.GroundCheckDistance = F(pc, "GroundCheckDistance", s.GroundCheckDistance);
                    s.MaxSpeedOnGround = F(pc, "MaxSpeedOnGround", s.MaxSpeedOnGround);
                    s.MovementSharpnessOnGround = F(pc, "MovementSharpnessOnGround", s.MovementSharpnessOnGround);
                    s.MaxSpeedCrouchedRatio = F(pc, "MaxSpeedCrouchedRatio", s.MaxSpeedCrouchedRatio);
                    s.MaxSpeedInAir = F(pc, "MaxSpeedInAir", s.MaxSpeedInAir);
                    s.AccelerationSpeedInAir = F(pc, "AccelerationSpeedInAir", s.AccelerationSpeedInAir);
                    s.SprintSpeedModifier = F(pc, "SprintSpeedModifier", s.SprintSpeedModifier);
                    s.KillHeight = F(pc, "KillHeight", s.KillHeight);
                    s.RotationSpeed = F(pc, "RotationSpeed", s.RotationSpeed);
                    s.AimingRotationMultiplier = F(pc, "AimingRotationMultiplier", s.AimingRotationMultiplier);
                    s.JumpForce = F(pc, "JumpForce", s.JumpForce);
                    s.FootstepSfxFrequency = F(pc, "FootstepSfxFrequency", s.FootstepSfxFrequency);
                    s.FootstepSfxFrequencyWhileSprinting = F(pc, "FootstepSfxFrequencyWhileSprinting", s.FootstepSfxFrequencyWhileSprinting);
                    s.CameraHeightRatio = F(pc, "CameraHeightRatio", s.CameraHeightRatio);
                    s.CapsuleHeightStanding = F(pc, "CapsuleHeightStanding", s.CapsuleHeightStanding);
                    s.CapsuleHeightCrouching = F(pc, "CapsuleHeightCrouching", s.CapsuleHeightCrouching);
                    s.CrouchingSharpness = F(pc, "CrouchingSharpness", s.CrouchingSharpness);
                    s.RecievesFallDamage = F(pc, "RecievesFallDamage", 0f) != 0f;
                    s.MinSpeedForFallDamage = F(pc, "MinSpeedForFallDamage", s.MinSpeedForFallDamage);
                    s.MaxSpeedForFallDamage = F(pc, "MaxSpeedForFallDamage", s.MaxSpeedForFallDamage);
                    s.FallDamageAtMinSpeed = F(pc, "FallDamageAtMinSpeed", s.FallDamageAtMinSpeed);
                    s.FallDamageAtMaxSpeed = F(pc, "FallDamageAtMaxSpeed", s.FallDamageAtMaxSpeed);
                }
                JsonElement ih;
                if (c.TryGetValue("PlayerInputHandler", out ih))
                {
                    s.LookSensitivity = F(ih, "LookSensitivity", s.LookSensitivity);
                    s.InvertXAxis = F(ih, "InvertXAxis", 0f) != 0f;
                    s.InvertYAxis = F(ih, "InvertYAxis", 0f) != 0f;
                }
            }
            return s;
        }

        private static float F(JsonElement obj, string name, float fallback)
        {
            JsonElement v;
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.Number)
            {
                return (float)v.GetDouble();
            }
            return fallback;
        }
    }
}
