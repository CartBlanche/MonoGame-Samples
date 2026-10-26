using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UnityExtractor.Unity;
using UnityExtractor.Yaml;

namespace Shooter.CharacterExport;

/// <summary>One clip as known to Unity (from the .fbx.meta importer settings).</summary>
public sealed class ClipInfo
{
    public string UnityName = "";
    public string ContentAsset = "";
    public bool LoopTime;
    public bool LoopPose;
    public double FirstFrame = -1;
    public double LastFrame = -1;
    public bool HasClipSettings;
    public string FbxFile = "";
}

/// <summary>Resolves Unity guids to assets under Assets/FPS/Animation.</summary>
public sealed class GuidIndex
{
    private readonly Dictionary<string, string> _pathByGuid = new Dictionary<string, string>();

    public GuidIndex(string assetsRoot)
    {
        Regex rx = new Regex(@"^guid:\s*([0-9a-f]{32})", RegexOptions.Multiline);
        foreach (string meta in Directory.EnumerateFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
        {
            Match m = rx.Match(File.ReadAllText(meta));
            if (m.Success)
            {
                _pathByGuid[m.Groups[1].Value] = meta.Substring(0, meta.Length - ".meta".Length);
            }
        }
    }

    public string? PathOf(string guid)
    {
        string? p;
        return _pathByGuid.TryGetValue(guid, out p) ? p : null;
    }
}

public static class ClipMeta
{
    public static ClipInfo Read(string fbxPath, string contentPrefix)
    {
        string file = Path.GetFileNameWithoutExtension(fbxPath);
        ClipInfo info = new ClipInfo();
        info.FbxFile = file;
        info.ContentAsset = contentPrefix + file;
        int at = file.IndexOf('@');
        info.UnityName = at >= 0 ? file.Substring(at + 1) : file;

        string meta = fbxPath + ".meta";
        if (!File.Exists(meta))
        {
            return info;
        }

        YamlNode root = UnityYamlParser.ParseText(File.ReadAllText(meta));
        YamlNode? importer = root.Get("ModelImporter");
        YamlNode? clips = importer?.Get("animations")?.Get("clipAnimations");
        if (clips != null && clips.Seq.Count > 0)
        {
            YamlNode c = clips.Seq[0];
            info.HasClipSettings = true;
            info.UnityName = c.Get("name")?.Text ?? info.UnityName;
            info.FirstFrame = ParseD(c.Get("firstFrame")?.Text);
            info.LastFrame = ParseD(c.Get("lastFrame")?.Text);
            info.LoopTime = c.Get("loopTime")?.Text == "1";
            info.LoopPose = c.Get("loop")?.Text == "1";
        }

        return info;
    }

    private static double ParseD(string? s)
    {
        double d;
        return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d) ? d : -1;
    }
}

/// <summary>Converts a Unity .controller into the flat JSON the runtime (Shooter.Animation.AnimatorController) loads.</summary>
public sealed class AnimatorExporter
{
    private static readonly string[] ConditionModes = { "", "if", "ifNot", "greater", "less", "", "equals", "notEqual" };
    private static readonly string[] ParamTypes = { "", "float", "", "int", "bool", "", "", "", "", "trigger" };
    private static readonly string[] InterruptionSources = { "none", "source", "destination", "sourceThenDestination", "destinationThenSource" };
    private static readonly string[] BlendTypes = { "simple1D", "simpleDirectional2D", "freeformDirectional2D", "freeformCartesian2D", "direct" };

    private readonly UnityFile _file;
    private readonly GuidIndex _guids;
    private readonly string _contentPrefix;
    private readonly Dictionary<string, ClipInfo> _clips = new Dictionary<string, ClipInfo>();

    public AnimatorExporter(UnityFile file, GuidIndex guids, string contentPrefix)
    {
        _file = file;
        _guids = guids;
        _contentPrefix = contentPrefix;
    }

    public IReadOnlyDictionary<string, ClipInfo> Clips
    {
        get { return _clips; }
    }

