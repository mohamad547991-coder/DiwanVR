using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class FixRegression044
    {
        [Serializable] private sealed class Check{public string name,detail;public bool passed;}
        [Serializable] private sealed class Result{public string status;public bool headsetTested=false;public List<Check> tests=new List<Check>();}
        private static IEnumerator routine;
        private static Result result;
        private static int lastFrame;
        private static RenderPipelineAsset originalPipeline;
        private static GameObject fixture;
        static FixRegression044(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Begin()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            SessionState.SetBool("DiwanFix044",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("DiwanFix044",false))return;
            result=new Result();originalPipeline=QualitySettings.renderPipeline;
            routine=Run();lastFrame=-1;EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying){CheckIt("Suite completes before play mode stops",false);Finish();return;}
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try{if(!routine.MoveNext())Finish();}
            catch(Exception error){CheckIt("Unhandled regression failure",false,error.ToString());Finish();}
        }
        private static void CheckIt(string name,bool passed,string detail="")=>result.tests.Add(new Check{name=name,passed=passed,detail=detail});
        private static IEnumerator Run()
        {
            yield return null;
            CheckIt("Missing scripts fail validation",!ValidationLogPolicy.IsExpectedEditorError("The referenced script (Unknown) on this Behaviour is missing!","",true));
            CheckIt("Real XR runtime errors fail validation",!ValidationLogPolicy.IsExpectedEditorError("OpenXR Diagnostic: XR_ERROR_RUNTIME_FAILURE","",true));
            CheckIt("Mixed XR errors cannot be hidden",!ValidationLogPolicy.IsExpectedEditorError("XR_ERROR_FORM_FACTOR_UNAVAILABLE XR_ERROR_RUNTIME_FAILURE","",true));
            CheckIt("No-headset exception is desktop-only",ValidationLogPolicy.IsExpectedEditorError("xrGetSystem: XR_ERROR_FORM_FACTOR_UNAVAILABLE","",true)
                && !ValidationLogPolicy.IsExpectedEditorError("xrGetSystem: XR_ERROR_FORM_FACTOR_UNAVAILABLE","",false));
            CheckIt("Product exceptions are never editor noise",!ValidationLogPolicy.IsExpectedEditorError("ArgumentOutOfRangeException: Index was out of range","DiwanVR.Training\nUnityEditor.Search.SearchDatabase",true));
            foreach(var color in new[]{Color.white,Color.cyan,Color.magenta,Color.black})
            {
                var pixels=new Color32[256];for(int i=0;i<pixels.Length;i++)pixels[i]=color;
                CheckIt("Reject empty render "+color,!RenderProof.HasSceneDetail(pixels,out _));
            }
            bool released=true,replaced=true;
            for(int i=0;i<6;i++)
            {
                fixture=new GameObject("MeshLifetimeFixture");var doc=fixture.AddComponent<PaperDocument>();doc.enabled=false;
                var front=new GameObject("front");front.transform.SetParent(fixture.transform);doc.frontSurface=front.AddComponent<MeshFilter>();
                var back=new GameObject("back");back.transform.SetParent(fixture.transform);doc.backSurface=back.AddComponent<MeshFilter>();
                var plain=new Mesh{name="Test photocopy"};doc.SetGeneratedMeshes(plain,plain);
                doc.Punch(true);Mesh a=doc.frontSurface.sharedMesh,b=doc.backSurface.sharedMesh;
                yield return null;replaced &= !plain;
                UnityEngine.Object.Destroy(fixture);fixture=null;
                yield return null;released &= !a && !b;
            }
            CheckIt("Punching releases the old shared photocopy mesh",replaced);
            CheckIt("Deleting six punched sheets releases all twelve generated meshes",released);
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if(!mobile){CheckIt("Mobile render pipeline exists",false);yield break;}
            QualitySettings.renderPipeline=mobile;
            yield return null;yield return null;
            bool rendered=RenderProof.Capture(Camera.main,"Validation/mobile_render.png",out string detail);
            CheckIt("Quest quality pipeline renders a nonblank office in the editor",rendered,detail);
        }
        private static void Finish()
        {
            EditorApplication.update-=Tick;QualitySettings.renderPipeline=originalPipeline;
            if(fixture)UnityEngine.Object.DestroyImmediate(fixture);
            result.status=result.tests.Exists(c=>!c.passed)?"failed":"passed";
            File.WriteAllText("Validation/fix044_tests.json",JsonUtility.ToJson(result,true));
            SessionState.SetBool("DiwanFix044",false);routine=null;EditorApplication.isPlaying=false;
        }
    }
}
