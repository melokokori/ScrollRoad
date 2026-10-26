using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScrollRoad.EditorTools
{
    // Sahne kurucusunun yazdığı sabit etiketlerin (LocalizedText) çeviri anahtarlarını Tools/loc/scene_keys.txt'e
    // döker; Tools/loc/extract.py koddaki anahtarlarla birleştirir. "Sahneleri Kur"dan sonra çalıştırılmalı.
    public static class LocalizationKeys
    {
        public const string OutputPath = "Tools/loc/scene_keys.txt";

        [MenuItem("ScrollRoad/Dil Anahtarlarını Topla (sahne)")]
        public static void Dump()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var keys = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<LocalizedText>(true))
                {
                    if (!string.IsNullOrEmpty(text.Key)) keys.Add(text.Key);
                    if (text.Args != null) foreach (var arg in text.Args) if (!string.IsNullOrEmpty(arg)) keys.Add(arg);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            // Satır başına bir anahtar; satır sonu ve ters bölü kaçışlı.
            File.WriteAllLines(OutputPath, keys.Select(k => k.Replace("\\", "\\\\").Replace("\n", "\\n")), new System.Text.UTF8Encoding(false));
            Debug.Log($"[LocalizationKeys] {keys.Count} sahne anahtarı → {OutputPath}");
        }
    }
}
