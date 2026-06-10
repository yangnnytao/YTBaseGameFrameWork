using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 数据持久化接口 —— 支持多种后端（PlayerPrefs / JSON文件 / SQLite）
    /// </summary>
    public interface IDataStorage
    {
        void SetString(string key, string value);
        string GetString(string key, string defaultValue = "");

        void SetInt(string key, int value);
        int GetInt(string key, int defaultValue = 0);

        void SetFloat(string key, float value);
        float GetFloat(string key, float defaultValue = 0f);

        void SetBool(string key, bool value);
        bool GetBool(string key, bool defaultValue = false);

        void DeleteKey(string key);
        bool HasKey(string key);
        void DeleteAll();
    }

    /// <summary>
    /// PlayerPrefs 存储后端 —— 最简单，适合小数据量
    /// </summary>
    public class PlayerPrefsStorage : IDataStorage
    {
        public static readonly PlayerPrefsStorage Instance = new PlayerPrefsStorage();

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public bool GetBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteAll() => PlayerPrefs.DeleteAll();
    }

    /// <summary>
    /// JSON 文件存储后端 —— 适合结构化数据、存档系统
    /// </summary>
    public class JsonFileStorage : IDataStorage
    {
        public static readonly JsonFileStorage Instance = new JsonFileStorage();

        [Serializable]
        private class StorageData
        {
            public Dictionary<string, string> strings = new Dictionary<string, string>();
            public Dictionary<string, int> ints = new Dictionary<string, int>();
            public Dictionary<string, float> floats = new Dictionary<string, float>();
            public Dictionary<string, int> bools = new Dictionary<string, int>();
        }

        private StorageData _data = new StorageData();
        private string _filePath;
        private bool _loaded = false;

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _filePath = System.IO.Path.Combine(Application.persistentDataPath, "storage.json");
            if (System.IO.File.Exists(_filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(_filePath);
                    _data = JsonUtility.FromJson<StorageData>(json) ?? new StorageData();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[JsonFileStorage] 加载失败: {e.Message}");
                    _data = new StorageData();
                }
            }
            _loaded = true;
        }

        private void Save()
        {
            EnsureLoaded();
            try
            {
                string json = JsonUtility.ToJson(_data);
                System.IO.File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileStorage] 保存失败: {e.Message}");
            }
        }

        public void SetString(string key, string value)
        {
            EnsureLoaded();
            _data.strings[key] = value;
            Save();
        }
        public string GetString(string key, string defaultValue = "")
        {
            EnsureLoaded();
            return _data.strings.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            EnsureLoaded();
            _data.ints[key] = value;
            Save();
        }
        public int GetInt(string key, int defaultValue = 0)
        {
            EnsureLoaded();
            return _data.ints.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            EnsureLoaded();
            _data.floats[key] = value;
            Save();
        }
        public float GetFloat(string key, float defaultValue = 0f)
        {
            EnsureLoaded();
            return _data.floats.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            EnsureLoaded();
            _data.bools[key] = value ? 1 : 0;
            Save();
        }
        public bool GetBool(string key, bool defaultValue = false)
        {
            EnsureLoaded();
            return _data.bools.TryGetValue(key, out var v) ? v == 1 : defaultValue;
        }

        public void DeleteKey(string key)
        {
            EnsureLoaded();
            _data.strings.Remove(key);
            _data.ints.Remove(key);
            _data.floats.Remove(key);
            _data.bools.Remove(key);
            Save();
        }
        public bool HasKey(string key)
        {
            EnsureLoaded();
            return _data.strings.ContainsKey(key)
                || _data.ints.ContainsKey(key)
                || _data.floats.ContainsKey(key)
                || _data.bools.ContainsKey(key);
        }
        public void DeleteAll()
        {
            _data = new StorageData();
            Save();
        }
    }
}
