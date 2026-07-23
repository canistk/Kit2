using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public class YamlReader
    {
        private readonly string m_Content;
        private Dictionary<string, object> m_Data;

        public YamlReader(string content)
        {
            m_Content = content;
            m_Data = new Dictionary<string, object>(StringComparer.Ordinal);
            Parse();
        }

        private void Parse()
        {
            if (string.IsNullOrEmpty(m_Content))
                return;

            var lines = m_Content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<KeyValuePair<int, Dictionary<string, object>>>();
            stack.Push(new KeyValuePair<int, Dictionary<string, object>>(-1, m_Data));

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#"))
                    continue;

                var indent = line.Length - trimmed.Length;

                while (stack.Count > 1 && stack.Peek().Key >= indent)
                {
                    stack.Pop();
                }

                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = trimmed.Substring(0, colonIndex).Trim();
                    var valueStr = trimmed.Substring(colonIndex + 1).Trim();

                    if (string.IsNullOrEmpty(valueStr))
                    {
                        var newDict = new Dictionary<string, object>(StringComparer.Ordinal);
                        stack.Peek().Value[key] = newDict;
                        stack.Push(new KeyValuePair<int, Dictionary<string, object>>(indent, newDict));
                    }
                    else
                    {
                        var value = ParseValue(valueStr);
                        stack.Peek().Value[key] = value;
                    }
                }
                else if (trimmed.StartsWith("-"))
                {
                    var valueStr = trimmed.Substring(1).Trim();
                    var currentDict = stack.Peek().Value;

                    if (!currentDict.ContainsKey("_items"))
                    {
                        currentDict["_items"] = new List<object>();
                    }

                    if (currentDict["_items"] is List<object> list)
                    {
                        list.Add(ParseValue(valueStr));
                    }
                }
            }
        }

        private object ParseValue(string valueStr)
        {
            if (string.IsNullOrEmpty(valueStr))
                return string.Empty;

            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }

            if (valueStr.StartsWith("'") && valueStr.EndsWith("'"))
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }

            var lexer = new Lexer(valueStr);
            if (lexer.NextToken(eSkipMethods.SkipAll))
            {
                var token = lexer.token;

                if (token.IsInteger())
                {
                    if (int.TryParse(token.value, out int intResult))
                        return intResult;
                }
                else if (token.IsFloat())
                {
                    if (float.TryParse(token.value, out float floatResult))
                        return floatResult;
                }
                else if (token.IsHexadecimal())
                {
                    if (int.TryParse(token.value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int hexResult))
                        return hexResult;
                }
                else if (token.IsIdentifier())
                {
                    var lower = token.value.ToLower();
                    if (lower == "true" || lower == "yes")
                        return true;
                    if (lower == "false" || lower == "no")
                        return false;
                    if (lower == "null")
                        return null;
                }
            }

            return valueStr;
        }

        private object GetValue(string path)
        {
            var keys = path.Split('.');
            object current = m_Data;

            foreach (var key in keys)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey(key))
                    {
                        current = dict[key];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        public bool HasKey(string path)
        {
            return GetValue(path) != null;
        }

        public string GetString(string path, string defaultValue = "")
        {
            var value = GetValue(path);
            if (value != null)
                return value.ToString();
            return defaultValue;
        }

        public int GetInt(string path, int defaultValue = 0)
        {
            var value = GetValue(path);
            if (value is int intValue)
                return intValue;
            if (value != null && int.TryParse(value.ToString(), out int result))
                return result;
            return defaultValue;
        }

        public float GetFloat(string path, float defaultValue = 0f)
        {
            var value = GetValue(path);
            if (value is float floatValue)
                return floatValue;
            if (value is int intValue)
                return (float)intValue;
            if (value != null && float.TryParse(value.ToString(), out float result))
                return result;
            return defaultValue;
        }

        public bool GetBool(string path, bool defaultValue = false)
        {
            var value = GetValue(path);
            if (value is bool boolValue)
                return boolValue;
            if (value != null)
            {
                var str = value.ToString().ToLower();
                if (str == "true" || str == "yes" || str == "1")
                    return true;
                if (str == "false" || str == "no" || str == "0")
                    return false;
            }
            return defaultValue;
        }

        public List<object> GetList(string path)
        {
            var value = GetValue(path);
            if (value is Dictionary<string, object> dict && dict.ContainsKey("_items"))
            {
                if (dict["_items"] is List<object> list)
                    return list;
            }
            return new List<object>();
        }

        public Dictionary<string, object> GetDictionary(string path)
        {
            var value = GetValue(path);
            if (value is Dictionary<string, object> dict)
                return dict;
            return new Dictionary<string, object>();
        }

        public string[] GetKeys(string path = "")
        {
            Dictionary<string, object> dict;

            if (string.IsNullOrEmpty(path))
            {
                dict = m_Data;
            }
            else
            {
                var value = GetValue(path);
                if (value is Dictionary<string, object> d)
                    dict = d;
                else
                    return new string[0];
            }

            var keys = new List<string>();
            foreach (var key in dict.Keys)
            {
                if (key != "_items")
                    keys.Add(key);
            }
            return keys.ToArray();
        }
    }
}
