using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace DiwanVR.Editor
{
    // XR Hands creates its OpenXR features after Unity imports the package.
    // Enable the provider after import and verify it again before building.
    [InitializeOnLoad]
    public sealed class QuestHandTrackingSetup : IPreprocessBuildWithReport
    {
        private const string TrackingFeature = "UnityEngine.XR.Hands.OpenXR.HandTracking";
        private const string AimFeature = "UnityEngine.XR.Hands.OpenXR.MetaHandTrackingAim";
        private static bool needsSetup = true;

        static QuestHandTrackingSetup()
        {
            EditorApplication.update += SetupWhenReady;
        }

        public int callbackOrder => -10000;

        private static void SetupWhenReady()
        {
            EnsureReady();
        }

        // Enables the OpenXR hand tracking features without relying on a
        // deferred editor update. Never touch the AssetDatabase while Unity is
        // entering or leaving play mode: an asset refresh there cancels the
        // play mode transition, the play-mode suites never report, and the
        // validation ends in a timeout instead of a result.
        public static bool EnsureReady()
        {
            if (!needsSetup) return true;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return false;
            try
            {
                if (!TryEnable(false)) return false;
            }
            catch (Exception error)
            {
                // A missing package or a half-imported feature must not throw
                // out of an EditorApplication.update handler.
                Debug.LogWarning("Diwan VR hand tracking setup deferred: " + error.Message);
                return false;
            }
            needsSetup = false;
            EditorApplication.update -= SetupWhenReady;
            return true;
        }

        private static bool TryEnable(bool failIfMissing)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            OpenXRFeature tracking = null;
            OpenXRFeature aim = null;
            if (settings)
            {
                foreach (var feature in settings.GetFeatures<OpenXRFeature>())
                {
                    if (!feature) continue;
                    string name = feature.GetType().FullName;
                    if (name == TrackingFeature) tracking = feature;
                    if (name == AimFeature) aim = feature;
                }
            }

            if (!tracking)
            {
                if (failIfMissing)
                    throw new BuildFailedException("XR Hands OpenXR HandTracking feature is missing. Resolve Unity packages and reimport the project before building.");
                return false;
            }

            bool changed = false;
            if (!tracking.enabled)
            {
                tracking.enabled = true;
                EditorUtility.SetDirty(tracking);
                changed = true;
            }
            // Optional Meta aim is useful on devices exposing XR_FB_hand_tracking_aim.
            if (aim && !aim.enabled)
            {
                aim.enabled = true;
                EditorUtility.SetDirty(aim);
                changed = true;
            }
            // Only write when something actually changed. An unconditional save
            // re-imports the OpenXR settings asset, and that asset refresh is
            // what interrupts a play mode transition.
            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                // Finish the import now. A refresh that is still pending when
                // play mode starts cancels that transition.
                AssetDatabase.Refresh();
            }
            return true;
        }

        public static void ConfigureForBuild()
        {
            // Importing XR Hands adds new feature types. Refresh the package's
            // feature registry before checking the Android settings.
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            TryEnable(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) TryEnable(true);
        }
    }
}
