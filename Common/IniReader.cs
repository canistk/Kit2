using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public class IniReader
    {
        private readonly string m_Content;
        private Dictionary<string, Dictionary<string, string>> m_Data;

        public IniReader(string content)
        {
            m_Content = content;
            m_Data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Parse();
        }

        private void Parse()
        {
            if (string.IsNullOrEmpty(m_Content))
                return;

            var lexer = new Lexer(m_Content);
            string currentSection = string.Empty;

            while (!lexer.IsCompleted())
            {
                if (!lexer.NextToken(eSkipMethods.SkipSpace))
                    break;

                var token = lexer.token;

                if (token.IsComment() || token.IsBlockOfComment())
                {
                    continue;
                }

                if (token.IsNewLine())
                {
                    continue;
                }

                if (token.IsOperator('['))
                {
                    if (!lexer.NextToken(eSkipMethods.SkipSpace))
                        throw new LexerException(lexer, "Expected section name after '['");

                    if (!lexer.token.IsIdentifier())
                        throw new LexerException(lexer, "Expected section name");

                    currentSection = lexer.token.value;

                    if (!lexer.NextToken(eSkipMethods.SkipSpace))
                        throw new LexerException(lexer, "Expected ']' after section name");

                    if (!lexer.token.IsOperator(']'))
                        throw new LexerException(lexer, "Expected ']' after section name");

                    if (!m_Data.ContainsKey(currentSection))
                        m_Data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    continue;
                }

                if (token.IsIdentifier())
                {
                    string key = token.value;

                    if (!lexer.NextToken(eSkipMethods.SkipSpace))
                        throw new LexerException(lexer, $"Expected '=' after key '{key}'");

                    if (!lexer.token.IsOperator('='))
                        throw new LexerException(lexer, $"Expected '=' after key '{key}'");

                    if (!lexer.NextToken(eSkipMethods.SkipSpace))
                        throw new LexerException(lexer, $"Expected value after '=' for key '{key}'");

                    string value = ReadValue(lexer);

                    if (!m_Data.ContainsKey(currentSection))
                        m_Data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    m_Data[currentSection][key] = value;
                }
            }
        }

        private string ReadValue(Lexer lexer)
        {
            var token = lexer.token;

            if (token.IsBlockOfString())
            {
                return token.value;
            }
            else if (token.IsInteger() || token.IsFloat() || token.IsHexadecimal())
            {
                return token.value;
            }
            else if (token.IsIdentifier())
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(token.value);

                while (!lexer.IsCompleted())
                {
                    var anchorBefore = lexer.GetAnchor();
                    if (!lexer.NextToken(eSkipMethods.None))
                        break;

                    var nextToken = lexer.token;
                    if (nextToken.IsNewLine() || nextToken.IsComment() || nextToken.IsBlockOfComment())
                        break;

                    if (nextToken.IsSpace())
                    {
                        sb.Append(' ');
                    }
                    else if (nextToken.IsIdentifier() || nextToken.IsInteger() || nextToken.IsFloat() || nextToken.IsOperator())
                    {
                        sb.Append(nextToken.value);
                    }
                    else
                    {
                        break;
                    }
                }

                return sb.ToString().TrimEnd();
            }

            return string.Empty;
        }

        public bool HasSection(string section)
        {
            return m_Data.ContainsKey(section);
        }

        public bool HasKey(string section, string key)
        {
            return m_Data.ContainsKey(section) && m_Data[section].ContainsKey(key);
        }

        public string GetString(string section, string key, string defaultValue = "")
        {
            if (HasKey(section, key))
                return m_Data[section][key];
            return defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            if (HasKey(section, key))
            {
                var value = m_Data[section][key];
                var lexer = new Lexer(value);
                if (lexer.NextToken(eSkipMethods.SkipAll))
                {
                    var token = lexer.token;
                    if (token.IsInteger())
                    {
                        if (int.TryParse(token.value, out int result))
                            return result;
                    }
                    else if (token.IsHexadecimal())
                    {
                        if (int.TryParse(token.value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int result))
                            return result;
                    }
                }
            }
            return defaultValue;
        }

        public float GetFloat(string section, string key, float defaultValue = 0f)
        {
            if (HasKey(section, key))
            {
                var value = m_Data[section][key];
                var lexer = new Lexer(value);
                if (lexer.NextToken(eSkipMethods.SkipAll))
                {
                    var token = lexer.token;
                    if (token.IsFloat() || token.IsInteger())
                    {
                        if (float.TryParse(token.value, out float result))
                            return result;
                    }
                }
            }
            return defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            if (HasKey(section, key))
            {
                var value = m_Data[section][key].ToLower();
                if (value == "true" || value == "1" || value == "yes")
                    return true;
                if (value == "false" || value == "0" || value == "no")
                    return false;
            }
            return defaultValue;
        }

        public string[] GetSections()
        {
            var sections = new string[m_Data.Keys.Count];
            m_Data.Keys.CopyTo(sections, 0);
            return sections;
        }

        public string[] GetKeys(string section)
        {
            if (m_Data.ContainsKey(section))
            {
                var keys = new string[m_Data[section].Keys.Count];
                m_Data[section].Keys.CopyTo(keys, 0);
                return keys;
            }
            return new string[0];
        }
    }
}
