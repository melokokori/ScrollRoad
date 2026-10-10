using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScrollRoad.EditorTools
{
    public static class ScrollRoadBuilder
    {
        public const string OutputPath = "Builds/Windows/ScrollRoad.exe";

        [MenuItem("ScrollRoad/Windows Build Al")]
        public static void BuildFromMenu() => BuildStandaloneWindows();

        public static string BuildStandaloneWindows()
        {
            ScrollRoadSceneBuilder.ConfigureProjectSettings();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScrollRoadSceneBuilder.GameScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build başarısız: {report.summary.result} ({report.summary.totalErrors} hata)");
                return $"FAILED {report.summary.result}";
            }

            // Steam testi için App ID dosyası exe'nin yanında olmalı (gerçek Steam dağıtımında gerekmez).
            string buildDir = Path.GetDirectoryName(Path.GetFullPath(OutputPath));
            if (File.Exists("steam_appid.txt")) File.Copy("steam_appid.txt", Path.Combine(buildDir, "steam_appid.txt"), true);

            string msg = $"Build başarılı: {OutputPath} ({report.summary.totalSize / 1024 / 1024} MB)";
            Debug.Log(msg);
            return msg;
        }
    }
}
