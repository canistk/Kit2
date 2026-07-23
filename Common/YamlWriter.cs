using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Kit2
{
    public class YamlWriter
    {
        private Dictionary<string, object> m_Data;

        public YamlWriter()
        {
            m_Data = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public YamlWriter(YamlReader reader)
        {
            m_Data = new Dictionary<string, object>(StringComparer.Ordinal);
            var keys = reader.GetKeys();
            foreach (var key in keys)
            {
                CopyValue(reader, key, m_Data);
            }
        }

        private void CopyValue(YamlReader reader, string key, Dictionary<string, object> targetDict)
        {
            var value = reader.GetDictionary(key);
            if (value != null && value.Count > 0)
            {
                var newDict = new Dictionary<string, object>(StringComparer.Ordinal);
                var subKeys = reader.GetKeys(key);
                foreach (var subKey in subKeys)
                {
                    CopyValue(reader, $"{key}.{subKey}", newDict);
                }
                targetDict[key] = newDict;
            }
            else
            {
                targetDict[key] = reader.GetString(key);
            }
        }

        private void SetValue(string path, object value)
        {
            var keys = path.Split('.');
            Dictionary<string, object> current = m_Data;

            for (int i = 0; i < keys.Length - 1; i++)
            {
                var key = keys[i];
                if (!current.ContainsKey(key))
                {
                    current[key] = new Dictionary<string, object>(StringComparer.Ordinal);
                }

                if (current[key] is Dictionary<string, object> dict)
                {
                    current = dict;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot set value at path '{path}', '{key}' is not a dictionary");
                }
            }

            current[keys[keys.Length - 1]] = value;
        }

        public void SetString(string path, string value)
        {
            SetValue(path, value ?? string.Empty);
        }

        public void SetInt(string path, int value)
        {
            SetValue(path, value);
        }

        public void SetFloat(string path, float value)
        {
            SetValue(path, value);
        }

        public void SetBool(string path, bool value)
        {
            SetValue(path, value);
        }

        public void SetList(string path, List<object> list)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            dict["_items"] = list;
            SetValue(path, dict);
        }

        public bool RemoveKey(string path)
        {
            var keys = path.Split('.');
            Dictionary<string, object> current = m_Data;

            for (int i = 0; i < keys.Length - 1; i++)
            {
                var key = keys[i];
                if (!current.ContainsKey(key))
                    return false;

                if (current[key] is Dictionary<string, object> dict)
                {
                    current = dict;
                }
                else
                {
                    return false;
                }
            }

            return current.Remove(keys[keys.Length - 1]);
        }

        public string Write()
        {
            var sb = new StringBuilder();
            WriteObject(m_Data, sb, 0);
            return sb.ToString();
        }

        private void WriteObject(Dictionary<string, object> dict, StringBuilder sb, int indent)
        {
            foreach (var kvp in dict)
            {
                if (kvp.Key == "_items")
                    continue;

                WriteIndent(sb, indent);
                sb.Append(kvp.Key).Append(':');

                if (kvp.Value is Dictionary<string, object> subDict)
                {
                    sb.AppendLine();

                    if (subDict.ContainsKey("_items") && subDict["_items"] is List<object> list)
                    {
                        WriteList(list, sb, indent + 2);
                    }
                    else
                    {
                        WriteObject(subDict, sb, indent + 2);
                    }
                }
                else
                {
                    sb.Append(' ');
                    WriteValue(kvp.Value, sb);
                    sb.AppendLine();
                }
            }
        }

        private void WriteList(List<object> list, StringBuilder sb, int indent)
        {
            foreach (var item in list)
            {
                WriteIndent(sb, indent);
                sb.Append('-').Append(' ');
                WriteValue(item, sb);
                sb.AppendLine();
            }
        }

        private void WriteValue(object value, StringBuilder sb)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            if (value is bool boolValue)
            {
                sb.Append(boolValue ? "true" : "false");
                return;
            }

            if (value is int || value is float || value is double)
            {
                sb.Append(value);
                return;
            }

            var strValue = value.ToString();

            if (NeedsQuotes(strValue))
            {
                sb.Append('\"').Append(strValue).Append('\"');
            }
            else
            {
                sb.Append(strValue);
            }
        }

        private bool NeedsQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var lexer = new Lexer(value);
            if (!lexer.NextToken(eSkipMethods.SkipAll))
                return true;

            var token = lexer.token;

            if (token.IsInteger() || token.IsFloat() || token.IsHexadecimal())
            {
                if (lexer.IsCompleted())
                    return true;
            }

            if (token.IsIdentifier())
            {
                var lower = token.value.ToLower();
                if (lower == "true" || lower == "false" || lower == "yes" || lower == "no" || lower == "null")
                    return true;
            }

            if (value.Contains(":") || value.Contains("#") || value.Contains("-") ||
                value.Contains("[") || value.Contains("]") || value.Contains("{") || value.Contains("}") ||
                value.Contains("&") || value.Contains("*") || value.Contains("!") ||
                value.Contains("|") || value.Contains(">") || value.Contains("'") || value.Contains("\"") ||
                value.StartsWith(" ") || value.EndsWith(" "))
            {
                return true;
            }

            return false;
        }

        private void WriteIndent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++)
            {
                sb.Append(' ');
            }
        }
    }
}
