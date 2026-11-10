using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace DiwanVR.Editor
{
    // Runs the real scene checks and the existing physics/office Play-mode
    // suites. A new directory and fresh timestamps keep historical results out
    // of this run. The harness serves release 0.4.2 and later.
    [InitializeOnLoad]
    public static class QuestValidation042
    {
        private const string Key="DiwanValidation042.";
        private const int StageTimeoutSeconds=300;

        [Serializable] private sealed class Check {public string name;public bool passed;public string detail;}
        [Serializable] private sealed class Result
        {
            public string sourceVersion=DiwanBuilder.SourceVersion;
            public string codeSha256,sceneSha256;
            public string runId,startedUtc,finishedUtc,status="running",detail="";
            public bool headsetTested=false;
            public List<Check> checks=new List<Check>();
            public List<string> errors=new List<string>();
            public List<string> ignoredErrors=new List<string>();
        }
        [Serializable] private sealed class Suite {public string status;}

        static QuestValidation042()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
        }

        [MenuItem("Diwan VR/0.4.4 - Test scene and interactions")]
        public static void Run(){Begin(false);}
        public static void RunAndExit(){Begin(true);}

        private static string DirectoryPath=>SessionState.GetString(Key+"directory","");
        private static string ReportPath=>Path.Combine(DirectoryPath,"result.json");
        private static Result Read()=>JsonUtility.FromJson<Result>(File.ReadAllText(ReportPath));
        private static void Write(Result result)=>File.WriteAllText(ReportPath,JsonUtility.ToJson(result,true));

        private static void Begin(bool quit)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key+"active",false))
                throw new InvalidOperationException("Stop Play mode or the current validation first.");
            var id=Guid.NewGuid().ToString("N");
            string directory=Path.Combine("Validation","run-"+id);
            Directory.CreateDirectory(directory);
            SessionState.SetString(Key+"directory",directory);
            SessionState.SetBool(Key+"quit",quit);
            Write(new Result{runId=id,startedUtc=DateTime.UtcNow.ToString("o"),codeSha256=SourceFingerprint.Code(),sceneSha256=SourceFingerprint.FileHash(DiwanBuilder.ScenePath)});
            SessionState.SetBool(Key+"active",true);
            // An automatic asset refresh in the middle of a play mode
            // transition cancels that transition. Nothing may refresh while
            // the validation runs.
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
                var result=Read();
                CheckScene(result);
                CheckPinch(result);
                Write(result);
                var start=DateTime.UtcNow;
                EditorTasks.Validate();
                Collect("scenario_tests.json",start);
                if(Read().checks.Exists(x=>!x.passed)){Finish("failed","Scene preflight failed.");return;}
                // Build/setup must complete before a test can claim readiness.
                if(!QuestHandTrackingSetup.EnsureReady())
                    throw new InvalidOperationException("Hand tracking setup is pending. Finish package import and retry.");
                StartStage(1);
            }
            catch(Exception error){Finish("failed",error.ToString());}
        }

        private static void CheckScene(Result result)
        {
            Action<string,bool,string> check=(n,p,d)=>result.checks.Add(new Check{name=n,passed=p,detail=d});
            var rig=UnityEngine.Object.FindFirstObjectByType<VRRig>();
            check("XR rig, head and both hands are connected",rig && rig.head && rig.left && rig.right,"");
            check("Camera renders both eyes",rig && rig.head && rig.head.stereoTargetEye==StereoTargetEyeMask.Both,"");
            int renderers=0;bool materials=true,meshes=true;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                renderers++;
                foreach(var material in renderer.sharedMaterials)materials &= material && material.shader;
                var filter=renderer.GetComponent<MeshFilter>();meshes &= filter && filter.sharedMesh;
            }
            check("Room contains mesh renderers",renderers>100,renderers.ToString());
            check("No missing mesh or material references",materials && meshes,"");
            int missingScripts=0;
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var child in root.GetComponentsInChildren<Transform>(true))
                    missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            check("No missing scripts, including inactive objects",missingScripts==0,missingScripts.ToString());
            if(rig)
                foreach(var hand in new[]{rig.left,rig.right})
                {
                    string handName=hand?hand.name:"hand";
                    if(!hand || !hand.visuals){check(handName+" has all 26 tracked joints",false,"no visual");continue;}
                    var ids=new HashSet<XRHandJointID>();
                    foreach(var bone in hand.visuals.GetComponentsInChildren<Transform>(true))
                    {
                        string name=bone.name;
                        if(name.StartsWith("L_")||name.StartsWith("R_"))name=name.Substring(2);
                        if(Enum.TryParse(name,true,out XRHandJointID id) && id>=XRHandJointID.Wrist && id<XRHandJointID.EndMarker)ids.Add(id);
                    }
                    check(handName+" has all 26 tracked joints",ids.Count==26,ids.Count.ToString());
                    check(handName+" has a skinned mesh",hand.visuals.GetComponentInChildren<SkinnedMeshRenderer>(true)!=null,"");
                }
            // The Quest render fix must survive in the project settings, not
            // only in the built player.
            try{QuestRenderCompatibility.Validate();check("Quest rendering keeps opaque draws (buffer discards off)",true,"");}
            catch(Exception error){check("Quest rendering keeps opaque draws (buffer discards off)",false,error.Message);}
        }

        private static void CheckPinch(Result result)
        {
            Action<string,bool> check=(n,p)=>result.checks.Add(new Check{name=n,passed=p,detail=""});
            var pinch=new TrackedPinch();
            check("Closed hand at startup cannot grab",!pinch.Update(true,.01f));
            check("Open hand arms without grabbing",!pinch.Update(true,.05f));
            check("Deliberate pinch begins grab",pinch.Update(true,.015f));
            check("Pinch tolerates threshold noise",pinch.Update(true,.028f));
            check("Opening releases grab",!pinch.Update(true,.04f));
            pinch.Update(true,.015f);
            check("Tracking loss releases grab",!pinch.Update(false,0));
            check("Closed reacquisition cannot grab",!pinch.Update(true,.01f));
            check("Invalid joint value cannot grab",!pinch.Update(true,float.NaN));
        }

        private static void StartStage(int stage)
        {
            SessionState.SetInt(Key+"stage",stage);
            SessionState.SetString(Key+"stageStart",DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(Key+"started",false);
            SessionState.SetBool(Key+"entered",false);
        }

        private static void Tick()
        {
            if(!SessionState.GetBool(Key+"active",false))return;
            int stage=SessionState.GetInt(Key+"stage",0);if(stage==0)return;
            try
            {
                var start=DateTime.Parse(SessionState.GetString(Key+"stageStart",""),null,System.Globalization.DateTimeStyles.RoundtripKind);
                double elapsed=(DateTime.UtcNow-start).TotalSeconds;
                if(elapsed>StageTimeoutSeconds)
                {
                    Finish("failed","The editor or validation stage did not finish within "+StageTimeoutSeconds+" seconds.");
                    return;
                }
                // 1. Never ask for play mode while the editor is compiling or
                // importing. A domain reload or an asset refresh during the
                // transition cancels it and the suite never reports.
                if(!SessionState.GetBool(Key+"started",false))
                {
                    if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
                    SessionState.SetBool(Key+"started",true);
                    SessionState.SetString(Key+"stageStart",DateTime.UtcNow.ToString("o"));
                    if(stage==1)PlayValidation.Begin();else if(stage==2)OfficePlay040.Begin();else FixRegression044.Begin();
                    return;
                }

                if(EditorApplication.isPlaying)SessionState.SetBool(Key+"entered",true);
                bool entered=SessionState.GetBool(Key+"entered",false);
                string report=stage==1?"play_mode_tests.json":stage==2?"office040_play_tests.json":"fix044_tests.json";

                // 2. The budget is checked before every early return, so a
                // transition that is stuck can never hang the whole run.
                if(elapsed>StageTimeoutSeconds)
                {
                    Finish("failed",entered
                        ? "The "+report+" suite did not produce a fresh report within "+StageTimeoutSeconds+" seconds."
                        : "Play mode never started within "+StageTimeoutSeconds+" seconds.");
                    return;
                }
                if(!entered)return;
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                // 3. The suite exited play mode. Its report must be fresh.
                if(!IsFresh(report,start))return;
                Collect(report,start);
                if(Read().checks.Exists(x=>!x.passed)){Finish("failed","Interaction tests reported a failure.");return;}
                if(stage<3)StartStage(stage+1);
                else Finish(Read().errors.Count==0?"passed":"failed",Read().errors.Count==0?"Desktop editor test; Quest tracking and performance are not tested.":"Product errors were logged during the run.");
            }
            catch(Exception error){Finish("failed",error.ToString());}
        }

        private static bool IsFresh(string name,DateTime started)
        {
            string path=Path.Combine("Validation",name);
            return File.Exists(path) && File.GetLastWriteTimeUtc(path)>=started;
        }

        private static void Collect(string name,DateTime started)
        {
            string path=Path.Combine("Validation",name);
            if(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<started)
                throw new InvalidOperationException("No fresh test report: "+name);
            string json=File.ReadAllText(path);
            File.WriteAllText(Path.Combine(DirectoryPath,name),json);
            var result=Read();result.checks.Add(new Check{name=name,passed=JsonUtility.FromJson<Suite>(json).status=="passed",detail=""});Write(result);
            if(name=="play_mode_tests.json" || name=="fix044_tests.json")
            {
                string image=name=="play_mode_tests.json"?"play_mode.png":"mobile_render.png";
                string source=Path.Combine("Validation",image);
                if(File.Exists(source) && File.GetLastWriteTimeUtc(source)>=started)
                    File.Copy(source,Path.Combine(DirectoryPath,image),true);
            }
        }

        private static void Log(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Key+"active",false) || (type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert))return;
            string text=message+"\n"+stack;
            var result=Read();
            var displays=new List<UnityEngine.XR.XRDisplaySubsystem>();SubsystemManager.GetSubsystems(displays);
            bool noHeadset=!displays.Exists(display=>display.running);
            if(ValidationLogPolicy.IsExpectedEditorError(message,stack,noHeadset))
            {
                if(result.ignoredErrors.Count<50){result.ignoredErrors.Add(text);Write(result);}
                return;
            }
            if(result.errors.Count<50){result.errors.Add(text);Write(result);}
        }

        private static void Finish(string status,string detail)
        {
            SessionState.SetBool(Key+"active",false);
            SessionState.SetInt(Key+"stage",0);
            var result=Read();result.status=status;result.detail=detail;result.finishedUtc=DateTime.UtcNow.ToString("o");Write(result);
            bool quit=SessionState.GetBool(Key+"quit",false);
            if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
            AssetDatabase.AllowAutoRefresh();
            Debug.Log("DIWAN_SUITE_"+status.ToUpperInvariant()+" "+Path.GetFullPath(ReportPath));
            if(quit)EditorApplication.delayCall+=()=>EditorApplication.Exit(status=="passed"?0:1);
        }
    }
}
