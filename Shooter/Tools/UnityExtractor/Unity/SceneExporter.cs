using System.Text.Json.Nodes;

namespace UnityExtractor.Unity;

/// <summary>Result of exporting one scene/prefab.</summary>
public sealed class ExportResult
{
    public JsonObject Json = new JsonObject();

    // Live (non-removed) components and GameObjects of the export, used for the component inventory.
    public List<Obj> LiveObjects = new List<Obj>();
}

/// <summary>Builds the JSON representation of a scene or prefab, either raw (1:1 with the YAML) or flattened.</summary>
public sealed class SceneExporter
{
    private static readonly HashSet<string> HiddenProps = new HashSet<string>(StringComparer.Ordinal)
    {
        "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
        "m_GameObject", "m_EditorHideFlags", "serializedVersion"
    };

    private readonly AssetIndex _index;
    private readonly PrefabExpander _expander;

    public SceneExporter(AssetIndex index, PrefabExpander expander)
    {
        _index = index;
        _expander = expander;
    }

    // ------------------------------------------------------------------ flattened

    public ExportResult ExportFlattened(UnityFile file, string kind)
    {
        int warningsBefore = _expander.Warnings.Count;
        ExpandedSpace space = _expander.Expand(file);
        ExportResult result = new ExportResult();

        RefRewriter rewriter = new RefRewriter(_index, id =>
        {
            Obj? o = space.LocalLookup?.Invoke(id);
            if (o == null)
            {
                JsonObject miss = new JsonObject();
                miss["ref"] = null;
                miss["unresolvedFileID"] = id;
                return miss;
            }

            JsonObject r = new JsonObject();
            r["ref"] = o.Uid;
            r["class"] = o.Type;
            return r;
        });

        // Roots order from the scene's SceneRoots document, when present.
        List<string> rootOrder = new List<string>();
        UnityDoc? sceneRoots = file.Docs.FirstOrDefault(d => d.ClassId == UnityClassIds.SceneRoots);
        if (sceneRoots?.Props["m_Roots"] is JsonArray rootsArr)
        {
            foreach (JsonNode? n in rootsArr)
            {
                Obj? o = space.LocalLookup?.Invoke(RefRewriter.ToLong(n?["fileID"]));
                if (o != null)
                {
                    rootOrder.Add(o.Uid);
                }
            }
        }

        TreeBuilder tree = new TreeBuilder(space.Objects, rootOrder);
        JsonArray roots = new JsonArray();
        foreach (TreeBuilder.Node root in tree.Roots)
        {
            roots.Add(tree.Emit(root, Affine.Identity, true, result.LiveObjects));
        }

        JsonObject json = result.Json;
        WriteHeader(json, file, kind, "flattened");
        json["settings"] = BuildSettings(file, rewriter);
        json["prefabInstances"] = BuildPrefabInstances(file, rewriter);
        json["roots"] = roots;

        JsonObject stats = new JsonObject();
        stats["rootGameObjects"] = tree.Roots.Count;
        stats["gameObjects"] = tree.NodeCount;
        stats["components"] = result.LiveObjects.Count(o => o.ClassId != UnityClassIds.GameObject);
        stats["directPrefabInstances"] = file.Docs.Count(d => d.ClassId == UnityClassIds.PrefabInstance);
        json["stats"] = stats;

        List<string> warnings = _expander.Warnings.Skip(warningsBefore).Concat(tree.Warnings).ToList();
        if (warnings.Count > 0)
        {
            JsonArray w = new JsonArray();
            foreach (string s in warnings.Distinct().Take(200))
            {
                w.Add(s);
            }

            json["warnings"] = w;
        }

        return result;
    }

    // ------------------------------------------------------------------ raw

    public ExportResult ExportRaw(UnityFile file, string kind)
    {
        ExportResult result = new ExportResult();
        RefRewriter rewriter = new RefRewriter(_index, id =>
        {
            if (id == 0)
            {
                return null;
            }

            JsonObject r = new JsonObject();
            r["fileID"] = id;
            return r;
        });

        List<Obj> objs = new List<Obj>();
        JsonArray stripped = new JsonArray();
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
                JsonObject s = new JsonObject();
                s["fileID"] = doc.FileId;
                s["type"] = doc.Type;
                s["prefabInstance"] = RefRewriter.ToLong(doc.Props["m_PrefabInstance"]?["fileID"]);
                JsonObject? src = doc.Props["m_CorrespondingSourceObject"] as JsonObject;
                if (src != null)
                {
                    s["correspondingSourceObject"] = rewriter.MakeRef((JsonObject)src.DeepClone());
                }

                stripped.Add(s);
                continue;
            }

