using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class EditorTasks
    {
        [Serializable] private class Request { public string action; }
        [Serializable] private class TestRecord { public string test; public bool passed; public string detail; }
        [Serializable] private class TestResult { public string status; public List<TestRecord> tests=new List<TestRecord>(); }
        [Serializable] private class BuildMessage { public string severity; public string message; }
        [Serializable] private class BuildMessages { public List<BuildMessage> messages=new List<BuildMessage>(); }
        private static bool busy;
        private static double nextPoll;
        static EditorTasks()
        {
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/editor_code_version.txt",DiwanBuilder.SourceVersion);
            EditorApplication.update+=Poll;
        }
        private static void Poll()
        {
            if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorUtility.scriptCompilationFailed||EditorApplication.timeSinceStartup<nextPoll)return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            string directory="TaskRequests";if(!Directory.Exists(directory))return;
            var requests=Directory.GetFiles(directory,"*.json");Array.Sort(requests,StringComparer.Ordinal);
            if(requests.Length==0)return;
            string path=requests[0];Request request;
            try{request=JsonUtility.FromJson<Request>(File.ReadAllText(path));}catch{return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode && request.action!="stop" && request.action!="devices")return;
            File.Move(path,path+".processing");busy=true;
            try
            {
                if(request.action=="baseline041")Regression041.Baseline();
                else if(request.action=="regression041")Regression041.Begin();
                else if(request.action=="build_release041")OfficeRelease041.BuildQuest();
                else if(request.action=="build_release042")OfficeRelease042.BuildQuest();
                else if(request.action=="validate042")QuestValidation042.Run();
                else if(request.action=="upgrade040")OfficeRelease040.Prepare();
                else if(request.action=="polish040")DiwanBuilder.PolishGeometry040();
                else if(request.action=="play040")OfficePlay040.Begin();
                else if(request.action=="preview040")OfficeRelease040.Previews();
                else if(request.action=="build_release040")OfficeRelease040.BuildQuest();
                else if(request.action=="bootstrap") { DiwanBuilder.Bootstrap(); RenderPreviews(); }
                else if(request.action=="preview")RenderPreviews();
                else if(request.action=="validate")Validate();
                else if(request.action=="build_android")BuildAndroid();
                else if(request.action=="build_windows")BuildWindows();
                else if(request.action=="play") { EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);EditorApplication.isPlaying=true; }
                else if(request.action=="stop")EditorApplication.isPlaying=false;
                else if(request.action=="devices")DeviceProbe();
                else if(request.action=="configure") { DiwanBuilder.ConfigureSettings();AssetDatabase.SaveAssets();File.WriteAllText("Validation/configured.txt",DateTime.UtcNow.ToString("o")); }
                else if(request.action=="bake")BakeLighting();
                else if(request.action=="interaction_preview")InteractionPreview();
                else if(request.action=="repair_rings")DiwanBuilder.RepairRingGeometry();
                else if(request.action=="play_validate")PlayValidation.Begin();
                else if(request.action=="upgrade030")DiwanBuilder.ApplyProfessionalOffice030();
                else if(request.action=="build_release030")OfficeRelease030.BuildQuest();
                else throw new InvalidOperationException("Unknown editor task: "+request.action);
                File.Move(path+".processing",path+".done");
            }
            catch(Exception ex)
            {
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/"+request.action+"_error.txt",ex.ToString());Debug.LogException(ex);
                File.Move(path+".processing",path+".failed");
            }
            finally{busy=false;}
        }
        [MenuItem("Diwan VR/Render scene previews")]
        public static void RenderPreviews()
        {
            if(!File.Exists(DiwanBuilder.ScenePath))return;
            Directory.CreateDirectory("Validation");
            var hands=UnityEngine.Object.FindObjectsByType<HandInteractor>(FindObjectsSortMode.None);
            foreach(var h in hands)if(h.visuals)h.visuals.gameObject.SetActive(false);
            var camera=new GameObject("ValidationPreviewCamera").AddComponent<Camera>();camera.nearClipPlane=.02f;camera.farClipPlane=20;camera.fieldOfView=65;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.59f,.64f,.68f);camera.stereoTargetEye=StereoTargetEyeMask.None;
            camera.fieldOfView=75;
            Render(camera,new Vector3(2.72f,2.05f,-2.55f),new Vector3(-.58f,1.12f,.87f),"room_preview.png");
            camera.fieldOfView=65;
            Render(camera,new Vector3(0,1.37f,-.32f),new Vector3(0,.81f,.28f),"desk_preview.png");
            Render(camera,new Vector3(-1.75f,1.47f,-.30f),new Vector3(-3.20f,1.22f,.59f),"cabinet_preview.png");
            camera.fieldOfView=60;
            Render(camera,new Vector3(.35f,1.64f,1.20f),new Vector3(1.61f,1.30f,2.985f),"library_preview.png");
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach(var h in hands)if(h.visuals)h.visuals.gameObject.SetActive(true);
            File.WriteAllText("Validation/preview_complete.txt",DateTime.UtcNow.ToString("o"));
        }
        private static void Render(Camera c,Vector3 pos,Vector3 look,string name)
        {
            c.transform.position=pos;c.transform.LookAt(look);
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);c.targetTexture=rt;
            c.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine("Validation",name),image.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        [MenuItem("Diwan VR/Validate scenario")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            var result=new TestResult();
            Action<string,bool,string> check=(name,pass,detail)=>result.tests.Add(new TestRecord {test=name,passed=pass,detail=detail});
            var session=UnityEngine.Object.FindFirstObjectByType<DiwanSession>();
            check("Five distinct ledgers",session && session.ledgers.Length==5,"Expected 5");
            check("Eight distinct binders",session && session.binders.Length==8,"Expected 8");
            check("No floating UI",UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length==0,"No Canvas in trainee scene");
            check("Door starts closed",session && session.cabinetDoor.IsClosed,"Glass blocks direct access");
            check("Correct register",session && session.ledgers[1].arabicName=="سجل الرسائل الواردة","Exact title");
            check("Correct binder",session && session.binders[2].arabicName=="مصنف الرسائل الواردة","Exact title");
            var floor=GameObject.Find("Subfloor");
            check("Expanded office floor",floor && floor.GetComponent<BoxCollider>().size.x>=7 && floor.GetComponent<BoxCollider>().size.z>=6.4f,"At least twice the original floor area");
            check("Reference bookcase present",GameObject.Find("AdministrativeBookcase")!=null,"Physical wooden bookcase");
            check("Computer added",UnityEngine.Object.FindFirstObjectByType<PhysicalMonitor>()!=null,"A physical monitor with a power key");
            check("Printer added",UnityEngine.Object.FindFirstObjectByType<OfficePrinter>()!=null,"Scanner lid, output tray and physical controls");
            check("Office seating upgraded",GameObject.Find("ErgonomicEmployeeChair") && GameObject.Find("OfficeChair") && GameObject.Find("WaitingBench"),"Employee, visitor and waiting seats");
            var whiteSheets=UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None);
            int reams=0,sheets=0;bool paperLocked=true;
            foreach(var g in whiteSheets)
            {
                if(g.itemId.StartsWith("BLANK_REAM"))reams++;
                if(g.itemId.StartsWith("BLANK_SHEET"))sheets++;
                if(g.itemId.StartsWith("BLANK"))paperLocked &= g.IsBlockedByStorage && !g.TryGrab(null);
            }
            check("White stationery in glass cabinet",reams==3 && sheets==4,"Three paper reams and four separate blank sheets");
            check("Closed door blocks blank stationery",paperLocked,"No grabbing through closed glazing");
            if(session)
            {
                var binder=session.binders[2];var paper=session.letter;
                check("Unpunched paper rejected",!binder.TryMount(paper),"Cannot file an unpunched letter");
                session.cabinetDoor.SetAngle(session.cabinetDoor.openAngle);binder.transform.SetPositionAndRotation(new Vector3(-.2f,.84f,.1f),Quaternion.identity);
                binder.cover.SetAngle(binder.cover.openAngle);binder.ringsOpen=true;
                check("Open rings still require punching",!binder.TryMount(paper),"No snap-through before punching");
                paper.Punch(true);
                check("Hole state applied",paper.punchedCorrectly && !paper.damaged,"Two-hole punch");
                check("Prepared paper mounts",binder.TryMount(paper),"Compatible open folder accepts the prepared letter");
                check("Mounted identity preserved",paper.documentId=="DOC001" && binder.containedPaper==paper,"The original letter remains the same document");
                binder.ToggleRings();
                check("Closed rings fasten paper",!paper.GetComponent<Grabbable>().canGrab,"No pull-through closed metal rings");
                binder.ToggleRings();
                check("Reopened rings permit removal",paper.GetComponent<Grabbable>().canGrab,"Physical state updates after reopening");
                check("Field routing",WritingSurface.GetField(.92f)==0 && WritingSurface.GetField(.4f)==3,"Right-to-left record fields");
                var writing=session.ledgers[1].surface;
                writing.Touch(writing.transform.TransformPoint(new Vector3(.01f,0,0)));writing.End();
                string json=JsonUtility.ToJson(new SessionReport { handwriting=writing.Strokes });
                check("Handwriting survives JSON serialization",JsonUtility.FromJson<SessionReport>(json).handwriting.Count>0,"No OCR assumed");
            }
            result.status=result.tests.Exists(x=>!x.passed)?"failed":"passed";
            File.WriteAllText("Validation/scenario_tests.json",JsonUtility.ToJson(result,true));
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            Debug.Log("DIWAN_VALIDATION_"+result.status.ToUpperInvariant());
        }
        [MenuItem("Diwan VR/Build Quest APK")]
        public static void BuildAndroid()
        {
            Directory.CreateDirectory("Builds/Quest");
            Directory.CreateDirectory("TaskRequests");
            EditorUserBuildSettings.selectedBuildTargetGroup=BuildTargetGroup.Android;
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
            {
                // The actual build is queued again after the domain reload from the platform switch.
                if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android platform switch failed. Install Android Build Support for this Unity editor.");
                File.WriteAllText("TaskRequests/20_android_after_switch.json","{\"action\":\"build_android\"}");return;
            }
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{DiwanBuilder.ScenePath},locationPathName="Builds/Quest/DiwanVR.apk",target=BuildTarget.Android,options=BuildOptions.None});
            SaveBuild(r,"android");
        }
        [MenuItem("Diwan VR/Build Windows preview")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{DiwanBuilder.ScenePath},locationPathName="Builds/Windows/DiwanVR.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});SaveBuild(r,"windows");
        }
        private static void SaveBuild(BuildReport r,string kind)
        {
            string json="{\"result\":\""+r.summary.result+"\",\"errors\":"+r.summary.totalErrors+",\"warnings\":"+r.summary.totalWarnings+",\"bytes\":"+r.summary.totalSize+",\"version\":\""+PlayerSettings.bundleVersion+"\",\"versionCode\":"+PlayerSettings.Android.bundleVersionCode+",\"builtUtc\":\""+DateTime.UtcNow.ToString("o")+"\"}";
            File.WriteAllText("Validation/"+kind+"_build.json",json);
            var details=new BuildMessages();
            foreach(var step in r.steps)foreach(var message in step.messages)
                if(message.type==LogType.Warning || message.type==LogType.Error || message.type==LogType.Exception)
                    details.messages.Add(new BuildMessage {severity=message.type.ToString(),message=message.content});
            File.WriteAllText("Validation/"+kind+"_build_messages.json",JsonUtility.ToJson(details,true));
            if(r.summary.result!=BuildResult.Succeeded)throw new Exception(kind+" build failed; inspect Unity Console.");
        }
        private static void DeviceProbe()
        {
            string exe=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");
            var info=new System.Diagnostics.ProcessStartInfo(exe,"devices"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var p=System.Diagnostics.Process.Start(info)){string output=p.StandardOutput.ReadToEnd();p.WaitForExit(15000);File.WriteAllText("Validation/adb_devices.txt",output);}
        }
        public static void BakeLighting()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            // Procedural boxes share tiled material UVs across their faces.
            // Baked lighting needs separate, non-overlapping charts in UV2.
            var meshes=new HashSet<Mesh>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(!GameObjectUtility.AreStaticEditorFlagsSet(filter.gameObject,StaticEditorFlags.ContributeGI))continue;
                var mesh=filter.sharedMesh;if(!mesh)continue;
                if(filter.name=="PorcelainTile")filter.GetComponent<MeshRenderer>().scaleInLightmap=2;
                if(!meshes.Add(mesh))continue;
                if(mesh.uv2.Length==0)Unwrapping.GenerateSecondaryUVSet(mesh);
                if(mesh.uv.Length==mesh.vertexCount)mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
            }
            AssetDatabase.SaveAssets();
            var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>("Assets/Diwan/OfficeLighting.asset");
            if(!settings){settings=new LightingSettings();AssetDatabase.CreateAsset(settings,"Assets/Diwan/OfficeLighting.asset");}
            settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;settings.lightmapResolution=32;settings.lightmapMaxSize=1024;settings.directSampleCount=48;settings.indirectSampleCount=192;settings.environmentSampleCount=96;
            settings.ao=true;settings.aoMaxDistance=.25f;settings.aoExponentIndirect=.6f;settings.aoExponentDirect=0;
            Lightmapping.lightingSettings=settings;
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.lightmapBakeType=light.type==LightType.Directional?LightmapBakeType.Mixed:LightmapBakeType.Baked;
            bool result=Lightmapping.Bake();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Assets/Diwan/Reflections");
            foreach(var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
                if(probe.mode==UnityEngine.Rendering.ReflectionProbeMode.Baked)Lightmapping.BakeReflectionProbe(probe,"Assets/Diwan/Reflections/OfficeReflection.exr");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            File.WriteAllText("Validation/lighting_bake.json","{\"baked\":"+result.ToString().ToLowerInvariant()+"}");
            RenderPreviews();
        }
        private static void InteractionPreview()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            var session=UnityEngine.Object.FindFirstObjectByType<DiwanSession>();var binder=session.binders[2];
            binder.transform.SetPositionAndRotation(new Vector3(-.25f,.837f,.15f),Quaternion.identity);binder.cover.SetAngle(binder.cover.openAngle);binder.ToggleRings();
            session.ledgers[1].transform.SetPositionAndRotation(new Vector3(.50f,.84f,.18f),Quaternion.identity);session.ledgers[1].cover.SetAngle(170);
            var c=new GameObject("InteractionPreviewCamera").AddComponent<Camera>();c.nearClipPlane=.02f;c.farClipPlane=20;c.fieldOfView=60;c.stereoTargetEye=StereoTargetEyeMask.None;
            Render(c,new Vector3(-.10f,1.39f,-.35f),new Vector3(-.12f,.83f,.18f),"interaction_preview.png");UnityEngine.Object.DestroyImmediate(c.gameObject);
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
        }
    }
}
