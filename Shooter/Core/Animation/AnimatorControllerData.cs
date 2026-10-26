using System.Collections.Generic;
using System.Text.Json;

namespace Shooter.Animation
{
    // POCOs for the *.animator.json files written by Tools/Shooter.CharacterExport (format "shooter.animator/1").

    public sealed class AnimatorControllerData
    {
        public string Controller { get; set; }
        public string Rig { get; set; }
        public string Format { get; set; }
        public List<AnimatorParameterData> Parameters { get; set; } = new List<AnimatorParameterData>();
        public List<AnimatorLayerData> Layers { get; set; } = new List<AnimatorLayerData>();
        public List<AnimatorClipData> Clips { get; set; } = new List<AnimatorClipData>();

        public static AnimatorControllerData FromJson(string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<AnimatorControllerData>(json, options);
        }
    }

    public sealed class AnimatorParameterData
    {
        public string Name { get; set; }

        /// <summary>"float", "int", "bool" or "trigger".</summary>
        public string Type { get; set; }

        public double DefaultFloat { get; set; }
        public long DefaultInt { get; set; }
        public bool DefaultBool { get; set; }
    }

    public sealed class AnimatorClipData
    {
        /// <summary>Unity clip name (not unique across rigs, e.g. "Idle").</summary>
        public string Name { get; set; }

        /// <summary>ContentManager asset name, e.g. "Characters/HoverBot@Idle".</summary>
        public string Asset { get; set; }

        public bool LoopTime { get; set; }
        public bool LoopPose { get; set; }
        public double FirstFrame { get; set; }
        public double LastFrame { get; set; }
        public bool HasClipSettings { get; set; }
    }

    public sealed class AnimatorMaskData
    {
        public string Name { get; set; }

        /// <summary>Bone names whose mask weight is &gt; 0.</summary>
        public List<string> IncludedTransforms { get; set; } = new List<string>();

        public List<string> ExcludedTransforms { get; set; } = new List<string>();
    }

    public sealed class AnimatorLayerData
    {
        public string Name { get; set; }

        /// <summary>"override" or "additive".</summary>
        public string BlendMode { get; set; }

        public double DefaultWeight { get; set; }
        public int SyncedLayerIndex { get; set; }
        public bool SyncedLayerAffectsTiming { get; set; }
        public bool IkPass { get; set; }
        public AnimatorMaskData AvatarMask { get; set; }

        /// <summary>State id (path) entered on start and after an Exit transition; null when the layer has no states.</summary>
        public string DefaultState { get; set; }

        public List<AnimatorTransitionData> AnyStateTransitions { get; set; } = new List<AnimatorTransitionData>();
        public List<AnimatorStateData> States { get; set; } = new List<AnimatorStateData>();
    }

    public sealed class AnimatorMotionData
    {
        /// <summary>"clip" or "blendTree".</summary>
        public string Type { get; set; }

        public string Clip { get; set; }
        public string Asset { get; set; }

        // blend tree fields (the shipped controllers only contain empty blend trees in unreachable states)
        public string Name { get; set; }
        public string BlendType { get; set; }
        public string BlendParameter { get; set; }
        public string BlendParameterY { get; set; }
        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }
        public bool UseAutomaticThresholds { get; set; }
        public List<AnimatorBlendChildData> Children { get; set; }
    }

    public sealed class AnimatorBlendChildData
    {
        public AnimatorMotionData Motion { get; set; }
        public double Threshold { get; set; }
        public double TimeScale { get; set; }
        public double CycleOffset { get; set; }
        public string DirectBlendParameter { get; set; }
    }

    public sealed class AnimatorStateData
    {
        /// <summary>Unique within the layer, path style ("Locomotion/Patrolling").</summary>
        public string Id { get; set; }

        public string Name { get; set; }
        public double Speed { get; set; } = 1.0;
        public string SpeedParameter { get; set; }
        public double CycleOffset { get; set; }
        public string CycleOffsetParameter { get; set; }
        public string TimeParameter { get; set; }
        public bool Mirror { get; set; }
        public bool WriteDefaultValues { get; set; }
        public string Tag { get; set; }
        public AnimatorMotionData Motion { get; set; }
        public List<AnimatorTransitionData> Transitions { get; set; } = new List<AnimatorTransitionData>();
    }

    public sealed class AnimatorTransitionData
    {
        public bool IsExit { get; set; }

        /// <summary>Destination state id; null for Exit transitions (go to the layer's default state).</summary>
        public string Destination { get; set; }

        public bool HasExitTime { get; set; }

        /// <summary>Normalized time of the source state at which the transition may start.</summary>
        public double ExitTime { get; set; }

        /// <summary>true: Duration is seconds; false: Duration is a fraction of the source state's length.</summary>
        public bool HasFixedDuration { get; set; }

        public double Duration { get; set; }

        /// <summary>Normalized start time in the destination state.</summary>
        public double Offset { get; set; }

        public string InterruptionSource { get; set; }
        public bool OrderedInterruption { get; set; }
        public bool CanTransitionToSelf { get; set; }
        public bool Mute { get; set; }
        public bool Solo { get; set; }
        public List<AnimatorConditionData> Conditions { get; set; } = new List<AnimatorConditionData>();
    }

    public sealed class AnimatorConditionData
    {
        public string Parameter { get; set; }

        /// <summary>if, ifNot, greater, less, equals, notEqual.</summary>
        public string Mode { get; set; }

        public double Threshold { get; set; }
    }
}
