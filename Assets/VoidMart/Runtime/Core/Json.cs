using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VoidMart.Core
{
    /// <summary>
    /// Small dependency-free JSON tree.  Unity's JsonUtility cannot express the dictionary in the
    /// spec's save schema (<c>store.machineStates</c>) nor round-trip doubles reliably, so the
    /// save pipeline uses this instead.  Culture-invariant on purpose: a save written on a device
    /// with comma decimal separators must load anywhere.
    /// </summary>
    public sealed class JsonNode
    {
        public enum NodeType { Null, Bool, Number, String, Array, Object }

        public NodeType Type { get; private set; }

        bool m_Bool;
        double m_Number;
        string m_String;
        List<JsonNode> m_Array;
        Dictionary<string, JsonNode> m_Object;

        public static JsonNode Null() => new JsonNode { Type = NodeType.Null };
        public static JsonNode NewObject() => new JsonNode { Type = NodeType.Object, m_Object = new Dictionary<string, JsonNode>() };
        public static JsonNode NewArray() => new JsonNode { Type = NodeType.Array, m_Array = new List<JsonNode>() };
        public static JsonNode From(bool v) => new JsonNode { Type = NodeType.Bool, m_Bool = v };
        public static JsonNode From(double v) => new JsonNode { Type = NodeType.Number, m_Number = v };
        public static JsonNode From(long v) => new JsonNode { Type = NodeType.Number, m_Number = v };
        public static JsonNode From(int v) => new JsonNode { Type = NodeType.Number, m_Number = v };
        public static JsonNode From(string v) => v == null ? Null() : new JsonNode { Type = NodeType.String, m_String = v };

        public int Count => Type == NodeType.Array ? m_Array.Count : Type == NodeType.Object ? m_Object.Count : 0;
        public IEnumerable<string> Keys => m_Object != null ? (IEnumerable<string>)m_Object.Keys : Array.Empty<string>();
        public IReadOnlyList<JsonNode> Items => m_Array ?? (IReadOnlyList<JsonNode>)Array.Empty<JsonNode>();

        public JsonNode this[string key]
        {
            get
            {
                if (Type != NodeType.Object || !m_Object.TryGetValue(key, out var node)) return null;
                return node;
            }
            set
            {
                if (Type != NodeType.Object)
                {
                    Type = NodeType.Object;
                    m_Object = new Dictionary<string, JsonNode>();
                }
                m_Object[key] = value ?? Null();
            }
        }

        public JsonNode this[int index] => Type == NodeType.Array && index >= 0 && index < m_Array.Count ? m_Array[index] : null;

        public void Add(JsonNode node)
        {
            if (Type != NodeType.Array)
            {
                Type = NodeType.Array;
                m_Array = new List<JsonNode>();
            }
            m_Array.Add(node ?? Null());
        }

        public bool Has(string key) => Type == NodeType.Object && m_Object.ContainsKey(key);

        public double AsDouble(double fallback = 0d) => Type == NodeType.Number ? m_Number
            : Type == NodeType.String && double.TryParse(m_String, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d
            : fallback;

        public float AsFloat(float fallback = 0f) => (float)AsDouble(fallback);
        public int AsInt(int fallback = 0) => Type == NodeType.Number ? (int)Math.Round(m_Number) : fallback;
        public long AsLong(long fallback = 0L) => Type == NodeType.Number ? (long)Math.Round(m_Number) : fallback;
        public bool AsBool(bool fallback = false) => Type == NodeType.Bool ? m_Bool : Type == NodeType.Number ? m_Number != 0d : fallback;
        public string AsString(string fallback = "") => Type == NodeType.String ? m_String
            : Type == NodeType.Number ? m_Number.ToString(CultureInfo.InvariantCulture)
            : Type == NodeType.Bool ? (m_Bool ? "true" : "false")
            : fallback;

        // ------------------------------------------------------------------ writing

        public string ToJson(bool pretty = false)
        {
            var sb = new StringBuilder(512);
            Write(sb, pretty, 0);
            return sb.ToString();
        }

        void Write(StringBuilder sb, bool pretty, int indent)
        {
            switch (Type)
            {
                case NodeType.Null: sb.Append("null"); break;
                case NodeType.Bool: sb.Append(m_Bool ? "true" : "false"); break;
                case NodeType.Number: WriteNumber(sb, m_Number); break;
                case NodeType.String: WriteString(sb, m_String); break;
                case NodeType.Array:
                {
                    if (m_Array.Count == 0) { sb.Append("[]"); break; }
                    sb.Append('[');
                    for (int i = 0; i < m_Array.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        NewLine(sb, pretty, indent + 1);
                        m_Array[i].Write(sb, pretty, indent + 1);
                    }
                    NewLine(sb, pretty, indent);
                    sb.Append(']');
                    break;
                }
                case NodeType.Object:
                {
                    if (m_Object.Count == 0) { sb.Append("{}"); break; }
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in m_Object)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        NewLine(sb, pretty, indent + 1);
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        if (pretty) sb.Append(' ');
                        kv.Value.Write(sb, pretty, indent + 1);
                    }
                    NewLine(sb, pretty, indent);
                    sb.Append('}');
                    break;
                }
            }
        }

        static void NewLine(StringBuilder sb, bool pretty, int indent)
        {
            if (!pretty) return;
            sb.Append('\n');
            for (int i = 0; i < indent; i++) sb.Append("  ");
        }

        static void WriteNumber(StringBuilder sb, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) { sb.Append('0'); return; }
            if (Math.Abs(value - Math.Floor(value)) < double.Epsilon && Math.Abs(value) < 1e15)
                sb.Append(((long)value).ToString(CultureInfo.InvariantCulture));
            else
                sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ------------------------------------------------------------------ parsing

        public static JsonNode Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int index = 0;
            try
            {
                var node = ParseValue(json, ref index);
                return node;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void SkipWhite(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static JsonNode ParseValue(string s, ref int i)
        {
            SkipWhite(s, ref i);
            if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return From(ParseString(s, ref i));
                case 't':
                    Expect(s, ref i, "true");
                    return From(true);
                case 'f':
                    Expect(s, ref i, "false");
                    return From(false);
                case 'n':
                    Expect(s, ref i, "null");
                    return Null();
                default: return ParseNumber(s, ref i);
            }
        }

        static void Expect(string s, ref int i, string literal)
        {
            if (i + literal.Length > s.Length || string.CompareOrdinal(s, i, literal, 0, literal.Length) != 0)
                throw new FormatException("Bad literal at " + i);
            i += literal.Length;
        }

        static JsonNode ParseObject(string s, ref int i)
        {
            var node = NewObject();
            i++; // {
            SkipWhite(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return node; }
            while (i < s.Length)
            {
                SkipWhite(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhite(s, ref i);
                if (s[i] != ':') throw new FormatException("Expected ':' at " + i);
                i++;
                node[key] = ParseValue(s, ref i);
                SkipWhite(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return node; }
                throw new FormatException("Expected ',' or '}' at " + i);
            }
            return node;
        }

        static JsonNode ParseArray(string s, ref int i)
        {
            var node = NewArray();
            i++; // [
            SkipWhite(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return node; }
            while (i < s.Length)
            {
                node.Add(ParseValue(s, ref i));
                SkipWhite(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return node; }
                throw new FormatException("Expected ',' or ']' at " + i);
            }
            return node;
        }

        static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("Expected string at " + i);
            i++;
            var sb = new StringBuilder(32);
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            throw new FormatException("Unterminated string");
        }

        static JsonNode ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            string slice = s.Substring(start, i - start);
            return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? From(d)
                : throw new FormatException("Bad number '" + slice + "'");
        }
    }
}
