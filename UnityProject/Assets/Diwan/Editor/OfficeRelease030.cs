using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class OfficeRelease030
    {
        private static bool pending;
        static OfficeRelease030()
        {
            EditorSceneManager.sceneOpened+=(scene,mode)=>{if(scene.path==DiwanBuilder.ScenePath)QueueUpgrade();};
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)QueueUpgrade();};
            QueueUpgrade();
        }
        private static void QueueUpgrade()
        {
            if(pending)return;pending=true;EditorApplication.delayCall+=TryUpgrade;
        }
        private static void TryUpgrade()
        {
            pending=false;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating){QueueUpgrade();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=DiwanBuilder.ScenePath)return;
            if(GameObject.Find(DiwanBuilder.Hardware030Marker))return;
            try { DiwanBuilder.ApplyProfessionalOffice030();ApplyQuality(1f);Debug.Log("DIWAN_OFFICE_030_APPLIED: computer, printer and seating are ready; bake lighting before final headset use."); }
            catch(Exception error){Debug.LogException(error);}
        }
        [MenuItem("Diwan VR/0.3 - Prepare scene and lighting")]
        public static void PrepareScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before preparing the office.");
            if(SceneManager.GetActiveScene().path!=DiwanBuilder.ScenePath)EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            if(!GameObject.Find(DiwanBuilder.Hardware030Marker))DiwanBuilder.ApplyProfessionalOffice030();
            ApplyQuality(1f);DiwanBuilder.ConfigureSettings();EditorTasks.BakeLighting();EditorTasks.Validate();
            Debug.Log("DIWAN_OFFICE_030_PREPARED");
        }
        [MenuItem("Diwan VR/0.3 - Bake and build Quest APK")]
        public static void BuildQuest()
        {
            PrepareScene();
            // Unity requires its installed Android module for this step.
            EditorTasks.BuildAndroid();
        }
        public static void BuildQuestBatch()
        {
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Launch the batch build with -buildTarget Android.");
            BuildQuest();
        }
        [MenuItem("Diwan VR/Graphics/Quest balanced - native resolution")]
        public static void Balanced(){ApplyQuality(1f);}
        [MenuItem("Diwan VR/Graphics/Quest high - test performance on headset")]
        public static void High(){ApplyQuality(1.1f);}
        private static void ApplyQuality(float scale)
        {
            foreach(string name in new[]{"Mobile_RPAsset","PC_RPAsset"})
            {
                var asset=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/"+name+".asset");if(!asset)continue;
                var settings=new SerializedObject(asset);
                SetInt(settings,"m_MSAA",4);SetFloat(settings,"m_RenderScale",name.StartsWith("Mobile")?scale:1.1f);
                SetInt(settings,"m_MainLightShadowmapResolution",2048);SetInt(settings,"m_ShadowCascadeCount",2);SetFloat(settings,"m_ShadowDistance",12);
                SetBool(settings,"m_ReflectionProbeBlending",false);SetBool(settings,"m_ReflectionProbeBoxProjection",true);SetBool(settings,"m_SoftShadowsSupported",true);
                settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
            }
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            AssetDatabase.SaveAssets();
        }
        private static void SetInt(SerializedObject s,string n,int v){var p=s.FindProperty(n);if(p!=null)p.intValue=v;}
        private static void SetFloat(SerializedObject s,string n,float v){var p=s.FindProperty(n);if(p!=null)p.floatValue=v;}
        private static void SetBool(SerializedObject s,string n,bool v){var p=s.FindProperty(n);if(p!=null)p.boolValue=v;}
    }
}
