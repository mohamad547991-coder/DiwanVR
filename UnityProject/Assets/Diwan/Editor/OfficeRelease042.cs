using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DiwanVR.Editor
{
    // Build into a new, unique directory. A previous APK or build report can
    // therefore never be mistaken for the result of this invocation.
    public static class OfficeRelease042
    {
        [Serializable]
        private sealed class Receipt
        {
            public string runId;
            public string version;
            public int versionCode;
            public string result;
            public string apkPath;
            public long bytes;
        }

        [MenuItem("Diwan VR/0.4.2 - Build diagnostic Quest APK")]
        public static void BuildQuest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch the active build target to Android before building.");

            var rawRunId = Environment.GetEnvironmentVariable("DIWAN_042_BUILD_RUN_ID");
            Guid runId;
            if (string.IsNullOrEmpty(rawRunId)) runId = Guid.NewGuid();
            else if (!Guid.TryParseExact(rawRunId, "N", out runId))
                throw new InvalidOperationException("Invalid build run identifier.");

            string run = runId.ToString("N");
            string directory = Path.Combine("Builds", "Quest", "run-" + run);
            if (Directory.Exists(directory))
                throw new InvalidOperationException("Build run directory already exists; refusing to reuse it.");
            Directory.CreateDirectory(directory);

            DiwanBuilder.ConfigureSettings();
            QuestHandTrackingSetup.ConfigureForBuild();
            PlayerSettings.bundleVersion = "0.4.2";
            PlayerSettings.Android.bundleVersionCode = 6;
            AssetDatabase.SaveAssets();

            string apkPath = Path.Combine(directory, "DiwanVR_0.4.2_Quest.apk").Replace('\\', '/');
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DiwanBuilder.ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(apkPath))
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            var info = new FileInfo(apkPath);
            if (info.Length <= 0) throw new InvalidOperationException("Build returned an empty APK.");

            var receipt = new Receipt
            {
                runId = run,
                version = "0.4.2",
                versionCode = 6,
                result = report.summary.result.ToString(),
                apkPath = apkPath,
                bytes = info.Length
            };
            File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonUtility.ToJson(receipt, true));
            Debug.Log("DIWAN_042_BUILD_SUCCEEDED " + Path.GetFullPath(apkPath));
        }
    }
}
