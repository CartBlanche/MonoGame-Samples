using System.Text.Json.Nodes;

namespace UnityExtractor.Unity;

/// <summary>A GameObject or component in the expanded (instantiated) object graph.</summary>
public sealed class Obj
{
    public string Uid = "";
    public int ClassId;
    public string TypeName = "";
    public JsonObject Props = new JsonObject();
    public bool Removed;
    public string? OriginPrefab;
    public bool OriginRoot;

    public string Type
    {
        get { return UnityClassIds.NameOf(ClassId, TypeName); }
    }

    public bool IsTransform
    {
        get { return UnityClassIds.IsTransform(ClassId); }
    }

    public string? GameObjectUid
    {
        get { return RefRewriter.RefId(Props["m_GameObject"]); }
    }

    public string? FatherUid
    {
        get { return RefRewriter.RefId(Props["m_Father"]); }
    }
}

public sealed class InstanceRecord
{
    public UnityDoc Doc = new UnityDoc();
    public string SourceGuid = "";
    public string? SourcePath;
    public ExpandedSpace? Sub;
}

public sealed class ExpandedSpace
{
    public List<Obj> Objects = new List<Obj>();

    // Keys in the "space" of this file as seen by an outer file's modifications (own fileIDs, plus nested
    // objects keyed as innerKey ^ nestedInstanceFileID).
    public Dictionary<long, Obj> ByKey = new Dictionary<long, Obj>();

    public List<InstanceRecord> Instances = new List<InstanceRecord>();

    // Resolves a fileID of the expanded file itself (own objects or stripped placeholders) to its object.
    public Func<long, Obj?>? LocalLookup;

    // For model (FBX etc.) sources whose internal hierarchy is not stored as YAML: creates/finds the object
    // an override targets on demand.
    public Func<long, Obj?>? CreateOnDemand;
}

/// <summary>
/// Expands a scene/prefab: instantiates nested prefab instances recursively, applies their modifications
/// and removed components/GameObjects, and rewrites all local references to globally unique ids.
/// </summary>
public sealed class PrefabExpander
{
    private const int MaxDepth = 24;

    private readonly AssetIndex _index;
    private readonly Dictionary<string, UnityFile> _fileCache = new Dictionary<string, UnityFile>();

    public List<string> Warnings { get; } = new List<string>();

    public PrefabExpander(AssetIndex index)
    {
        _index = index;
    }

    public UnityFile LoadFile(AssetEntry entry)
    {
        if (!_fileCache.TryGetValue(entry.RelPath, out UnityFile? file))
        {
            file = UnityFile.Load(entry.RelPath, entry.AbsPath);
            _fileCache[entry.RelPath] = file;
        }

        return file;
    }

    public void Warn(string message)
    {
        if (Warnings.Count < 500)
        {
            Warnings.Add(message);
        }
    }

