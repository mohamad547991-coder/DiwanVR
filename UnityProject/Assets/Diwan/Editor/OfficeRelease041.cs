using System;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace DiwanVR.Editor
{
    public static class OfficeRelease041
    {
        [MenuItem("Diwan VR/0.4.1 - Build corrected Quest APK")]
        public static void BuildQuest()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before building.");
            DiwanBuilder.ConfigureSettings();
            // The feature was already inactive. Remove its unused list entry
            // as OpenXR's validator warns even when the pass is disabled.
            foreach(string guid in AssetDatabase.FindAssets("t:ScriptableRendererData",new[]{"Assets/Settings"}))
            {
                var data=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if(data.rendererFeatures.RemoveAll(f=>f && !f.isActive && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))>0)EditorUtility.SetDirty(data);
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(data)))
                    if(asset is ScriptableRendererFeature feature && !feature.isActive && !data.rendererFeatures.Contains(feature) && feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion") && AssetDatabase.IsSubAsset(feature))
                    {UnityEngine.Object.DestroyImmediate(feature,true);EditorUtility.SetDirty(data);}
                if(data.rendererFeatures.Count==0)
                {
                    var so=new SerializedObject(data);var map=so.FindProperty("m_RendererFeatureMap");
                    if(map!=null && map.isArray){map.arraySize=0;so.ApplyModifiedPropertiesWithoutUndo();}
                }
            }
            AssetDatabase.SaveAssets();
            EditorTasks.Validate();EditorTasks.BuildAndroid();
        }
    }
}