    public JsonObject Export(string rigName)
    {
        UnityDoc ctrl = _file.Docs.First(d => d.TypeName == "AnimatorController");
        JsonObject result = new JsonObject();
        result["controller"] = ctrl.Props["m_Name"]?.GetValue<string>() ?? "";
        result["rig"] = rigName;
        result["format"] = "shooter.animator/1";

        JsonArray parameters = new JsonArray();
        foreach (JsonNode? p in ctrl.Props["m_AnimatorParameters"]!.AsArray())
        {
            JsonObject po = p!.AsObject();
            int type = (int)po["m_Type"]!.GetValue<long>();
            JsonObject o = new JsonObject();
            o["name"] = po["m_Name"]!.GetValue<string>();
            o["type"] = ParamTypes[type];
            o["defaultFloat"] = po["m_DefaultFloat"]!.D();
            o["defaultInt"] = po["m_DefaultInt"]!.GetValue<long>();
            o["defaultBool"] = po["m_DefaultBool"]!.GetValue<long>() != 0;
            parameters.Add(o);
        }

        result["parameters"] = parameters;

        JsonArray layers = new JsonArray();
        foreach (JsonNode? l in ctrl.Props["m_AnimatorLayers"]!.AsArray())
        {
            layers.Add(ExportLayer(l!.AsObject()));
        }

        result["layers"] = layers;

        JsonArray clips = new JsonArray();
        foreach (ClipInfo c in _clips.Values.OrderBy(c => c.UnityName, StringComparer.Ordinal))
        {
            JsonObject o = new JsonObject();
            o["name"] = c.UnityName;
            o["asset"] = c.ContentAsset;
            o["loopTime"] = c.LoopTime;
            o["loopPose"] = c.LoopPose;
            o["firstFrame"] = c.FirstFrame;
            o["lastFrame"] = c.LastFrame;
            o["hasClipSettings"] = c.HasClipSettings;
            clips.Add(o);
        }

        result["clips"] = clips;
        return result;
    }

    private JsonObject ExportLayer(JsonObject layer)
    {
        JsonObject o = new JsonObject();
        o["name"] = layer["m_Name"]!.GetValue<string>();
        o["blendMode"] = layer["m_BlendingMode"]!.GetValue<long>() == 1 ? "additive" : "override";
        o["defaultWeight"] = layer["m_DefaultWeight"]!.D();
        o["syncedLayerIndex"] = layer["m_SyncedLayerIndex"]!.GetValue<long>();
        o["syncedLayerAffectsTiming"] = layer["m_SyncedLayerAffectsTiming"]!.GetValue<long>() != 0;
        o["ikPass"] = layer["m_IKPass"]!.GetValue<long>() != 0;
        o["avatarMask"] = ExportMask(layer["m_Mask"]);

        UnityDoc root = _file.ById[FileId(layer["m_StateMachine"])];

        // Flatten: sub-state-machines are only visual groups in these controllers (no entry/SM transitions); states get
        // path ids like "Locomotion/Patrolling".
        List<(UnityDoc state, string path)> states = new List<(UnityDoc, string)>();
        Collect(root, "", states);
        Dictionary<long, string> pathById = states.ToDictionary(s => s.state.FileId, s => s.path);

        JsonArray stateArr = new JsonArray();
        foreach ((UnityDoc state, string path) in states)
        {
            stateArr.Add(ExportState(state, path, pathById));
        }

        o["defaultState"] = DefaultStateOf(root, pathById);

        JsonArray any = new JsonArray();
        foreach (JsonNode? t in root.Props["m_AnyStateTransitions"]!.AsArray())
        {
            JsonObject? tr = ExportTransition(_file.ById[FileId(t)], pathById);
            if (tr != null)
            {
                any.Add(tr);
            }
        }

        o["anyStateTransitions"] = any;
        o["states"] = stateArr;
        return o;
    }

    private string? DefaultStateOf(UnityDoc sm, Dictionary<long, string> pathById)
    {
        long id = FileId(sm.Props["m_DefaultState"]);
        string? p;
        return id != 0 && pathById.TryGetValue(id, out p) ? p : null;
    }

