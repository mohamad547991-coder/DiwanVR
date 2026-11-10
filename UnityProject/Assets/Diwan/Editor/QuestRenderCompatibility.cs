using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace DiwanVR.Editor
{
    // Vulkan memoryless MSAA attachments can lose opaque draws between URP
    // render passes on affected Quest runtimes. Keep their contents until the
    // final resolve. Retest GPU cost on the headset before enabling this again.
    // https://developers.meta.com/vr/support/investigations/2983595051984902/
    public sealed class QuestRenderCompatibility : IPreprocessBuildWithReport
    {
        private const string MetaFeatureType =
            "UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature";

        // Run after XR setup. Fail the build if another setup step regresses it.
        public int callbackOrder => 10000;

        public static void Apply()
        {
            var settings = AndroidSettings();
            SetDiscards(settings, false);
            // MetaQuestFeature copies its legacy field to OpenXRSettings when
            // building/migrating. Both serialized values must agree.
            foreach (var feature in settings.GetFeatures<OpenXRFeature>())
                if (feature && feature.GetType().FullName == MetaFeatureType)
                    SetDiscards(feature, false);
        }

        public static void Validate()
        {
            var settings = AndroidSettings();
            RequireDiscardsDisabled(settings);
            bool questEnabled = false;
            foreach (var feature in settings.GetFeatures<OpenXRFeature>())
            {
                if (!feature || feature.GetType().FullName != MetaFeatureType) continue;
                RequireDiscardsDisabled(feature);
                questEnabled |= feature.enabled;
            }
            if (!questEnabled)
                throw new BuildFailedException("Android Meta Quest Support must be enabled.");
            if (PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android)
                .Contains("DIWAN_EXPERIMENTAL_SHADER_STRIPPING"))
                throw new BuildFailedException("Disable DIWAN_EXPERIMENTAL_SHADER_STRIPPING for the Quest release.");
        }

        private static OpenXRSettings AndroidSettings()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (!settings) throw new BuildFailedException("Android OpenXR settings are missing.");
            return settings;
        }

        internal static void SetDiscards(Object target, bool value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty("m_optimizeBufferDiscards");
            if (property == null)
                throw new BuildFailedException("Buffer discard setting is missing on " + target.name);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void RequireDiscardsDisabled(Object target)
        {
            var property = new SerializedObject(target).FindProperty("m_optimizeBufferDiscards");
            if (property == null || property.boolValue)
                throw new BuildFailedException("Optimize Buffer Discards must be OFF on " + target.name
                    + ". Run DiwanBuilder.ConfigureSettings before rebuilding.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) Validate();
        }
    }
}
