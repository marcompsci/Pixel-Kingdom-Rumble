using System;
using System.IO;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Crash-safe JSON persistence in Application.persistentDataPath.
    /// Writes to a temp file, keeps the previous good file as .bak, and falls back to it on load.
    /// </summary>
    public static class JsonFileStore
    {
        public static string PathFor(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

        public static bool TryWrite<T>(string fileName, T data)
        {
            string path = PathFor(fileName);
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(tmp, json);
                if (File.Exists(path))
                {
                    File.Copy(path, bak, overwrite: true);
                    File.Delete(path);
                }
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileStore] Failed to write {fileName}: {e.Message}");
                return false;
            }
        }

        /// <summary>Returns null if neither the file nor its backup can be read.</summary>
        public static T TryRead<T>(string fileName) where T : class
        {
            string path = PathFor(fileName);
            return ReadOne<T>(path) ?? ReadOne<T>(path + ".bak");
        }

        public static void Delete(string fileName)
        {
            string path = PathFor(fileName);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        static T ReadOne<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonFileStore] Could not read {Path.GetFileName(path)}: {e.Message}");
                return null;
            }
        }
    }
}