    private void Collect(UnityDoc sm, string prefix, List<(UnityDoc, string)> list)
    {
        foreach (JsonNode? cs in sm.Props["m_ChildStates"]!.AsArray())
        {
            UnityDoc st = _file.ById[FileId(cs!["m_State"])];
            list.Add((st, prefix + st.Props["m_Name"]!.GetValue<string>()));
        }

        foreach (JsonNode? cm in sm.Props["m_ChildStateMachines"]!.AsArray())
        {
            UnityDoc child = _file.ById[FileId(cm!["m_StateMachine"])];
            Collect(child, prefix + child.Props["m_Name"]!.GetValue<string>() + "/", list);
        }
    }

    private JsonObject ExportState(UnityDoc state, string path, Dictionary<long, string> pathById)
    {
        JsonObject p = state.Props;
        JsonObject o = new JsonObject();
        o["id"] = path;
        o["name"] = p["m_Name"]!.GetValue<string>();
        o["speed"] = p["m_Speed"]!.D();
        o["speedParameter"] = Param(p, "m_SpeedParameterActive", "m_SpeedParameter");
        o["cycleOffset"] = p["m_CycleOffset"]!.D();
        o["cycleOffsetParameter"] = Param(p, "m_CycleOffsetParameterActive", "m_CycleOffsetParameter");
        o["timeParameter"] = Param(p, "m_TimeParameterActive", "m_TimeParameter");
        o["mirror"] = p["m_Mirror"]!.GetValue<long>() != 0;
        o["writeDefaultValues"] = p["m_WriteDefaultValues"]!.GetValue<long>() != 0;
        o["tag"] = p["m_Tag"]?.ToString() ?? "";
        o["motion"] = ExportMotion(p["m_Motion"]);

        JsonArray trs = new JsonArray();
        foreach (JsonNode? t in p["m_Transitions"]!.AsArray())
        {
            JsonObject? tr = ExportTransition(_file.ById[FileId(t)], pathById);
            if (tr != null)
            {
                trs.Add(tr);
            }
        }

        o["transitions"] = trs;
        return o;
    }

    private static JsonNode? Param(JsonObject p, string activeKey, string nameKey)
    {
        bool active = p[activeKey]?.GetValue<long>() == 1;
        string name = p[nameKey]?.ToString() ?? "";
        return active && name.Length > 0 ? JsonValue.Create(name) : null;
    }

    private JsonObject? ExportTransition(UnityDoc doc, Dictionary<long, string> pathById)
    {
        JsonObject p = doc.Props;
        JsonObject o = new JsonObject();
        bool isExit = p["m_IsExit"]!.GetValue<long>() != 0;
        long dstState = FileId(p["m_DstState"]);
        long dstSm = FileId(p["m_DstStateMachine"]);
        o["isExit"] = isExit;
        if (isExit)
        {
            o["destination"] = null;
        }
        else if (dstState != 0)
        {
            o["destination"] = pathById[dstState];
        }
        else if (dstSm != 0)
        {
            UnityDoc sm = _file.ById[dstSm];
            o["destination"] = DefaultStateOf(sm, pathById);
        }
        else
        {
            Console.Error.WriteLine("  warning: transition " + doc.FileId + " has no destination; skipped");
            return null;
        }

        o["hasExitTime"] = p["m_HasExitTime"]!.GetValue<long>() != 0;
        o["exitTime"] = p["m_ExitTime"]!.D();
        o["hasFixedDuration"] = p["m_HasFixedDuration"]!.GetValue<long>() != 0;
        o["duration"] = p["m_TransitionDuration"]!.D();
        o["offset"] = p["m_TransitionOffset"]!.D();
        o["interruptionSource"] = InterruptionSources[(int)p["m_InterruptionSource"]!.GetValue<long>()];
        o["orderedInterruption"] = p["m_OrderedInterruption"]!.GetValue<long>() != 0;
        o["canTransitionToSelf"] = p["m_CanTransitionToSelf"]!.GetValue<long>() != 0;
        o["mute"] = p["m_Mute"]!.GetValue<long>() != 0;
        o["solo"] = p["m_Solo"]!.GetValue<long>() != 0;

        JsonArray conds = new JsonArray();
        foreach (JsonNode? c in p["m_Conditions"]!.AsArray())
        {
            JsonObject co = new JsonObject();
            co["parameter"] = c!["m_ConditionEvent"]!.ToString();
            co["mode"] = ConditionModes[(int)c["m_ConditionMode"]!.GetValue<long>()];
            co["threshold"] = c["m_EventTreshold"]!.D();
            conds.Add(co);
        }

        o["conditions"] = conds;
        return o;
    }

