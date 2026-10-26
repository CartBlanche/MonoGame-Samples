using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Shooter.Pipeline;
using UnityExtractor.Unity;

namespace Shooter.CharacterExport;

/// <summary>
/// Exports, for HoverBot and Turret:
///   Core/Content/Characters/&lt;Name&gt;.animator.json  (Unity Animator controller as flat JSON)
///   Core/Content/Characters/&lt;Name&gt;.skeleton.json  (real bind-pose transforms of every bone, Unity space)
/// Usage: dotnet run --project Tools/Shooter.CharacterExport -- [unityProjectRoot] [outDir]
/// </summary>
internal static class Program
{
    private const string SpaceDescription =
        "MonoGame world space (right-handed, Y up, -Z forward) = Unity world converted with F = diag(1,1,-1) = RotationY(180) of the FBX data, "
        + "scaled by 0.01 (cm -> m). position (-x,y,-z)*0.01, quaternion (-qx,qy,-qz,qw). "
        + "To get Unity-space values apply F again: position (x,y,-z), quaternion (-x,-y,z,w) (see unityGlobalPosition).";

    private sealed class ImportContext : ContentImporterContext
    {
        private readonly ContentBuildLogger _logger = new Logger();
        public override string IntermediateDirectory { get { return Path.GetTempPath(); } }
        public override string OutputDirectory { get { return Path.GetTempPath(); } }
        public override ContentBuildLogger Logger { get { return _logger; } }
        public override void AddDependency(string filename) { }
    }

    private sealed class Logger : ContentBuildLogger
    {
        public override void LogMessage(string message, params object[] messageArgs) { }
        public override void LogImportantMessage(string message, params object[] messageArgs) { }
        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
            Console.Error.WriteLine("  warning: " + string.Format(message, messageArgs));
        }
    }

    private static int Main(string[] args)
    {
        string repo = FindRepoRoot();
        string unity = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(repo, "..", "FirstPersonShooter"));
        string outDir = args.Length > 1 ? args[1] : Path.Combine(repo, "Core", "Content", "Characters");
        string anim = Path.Combine(unity, "Assets", "FPS", "Animation");
        Directory.CreateDirectory(outDir);

        GuidIndex guids = new GuidIndex(anim);
        SpaceConversion space = new SpaceConversion(CoordinateSpace.MonoGame, 0.01f);
        JsonSerializerOptions json = new JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };

        foreach (string rig in new[] { "HoverBot", "Turret" })
        {
            string controllerPath = Path.Combine(anim, "Controllers", rig + "_AnimationController.controller");
            UnityFile file = UnityFile.Load(controllerPath, controllerPath);
            AnimatorExporter exporter = new AnimatorExporter(file, guids, "Characters/");
            JsonObject animator = exporter.Export(rig);
            File.WriteAllText(Path.Combine(outDir, rig + ".animator.json"), animator.ToJsonString(json) + "\n");

            ContentImporterContext ctx = new ImportContext();
            NodeContent scene = new SkinnedFbxImporter().Import(Path.Combine(anim, "Rigs", rig + ".fbx"), ctx);
            JsonObject skeleton = SkeletonExporter.Export(rig, scene, space, SpaceDescription);
            File.WriteAllText(Path.Combine(outDir, rig + ".skeleton.json"), skeleton.ToJsonString(json) + "\n");

            Console.WriteLine(rig + ": " + animator["layers"]!.AsArray().Count + " layers, "
                + exporter.Clips.Count + " clips, " + skeleton["bones"]!.AsArray().Count + " bones");
        }

        return 0;
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Shooter.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new InvalidOperationException("Run from inside the Shooter repo.");
    }
}
