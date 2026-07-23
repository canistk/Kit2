using System;
using System.IO;
using UnityEngine;

namespace Kit2
{
    public class IniFile
    {
        private string m_FilePath;
        private IniReader m_Reader;
        private IniWriter m_Writer;

        public IniFile(string filePath)
        {
            m_FilePath = filePath;
            Load();
        }

        public string FilePath => m_FilePath;

        public void Load()
        {
            if (File.Exists(m_FilePath))
            {
                var content = File.ReadAllText(m_FilePath);
                m_Reader = new IniReader(content);
                m_Writer = new IniWriter(m_Reader);
            }
            else
            {
                m_Reader = new IniReader(string.Empty);
                m_Writer = new IniWriter();
            }
        }

        public void Save()
        {
            var content = m_Writer.Write();
            var directory = Path.GetDirectoryName(m_FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(m_FilePath, content);
        }

        public bool HasSection(string section)
        {
            return m_Reader.HasSection(section);
        }

        public bool HasKey(string section, string key)
        {
            return m_Reader.HasKey(section, key);
        }

        public string GetString(string section, string key, string defaultValue = "")
        {
            return m_Reader.GetString(section, key, defaultValue);
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            return m_Reader.GetInt(section, key, defaultValue);
        }

        public float GetFloat(string section, string key, float defaultValue = 0f)
        {
            return m_Reader.GetFloat(section, key, defaultValue);
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            return m_Reader.GetBool(section, key, defaultValue);
        }

        public void SetString(string section, string key, string value)
        {
            m_Writer.SetString(section, key, value);
            m_Reader = new IniReader(m_Writer.Write());
        }

        public void SetInt(string section, string key, int value)
        {
            m_Writer.SetInt(section, key, value);
            m_Reader = new IniReader(m_Writer.Write());
        }

        public void SetFloat(string section, string key, float value)
        {
            m_Writer.SetFloat(section, key, value);
            m_Reader = new IniReader(m_Writer.Write());
        }

        public void SetBool(string section, string key, bool value)
        {
            m_Writer.SetBool(section, key, value);
            m_Reader = new IniReader(m_Writer.Write());
        }

        public bool RemoveKey(string section, string key)
        {
            var result = m_Writer.RemoveKey(section, key);
            if (result)
            {
                m_Reader = new IniReader(m_Writer.Write());
            }
            return result;
        }

        public bool RemoveSection(string section)
        {
            var result = m_Writer.RemoveSection(section);
            if (result)
            {
                m_Reader = new IniReader(m_Writer.Write());
            }
            return result;
        }

        public string[] GetSections()
        {
            return m_Reader.GetSections();
        }

        public string[] GetKeys(string section)
        {
            return m_Reader.GetKeys(section);
        }
    }
}
