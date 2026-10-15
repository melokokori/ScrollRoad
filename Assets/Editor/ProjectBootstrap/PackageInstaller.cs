using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    // Paket ekleme/çıkarmayı PackageManager Client API ile headless yapar.
    // Çalıştırma: Unity.exe -batchmode -projectPath <proje> -executeMethod ProjectBootstrap.PackageInstaller.Install  (-quit YOK)
    public static class PackageInstaller
    {
        static readonly string[] PackagesToAdd =
        {
            // Claude Code <-> Unity Editor köprüsü (MCP for Unity, http://127.0.0.1:8080/mcp)
            "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main",
        };

        static readonly string[] PackagesToRemove =
        {
            "com.unity.visualscripting",
            "com.unity.multiplayer.center",
            "com.unity.collab-proxy",
            // Şerit saydamlığı artık kendi Win32 kodumuzla (Smart App Control imzasız LibUniWinC.dll'yi engelliyordu)
            "com.kirurobo.uniwinc",
        };

        const double TimeoutSeconds = 600;

        static AddAndRemoveRequest _request;
        static double _deadline;

        public static void Install()
        {
            // Zaten çıkarılmış paketleri tekrar çıkarmaya çalışma (betik birden çok kez çalıştırılabilsin).
            var manifest = System.IO.File.ReadAllText("Packages/manifest.json");
            var toRemove = PackagesToRemove.Where(p => manifest.Contains($"\"{p}\"")).ToArray();

            Debug.Log($"[PackageInstaller] Ekleniyor: {string.Join(", ", PackagesToAdd)} | Çıkarılıyor: {string.Join(", ", toRemove)}");
            _request = Client.AddAndRemove(packagesToAdd: PackagesToAdd, packagesToRemove: toRemove);
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[PackageInstaller] UPM beklenirken zaman aşımı.");
                    EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Success)
            {
                var names = _request.Result.Select(p => $"{p.name}@{p.version}");
                Debug.Log($"[PackageInstaller] Çözüldü: {string.Join(", ", names)}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageInstaller] Başarısız: {_request.Error?.message}");
                EditorApplication.Exit(1);
            }
        }
    }
}
