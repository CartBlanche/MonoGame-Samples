using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace GodotExtract;

// ---------- value model ----------
public abstract class Val
{
    public abstract JsonNode? ToJson();
    public abstract void Write(StringBuilder sb);
    public override string ToString() { var sb = new StringBuilder(); Write(sb); return sb.ToString(); }
    /// <summary>All numeric leaves in order (for Vector3/Color/Transform3D/Packed arrays).</summary>
    public IEnumerable<double> Numbers()
    {
        switch (this)
        {
            case VNum n: yield return n.D; break;
            case VCall c: foreach (var a in c.Args) foreach (var d in a.Numbers()) yield return d; break;
            case VArr a: foreach (var e in a.Items) foreach (var d in e.Numbers()) yield return d; break;
        }
    }
}
public sealed class VNum : Val
{
    public double D; public string Raw;
    public VNum(double d, string raw) { D = d; Raw = raw; }
    public override JsonNode? ToJson() => double.IsFinite(D) ? JsonValue.Create(D) : JsonValue.Create(Raw);
    public override void Write(StringBuilder sb) => sb.Append(Raw);
}
public sealed class VStr : Val
{
    public string S; public string Prefix; // "", "&" (StringName), "^" (NodePath)
    public VStr(string s, string prefix = "") { S = s; Prefix = prefix; }
    public override JsonNode? ToJson() => JsonValue.Create(S);
    public override void Write(StringBuilder sb)
    {
        sb.Append(Prefix).Append('"');
        foreach (var ch in S)
            switch (ch) { case '"': sb.Append("\\\""); break; case '\\': sb.Append("\\\\"); break; case '\n': sb.Append("\\n"); break; case '\t': sb.Append("\\t"); break; default: sb.Append(ch); break; }
        sb.Append('"');
    }
}
public sealed class VBool : Val
{
    public bool B; public VBool(bool b) { B = b; }
    public override JsonNode? ToJson() => JsonValue.Create(B);
    public override void Write(StringBuilder sb) => sb.Append(B ? "true" : "false");
}
public sealed class VNull : Val
{
    public override JsonNode? ToJson() => null;
    public override void Write(StringBuilder sb) => sb.Append("null");
}
public sealed class VIdent : Val
{
    public string Name; public VIdent(string n) { Name = n; }
    public override JsonNode? ToJson() => JsonValue.Create(Name);
    public override void Write(StringBuilder sb) => sb.Append(Name);
}
public sealed class VArr : Val
{
    public List<Val> Items = new();
    public override JsonNode? ToJson() { var a = new JsonArray(); foreach (var i in Items) a.Add(i.ToJson()); return a; }
    public override void Write(StringBuilder sb)
    {
        sb.Append('[');
        for (int i = 0; i < Items.Count; i++) { if (i > 0) sb.Append(", "); Items[i].Write(sb); }
        sb.Append(']');
    }
}
public sealed class VDict : Val
{
    public List<(Val K, Val V)> Items = new();
    public override JsonNode? ToJson()
    {
        var o = new JsonObject();
        foreach (var (k, v) in Items) o[k is VStr s ? s.S : k.ToString()] = v.ToJson();
        return o;
    }
    public override void Write(StringBuilder sb)
    {
        sb.Append('{');
        for (int i = 0; i < Items.Count; i++) { if (i > 0) sb.Append(", "); Items[i].K.Write(sb); sb.Append(": "); Items[i].V.Write(sb); }
        sb.Append('}');
    }
}
/// <summary>key:value pair inside call args, e.g. Object(InputEventKey,"keycode":0).</summary>
public sealed class VPair : Val
{
    public Val K, V; public VPair(Val k, Val v) { K = k; V = v; }
    public override JsonNode? ToJson() { var o = new JsonObject(); o[K is VStr s ? s.S : K.ToString()] = V.ToJson(); return o; }
    public override void Write(StringBuilder sb) { K.Write(sb); sb.Append(": "); V.Write(sb); }
}
public sealed class VCall : Val
{
    public string Name; public List<Val> Args = new();
    public VCall(string n) { Name = n; }
    public string? FirstString => Args.Count > 0 && Args[0] is VStr s ? s.S : null;
    public override JsonNode? ToJson()
    {
        var o = new JsonObject();
        if (Name == "ExtResource") { o["ext"] = FirstString; return o; }
        if (Name == "SubResource") { o["sub"] = FirstString; return o; }
        o["type"] = Name;
        var a = new JsonArray(); foreach (var x in Args) a.Add(x.ToJson());
        o["values"] = a;
        return o;
    }
    public override void Write(StringBuilder sb)
    {
        sb.Append(Name).Append('(');
        for (int i = 0; i < Args.Count; i++) { if (i > 0) sb.Append(", "); Args[i].Write(sb); }
        sb.Append(')');
    }
}

