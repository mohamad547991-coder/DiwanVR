using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DiwanVR.Editor
{
    public static class OfficeRelease040
    {
        [MenuItem("Diwan VR/0.4 - Prepare high definition office")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            DiwanBuilder.ApplyOffice040();DiwanBuilder.PolishGeometry040();ConfigureClarity();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("DIWAN_OFFICE_040_READY");
        }
        [MenuItem("Diwan VR/0.4 - Bake and build Quest APK")]
        public static void BuildQuest()
        {
            Prepare();EditorTasks.BakeLighting();EditorTasks.Validate();EditorTasks.BuildAndroid();
        }
        public static void ConfigureClarity()
        {
            AssetDatabase.Refresh();
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Diwan/Textures"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;
                string name=Path.GetFileNameWithoutExtension(path);
                bool printed=name.Contains("Letter") || name.Contains("Page") || name.Contains("Label") || name.Contains("Sign") || name.Contains("Atlas") || name.Contains("Slip") || path.Contains("Office040") || name.Contains("Desktop") || name.Contains("Legends");
                importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.mipMapBias=printed?-.25f:0;importer.maxTextureSize=printed?4096:2048;
                if(printed)
                {
                    importer.textureCompression=TextureImporterCompression.Uncompressed;
                    var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_4x4;android.compressionQuality=100;android.crunchedCompression=false;importer.SetPlatformTextureSettings(android);
                    importer.alphaIsTransparency=name=="StampImprint";
                }
                importer.SaveAndReimport();
            }
            QualitySettings.globalTextureMipmapLimit=0;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            foreach(string name in new[]{"Mobile_RPAsset","PC_RPAsset"})
            {
                var asset=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/"+name+".asset");if(!asset)continue;var so=new SerializedObject(asset);
                var p=so.FindProperty("m_RenderScale");if(p!=null)p.floatValue=name.StartsWith("Mobile")?1.1f:1.15f;
                p=so.FindProperty("m_MSAA");if(p!=null)p.intValue=4;
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
            }
            PlayerSettings.bundleVersion="0.4.1";PlayerSettings.Android.bundleVersionCode=5;AssetDatabase.SaveAssets();
        }
        [MenuItem("Diwan VR/0.4 - Render detailed previews")]
        public static void Previews()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);Directory.CreateDirectory("Validation");
            var camera=new GameObject("Office040ReviewCamera").AddComponent<Camera>();camera.nearClipPlane=.015f;camera.farClipPlane=20;camera.fieldOfView=67;camera.stereoTargetEye=StereoTargetEyeMask.None;
            Render(camera,new Vector3(2.5f,1.82f,-2.55f),new Vector3(-.45f,1.12f,1.10f),"office040_overview.png",2400,1500);
            camera.fieldOfView=55;Render(camera,new Vector3(.06f,1.40f,-.42f),new Vector3(-.18f,.85f,.45f),"office040_desk.png",2400,1500);
            Render(camera,new Vector3(-.3f,1.65f,1.12f),new Vector3(-3.1f,1.1f,1.65f),"office040_archives.png",2400,1500);
            var left=GameObject.Find("CredenzaDoorHinge0").GetComponent<Hinge>();var right=GameObject.Find("CredenzaDoorHinge1").GetComponent<Hinge>();left.SetAngle(left.openAngle);right.SetAngle(right.openAngle);
            Render(camera,new Vector3(-1.10f,1.28f,1.48f),new Vector3(-1.96f,.65f,2.88f),"office040_printer.png",2400,1500);
            camera.fieldOfView=65;Render(camera,new Vector3(-1.96f,.74f,1.40f),new Vector3(-1.96f,.40f,2.88f),"office040_storage.png",2400,1500);
            left.SetAngle(0);right.SetAngle(0);UnityEngine.Object.DestroyImmediate(camera.gameObject);EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
        }
        private static void Render(Camera camera,Vector3 position,Vector3 target,string file,int width,int height)
        {
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));var rt=new RenderTexture(width,height,24){antiAliasing=4};rt.Create();camera.targetTexture=rt;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes("Validation/"+file,tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
