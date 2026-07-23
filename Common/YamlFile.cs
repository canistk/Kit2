using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kit2
{
    public class YamlFile
    {
        private string m_FilePath;
        private YamlReader m_Reader;
        private YamlWriter m_Writer;

        public YamlFile(string filePath)
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
                m_Reader = new YamlReader(content);
                m_Writer = new YamlWriter(m_Reader);
            }
            else
            {
                m_Reader = new YamlReader(string.Empty);
                m_Writer = new YamlWriter();
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

        public bool HasKey(string path)
        {
            return m_Reader.HasKey(path);
        }

        public string GetString(string path, string defaultValue = "")
        {
            return m_Reader.GetString(path, defaultValue);
        }

        public int GetInt(string path, int defaultValue = 0)
        {
            return m_Reader.GetInt(path, defaultValue);
        }

        public float GetFloat(string path, float defaultValue = 0f)
        {
            return m_Reader.GetFloat(path, defaultValue);
        }

        public bool GetBool(string path, bool defaultValue = false)
        {
            return m_Reader.GetBool(path, defaultValue);
        }

        public List<object> GetList(string path)
        {
            return m_Reader.GetList(path);
        }

        public Dictionary<string, object> GetDictionary(string path)
        {
            return m_Reader.GetDictionary(path);
        }

        public void SetString(string path, string value)
        {
            m_Writer.SetString(path, value);
            m_Reader = new YamlReader(m_Writer.Write());
        }

        public void SetInt(string path, int value)
        {
            m_Writer.SetInt(path, value);
            m_Reader = new YamlReader(m_Writer.Write());
        }

        public void SetFloat(string path, float value)
        {
            m_Writer.SetFloat(path, value);
            m_Reader = new YamlReader(m_Writer.Write());
        }

        public void SetBool(string path, bool value)
        {
            m_Writer.SetBool(path, value);
            m_Reader = new YamlReader(m_Writer.Write());
        }

        public void SetList(string path, List<object> list)
        {
            m_Writer.SetList(path, list);
            m_Reader = new YamlReader(m_Writer.Write());
        }

        public bool RemoveKey(string path)
        {
            var result = m_Writer.RemoveKey(path);
            if (result)
            {
                m_Reader = new YamlReader(m_Writer.Write());
            }
            return result;
        }

        public string[] GetKeys(string path = "")
        {
            return m_Reader.GetKeys(path);
        }
    }
}
