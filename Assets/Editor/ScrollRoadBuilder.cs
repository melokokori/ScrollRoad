using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScrollRoad.EditorTools
{
    public static class ScrollRoadBuilder
    {
        public const string OutputPath = "Builds/Windows/ScrollRoad.exe";

        // Geliştirici build'i (ayrı klasör): "-unlockall" inceleme modu ve F3 paneli yalnız burada çalışır.
        public const string ReviewOutputPath = "Builds/Review/ScrollRoad.exe";

        [MenuItem("ScrollRoad/Windows Build Al")]
        public static void BuildFromMenu() => BuildStandaloneWindows();

        [MenuItem("ScrollRoad/İnceleme Build Al (geliştirici, -unlockall)")]
        public static void BuildReviewFromMenu() => BuildStandaloneWindows(ReviewOutputPath, BuildOptions.Development);

        public static string BuildStandaloneWindows() => BuildStandaloneWindows(OutputPath, BuildOptions.None);

        // Batchmode: Unity.exe -batchmode -quit -projectPath <proje> -executeMethod ScrollRoad.EditorTools.ScrollRoadBuilder.BuildFromCommandLine [-buildOut <exe yolu>]
        // Oyun Builds/Windows'tan çalışırken (dosyalar kilitli) yalnız derleme doğrulaması için başka klasöre build alınabilir.
        public static void BuildFromCommandLine()
        {
            string output = OutputPath;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOut") output = args[i + 1];
            if (BuildStandaloneWindows(output, BuildOptions.None).StartsWith("FAILED")) EditorApplication.Exit(1);
        }

        // Editor Play'de her araç/renk/gösterge skini açık (CarCatalog.ReviewUnlockAll). Kayda dokunmaz.
        const string ReviewMenu = "ScrollRoad/İnceleme modu (her şey açık)";

        [MenuItem(ReviewMenu)]
        static void ToggleReviewMode() => EditorPrefs.SetBool(CarCatalog.ReviewPrefKey, !EditorPrefs.GetBool(CarCatalog.ReviewPrefKey, false));

        [MenuItem(ReviewMenu, true)]
        static bool ToggleReviewModeValidate()
        {
            Menu.SetChecked(ReviewMenu, EditorPrefs.GetBool(CarCatalog.ReviewPrefKey, false));
            return true;
        }

        public static string BuildStandaloneWindows(string outputPath, BuildOptions buildOptions)
        {
            ScrollRoadSceneBuilder.ConfigureProjectSettings();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScrollRoadSceneBuilder.GameScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build başarısız: {report.summary.result} ({report.summary.totalErrors} hata)");
                return $"FAILED {report.summary.result}";
            }

            // Steam testi için App ID dosyası exe'nin yanında olmalı (gerçek Steam dağıtımında gerekmez).
            string buildDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (File.Exists("steam_appid.txt")) File.Copy("steam_appid.txt", Path.Combine(buildDir, "steam_appid.txt"), true);

            string msg = $"Build başarılı: {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)";
            Debug.Log(msg);
            return msg;
        }
    }
}