    public ExpandedSpace Expand(UnityFile file, string prefix = "", int depth = 0)
    {
        ExpandedSpace space = new ExpandedSpace();
        if (depth > MaxDepth)
        {
            Warn("max prefab nesting depth exceeded at " + file.Path);
            return space;
        }

        bool isPrefab = file.Path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        Dictionary<long, Obj> local = new Dictionary<long, Obj>();
        Dictionary<long, UnityDoc> stripped = new Dictionary<long, UnityDoc>();
        Dictionary<long, InstanceRecord> instances = new Dictionary<long, InstanceRecord>();
        List<Obj> own = new List<Obj>();

        foreach (UnityDoc doc in file.Docs)
        {
            if (doc.ClassId == UnityClassIds.PrefabInstance
                || doc.ClassId == UnityClassIds.SceneRoots
                || UnityClassIds.IsSettings(doc.ClassId))
            {
                continue;
            }

            if (doc.Stripped)
            {
                stripped[doc.FileId] = doc;
                continue;
            }

            Obj o = new Obj
            {
                Uid = prefix + doc.FileId,
                ClassId = doc.ClassId,
                TypeName = doc.TypeName,
                Props = (JsonObject)doc.Props.DeepClone(),
                OriginPrefab = isPrefab ? file.Path : null
            };

            if (UnityClassIds.IsTransform(doc.ClassId) && RefRewriter.ToLong(doc.Props["m_Father"]?["fileID"]) == 0)
            {
                o.OriginRoot = true;
            }

            local[doc.FileId] = o;
            own.Add(o);
        }

        space.Objects.AddRange(own);

        foreach (UnityDoc doc in file.Docs)
        {
            if (doc.ClassId != UnityClassIds.PrefabInstance || doc.Stripped)
            {
                continue;
            }

            string guid = doc.Props["m_SourcePrefab"]?["guid"]?.ToString() ?? "";
            InstanceRecord rec = new InstanceRecord { Doc = doc, SourceGuid = guid };
            AssetEntry? entry = _index.FindByGuid(guid);
            if (entry == null)
            {
                Warn(file.Path + ": prefab instance " + doc.FileId + " references unknown prefab guid " + guid);
                continue;
            }

            rec.SourcePath = entry.RelPath;
            rec.Sub = IsYamlPrefab(entry)
                ? Expand(LoadFile(entry), prefix + doc.FileId + "/", depth + 1)
                : ExpandModel(entry, prefix + doc.FileId + "/");
            space.Instances.Add(rec);
            instances[doc.FileId] = rec;
            space.Objects.AddRange(rec.Sub.Objects);
        }

        Obj? LocalObj(long id)
        {
            if (local.TryGetValue(id, out Obj? o))
            {
                return o;
            }

            if (stripped.TryGetValue(id, out UnityDoc? sd))
            {
                long instId = RefRewriter.ToLong(sd.Props["m_PrefabInstance"]?["fileID"]);
                long source = RefRewriter.ToLong(sd.Props["m_CorrespondingSourceObject"]?["fileID"]);
                if (instances.TryGetValue(instId, out InstanceRecord? rec) && rec.Sub != null)
                {
                    if (rec.Sub.ByKey.TryGetValue(source, out Obj? target))
                    {
                        return target;
                    }

                    return rec.Sub.CreateOnDemand?.Invoke(source);
                }
            }

            return null;
        }

        space.LocalLookup = LocalObj;

        JsonNode? ResolveLocal(long id)
        {
            Obj? o = LocalObj(id);
            if (o == null)
            {
                Warn(file.Path + ": unresolved local reference fileID " + id);
                JsonObject miss = new JsonObject();
                miss["ref"] = null;
                miss["unresolvedFileID"] = id;
                return miss;
            }

            JsonObject r = new JsonObject();
            r["ref"] = o.Uid;
            r["class"] = o.Type;
            return r;
        }

        RefRewriter rewriter = new RefRewriter(_index, ResolveLocal);

        foreach (InstanceRecord rec in space.Instances)
        {
            ApplyInstance(file, rec, rewriter);
        }

        foreach (Obj o in own)
        {
            o.Props = (JsonObject)rewriter.Rewrite(o.Props)!;
        }

        // Model stubs may have gained placeholder nodes while overrides/references were resolved.
        space.Objects.Clear();
        space.Objects.AddRange(own);
        foreach (InstanceRecord rec in space.Instances)
        {
            space.Objects.AddRange(rec.Sub!.Objects);
        }

        foreach (KeyValuePair<long, Obj> kv in local)
        {
            space.ByKey[kv.Key] = kv.Value;
        }

        foreach (InstanceRecord rec in space.Instances)
        {
            foreach (KeyValuePair<long, Obj> kv in rec.Sub!.ByKey)
            {
                space.ByKey[kv.Key ^ rec.Doc.FileId] = kv.Value;
            }
        }

        return space;
    }

    public static bool IsYamlPrefab(AssetEntry entry)
    {
        string ext = entry.Extension;
        return ext == ".prefab" || ext == ".unity";
    }