// ---------- value parser ----------
public sealed class ValParser
{
    readonly string s; int i;
    public ValParser(string text, int start = 0) { s = text; i = start; }
    public int Pos => i;
    public static Val Parse(string text) { var p = new ValParser(text); var v = p.ParseValue(); return v; }

    public void SkipWs() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
    public bool Eof { get { SkipWs(); return i >= s.Length; } }
    public char Peek { get { SkipWs(); return i < s.Length ? s[i] : '\0'; } }

    public Val ParseValue()
    {
        SkipWs();
        if (i >= s.Length) throw new FormatException("unexpected end of value");
        char c = s[i];
        if (c == '"') return new VStr(ReadString());
        if ((c == '&' || c == '^') && i + 1 < s.Length && s[i + 1] == '"') { i++; return new VStr(ReadString(), c.ToString()); }
        if (c == '[') return ParseArray();
        if (c == '{') return ParseDict();
        if (char.IsDigit(c) || c == '-' || c == '+' || c == '.') return ParseNumber();
        if (char.IsLetter(c) || c == '_') return ParseIdentOrCall();
        throw new FormatException($"unexpected '{c}' at {i} in: {Trunc(s)}");
    }
    static string Trunc(string x) => x.Length > 80 ? x[..80] + "..." : x;

    string ReadString()
    {
        i++; var sb = new StringBuilder();
        while (i < s.Length && s[i] != '"')
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                i++;
                switch (s[i])
                {
                    case 'n': sb.Append('\n'); break; case 't': sb.Append('\t'); break; case 'r': sb.Append('\r'); break;
                    case 'u' when i + 4 < s.Length: sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                    default: sb.Append(s[i]); break;
                }
            }
            else sb.Append(s[i]);
            i++;
        }
        i++; // closing quote
        return sb.ToString();
    }

    Val ParseNumber()
    {
        int st = i;
        if (s[i] == '-' || s[i] == '+') i++;
        if (i < s.Length && char.IsLetter(s[i]))
        {
            int w = i; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
            string id = s[w..i];
            if (id == "inf") return new VNum(s[st] == '-' ? double.NegativeInfinity : double.PositiveInfinity, s[st..i]);
            if (id == "nan") return new VNum(double.NaN, s[st..i]);
            throw new FormatException($"bad number '{s[st..i]}'");
        }
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' ||
               ((s[i] == '-' || s[i] == '+') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
        string raw = s[st..i];
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) throw new FormatException($"bad number '{raw}'");
        return new VNum(d, raw);
    }

    Val ParseArray()
    {
        i++; var a = new VArr();
        while (true)
        {
            SkipWs();
            if (i >= s.Length) throw new FormatException("unterminated array");
            if (s[i] == ']') { i++; return a; }
            a.Items.Add(ParseValue()); SkipWs();
            if (i < s.Length && s[i] == ',') i++;
        }
    }

    Val ParseDict()
    {
        i++; var d = new VDict();
        while (true)
        {
            SkipWs();
            if (i >= s.Length) throw new FormatException("unterminated dict");
            if (s[i] == '}') { i++; return d; }
            var k = ParseValue(); SkipWs();
            if (i >= s.Length || s[i] != ':') throw new FormatException("expected ':' in dict");
            i++;
            var v = ParseValue(); d.Items.Add((k, v)); SkipWs();
            if (i < s.Length && s[i] == ',') i++;
        }
    }

    Val ParseIdentOrCall()
    {
        int st = i; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        string id = s[st..i];
        if (i < s.Length && s[i] == '(')
        {
            i++; var c = new VCall(id);
            while (true)
            {
                SkipWs();
                if (i >= s.Length) throw new FormatException("unterminated call " + id);
                if (s[i] == ')') { i++; return c; }
                var a = ParseValue(); SkipWs();
                if (i < s.Length && s[i] == ':') { i++; a = new VPair(a, ParseValue()); SkipWs(); }
                c.Args.Add(a);
                if (i < s.Length && s[i] == ',') i++;
            }
        }
        return id switch { "true" => new VBool(true), "false" => new VBool(false), "null" => new VNull(), "inf" => new VNum(double.PositiveInfinity, "inf"), "nan" => new VNum(double.NaN, "nan"), _ => new VIdent(id) };
    }
}

// ---------- file model ----------
public sealed class Section
{
    public string Kind = "";
    public Dictionary<string, Val> Attrs = new();
    public Dictionary<string, Val> Props = new();

