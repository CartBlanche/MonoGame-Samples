using System.Text.Json.Nodes;

namespace UnityExtractor.Unity;

/// <summary>Builds the GameObject hierarchy from an expanded object list (parenting via Transform.m_Father).</summary>
public sealed class TreeBuilder
{
    public sealed class Node
    {
        public Obj Go = null!;
        public Obj? Transform;
        public List<Obj> Components = new List<Obj>();
        public List<Node> Children = new List<Node>();
        public Node? Parent;
        public int Sequence;
        public Quat WorldRotation = Quat.Identity;
    }

    private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();

    public List<Node> Roots { get; } = new List<Node>();

    public List<string> Warnings { get; } = new List<string>();

    public int NodeCount
    {
        get { return _nodes.Count; }
    }

    public TreeBuilder(List<Obj> objects, List<string> rootOrder)
    {
        Dictionary<string, Obj> byUid = new Dictionary<string, Obj>();
        foreach (Obj o in objects)
        {
            byUid[o.Uid] = o;
        }

        int seq = 0;
        foreach (Obj o in objects)
        {
            if (o.ClassId == UnityClassIds.GameObject && !o.Removed)
            {
                _nodes[o.Uid] = new Node { Go = o, Sequence = seq++ };
            }
        }

        foreach (Obj o in objects)
        {
            if (o.Removed || o.ClassId == UnityClassIds.GameObject)
            {
                continue;
            }

            string? goUid = o.GameObjectUid;
            if (goUid == null || !_nodes.TryGetValue(goUid, out Node? node))
            {
                if (goUid != null && !byUid.ContainsKey(goUid))
                {
                    Warnings.Add("component " + o.Uid + " (" + o.Type + ") references missing GameObject " + goUid);
                }

                continue;
            }

            if (o.IsTransform)
            {
                node.Transform = o;
            }
            else
            {
                node.Components.Add(o);
            }
        }

        foreach (Node n in _nodes.Values)
        {
            n.Components = OrderComponents(n.Go, n.Components);
        }

        // Parenting.
        foreach (Node n in _nodes.Values.OrderBy(x => x.Sequence))
        {
            string? fatherUid = n.Transform?.FatherUid;
            Node? parent = null;
            if (fatherUid != null)
            {
                if (byUid.TryGetValue(fatherUid, out Obj? fatherTr) && fatherTr.GameObjectUid != null)
                {
                    _nodes.TryGetValue(fatherTr.GameObjectUid, out parent);
                }

                if (parent == null)
                {
                    Warnings.Add("GameObject '" + n.Go.Props["m_Name"] + "' (" + n.Go.Uid + ") has unresolved/removed parent " + fatherUid + "; treated as root");
                }
            }

            n.Parent = parent;
            if (parent != null)
            {
                parent.Children.Add(n);
            }
            else
            {
                Roots.Add(n);
            }
        }

        foreach (Node n in _nodes.Values)
        {
            if (n.Children.Count > 1 && n.Transform != null)
            {
                SortByList(n.Children, ChildOrder(n.Transform));
            }
        }

        if (rootOrder.Count > 0)
        {
            Dictionary<string, int> pos = new Dictionary<string, int>();
            for (int i = 0; i < rootOrder.Count; i++)
            {
                pos[rootOrder[i]] = i;
            }

            Roots.Sort((a, b) =>
            {
                int pa = a.Transform != null && pos.TryGetValue(a.Transform.Uid, out int x) ? x : int.MaxValue;
                int pb = b.Transform != null && pos.TryGetValue(b.Transform.Uid, out int y) ? y : int.MaxValue;
                if (pa != pb)
                {
                    return pa.CompareTo(pb);
                }

                return a.Sequence.CompareTo(b.Sequence);
            });
        }
    }

    private static Dictionary<string, int> ChildOrder(Obj transform)
    {
        Dictionary<string, int> order = new Dictionary<string, int>();
        if (transform.Props["m_Children"] is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                string? id = RefRewriter.RefId(arr[i]);
                if (id != null)
                {
                    order[id] = i;
                }
            }
        }

