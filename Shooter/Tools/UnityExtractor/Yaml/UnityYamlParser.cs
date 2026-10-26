using System.Text;

namespace UnityExtractor.Yaml;

/// <summary>
/// Hand-rolled parser for the YAML subset that Unity writes (block maps/sequences, flow maps/sequences,
/// plain/quoted scalars, wrapped long scalars). It does not handle anchors, tags or multi-document
/// streams: the caller splits documents (see <see cref="Unity.UnityFile"/>).
/// </summary>
public sealed class UnityYamlParser
{
    private struct Line
    {
        public int Indent;
        public string Text;

        public Line(int indent, string text)
        {
            Indent = indent;
            Text = text;
        }
    }

    private readonly List<Line> _lines = new List<Line>();
    private int _pos;

    private UnityYamlParser(IEnumerable<string> rawLines)
    {
        foreach (string raw in rawLines)
        {
            string s = raw.TrimEnd('\r', '\n');
            int indent = 0;
            while (indent < s.Length && s[indent] == ' ')
            {
                indent++;
            }

            if (indent >= s.Length)
            {
                continue;
            }

            if (s[indent] == '#')
            {
                continue;
            }

            _lines.Add(new Line(indent, s.Substring(indent).TrimEnd()));
        }
    }

    public static YamlNode Parse(IEnumerable<string> rawLines)
    {
        UnityYamlParser p = new UnityYamlParser(rawLines);
        if (p._lines.Count == 0)
        {
            return new YamlNode { Kind = YamlKind.Map };
        }

        YamlNode node = p.ParseNode(p._lines[0].Indent);
        if (p._pos < p._lines.Count)
        {
            Console.Error.WriteLine("warning: YAML parser stopped early at: " + p._lines[p._pos].Text);
        }

        return node;
    }

    public static YamlNode ParseText(string text)
    {
        return Parse(text.Split('\n'));
    }

    private static bool IsSeqItem(string text)
    {
        return text == "-" || text.StartsWith("- ", StringComparison.Ordinal);
    }

    private YamlNode ParseNode(int indent)
    {
        if (IsSeqItem(_lines[_pos].Text))
        {
            return ParseSeq(indent);
        }

        return ParseMap(indent);
    }

