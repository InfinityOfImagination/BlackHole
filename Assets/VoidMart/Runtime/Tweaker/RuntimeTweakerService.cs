using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VoidMart.Tweaker
{
    /// <summary>
    /// Reflection-driven runtime parameter engine (spec section 6).  Any object that registers
    /// exposes every <see cref="TweakableAttribute"/> field/property to the debug overlay and to
    /// the editor Master Control window, grouped by category, with no recompilation required.
    /// </summary>
    public static class RuntimeTweakerService
    {
        public sealed class Entry
        {
            public object Target;
            public FieldInfo Field;
            public PropertyInfo Property;
            public TweakableAttribute Attribute;
            public string DisplayName;
            public object DefaultValue;

            public Type ValueType => Field != null ? Field.FieldType : Property.PropertyType;

            public object GetValue() => Field != null ? Field.GetValue(Target) : Property.GetValue(Target, null);

            public void SetValue(object value)
            {
                if (Field != null) Field.SetValue(Target, value);
                else if (Property.CanWrite) Property.SetValue(Target, value, null);
            }

            public float GetFloat()
            {
                object value = GetValue();
                switch (value)
                {
                    case float f: return f;
                    case int i: return i;
                    case double d: return (float)d;
                    case bool b: return b ? 1f : 0f;
                    default: return 0f;
                }
            }

            public void SetFloat(float value)
            {
                var type = ValueType;
                if (type == typeof(float)) SetValue(value);
                else if (type == typeof(int)) SetValue(Mathf.RoundToInt(value));
                else if (type == typeof(double)) SetValue((double)value);
                else if (type == typeof(bool)) SetValue(value > 0.5f);
            }

            public bool IsNumeric
            {
                get
                {
                    var type = ValueType;
                    return type == typeof(float) || type == typeof(int) || type == typeof(double);
                }
            }

            public bool IsBool => ValueType == typeof(bool);

            public void ResetToDefault()
            {
                if (DefaultValue != null) SetValue(DefaultValue);
            }
        }

        static readonly Dictionary<string, List<Entry>> s_Categories = new Dictionary<string, List<Entry>>(16);
        static readonly List<object> s_Targets = new List<object>(32);

        public static IReadOnlyDictionary<string, List<Entry>> Categories => s_Categories;
        public static int EntryCount { get; private set; }

        public static void Register(object target)
        {
            if (target == null || s_Targets.Contains(target)) return;
            s_Targets.Add(target);
            Scan(target, target.GetType(), 0);
        }

        static void Scan(object target, Type type, int depth)
        {
            if (target == null || depth > 2) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                var attribute = field.GetCustomAttribute<TweakableAttribute>();
                if (attribute != null)
                {
                    AddEntry(new Entry
                    {
                        Target = target,
                        Field = field,
                        Attribute = attribute,
                        DisplayName = attribute.Label ?? Prettify(field.Name),
                        DefaultValue = field.GetValue(target)
                    });
                    continue;
                }

                // Walk into serialisable config sections so nested [Tweakable] fields are found.
                if (field.IsPublic && !field.FieldType.IsPrimitive && field.FieldType.IsClass &&
                    field.FieldType != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) &&
                    field.FieldType.IsDefined(typeof(SerializableAttribute), false))
                {
                    Scan(field.GetValue(target), field.FieldType, depth + 1);
                }
            }

            foreach (var property in type.GetProperties(flags))
            {
                var attribute = property.GetCustomAttribute<TweakableAttribute>();
                if (attribute == null || !property.CanRead) continue;
                AddEntry(new Entry
                {
                    Target = target,
                    Property = property,
                    Attribute = attribute,
                    DisplayName = attribute.Label ?? Prettify(property.Name),
                    DefaultValue = property.GetValue(target, null)
                });
            }
        }

        static void AddEntry(Entry entry)
        {
            string category = string.IsNullOrEmpty(entry.Attribute.Category) ? "General" : entry.Attribute.Category;
            if (!s_Categories.TryGetValue(category, out var list))
            {
                list = new List<Entry>(16);
                s_Categories.Add(category, list);
            }
            list.Add(entry);
            EntryCount++;
        }

        public static void Unregister(object target)
        {
            if (target == null) return;
            s_Targets.Remove(target);
            foreach (var kv in s_Categories)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(list[i].Target, target)) { list.RemoveAt(i); EntryCount--; }
            }
        }

        public static void Clear()
        {
            s_Categories.Clear();
            s_Targets.Clear();
            EntryCount = 0;
        }

        public static void ResetAll()
        {
            foreach (var kv in s_Categories)
                for (int i = 0; i < kv.Value.Count; i++) kv.Value[i].ResetToDefault();
        }

        static string Prettify(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            if (raw.StartsWith("m_", StringComparison.Ordinal)) raw = raw.Substring(2);
            var builder = new System.Text.StringBuilder(raw.Length + 6);
            builder.Append(char.ToUpperInvariant(raw[0]));
            for (int i = 1; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsUpper(c) && !char.IsUpper(raw[i - 1])) builder.Append(' ');
                builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