        return order;
    }

    private static void SortByList(List<Node> nodes, Dictionary<string, int> order)
    {
        nodes.Sort((a, b) =>
        {
            int pa = a.Transform != null && order.TryGetValue(a.Transform.Uid, out int x) ? x : int.MaxValue;
            int pb = b.Transform != null && order.TryGetValue(b.Transform.Uid, out int y) ? y : int.MaxValue;
            if (pa != pb)
            {
                return pa.CompareTo(pb);
            }

            return a.Sequence.CompareTo(b.Sequence);
        });
    }

    /// <summary>Orders components by the GameObject's m_Component list; unlisted (added) components follow.</summary>
    public static List<Obj> OrderComponents(Obj go, List<Obj> comps)
    {
        Dictionary<string, int> order = new Dictionary<string, int>();
        if (go.Props["m_Component"] is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                string? id = RefRewriter.RefId(arr[i]?["component"]);
                if (id != null)
                {
                    order[id] = i;
                }
            }
        }

        return comps
            .Select((c, i) => new { c, i })
            .OrderBy(x => order.TryGetValue(x.c.Uid, out int p) ? p : int.MaxValue)
            .ThenBy(x => x.i)
            .Select(x => x.c)
            .ToList();
    }

    private static double D(JsonNode? n, double fallback = 0)
    {
        if (n is JsonValue v)
        {
            if (v.TryGetValue(out double d))
            {
                return d;
            }

            if (v.TryGetValue(out long l))
            {
                return l;
            }
        }

        return fallback;
    }

    private static double R(double d)
    {
        double r = Math.Round(d, 6);
        return r == 0 ? 0 : r;
    }

    private static JsonArray Vec(double x, double y, double z)
    {
        return new JsonArray(R(x), R(y), R(z));
    }

    public JsonObject Emit(Node node, Affine parentWorld, bool parentActive, List<Obj> live)
    {
        Obj go = node.Go;
        live.Add(go);

        JsonObject j = new JsonObject();
        j["id"] = go.Uid;
        j["name"] = go.Props["m_Name"]?.DeepClone();
        bool active = RefRewriter.ToLong(go.Props["m_IsActive"]) != 0;
        j["active"] = active;
        j["activeInHierarchy"] = active && parentActive;
        j["layer"] = go.Props["m_Layer"]?.DeepClone();
        j["tag"] = go.Props["m_TagString"]?.DeepClone();
        j["staticFlags"] = go.Props["m_StaticEditorFlags"]?.DeepClone();
        if (go.OriginPrefab != null)
        {
            j["prefab"] = go.OriginPrefab;
        }

        Affine world = parentWorld;
        Quat worldRot = node.Parent != null ? node.Parent.WorldRotation : Quat.Identity;

        if (node.Transform != null)
        {
            live.Add(node.Transform);
            JsonObject t = node.Transform.Props;
            Vec3 lp = new Vec3(D(t["m_LocalPosition"]?["x"]), D(t["m_LocalPosition"]?["y"]), D(t["m_LocalPosition"]?["z"]));
            Quat lr = new Quat(D(t["m_LocalRotation"]?["x"]), D(t["m_LocalRotation"]?["y"]), D(t["m_LocalRotation"]?["z"]), D(t["m_LocalRotation"]?["w"], 1));
            Vec3 ls = new Vec3(D(t["m_LocalScale"]?["x"], 1), D(t["m_LocalScale"]?["y"], 1), D(t["m_LocalScale"]?["z"], 1));

            world = parentWorld.Compose(Affine.FromTrs(lp, lr, ls));
            Quat wr = (worldRot * lr).Normalized();
            if (node.Parent == null)
            {
                wr = lr;
            }

            node.WorldRotation = wr;
            Vec3 wp = world.T;
            Vec3 wsc = world.LossyScale();

            JsonObject tj = new JsonObject();
            tj["localPosition"] = new JsonArray(lp.X, lp.Y, lp.Z);
            tj["localRotation"] = new JsonArray(lr.X, lr.Y, lr.Z, lr.W);
            tj["localScale"] = new JsonArray(ls.X, ls.Y, ls.Z);
            tj["worldPosition"] = Vec(wp.X, wp.Y, wp.Z);
            tj["worldRotation"] = new JsonArray(R(wr.X), R(wr.Y), R(wr.Z), R(wr.W));
            tj["worldScale"] = Vec(wsc.X, wsc.Y, wsc.Z);
            j["transform"] = tj;

            if (node.Transform.ClassId == UnityClassIds.RectTransform)
            {
                JsonObject rt = new JsonObject();
                foreach (string key in new[] { "m_AnchorMin", "m_AnchorMax", "m_AnchoredPosition", "m_SizeDelta", "m_Pivot" })
                {
                    rt[key] = t[key]?.DeepClone();
                }

                j["rectTransform"] = rt;
            }
        }

        JsonArray comps = new JsonArray();
        foreach (Obj c in node.Components)
        {
            live.Add(c);
            comps.Add(SceneExporter.EmitComponent(c, c.Uid));
        }

        j["components"] = comps;

        JsonArray kids = new JsonArray();
        foreach (Node child in node.Children)
        {
            kids.Add(Emit(child, world, active && parentActive, live));
        }

        j["children"] = kids;
        return j;
    }
}
