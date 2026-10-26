using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace Shooter.Rendering.Particles
{
    public enum ParticleShapeType
    {
        Sphere = 0, SphereShell = 1, Hemisphere = 2, HemisphereShell = 3, Cone = 4, Box = 5, Mesh = 6, ConeShell = 7, ConeVolume = 8,
        ConeVolumeShell = 9, Circle = 10, CircleEdge = 11, SingleSidedEdge = 12, MeshRenderer = 13, SkinnedMeshRenderer = 14,
        BoxShell = 15, BoxEdge = 16, Donut = 17, Rectangle = 18, Sprite = 19, SpriteRenderer = 20
    }

    public enum ParticleRenderMode { Billboard = 0, Stretch = 1, HorizontalBillboard = 2, VerticalBillboard = 3, Mesh = 4, None = 5 }

    public sealed class ParticleBurst
    {
        public float Time;
        public MinMaxCurve Count = MinMaxCurve.Constant(0);
        public int CycleCount = 1;
        public float RepeatInterval = 0.01f;
        public float Probability = 1f;
    }

    /// <summary>
    /// Data-driven description of one Unity ShuriKen ParticleSystem (+ its ParticleSystemRenderer), parsed from the RAW serialisation
    /// that Tools/UnityExtractor keeps (see <see cref="FromRaw"/>). Everything here is in Unity conventions (degrees where Unity serialises
    /// degrees, radians for rotation properties); the simulation converts to the MonoGame world (Z mirrored) at the boundaries.
    /// </summary>
    public sealed class ParticleSystemData
    {
        public string Name;
        public string MaterialPath;
        public string MaterialName;
        public string MeshName;       // renderer m_Mesh path/guid when mode == Mesh

        // main module
        public float Duration = 5f;
        public bool Looping = true;
        public bool Prewarm;
        public bool PlayOnAwake = true;
        public MinMaxCurve StartDelay = MinMaxCurve.Constant(0);
        public float SimulationSpeed = 1f;
        public bool WorldSpace;       // moveWithTransform: 0 local, 1 world, 2 custom (treated as world)
        public int ScalingMode;       // 0 hierarchy, 1 local, 2 shape
        public Vector3 LocalScale = Vector3.One;   // the system's own transform scale (baked "localScale"); ScalingMode Local ignores the parents' scale
        public int MaxParticles = 1000;

        // initial module
        public MinMaxCurve StartLifetime = MinMaxCurve.Constant(5);
        public MinMaxCurve StartSpeed = MinMaxCurve.Constant(5);
        public MinMaxGradient StartColor = MinMaxGradient.White();
        public MinMaxCurve StartSizeX = MinMaxCurve.Constant(1);
        public MinMaxCurve StartSizeY = MinMaxCurve.Constant(1);
        public MinMaxCurve StartSizeZ = MinMaxCurve.Constant(1);
        public bool Size3D;
        public MinMaxCurve StartRotationX = MinMaxCurve.Constant(0);
        public MinMaxCurve StartRotationY = MinMaxCurve.Constant(0);
        public MinMaxCurve StartRotationZ = MinMaxCurve.Constant(0);
        public bool Rotation3D;
        public float RandomizeRotationDirection;
        public MinMaxCurve GravityModifier = MinMaxCurve.Constant(0);

        // emission
        public bool EmissionEnabled = true;
        public MinMaxCurve RateOverTime = MinMaxCurve.Constant(10);
        public MinMaxCurve RateOverDistance = MinMaxCurve.Constant(0);
        public List<ParticleBurst> Bursts = new List<ParticleBurst>();

        // shape
        public bool ShapeEnabled = true;
        public ParticleShapeType Shape = ParticleShapeType.Cone;
        public float ShapeAngle = 25f;     // degrees
        public float ShapeRadius = 1f;
        public float ShapeRadiusThickness = 1f;
        public float ShapeArc = 360f;
        public float ShapeLength = 5f;
        public Vector3 ShapeBoxThickness;
        public Vector3 ShapePosition;
        public Vector3 ShapeRotation;      // euler degrees
        public Vector3 ShapeScale = Vector3.One;
        public float DonutRadius = 0.2f;
        public bool AlignToDirection;
        public float RandomDirectionAmount;
        public float SphericalDirectionAmount;
        public float RandomPositionAmount;

        // lifetime modules
        public bool VelocityEnabled;
        public MinMaxCurve VelocityX = MinMaxCurve.Constant(0), VelocityY = MinMaxCurve.Constant(0), VelocityZ = MinMaxCurve.Constant(0);
        public bool VelocityWorldSpace;
        public MinMaxCurve SpeedModifier = MinMaxCurve.Constant(1);

        public bool ForceEnabled;
        public MinMaxCurve ForceX = MinMaxCurve.Constant(0), ForceY = MinMaxCurve.Constant(0), ForceZ = MinMaxCurve.Constant(0);
        public bool ForceWorldSpace;
        public bool ForceRandomizePerFrame;

        public bool ClampEnabled;
        public MinMaxCurve ClampX = MinMaxCurve.Constant(1), ClampY = MinMaxCurve.Constant(1), ClampZ = MinMaxCurve.Constant(1);
        public MinMaxCurve ClampMagnitude = MinMaxCurve.Constant(1);
        public bool ClampSeparateAxis;
        public bool ClampWorldSpace;
        public float ClampDampen;
        public MinMaxCurve ClampDrag = MinMaxCurve.Constant(0);
        public bool ClampDragBySize = true, ClampDragByVelocity = true;

        public bool SizeEnabled;
        public MinMaxCurve SizeX = MinMaxCurve.Constant(1), SizeY = MinMaxCurve.Constant(1), SizeZ = MinMaxCurve.Constant(1);
        public bool SizeSeparateAxes;

        public bool RotationEnabled;
        public MinMaxCurve RotationX = MinMaxCurve.Constant(0), RotationY = MinMaxCurve.Constant(0), RotationZ = MinMaxCurve.Constant(0);
        public bool RotationSeparateAxes;

        public bool ColorEnabled;
        public MinMaxGradient ColorOverLifetime = MinMaxGradient.White();

        // Trails module (mode PerParticle): a ribbon behind each particle
        public float TrailRatio = 1f;
        public MinMaxCurve TrailLifetime = MinMaxCurve.Constant(1f);
        public float TrailMinVertexDistance = 0.2f;
        public bool TrailDieWithParticles, TrailSizeAffectsWidth = true, TrailSizeAffectsLifetime, TrailInheritColor;
        public bool TrailWorldSpace;                                             // false: the ribbon moves with the system (a weapon's trails must not smear behind it)
        public MinMaxGradient TrailColorOverLifetime = MinMaxGradient.White();   // over the particle's lifetime (the whole ribbon)
        public MinMaxGradient TrailColorOverTrail = MinMaxGradient.White();      // along the ribbon, 0 = head, 1 = tail
        public MinMaxCurve TrailWidth = MinMaxCurve.Constant(1f);                // along the ribbon
        public string TrailMaterialName;                                         // the renderer's second material (the trail material)

        public bool NoiseEnabled;
        public MinMaxCurve NoiseStrengthX = MinMaxCurve.Constant(1), NoiseStrengthY = MinMaxCurve.Constant(1), NoiseStrengthZ = MinMaxCurve.Constant(1);
        public bool NoiseSeparateAxes;
        public float NoiseFrequency = 0.5f;
        public float NoiseDamping = 1f;
        public int NoiseOctaves = 1;
        public float NoiseOctaveMultiplier = 0.5f;
        public float NoiseOctaveScale = 2f;
        public MinMaxCurve NoiseScrollSpeed = MinMaxCurve.Constant(0);

        public bool UvEnabled;
        public int UvMode;         // 0 grid, 1 sprites
        public int UvTimeMode;     // 0 lifetime, 1 speed, 2 fps
        public float UvFps = 30f;
        public MinMaxCurve UvFrameOverTime = MinMaxCurve.Constant(0);
        public MinMaxCurve UvStartFrame = MinMaxCurve.Constant(0);
        public Vector2 UvSpeedRange = new Vector2(0, 1);
        public int UvTilesX = 1, UvTilesY = 1;
        public int UvAnimationType;    // 0 whole sheet, 1 single row
        public int UvRowIndex;
        public float UvCycles = 1f;
        public bool UvRandomRow = true;
        public bool UvFlipU, UvFlipV;

        public bool InheritVelocityEnabled;
        public int InheritVelocityMode;
        public MinMaxCurve InheritVelocity = MinMaxCurve.Constant(0);

        // modules that are parsed only so they can be reported
        public bool TrailsEnabled, CollisionEnabled, SubEmittersEnabled, LightsEnabled, ExternalForcesEnabled, SizeBySpeedEnabled,
            RotationBySpeedEnabled, ColorBySpeedEnabled, CustomDataEnabled, TriggerEnabled;

        // renderer
        public ParticleRenderMode RenderMode = ParticleRenderMode.Billboard;
        public float LengthScale = 2f;
        public float VelocityScale;
        public float CameraVelocityScale;
        public float MinParticleSize;
        public float MaxParticleSize = 0.5f;
        public int SortMode;
        public float SortingFudge;
        public int RenderAlignment;
        public Vector3 Pivot;
        public int RendererPriority;
        public bool ApplyActiveColorSpace = true;

        /// <summary>Names of the Unity modules enabled on this system (for the usage report).</summary>
        public List<string> EnabledModules = new List<string>();

        // --------------------------------------------------------------------------------------------- loading

        /// <summary>
        /// Parses the <c>props</c> object of a "ParticleSystem" component and (optionally) the <c>props</c> of the sibling
        /// "ParticleSystemRenderer" exactly as written by Tools/UnityExtractor.
        /// </summary>
        public static ParticleSystemData FromRaw(string name, JsonElement ps, JsonElement renderer)
        {
            var d = new ParticleSystemData { Name = name };

            d.Duration = Math.Max(0.01f, UJson.F(ps, "lengthInSec", 5f));
            d.Looping = UJson.B(ps, "looping");
            d.Prewarm = UJson.B(ps, "prewarm");
            d.PlayOnAwake = UJson.B(ps, "playOnAwake", true);
            d.StartDelay = MinMaxCurve.FromJson(ps, "startDelay", 0f);
            d.SimulationSpeed = UJson.F(ps, "simulationSpeed", 1f);
            d.WorldSpace = UJson.I(ps, "moveWithTransform") != 0;
            d.ScalingMode = UJson.I(ps, "scalingMode");

            if (UJson.Has(ps, "InitialModule", out JsonElement im))
            {
                d.StartLifetime = MinMaxCurve.FromJson(im, "startLifetime", 5f);
                d.StartSpeed = MinMaxCurve.FromJson(im, "startSpeed", 5f);
                d.StartColor = MinMaxGradient.FromJson(im, "startColor");
                d.StartSizeX = MinMaxCurve.FromJson(im, "startSize", 1f);
                d.StartSizeY = MinMaxCurve.FromJson(im, "startSizeY", 1f);
                d.StartSizeZ = MinMaxCurve.FromJson(im, "startSizeZ", 1f);
                d.Size3D = UJson.B(im, "size3D");
                d.StartRotationX = MinMaxCurve.FromJson(im, "startRotationX", 0f);
                d.StartRotationY = MinMaxCurve.FromJson(im, "startRotationY", 0f);
                d.StartRotationZ = MinMaxCurve.FromJson(im, "startRotation", 0f);
                d.Rotation3D = UJson.B(im, "rotation3D");
                d.RandomizeRotationDirection = UJson.F(im, "randomizeRotationDirection");
                d.MaxParticles = Math.Max(1, UJson.I(im, "maxNumParticles", 1000));
                d.GravityModifier = MinMaxCurve.FromJson(im, "gravityModifier", 0f);
            }

            if (UJson.Has(ps, "EmissionModule", out JsonElement em))
            {
                d.EmissionEnabled = UJson.B(em, "enabled", true);
                d.RateOverTime = MinMaxCurve.FromJson(em, "rateOverTime", 10f);
                d.RateOverDistance = MinMaxCurve.FromJson(em, "rateOverDistance", 0f);
                if (UJson.Has(em, "m_Bursts", out JsonElement bursts) && bursts.ValueKind == JsonValueKind.Array)
                {
                    int count = UJson.I(em, "m_BurstCount", bursts.GetArrayLength());
                    int i = 0;
                    foreach (JsonElement b in bursts.EnumerateArray())
                    {
                        if (i++ >= count) break;
                        d.Bursts.Add(new ParticleBurst
                        {
                            Time = UJson.F(b, "time"),
                            Count = MinMaxCurve.FromJson(b, "countCurve", 0f),
                            CycleCount = UJson.I(b, "cycleCount", 1),
                            RepeatInterval = Math.Max(0.001f, UJson.F(b, "repeatInterval", 0.01f)),
                            Probability = UJson.F(b, "probability", 1f)
                        });
                    }
                }
            }

            if (UJson.Has(ps, "ShapeModule", out JsonElement sh))
            {
                d.ShapeEnabled = UJson.B(sh, "enabled", true);
                d.Shape = (ParticleShapeType)UJson.I(sh, "type", 4);
                d.ShapeAngle = UJson.F(sh, "angle", 25f);
                d.ShapeRadius = MultiMode(sh, "radius", 1f);
                d.ShapeRadiusThickness = UJson.F(sh, "radiusThickness", 1f);
                d.ShapeArc = MultiMode(sh, "arc", 360f);
                d.ShapeLength = UJson.F(sh, "length", 5f);
                d.ShapeBoxThickness = UJson.V3(sh, "boxThickness", Vector3.Zero);
                d.ShapePosition = UJson.V3(sh, "m_Position", Vector3.Zero);
                d.ShapeRotation = UJson.V3(sh, "m_Rotation", Vector3.Zero);
                d.ShapeScale = UJson.V3(sh, "m_Scale", Vector3.One);
                d.DonutRadius = UJson.F(sh, "donutRadius", 0.2f);
                d.AlignToDirection = UJson.B(sh, "alignToDirection");
                d.RandomDirectionAmount = UJson.F(sh, "randomDirectionAmount");
                d.SphericalDirectionAmount = UJson.F(sh, "sphericalDirectionAmount");
                d.RandomPositionAmount = UJson.F(sh, "randomPositionAmount");
            }

            if (UJson.Has(ps, "VelocityModule", out JsonElement vm))
            {
                d.VelocityEnabled = UJson.B(vm, "enabled");
                d.VelocityX = MinMaxCurve.FromJson(vm, "x", 0f);
                d.VelocityY = MinMaxCurve.FromJson(vm, "y", 0f);
                d.VelocityZ = MinMaxCurve.FromJson(vm, "z", 0f);
                d.VelocityWorldSpace = UJson.B(vm, "inWorldSpace");
                d.SpeedModifier = MinMaxCurve.FromJson(vm, "speedModifier", 1f);
            }

            if (UJson.Has(ps, "ForceModule", out JsonElement fm))
            {
                d.ForceEnabled = UJson.B(fm, "enabled");
                d.ForceX = MinMaxCurve.FromJson(fm, "x", 0f);
                d.ForceY = MinMaxCurve.FromJson(fm, "y", 0f);
                d.ForceZ = MinMaxCurve.FromJson(fm, "z", 0f);
                d.ForceWorldSpace = UJson.B(fm, "inWorldSpace");
                d.ForceRandomizePerFrame = UJson.B(fm, "randomizePerFrame");
            }

            if (UJson.Has(ps, "ClampVelocityModule", out JsonElement cm))
            {
                d.ClampEnabled = UJson.B(cm, "enabled");
                d.ClampX = MinMaxCurve.FromJson(cm, "x", 1f);
                d.ClampY = MinMaxCurve.FromJson(cm, "y", 1f);
                d.ClampZ = MinMaxCurve.FromJson(cm, "z", 1f);
                d.ClampMagnitude = MinMaxCurve.FromJson(cm, "magnitude", 1f);
                d.ClampSeparateAxis = UJson.B(cm, "separateAxis");
                d.ClampWorldSpace = UJson.B(cm, "inWorldSpace");
                d.ClampDampen = UJson.F(cm, "dampen");
                d.ClampDrag = MinMaxCurve.FromJson(cm, "drag", 0f);
                d.ClampDragBySize = UJson.B(cm, "multiplyDragByParticleSize", true);
                d.ClampDragByVelocity = UJson.B(cm, "multiplyDragByParticleVelocity", true);
            }

            if (UJson.Has(ps, "SizeModule", out JsonElement szm))
            {
                d.SizeEnabled = UJson.B(szm, "enabled");
                d.SizeX = MinMaxCurve.FromJson(szm, "curve", 1f);
                d.SizeY = MinMaxCurve.FromJson(szm, "y", 1f);
                d.SizeZ = MinMaxCurve.FromJson(szm, "z", 1f);
                d.SizeSeparateAxes = UJson.B(szm, "separateAxes");
            }

            if (UJson.Has(ps, "RotationModule", out JsonElement rm))
            {
                d.RotationEnabled = UJson.B(rm, "enabled");
                d.RotationX = MinMaxCurve.FromJson(rm, "x", 0f);
                d.RotationY = MinMaxCurve.FromJson(rm, "y", 0f);
                d.RotationZ = MinMaxCurve.FromJson(rm, "curve", 0f);
                d.RotationSeparateAxes = UJson.B(rm, "separateAxes");
            }

            if (UJson.Has(ps, "ColorModule", out JsonElement colm))
            {
                d.ColorEnabled = UJson.B(colm, "enabled");
                d.ColorOverLifetime = MinMaxGradient.FromJson(colm, "gradient");
            }

            if (UJson.Has(ps, "NoiseModule", out JsonElement nm))
            {
                d.NoiseEnabled = UJson.B(nm, "enabled");
                d.NoiseStrengthX = MinMaxCurve.FromJson(nm, "strength", 1f);
                d.NoiseStrengthY = MinMaxCurve.FromJson(nm, "strengthY", 1f);
                d.NoiseStrengthZ = MinMaxCurve.FromJson(nm, "strengthZ", 1f);
                d.NoiseSeparateAxes = UJson.B(nm, "separateAxes");
                d.NoiseFrequency = UJson.F(nm, "frequency", 0.5f);
                d.NoiseDamping = UJson.F(nm, "damping", 1f);
                d.NoiseOctaves = Math.Max(1, UJson.I(nm, "octaves", 1));
                d.NoiseOctaveMultiplier = UJson.F(nm, "octaveMultiplier", 0.5f);
                d.NoiseOctaveScale = UJson.F(nm, "octaveScale", 2f);
                d.NoiseScrollSpeed = MinMaxCurve.FromJson(nm, "scrollSpeed", 0f);
            }

            if (UJson.Has(ps, "UVModule", out JsonElement uvm))
            {
                d.UvEnabled = UJson.B(uvm, "enabled");
                d.UvMode = UJson.I(uvm, "mode");
                d.UvTimeMode = UJson.I(uvm, "timeMode");
                d.UvFps = UJson.F(uvm, "fps", 30f);
                d.UvFrameOverTime = MinMaxCurve.FromJson(uvm, "frameOverTime", 0f);
                d.UvStartFrame = MinMaxCurve.FromJson(uvm, "startFrame", 0f);
                if (UJson.Has(uvm, "speedRange", out JsonElement sr)) d.UvSpeedRange = new Vector2(UJson.F(sr, "x"), UJson.F(sr, "y", 1f));
                d.UvTilesX = Math.Max(1, UJson.I(uvm, "tilesX", 1));
                d.UvTilesY = Math.Max(1, UJson.I(uvm, "tilesY", 1));
                d.UvAnimationType = UJson.I(uvm, "animationType");
                d.UvRowIndex = UJson.I(uvm, "rowIndex");
                d.UvCycles = UJson.F(uvm, "cycles", 1f);
                d.UvRandomRow = UJson.B(uvm, "randomRow", true);
                d.UvFlipU = UJson.F(uvm, "flipU") > 0f;
                d.UvFlipV = UJson.F(uvm, "flipV") > 0f;
            }

            if (UJson.Has(ps, "InheritVelocityModule", out JsonElement ivm))
            {
                d.InheritVelocityEnabled = UJson.B(ivm, "enabled");
                d.InheritVelocityMode = UJson.I(ivm, "m_Mode");
                d.InheritVelocity = MinMaxCurve.FromJson(ivm, "m_Curve", 0f);
            }

            d.TrailsEnabled = EnabledFlag(ps, "TrailModule");
            if (d.TrailsEnabled && UJson.Has(ps, "TrailModule", out JsonElement trm))
            {
                d.TrailRatio = UJson.F(trm, "ratio", 1f);
                d.TrailLifetime = MinMaxCurve.FromJson(trm, "lifetime", 1f);
                d.TrailMinVertexDistance = UJson.F(trm, "minVertexDistance", 0.2f);
                d.TrailDieWithParticles = UJson.B(trm, "dieWithParticles");
                d.TrailWorldSpace = UJson.B(trm, "worldSpace");
                d.TrailSizeAffectsWidth = !UJson.Has(trm, "sizeAffectsWidth", out _) || UJson.B(trm, "sizeAffectsWidth");
                d.TrailSizeAffectsLifetime = UJson.B(trm, "sizeAffectsLifetime");
                d.TrailInheritColor = UJson.B(trm, "inheritParticleColor");
                d.TrailColorOverLifetime = MinMaxGradient.FromJson(trm, "colorOverLifetime");
                d.TrailColorOverTrail = MinMaxGradient.FromJson(trm, "colorOverTrail");
                d.TrailWidth = MinMaxCurve.FromJson(trm, "widthOverTrail", 1f);
            }
            d.CollisionEnabled = EnabledFlag(ps, "CollisionModule");
            d.SubEmittersEnabled = EnabledFlag(ps, "SubModule");
            d.LightsEnabled = EnabledFlag(ps, "LightsModule");
            d.ExternalForcesEnabled = EnabledFlag(ps, "ExternalForcesModule");
            d.SizeBySpeedEnabled = EnabledFlag(ps, "SizeBySpeedModule");
            d.RotationBySpeedEnabled = EnabledFlag(ps, "RotationBySpeedModule");
            d.ColorBySpeedEnabled = EnabledFlag(ps, "ColorBySpeedModule");
            d.CustomDataEnabled = EnabledFlag(ps, "CustomDataModule");
            d.TriggerEnabled = EnabledFlag(ps, "TriggerModule");

            if (ps.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty p in ps.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Object && p.Name.EndsWith("Module", StringComparison.Ordinal) && UJson.B(p.Value, "enabled"))
                        d.EnabledModules.Add(p.Name);
                }
            }

            if (renderer.ValueKind == JsonValueKind.Object)
            {
                d.RenderMode = (ParticleRenderMode)UJson.I(renderer, "m_RenderMode");
                d.LengthScale = UJson.F(renderer, "m_LengthScale", 2f);
                d.VelocityScale = UJson.F(renderer, "m_VelocityScale");
                d.CameraVelocityScale = UJson.F(renderer, "m_CameraVelocityScale");
                d.MinParticleSize = UJson.F(renderer, "m_MinParticleSize");
                d.MaxParticleSize = UJson.F(renderer, "m_MaxParticleSize", 0.5f);
                d.SortMode = UJson.I(renderer, "m_SortMode");
                d.SortingFudge = UJson.F(renderer, "m_SortingFudge");
                d.RenderAlignment = UJson.I(renderer, "m_RenderAlignment");
                d.Pivot = UJson.V3(renderer, "m_Pivot", Vector3.Zero);
                d.RendererPriority = UJson.I(renderer, "m_RendererPriority");
                d.ApplyActiveColorSpace = UJson.B(renderer, "m_ApplyActiveColorSpace", true);
                if (UJson.Has(renderer, "m_Materials", out JsonElement mats) && mats.ValueKind == JsonValueKind.Array && mats.GetArrayLength() > 0)
                {
                    JsonElement m0 = mats[0];
                    if (m0.ValueKind == JsonValueKind.Object && m0.TryGetProperty("path", out JsonElement path) && path.ValueKind == JsonValueKind.String)
                    {
                        d.MaterialPath = path.GetString();
                        d.MaterialName = Path.GetFileNameWithoutExtension(d.MaterialPath);
                    }
                    d.TrailMaterialName = d.MaterialName;
                    if (mats.GetArrayLength() > 1 && mats[1].ValueKind == JsonValueKind.Object && mats[1].TryGetProperty("path", out JsonElement tpath) && tpath.ValueKind == JsonValueKind.String)
                        d.TrailMaterialName = Path.GetFileNameWithoutExtension(tpath.GetString());
                }
                if (UJson.Has(renderer, "m_Mesh", out JsonElement mesh) && mesh.ValueKind == JsonValueKind.Object && mesh.TryGetProperty("path", out JsonElement mp) && mp.ValueKind == JsonValueKind.String)
                    d.MeshName = mp.GetString();
            }
            return d;
        }

        private static bool EnabledFlag(JsonElement ps, string module)
        {
            return UJson.Has(ps, module, out JsonElement m) && UJson.B(m, "enabled");
        }

        private static float MultiMode(JsonElement parent, string name, float fallback)
        {
            if (!UJson.Has(parent, name, out JsonElement e))
                return fallback;
            return UJson.F(e, "value", fallback);
        }

        /// <summary>One system of a baked particle file (see Tools/ParticleBake).</summary>
        public sealed class Placement
        {
            public ParticleSystemData Data;
            public string Path;                 // hierarchy path inside the prefab / scene
            public Matrix World = Matrix.Identity; // world matrix, MonoGame space
            public bool Active = true;          // the GameObject is active in the prefab (inactive ones, e.g. VFX_HealthGlow, are never drawn in Unity)
        }

        /// <summary>
        /// Reads a baked particle file ({"systems":[{"name","path","world":[16 floats, MonoGame space],"ps":{...raw props...},"renderer":{...}}]})
        /// or the raw extractor prefab/scene JSON (components of type ParticleSystem are located recursively; world matrices are then
        /// built from transform.worldPosition/worldRotation/worldScale and converted to MonoGame space).
        /// </summary>
        public static List<Placement> Load(Stream stream)
        {
            using (JsonDocument doc = JsonDocument.Parse(stream))
            {
                var result = new List<Placement>();
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("systems", out JsonElement systems))
                {
                    foreach (JsonElement s in systems.EnumerateArray())
                    {
                        string name = s.TryGetProperty("name", out JsonElement n) ? n.GetString() : "ParticleSystem";
                        JsonElement ps = s.GetProperty("ps");
                        JsonElement rend = s.TryGetProperty("renderer", out JsonElement r) ? r : default;
                        var pl = new Placement { Data = FromRaw(name, ps, rend), Path = s.TryGetProperty("path", out JsonElement p) ? p.GetString() : name, World = Matrix.Identity };
                        if (s.TryGetProperty("active", out JsonElement act) && (act.ValueKind == JsonValueKind.False || act.ValueKind == JsonValueKind.True))
                        {
                            pl.Active = act.GetBoolean();
                        }
                        if (s.TryGetProperty("localScale", out JsonElement ls) && ls.ValueKind == JsonValueKind.Array && ls.GetArrayLength() == 3)
                        {
                            pl.Data.LocalScale = new Vector3((float)ls[0].GetDouble(), (float)ls[1].GetDouble(), (float)ls[2].GetDouble());
                        }
                        if (s.TryGetProperty("world", out JsonElement w) && w.ValueKind == JsonValueKind.Array && w.GetArrayLength() == 16)
                        {
                            float[] f = new float[16];
                            int i = 0;
                            foreach (JsonElement x in w.EnumerateArray()) f[i++] = (float)x.GetDouble();
                            pl.World = new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
                        }
                        result.Add(pl);
                    }
                }
                else if (root.TryGetProperty("roots", out JsonElement roots))
                {
                    foreach (JsonElement go in roots.EnumerateArray())
                        CollectRaw(go, "", result);
                }
                return result;
            }
        }

        private static void CollectRaw(JsonElement go, string parentPath, List<Placement> output)
        {
            string name = go.TryGetProperty("name", out JsonElement n) ? n.GetString() : "";
            string path = parentPath + "/" + name;
            if (go.TryGetProperty("components", out JsonElement comps))
            {
                JsonElement ps = default, rend = default;
                bool hasPs = false;
                foreach (JsonElement c in comps.EnumerateArray())
                {
                    string type = c.TryGetProperty("type", out JsonElement t) ? t.GetString() : "";
                    if (type == "ParticleSystem") { ps = c.GetProperty("props"); hasPs = true; }
                    else if (type == "ParticleSystemRenderer") rend = c.GetProperty("props");
                }
                if (hasPs)
                {
                    Matrix world = Matrix.Identity;
                    if (go.TryGetProperty("transform", out JsonElement tr))
                    {
                        Vector3 pos = ReadV3(tr, "worldPosition", Vector3.Zero);
                        Vector3 scl = ReadV3(tr, "worldScale", Vector3.One);
                        Quaternion q = Quaternion.Identity;
                        if (tr.TryGetProperty("worldRotation", out JsonElement wr) && wr.GetArrayLength() == 4)
                            q = new Quaternion((float)wr[0].GetDouble(), (float)wr[1].GetDouble(), (float)wr[2].GetDouble(), (float)wr[3].GetDouble());
                        // Unity -> MonoGame: position (x,y,-z), quaternion (-x,-y,z,w)
                        world = Matrix.CreateScale(scl) * Matrix.CreateFromQuaternion(new Quaternion(-q.X, -q.Y, q.Z, q.W)) * Matrix.CreateTranslation(pos.X, pos.Y, -pos.Z);
                    }
                    output.Add(new Placement { Data = FromRaw(name, ps, rend), Path = path, World = world });
                }
            }
            if (go.TryGetProperty("children", out JsonElement kids))
                foreach (JsonElement k in kids.EnumerateArray())
                    CollectRaw(k, path, output);
        }

        private static Vector3 ReadV3(JsonElement parent, string name, Vector3 fallback)
        {
            if (!parent.TryGetProperty(name, out JsonElement e) || e.ValueKind != JsonValueKind.Array || e.GetArrayLength() < 3)
                return fallback;
            return new Vector3((float)e[0].GetDouble(), (float)e[1].GetDouble(), (float)e[2].GetDouble());
        }
    }
}
