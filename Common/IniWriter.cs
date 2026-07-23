using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Kit2
{
    public class IniWriter
    {
        private Dictionary<string, Dictionary<string, string>> m_Data;

        public IniWriter()
        {
            m_Data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        public IniWriter(IniReader reader)
        {
            m_Data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var sections = reader.GetSections();
            foreach (var section in sections)
            {
                var keys = reader.GetKeys(section);
                foreach (var key in keys)
                {
                    SetString(section, key, reader.GetString(section, key));
                }
            }
        }

        public void SetString(string section, string key, string value)
        {
            if (!m_Data.ContainsKey(section))
                m_Data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            m_Data[section][key] = value ?? string.Empty;
        }

        public void SetInt(string section, string key, int value)
        {
            SetString(section, key, value.ToString());
        }

        public void SetFloat(string section, string key, float value)
        {
            SetString(section, key, value.ToString());
        }

        public void SetBool(string section, string key, bool value)
        {
            SetString(section, key, value ? "true" : "false");
        }

        public bool RemoveKey(string section, string key)
        {
            if (m_Data.ContainsKey(section))
            {
                return m_Data[section].Remove(key);
            }
            return false;
        }

        public bool RemoveSection(string section)
        {
            return m_Data.Remove(section);
        }

        public string Write()
        {
            var sb = new StringBuilder();

            foreach (var section in m_Data)
            {
                if (section.Value.Count == 0)
                    continue;

                sb.Append('[').Append(section.Key).Append(']').AppendLine();

                foreach (var kvp in section.Value)
                {
                    var value = kvp.Value;
                    var needsQuotes = NeedsQuotes(value);

                    sb.Append(kvp.Key).Append('=');
                    if (needsQuotes)
                        sb.Append('\"');
                    sb.Append(value);
                    if (needsQuotes)
                        sb.Append('\"');
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private bool NeedsQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var lexer = new Lexer(value);
            if (!lexer.NextToken(eSkipMethods.SkipAll))
                return false;

            var token = lexer.token;
            if (token.IsInteger() || token.IsFloat() || token.IsHexadecimal())
            {
                if (lexer.IsCompleted())
                    return false;
            }

            if (value.Contains(" ") || value.Contains("\t") || value.Contains("\n"))
                return true;

            foreach (char c in value)
            {
                if (c == '=' || c == '[' || c == ']' || c == ';' || c == '#')
                    return true;
            }

            return false;
        }
    }
}
