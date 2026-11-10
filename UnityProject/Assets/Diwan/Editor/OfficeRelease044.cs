using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwanVR.Editor
{
    public static class OfficeRelease044
    {
        [Serializable] private sealed class ValidationRecord{public string status,sourceVersion,codeSha256,sceneSha256;}
        [Serializable] private sealed class Receipt
        {
            public string runId, version, result, apkPath, sha256, finishedUtc, codeSha256, sceneSha256;
            public int versionCode;
            public long bytes;
            public bool headsetTested = false;
        }

        [MenuItem("Diwan VR/0.4.4 - Build Quest rendering fix")]
        public static void BuildQuest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch the active build target to Android first.");
            string rawId = Environment.GetEnvironmentVariable("DIWAN_044_BUILD_RUN_ID");
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
            PlayerSettings.bundleVersion = "0.4.4";
            PlayerSettings.Android.bundleVersionCode = 8;
            AssetDatabase.SaveAssets();

            var checksDirectory="Validation";
            if(!Directory.Exists(checksDirectory))throw new InvalidOperationException("Run Test_Unity_0.4.4.ps1 before building.");
            var latest=System.Linq.Enumerable.OrderByDescending(Directory.GetFiles(checksDirectory,"result.json",SearchOption.AllDirectories),File.GetLastWriteTimeUtc).FirstOrDefault();
            if(latest==null)throw new InvalidOperationException("Run the 0.4.4 validation before building.");
            var validation=JsonUtility.FromJson<ValidationRecord>(File.ReadAllText(latest));
            if(validation.status!="passed" || validation.sourceVersion!="0.4.4" || validation.codeSha256!=SourceFingerprint.Code()
                || validation.sceneSha256!=SourceFingerprint.FileHash(DiwanBuilder.ScenePath))
                throw new InvalidOperationException("The latest validation did not pass against this source and scene. Re-run Test_Unity_0.4.4.ps1.");

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

            string apk = Path.Combine(directory, "DiwanVR_0.4.4_Quest.apk").Replace('\\', '/');
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
                runId = id.ToString("N"), version = "0.4.4", versionCode = 8,
                result = "Succeeded", apkPath = apk, bytes = bytes, sha256 = hash,
                finishedUtc = DateTime.UtcNow.ToString("o"), codeSha256=SourceFingerprint.Code(),sceneSha256=SourceFingerprint.FileHash(DiwanBuilder.ScenePath)
            }, true));
            Debug.Log("DIWAN_044_BUILD_SUCCEEDED " + Path.GetFullPath(apk));
        }
    }
}