    /// <summary>
    /// Stub expansion for an instance of a model asset (FBX, OBJ, ...): the model's internal node hierarchy is
    /// binary, so only the root GameObject/Transform are materialised. Overrides that target anything else are
    /// kept on a "ModelInstance" pseudo-component as node overrides (fileID, class, node name, overridden props).
    /// </summary>
    private ExpandedSpace ExpandModel(AssetEntry entry, string prefix)
    {
        ExpandedSpace space = new ExpandedSpace();
        Dictionary<long, string> names = _index.SubAssetNames(entry);
        RefRewriter rewriter = new RefRewriter(_index, id => null);

        Obj go = new Obj
        {
            Uid = prefix + "100000",
            ClassId = UnityClassIds.GameObject,
            OriginPrefab = entry.RelPath,
            Props = new JsonObject
            {
                ["m_Name"] = System.IO.Path.GetFileNameWithoutExtension(entry.RelPath),
                ["m_IsActive"] = 1,
                ["m_Layer"] = 0,
                ["m_TagString"] = "Untagged"
            }
        };

        Obj tr = new Obj
        {
            Uid = prefix + "400000",
            ClassId = UnityClassIds.Transform,
            OriginPrefab = entry.RelPath,
            OriginRoot = true,
            Props = new JsonObject
            {
                ["m_GameObject"] = new JsonObject { ["ref"] = go.Uid, ["class"] = "GameObject" },
                ["m_LocalRotation"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["z"] = 0, ["w"] = 1 },
                ["m_LocalPosition"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
                ["m_LocalScale"] = new JsonObject { ["x"] = 1, ["y"] = 1, ["z"] = 1 },
                ["m_Father"] = null,
                ["m_Children"] = new JsonArray()
            }
        };

        JsonObject nodes = new JsonObject();
        Obj modelInstance = new Obj
        {
            Uid = prefix + "modelInstance",
            ClassId = -1,
            TypeName = "ModelInstance",
            OriginPrefab = entry.RelPath,
            Props = new JsonObject
            {
                ["m_GameObject"] = new JsonObject { ["ref"] = go.Uid, ["class"] = "GameObject" },
                ["model"] = rewriter.MakeExternal(100100000, entry.Guid),
                ["note"] = "Instance of a model asset; its internal node hierarchy is not available from YAML. Overrides on inner nodes are listed in nodeOverrides.",
                ["nodeOverrides"] = new JsonArray()
            }
        };

        space.Objects.Add(go);
        space.Objects.Add(tr);
        space.Objects.Add(modelInstance);
        space.ByKey[100000] = go;
        space.ByKey[400000] = tr;

        Dictionary<long, Obj> scratch = new Dictionary<long, Obj>();
        space.CreateOnDemand = fileId =>
        {
            long classId = fileId > 0 && fileId < 100000000 ? fileId / 100000 : 0;

            // Inner GameObjects/Transforms of the model (bones, sub-meshes): materialise a placeholder node
            // parented flat under the model root, so that instances parented to a bone attach somewhere.
            if (classId == 1 || classId == 4)
            {
                long trId = classId == 4 ? fileId : fileId + 300000;
                long goId = trId - 300000;
                if (scratch.TryGetValue(trId, out Obj? existingTr))
                {
                    return classId == 4 ? existingTr : space.ByKey[goId];
                }

                string nodeName = names.TryGetValue(goId, out string? gn) ? gn : "Node" + goId;
                Obj nodeGo = new Obj
                {
                    Uid = prefix + goId,
                    ClassId = UnityClassIds.GameObject,
                    OriginPrefab = entry.RelPath,
                    Props = new JsonObject
                    {
                        ["m_Name"] = nodeName,
                        ["m_IsActive"] = 1,
                        ["m_Layer"] = 0,
                        ["m_TagString"] = "Untagged"
                    }
                };

                Obj nodeTr = new Obj
                {
                    Uid = prefix + trId,
                    ClassId = UnityClassIds.Transform,
                    OriginPrefab = entry.RelPath,
                    Props = new JsonObject
                    {
                        ["m_GameObject"] = new JsonObject { ["ref"] = nodeGo.Uid, ["class"] = "GameObject" },
                        ["m_LocalRotation"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["z"] = 0, ["w"] = 1 },
                        ["m_LocalPosition"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
                        ["m_LocalScale"] = new JsonObject { ["x"] = 1, ["y"] = 1, ["z"] = 1 },
                        ["m_Father"] = new JsonObject { ["ref"] = tr.Uid, ["class"] = "Transform" },
                        ["m_Children"] = new JsonArray()
                    }
                };

                Obj marker = new Obj
                {
                    Uid = prefix + "node" + goId,
                    ClassId = -2,
                    TypeName = "ModelNode",
                    OriginPrefab = entry.RelPath,
                    Props = new JsonObject
                    {
                        ["m_GameObject"] = new JsonObject { ["ref"] = nodeGo.Uid, ["class"] = "GameObject" },
                        ["model"] = modelInstance.Props["model"]?.DeepClone(),
                        ["modelNodeFileID"] = goId,
                        ["note"] = "Placeholder for a node inside the model file; its real local transform is unknown (identity assumed)."
                    }
                };

                space.Objects.Add(nodeGo);
                space.Objects.Add(nodeTr);
                space.Objects.Add(marker);
                space.ByKey[goId] = nodeGo;
                space.ByKey[trId] = nodeTr;
                scratch[trId] = nodeTr;
                return classId == 4 ? nodeTr : nodeGo;
            }

            if (scratch.TryGetValue(fileId, out Obj? existing))
            {
                return existing;
            }

            JsonObject node = new JsonObject();
            node["fileID"] = fileId;
            node["class"] = AssetCollector.SubAssetClassOr(fileId, (int)classId);
            node["name"] = names.TryGetValue(fileId, out string? n) ? n : null;
            JsonObject overrides = new JsonObject();
            node["overrides"] = overrides;
            ((JsonArray)modelInstance.Props["nodeOverrides"]!).Add(node);

            Obj o = new Obj { Uid = prefix + fileId, ClassId = -2, TypeName = "ModelNodeOverrides", Props = overrides, OriginPrefab = entry.RelPath };
            scratch[fileId] = o;
            return o;
        };

        return space;
    }

    private void ApplyInstance(UnityFile file, InstanceRecord rec, RefRewriter rewriter)
    {
        ExpandedSpace sub = rec.Sub!;
        JsonObject? modification = rec.Doc.Props["m_Modification"] as JsonObject;
        if (modification == null)
        {
            return;
        }

        // Parent transform for the instance root(s).
        JsonNode? parentRef = null;
        if (modification["m_TransformParent"] is JsonObject tp)
        {
            parentRef = rewriter.MakeRef((JsonObject)tp.DeepClone());
        }

        foreach (Obj o in sub.Objects)
        {
            if (o.IsTransform && !o.Removed && o.Props["m_Father"] == null)
            {
                o.Props["m_Father"] = parentRef?.DeepClone();
            }
        }

        if (modification["m_Modifications"] is JsonArray mods)
        {
            foreach (JsonNode? modNode in mods)
            {
                if (modNode is not JsonObject mod)
                {
                    continue;
                }

                long targetId = RefRewriter.ToLong(mod["target"]?["fileID"]);
                string path = mod["propertyPath"]?.ToString() ?? "";
                string value = mod["value"]?.ToString() ?? "";
                if (!sub.ByKey.TryGetValue(targetId, out Obj? target) && sub.CreateOnDemand != null)
                {
                    target = sub.CreateOnDemand(targetId);
                }

                if (target == null)
                {
                    Warn(file.Path + ": override target " + targetId + " (" + path + ") not found in " + rec.SourcePath);
                    continue;
                }

                JsonNode? objRef = null;
                bool hasObjRef = false;
                if (mod["objectReference"] is JsonObject orf && RefRewriter.ToLong(orf["fileID"]) != 0)
                {
                    objRef = rewriter.MakeRef((JsonObject)orf.DeepClone());
                    hasObjRef = objRef != null;
                }

                if (!PropertyPathSetter.Apply(target.Props, path, value, objRef, hasObjRef))
                {
                    Warn(file.Path + ": could not apply override " + path + " on " + target.Type + " from " + rec.SourcePath);
                }
            }
        }

        if (modification["m_RemovedComponents"] is JsonArray removedComps)
        {
            foreach (JsonNode? n in removedComps)
            {
                long id = RefRewriter.ToLong(n?["fileID"]);
                if (sub.ByKey.TryGetValue(id, out Obj? o))
                {
                    o.Removed = true;
                }
            }
        }

        if (modification["m_RemovedGameObjects"] is JsonArray removedGos)
        {
            foreach (JsonNode? n in removedGos)
            {
                long id = RefRewriter.ToLong(n?["fileID"]);
                if (sub.ByKey.TryGetValue(id, out Obj? go))
                {
                    RemoveGameObject(sub, go);
                }
            }
        }
    }

    private static void RemoveGameObject(ExpandedSpace sub, Obj go)
    {
        go.Removed = true;
        foreach (Obj o in sub.Objects)
        {
            if (o.GameObjectUid != go.Uid)
            {
                continue;
            }

            o.Removed = true;
            if (o.IsTransform)
            {
                foreach (Obj child in sub.Objects.Where(c => c.IsTransform && c.FatherUid == o.Uid).ToList())
                {
                    Obj? childGo = sub.Objects.FirstOrDefault(g => g.ClassId == UnityClassIds.GameObject && g.Uid == child.GameObjectUid);
                    if (childGo != null && !childGo.Removed)
                    {
                        RemoveGameObject(sub, childGo);
                    }
                }
            }
        }
    }
}
