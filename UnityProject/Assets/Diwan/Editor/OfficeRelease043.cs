using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwanVR.Editor
{
    public static class OfficeRelease043
    {
        [Serializable] private sealed class Receipt
        {
            public string runId, version, result, apkPath, sha256, finishedUtc;
            public int versionCode;
            public long bytes;
            public bool headsetTested = false;
        }

        [MenuItem("Diwan VR/0.4.3 - Build Quest rendering fix")]
        public static void BuildQuest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch the active build target to Android first.");
            string rawId = Environment.GetEnvironmentVariable("DIWAN_043_BUILD_RUN_ID");
            Guid id;
            if (string.IsNullOrEmpty(rawId)) id = Guid.NewGuid();
            else if (!Guid.TryParseExact(rawId, "N", out id))
                throw new InvalidOperationException("Invalid build run identifier.");
            string directory = Path.Combine("Builds", "Quest", "run-" + id.ToString("N"));
            if (Directory.Exists(directory)) throw new InvalidOperationException("Build directory already exists.");
            Directory.CreateDirectory(directory);

            DiwanBuilder.ConfigureSettings();
            QuestHandTrackingSetup.ConfigureForBuild();
            QuestRenderCompatibility.Apply();
            QuestRenderCompatibility.Validate();
            PlayerSettings.bundleVersion = "0.4.3";
            PlayerSettings.Android.bundleVersionCode = 7;
            AssetDatabase.SaveAssets();

            // Check the actual saved room that will be included in the APK.
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            var rig = UnityEngine.Object.FindFirstObjectByType<VRRig>();
            if (!rig || !rig.head || !rig.left || !rig.right ||
                rig.head.stereoTargetEye != StereoTargetEyeMask.Both)
                throw new InvalidOperationException("Room camera or XR rig is incomplete.");
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            if (renderers.Length < 100) throw new InvalidOperationException("The office scene is incomplete.");
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>();
                if (!mesh || !mesh.sharedMesh)
                    throw new InvalidOperationException("Missing mesh: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader)
                        throw new InvalidOperationException("Missing material/shader: " + renderer.name);
            }

            string apk = Path.Combine(directory, "DiwanVR_0.4.3_Quest.apk").Replace('\\', '/');
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DiwanBuilder.ScenePath },
                locationPathName = apk, target = BuildTarget.Android, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(apk))
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            long bytes = new FileInfo(apk).Length;
            if (bytes == 0) throw new InvalidOperationException("Empty APK.");
            string hash;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(apk))
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonUtility.ToJson(new Receipt
            {
                runId = id.ToString("N"), version = "0.4.3", versionCode = 7,
                result = "Succeeded", apkPath = apk, bytes = bytes, sha256 = hash,
                finishedUtc = DateTime.UtcNow.ToString("o")
            }, true));
            Debug.Log("DIWAN_043_BUILD_SUCCEEDED " + Path.GetFullPath(apk));
        }
    }
}