    public string? Attr(string k) => Attrs.TryGetValue(k, out var v) ? Str(v) : null;
    public static string? Str(Val v) => v switch { VStr s => s.S, VNum n => n.Raw, VIdent i => i.Name, VCall c => c.FirstString ?? c.ToString(), VBool b => b.B ? "true" : "false", _ => v.ToString() };
    /// <summary>For instance=ExtResource("id") / shape = SubResource("id"): returns the id.</summary>
    public string? RefId(string k) => Attrs.TryGetValue(k, out var v) && v is VCall c ? c.FirstString : null;
    public Val? Prop(string k) => Props.TryGetValue(k, out var v) ? v : null;
}

public sealed class GodotFile
{
    public string Path = "";
    public List<Section> Sections = new();
    public Section? Header => Sections.FirstOrDefault(s => s.Kind is "gd_scene" or "gd_resource");
    public IEnumerable<Section> Of(string kind) => Sections.Where(s => s.Kind == kind);
    public Dictionary<string, Section> Ext => Of("ext_resource").Where(s => s.Attr("id") != null).GroupBy(s => s.Attr("id")!).ToDictionary(g => g.Key, g => g.First());
    public Dictionary<string, Section> Sub => Of("sub_resource").Where(s => s.Attr("id") != null).GroupBy(s => s.Attr("id")!).ToDictionary(g => g.Key, g => g.First());
    public List<Section> Nodes => Of("node").ToList();
    public Section? Resource => Of("resource").FirstOrDefault();

    public static GodotFile ParseFile(string path) { var f = Parse(File.ReadAllText(path)); f.Path = path; return f; }

    public static GodotFile Parse(string text)
    {
        var f = new GodotFile();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        Section? cur = null;
        for (int ln = 0; ln < lines.Length; ln++)
        {
            string line = lines[ln];
            if (line.Length > 0 && line[0] == '﻿') line = line[1..];
            string t = line.Trim();
            if (t.Length == 0 || t[0] == ';') continue;
            if (t[0] == '[')
            {
                // section header (a value never starts a line, since multi-line values are consumed below)
                int end = t.LastIndexOf(']');
                cur = ParseHeader(t[1..end]);
                f.Sections.Add(cur);
                continue;
            }
            int eq = line.IndexOf('=');
            if (eq < 0 || cur == null) continue;
            string key = line[..eq].Trim();
            var sb = new StringBuilder(line[(eq + 1)..]);
            while (!Balanced(sb.ToString()) && ln + 1 < lines.Length) sb.Append('\n').Append(lines[++ln]);
            try { cur.Props[key.Trim('"')] = ValParser.Parse(sb.ToString()); }
            catch (FormatException e) { throw new FormatException($"{key}: {e.Message}"); }
        }
        return f;
    }

    static bool Balanced(string v)
    {
        int depth = 0; bool str = false;
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (str) { if (c == '\\') i++; else if (c == '"') str = false; continue; }
            if (c == '"') str = true;
            else if (c is '[' or '(' or '{') depth++;
            else if (c is ']' or ')' or '}') depth--;
        }
        return depth <= 0 && !str;
    }

    static Section ParseHeader(string body)
    {
        var sec = new Section();
        int i = 0; while (i < body.Length && !char.IsWhiteSpace(body[i])) i++;
        sec.Kind = body[..i];
        var p = new ValParser(body, i);
        while (!p.Eof)
        {
            // attr name
            int st = p.Pos; var s = body; int j = st; while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
            int k = j; while (k < s.Length && s[k] != '=' && !char.IsWhiteSpace(s[k])) k++;
            string name = s[j..k];
            p = new ValParser(body, k);
            if (p.Peek != '=') break;
            p = new ValParser(body, k); p.SkipWs(); // now at '='
            p = new ValParser(body, p.Pos + 1);
            var v = p.ParseValue();
            sec.Attrs[name] = v;
        }
        return sec;
    }

    public string ToText()
    {
        var sb = new StringBuilder();
        foreach (var s in Sections)
        {
            sb.Append('[').Append(s.Kind);
            foreach (var (k, v) in s.Attrs) { sb.Append(' ').Append(k).Append('='); v.Write(sb); }
            sb.Append("]\n");
            foreach (var (k, v) in s.Props) { sb.Append(k).Append(" = "); v.Write(sb); sb.Append('\n'); }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public JsonObject ToJson()
    {
        var root = new JsonObject { ["path"] = Path };
        var arr = new JsonArray();
        foreach (var s in Sections)
        {
            var o = new JsonObject { ["kind"] = s.Kind };
            var a = new JsonObject(); foreach (var (k, v) in s.Attrs) a[k] = v.ToJson();
            o["attrs"] = a;
            var p = new JsonObject(); foreach (var (k, v) in s.Props) p[k] = v.ToJson();
            o["props"] = p;
            arr.Add(o);
        }
        root["sections"] = arr;
        return root;
    }
}