    private JsonNode? ExportMotion(JsonNode? motionRef)
    {
        if (motionRef == null)
        {
            return null;
        }

        long id = FileId(motionRef);
        if (id == 0)
        {
            return null;
        }

        string? guid = motionRef["guid"]?.ToString();
        JsonObject o = new JsonObject();
        if (!string.IsNullOrEmpty(guid))
        {
            string? fbx = _guids.PathOf(guid);
            if (fbx == null)
            {
                throw new InvalidOperationException("Unresolved clip guid " + guid);
            }

            ClipInfo info = ClipMeta.Read(fbx, _contentPrefix);
            _clips[info.UnityName + "|" + info.ContentAsset] = info;
            o["type"] = "clip";
            o["clip"] = info.UnityName;
            o["asset"] = info.ContentAsset;
            return o;
        }

        UnityDoc bt = _file.ById[id];
        JsonObject p = bt.Props;
        o["type"] = "blendTree";
        o["name"] = p["m_Name"]!.GetValue<string>();
        o["blendType"] = BlendTypes[(int)p["m_BlendType"]!.GetValue<long>()];
        o["blendParameter"] = p["m_BlendParameter"]!.ToString();
        o["blendParameterY"] = p["m_BlendParameterY"]!.ToString();
        o["minThreshold"] = p["m_MinThreshold"]!.D();
        o["maxThreshold"] = p["m_MaxThreshold"]!.D();
        o["useAutomaticThresholds"] = p["m_UseAutomaticThresholds"]!.GetValue<long>() != 0;
        JsonArray children = new JsonArray();
        foreach (JsonNode? c in p["m_Childs"]!.AsArray())
        {
            JsonObject co = new JsonObject();
            co["motion"] = ExportMotion(c!["m_Motion"]);
            co["threshold"] = c["m_Threshold"]!.D();
            co["timeScale"] = c["m_TimeScale"]!.D();
            co["cycleOffset"] = c["m_CycleOffset"]!.D();
            co["directBlendParameter"] = c["m_DirectBlendParameter"]?.ToString() ?? "";
            children.Add(co);
        }

        o["children"] = children;
        return o;
    }

    private JsonNode? ExportMask(JsonNode? maskRef)
    {
        if (maskRef == null || FileId(maskRef) == 0)
        {
            return null;
        }

        string? guid = maskRef["guid"]?.ToString();
        string? path = guid != null ? _guids.PathOf(guid) : null;
        if (path == null)
        {
            return null;
        }

        UnityFile mf = UnityFile.Load(path, path);
        UnityDoc mask = mf.Docs.First(d => d.TypeName == "AvatarMask");
        JsonObject o = new JsonObject();
        o["name"] = mask.Props["m_Name"]!.GetValue<string>();
        JsonArray bones = new JsonArray();
        JsonArray excluded = new JsonArray();
        foreach (JsonNode? e in mask.Props["m_Elements"]!.AsArray())
        {
            string p = e!["m_Path"]?.ToString() ?? "";
            if (p.Length == 0)
            {
                continue;
            }

            string leaf = p.Substring(p.LastIndexOf('/') + 1);
            if (e["m_Weight"]!.D() > 0)
            {
                bones.Add(leaf);
            }
            else
            {
                excluded.Add(leaf);
            }
        }

        o["includedTransforms"] = bones;
        o["excludedTransforms"] = excluded;
        return o;
    }

    private static long FileId(JsonNode? r)
    {
        JsonNode? v = r?["fileID"];
        return v == null ? 0 : v.GetValue<long>();
    }
}

internal static class JsonExt
{
    /// <summary>Unity writes 1 or 0.5 for floats; the extractor's parser yields long or double.</summary>
    public static double D(this JsonNode n)
    {
        return double.Parse(n.ToString(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