            Obj o = new Obj
            {
                Uid = doc.FileId.ToString(),
                ClassId = doc.ClassId,
                TypeName = doc.TypeName,
                Props = (JsonObject)rewriter.Rewrite(doc.Props.DeepClone())!
            };
            objs.Add(o);
        }

        Dictionary<string, List<Obj>> compsByGo = new Dictionary<string, List<Obj>>();
        foreach (Obj o in objs)
        {
            string? goId = o.GameObjectUid;
            if (o.ClassId == UnityClassIds.GameObject || goId == null)
            {
                continue;
            }

            if (!compsByGo.TryGetValue(goId, out List<Obj>? list))
            {
                list = new List<Obj>();
                compsByGo[goId] = list;
            }

            list.Add(o);
        }

        JsonArray gameObjects = new JsonArray();
        foreach (Obj go in objs.Where(o => o.ClassId == UnityClassIds.GameObject))
        {
            JsonObject g = new JsonObject();
            g["fileID"] = long.Parse(go.Uid);
            g["name"] = go.Props["m_Name"]?.DeepClone();
            g["active"] = RefRewriter.ToLong(go.Props["m_IsActive"]) != 0;
            g["layer"] = go.Props["m_Layer"]?.DeepClone();
            g["tag"] = go.Props["m_TagString"]?.DeepClone();
            JsonArray comps = new JsonArray();
            if (compsByGo.TryGetValue(go.Uid, out List<Obj>? list))
            {
                foreach (Obj c in TreeBuilder.OrderComponents(go, list))
                {
                    comps.Add(EmitComponent(c, long.Parse(c.Uid)));
                }
            }

            g["components"] = comps;
            gameObjects.Add(g);
            result.LiveObjects.Add(go);
            if (compsByGo.TryGetValue(go.Uid, out List<Obj>? live))
            {
                result.LiveObjects.AddRange(live);
            }
        }

        JsonObject json = result.Json;
        WriteHeader(json, file, kind, "raw");
        json["settings"] = BuildSettings(file, rewriter);
        json["prefabInstances"] = BuildPrefabInstances(file, rewriter);
        json["strippedObjects"] = stripped;
        json["gameObjects"] = gameObjects;
        return result;
    }

    // ------------------------------------------------------------------ shared pieces

    private void WriteHeader(JsonObject json, UnityFile file, string kind, string mode)
    {
        json["source"] = file.Path;
        json["kind"] = kind;
        json["mode"] = mode;
        AssetEntry? entry = _index.FindByPath(file.Path);
        if (entry != null)
        {
            json["guid"] = entry.Guid;
        }
    }

    private JsonObject BuildSettings(UnityFile file, RefRewriter rewriter)
    {
        JsonObject settings = new JsonObject();
        foreach (UnityDoc doc in file.Docs)
        {
            if (UnityClassIds.IsSettings(doc.ClassId) && !doc.Stripped)
            {
                settings[doc.Type] = rewriter.Rewrite(doc.Props.DeepClone());
            }
        }

        return settings;
    }

    private JsonArray BuildPrefabInstances(UnityFile file, RefRewriter rewriter)
    {
        JsonArray arr = new JsonArray();
        foreach (UnityDoc doc in file.Docs)
        {
            if (doc.ClassId != UnityClassIds.PrefabInstance || doc.Stripped)
            {
                continue;
            }

            string guid = doc.Props["m_SourcePrefab"]?["guid"]?.ToString() ?? "";
            AssetEntry? src = _index.FindByGuid(guid);
            UnityFile? srcFile = src != null && PrefabExpander.IsYamlPrefab(src) ? _expander.LoadFile(src) : null;

            JsonObject inst = new JsonObject();
            inst["id"] = doc.FileId;
            inst["sourcePrefab"] = src?.RelPath;
            inst["sourcePrefabGuid"] = guid;

            JsonObject? modification = doc.Props["m_Modification"] as JsonObject;
            JsonObject? parent = modification?["m_TransformParent"] as JsonObject;
            inst["parentTransform"] = parent != null ? rewriter.MakeRef((JsonObject)parent.DeepClone()) : null;

            JsonArray overrides = new JsonArray();
            if (modification?["m_Modifications"] is JsonArray mods)
            {
                foreach (JsonNode? m in mods)
                {
                    if (m is not JsonObject mod)
                    {
                        continue;
                    }

                    JsonObject o = new JsonObject();
                    long targetId = RefRewriter.ToLong(mod["target"]?["fileID"]);
                    JsonObject target = new JsonObject();
                    target["fileID"] = targetId;
                    if (srcFile != null && srcFile.ById.TryGetValue(targetId, out UnityDoc? td))
                    {
                        target["type"] = td.Type;
                        target["gameObject"] = DescribeGameObject(srcFile, td);
                    }

                    o["target"] = target;
                    o["propertyPath"] = mod["propertyPath"]?.DeepClone();
                    o["value"] = mod["value"]?.DeepClone();
                    if (mod["objectReference"] is JsonObject orf && RefRewriter.ToLong(orf["fileID"]) != 0)
                    {
                        o["objectReference"] = rewriter.MakeRef((JsonObject)orf.DeepClone());
                    }

                    overrides.Add(o);
                }
            }

            inst["overrides"] = overrides;
            inst["removedComponents"] = FileIdList(modification?["m_RemovedComponents"]);
            inst["removedGameObjects"] = FileIdList(modification?["m_RemovedGameObjects"]);
            inst["addedGameObjects"] = AddedList(modification?["m_AddedGameObjects"]);
            inst["addedComponents"] = AddedList(modification?["m_AddedComponents"]);
            arr.Add(inst);
        }

        return arr;
    }

    private static string? DescribeGameObject(UnityFile srcFile, UnityDoc doc)
    {
        if (doc.ClassId == UnityClassIds.GameObject)
        {
            return doc.Props["m_Name"]?.ToString();
        }

        long goId = RefRewriter.ToLong(doc.Props["m_GameObject"]?["fileID"]);
        if (srcFile.ById.TryGetValue(goId, out UnityDoc? go))
        {
            return go.Props["m_Name"]?.ToString();
        }

        return null;
    }

    private static JsonArray FileIdList(JsonNode? node)
    {
        JsonArray arr = new JsonArray();
        if (node is JsonArray a)
        {
            foreach (JsonNode? n in a)
            {
                arr.Add(RefRewriter.ToLong(n?["fileID"]));
            }
        }

        return arr;
    }

    private static JsonArray AddedList(JsonNode? node)
    {
        JsonArray arr = new JsonArray();
        if (node is JsonArray a)
        {
            foreach (JsonNode? n in a)
            {
                JsonObject o = new JsonObject();
                o["targetCorrespondingSourceObject"] = RefRewriter.ToLong(n?["targetCorrespondingSourceObject"]?["fileID"]);
                o["insertIndex"] = n?["insertIndex"]?.DeepClone();
                o["addedObject"] = RefRewriter.ToLong(n?["addedObject"]?["fileID"]);
                arr.Add(o);
            }
        }

        return arr;
    }

    // ------------------------------------------------------------------ component emission

    public static string ScriptName(Obj o)
    {
        JsonNode? script = o.Props["m_Script"];
        string? path = script?["path"]?.ToString();
        if (!string.IsNullOrEmpty(path))
        {
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        string? editorClass = o.Props["m_EditorClassIdentifier"]?.ToString();
        if (!string.IsNullOrEmpty(editorClass))
        {
            int sep = editorClass.IndexOf("::", StringComparison.Ordinal);
            string type = sep >= 0 ? editorClass.Substring(sep + 2) : editorClass;
            return type;
        }

        return "<missing script>";
    }

    public static JsonObject EmitComponent(Obj c, object id)
    {
        JsonObject comp = new JsonObject();
        comp["id"] = id is long l ? JsonValue.Create(l) : JsonValue.Create(id.ToString());
        comp["type"] = c.Type;
        comp["classId"] = c.ClassId;

        JsonObject props = new JsonObject();
        bool isMono = c.ClassId == UnityClassIds.MonoBehaviour;
        if (isMono)
        {
            comp["script"] = ScriptName(c);
            JsonNode? script = c.Props["m_Script"];
            comp["scriptPath"] = script?["path"]?.DeepClone();
            comp["scriptGuid"] = script?["guid"]?.DeepClone();
        }

        foreach (KeyValuePair<string, JsonNode?> kv in c.Props)
        {
            if (HiddenProps.Contains(kv.Key) || (isMono && (kv.Key == "m_Script" || kv.Key == "m_EditorClassIdentifier")))
            {
                continue;
            }

            props[kv.Key] = kv.Value?.DeepClone();
        }

        comp[isMono ? "fields" : "props"] = props;
        return comp;
    }
}