    // Returns the index of the ':' that separates key from value, or -1.
    private static int FindKeySeparator(string s)
    {
        int i = 0;
        if (s.Length > 0 && (s[0] == '"' || s[0] == '\''))
        {
            char q = s[0];
            i = 1;
            while (i < s.Length)
            {
                if (q == '"' && s[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (s[i] == q)
                {
                    if (q == '\'' && i + 1 < s.Length && s[i + 1] == '\'')
                    {
                        i += 2;
                        continue;
                    }

                    i++;
                    break;
                }

                i++;
            }
        }

        for (; i < s.Length; i++)
        {
            if (s[i] == ':' && (i + 1 == s.Length || s[i + 1] == ' '))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool LooksLikeMapEntry(string s)
    {
        if (s.Length == 0 || s[0] == '{' || s[0] == '[')
        {
            return false;
        }

        return FindKeySeparator(s) >= 0;
    }

    private YamlNode ParseSeq(int indent)
    {
        YamlNode seq = new YamlNode { Kind = YamlKind.Seq };
        while (_pos < _lines.Count && _lines[_pos].Indent == indent && IsSeqItem(_lines[_pos].Text))
        {
            string text = _lines[_pos].Text;
            string rest = text.Length > 1 ? text.Substring(2) : "";
            string restTrim = rest.TrimStart();
            int contentIndent = indent + 2 + (rest.Length - restTrim.Length);

            if (restTrim.Length == 0)
            {
                _pos++;
                if (_pos < _lines.Count && _lines[_pos].Indent > indent)
                {
                    seq.Seq.Add(ParseNode(_lines[_pos].Indent));
                }
                else
                {
                    seq.Seq.Add(YamlNode.Scalar("", false));
                }
            }
            else if (IsSeqItem(restTrim) || LooksLikeMapEntry(restTrim))
            {
                // Re-interpret the remainder of the line as a nested block starting at contentIndent.
                _lines[_pos] = new Line(contentIndent, restTrim);
                seq.Seq.Add(ParseNode(contentIndent));
            }
            else
            {
                _pos++;
                seq.Seq.Add(ParseInlineValue(restTrim, indent));
            }
        }

        return seq;
    }

    private YamlNode ParseMap(int indent)
    {
        YamlNode map = new YamlNode { Kind = YamlKind.Map };
        while (_pos < _lines.Count && _lines[_pos].Indent == indent && !IsSeqItem(_lines[_pos].Text))
        {
            string text = _lines[_pos].Text;
            int sep = FindKeySeparator(text);
            if (sep < 0)
            {
                // Not a key line; skip it to avoid looping forever on malformed input.
                _pos++;
                continue;
            }

            string key = text.Substring(0, sep).Trim();
            if (key.Length >= 2 && (key[0] == '"' || key[0] == '\''))
            {
                key = key.Substring(1, key.Length - 2);
            }

            string rest = text.Substring(sep + 1).Trim();
            _pos++;

            YamlNode value;
            if (rest.Length == 0)
            {
                if (_pos < _lines.Count && _lines[_pos].Indent > indent)
                {
                    value = ParseNode(_lines[_pos].Indent);
                }
                else if (_pos < _lines.Count && _lines[_pos].Indent == indent && IsSeqItem(_lines[_pos].Text))
                {
                    value = ParseSeq(indent);
                }
                else
                {
                    value = YamlNode.Scalar("", false);
                }
            }
            else
            {
                value = ParseInlineValue(rest, indent);
            }

            map.Map.Add(new KeyValuePair<string, YamlNode>(key, value));
        }

        return map;
    }

    private YamlNode ParseInlineValue(string rest, int parentIndent)
    {
        if (rest[0] == '{' || rest[0] == '[')
        {
            string flow = rest;
            while (!IsBalanced(flow) && _pos < _lines.Count)
            {
                flow += " " + _lines[_pos].Text.Trim();
                _pos++;
            }

            FlowParser fp = new FlowParser(flow);
            return fp.ParseValue();
        }

        if (rest[0] == '"')
        {
            string s = rest;
            while (!IsDoubleQuoteClosed(s) && _pos < _lines.Count)
            {
                string next = _lines[_pos].Text;
                _pos++;
                if (s.EndsWith("\\", StringComparison.Ordinal) && !s.EndsWith("\\\\", StringComparison.Ordinal))
                {
                    s = s.Substring(0, s.Length - 1) + next.TrimStart();
                }
                else
                {
                    s += " " + next.TrimStart();
                }
            }

            return YamlNode.Scalar(FlowParser.ReadDoubleQuoted(s, 0, out _), true);
        }

        if (rest[0] == '\'')
        {
            string s = rest;
            while (!IsSingleQuoteClosed(s) && _pos < _lines.Count)
            {
                s += " " + _lines[_pos].Text.TrimStart();
                _pos++;
            }

            return YamlNode.Scalar(FlowParser.ReadSingleQuoted(s, 0, out _), true);
        }

        StringBuilder sb = new StringBuilder(rest);
        while (_pos < _lines.Count && _lines[_pos].Indent > parentIndent)
        {
            sb.Append(' ').Append(_lines[_pos].Text.Trim());
            _pos++;
        }

        return YamlNode.Scalar(sb.ToString(), false);
    }

    private static bool IsBalanced(string s)
    {
        int depth = 0;
        bool inD = false;
        bool inS = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (inD)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    inD = false;
                }

                continue;
            }

            if (inS)
            {
                if (c == '\'')
                {
                    inS = false;
                }

                continue;
            }

            if (c == '"')
            {
                inD = true;
            }
            else if (c == '\'' && (i == 0 || s[i - 1] == ' ' || s[i - 1] == '{' || s[i - 1] == '[' || s[i - 1] == ':' || s[i - 1] == ','))
            {
                inS = true;
            }
            else if (c == '{' || c == '[')
            {
                depth++;
            }
            else if (c == '}' || c == ']')
            {
                depth--;
            }
        }

        return depth <= 0 && !inD && !inS;
    }

    private static bool IsDoubleQuoteClosed(string s)
    {
        for (int i = 1; i < s.Length; i++)
        {
            if (s[i] == '\\')
            {
                i++;
            }
            else if (s[i] == '"')
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSingleQuoteClosed(string s)
    {
        for (int i = 1; i < s.Length; i++)
        {
            if (s[i] == '\'')
            {
                if (i + 1 < s.Length && s[i + 1] == '\'')
                {
                    i++;
                    continue;
                }

                return true;
            }
        }

        return false;
    }
}

/// <summary>Parser for single-line flow collections such as {fileID: 1, guid: abc, type: 3}.</summary>
internal sealed class FlowParser
{
    private readonly string _s;
    private int _i;

    public FlowParser(string s)
    {
        _s = s;
    }

    private void SkipWs()
    {
        while (_i < _s.Length && _s[_i] == ' ')
        {
            _i++;
        }
    }

    public YamlNode ParseValue()
    {
        SkipWs();
        if (_i >= _s.Length)
        {
            return YamlNode.Scalar("", false);
        }

        char c = _s[_i];
        if (c == '{')
        {
            _i++;
            YamlNode map = new YamlNode { Kind = YamlKind.Map };
            while (true)
            {
                SkipWs();
                if (_i >= _s.Length)
                {
                    break;
                }

                if (_s[_i] == '}')
                {
                    _i++;
                    break;
                }

                if (_s[_i] == ',')
                {
                    _i++;
                    continue;
                }

                string key = ParseKey();
                SkipWs();
                YamlNode value;
                if (_i < _s.Length && (_s[_i] == ',' || _s[_i] == '}'))
                {
                    value = YamlNode.Scalar("", false);
                }
                else
                {
                    value = ParseValue();
                }

                map.Map.Add(new KeyValuePair<string, YamlNode>(key, value));
            }

            return map;
        }

        if (c == '[')
        {
            _i++;
            YamlNode seq = new YamlNode { Kind = YamlKind.Seq };
            while (true)
            {
                SkipWs();
                if (_i >= _s.Length)
                {
                    break;
                }

                if (_s[_i] == ']')
                {
                    _i++;
                    break;
                }

                if (_s[_i] == ',')
                {
                    _i++;
                    continue;
                }

                seq.Seq.Add(ParseValue());
            }

            return seq;
        }

        if (c == '"')
        {
            string v = ReadDoubleQuoted(_s, _i, out int end);
            _i = end;
            return YamlNode.Scalar(v, true);
        }

        if (c == '\'')
        {
            string v = ReadSingleQuoted(_s, _i, out int end);
            _i = end;
            return YamlNode.Scalar(v, true);
        }

        int start = _i;
        while (_i < _s.Length && _s[_i] != ',' && _s[_i] != '}' && _s[_i] != ']')
        {
            _i++;
        }

        return YamlNode.Scalar(_s.Substring(start, _i - start).Trim(), false);
    }

    private string ParseKey()
    {
        SkipWs();
        string key;
        if (_i < _s.Length && _s[_i] == '"')
        {
            key = ReadDoubleQuoted(_s, _i, out int end);
            _i = end;
        }
        else if (_i < _s.Length && _s[_i] == '\'')
        {
            key = ReadSingleQuoted(_s, _i, out int end);
            _i = end;
        }
        else
        {
            int start = _i;
            while (_i < _s.Length && !(_s[_i] == ':' && (_i + 1 == _s.Length || _s[_i + 1] == ' ')) && _s[_i] != '}' && _s[_i] != ',')
            {
                _i++;
            }

            key = _s.Substring(start, _i - start).Trim();
        }

        SkipWs();
        if (_i < _s.Length && _s[_i] == ':')
        {
            _i++;
        }

        return key;
    }

    public static string ReadDoubleQuoted(string s, int start, out int end)
    {
        StringBuilder sb = new StringBuilder();
        int i = start + 1;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                char n = s[i + 1];
                i += 2;
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '0': sb.Append('\0'); break;
                    case 'a': sb.Append('\a'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'v': sb.Append('\v'); break;
                    case 'e': sb.Append('\u001b'); break;
                    case 'x':
                        i = AppendHex(sb, s, i, 2);
                        break;
                    case 'u':
                        i = AppendHex(sb, s, i, 4);
                        break;
                    case 'U':
                        i = AppendHex(sb, s, i, 8);
                        break;
                    default: sb.Append(n); break;
                }

                continue;
            }

            if (c == '"')
            {
                end = i + 1;
                return sb.ToString();
            }

            sb.Append(c);
            i++;
        }

        end = s.Length;
        return sb.ToString();
    }

    private static int AppendHex(StringBuilder sb, string s, int i, int digits)
    {
        if (i + digits <= s.Length
            && int.TryParse(s.AsSpan(i, digits), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int cp))
        {
            sb.Append(char.ConvertFromUtf32(cp));
            return i + digits;
        }

        return i;
    }

    public static string ReadSingleQuoted(string s, int start, out int end)
    {
        StringBuilder sb = new StringBuilder();
        int i = start + 1;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\'')
            {
                if (i + 1 < s.Length && s[i + 1] == '\'')
                {
                    sb.Append('\'');
                    i += 2;
                    continue;
                }

                end = i + 1;
                return sb.ToString();
            }

            sb.Append(c);
            i++;
        }

        end = s.Length;
        return sb.ToString();
    }
}
