using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class PlayValidation
    {
        private const float SettleTimeoutSeconds=15f;
        [Serializable] private class Check {public string name;public bool passed;public string detail;}
        [Serializable] private class Result {public string status;public List<Check> tests=new List<Check>();public bool headsetTested=false;}
        private static double start;
        private static int finishFrames;
        static PlayValidation(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Begin()
        {
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
            SessionState.SetBool("DiwanPlayValidation",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("DiwanPlayValidation",false))return;
            Application.runInBackground=true;start=Time.unscaledTime;finishFrames=0;EditorApplication.update+=Tick;
        }
        // The scene must come to rest before it is judged. Polling the bodies
        // is stable on a slow machine; a fixed delay is not.
        private static bool Settled(DiwanSession session)
        {
            if(Time.unscaledTime-start<1f)return false;
            if(!session||!session.letter)return true;
            if(Moving(session.letter))return false;
            foreach(var ledger in session.ledgers)
                if(ledger&&Moving(ledger))return false;
            return true;
        }
        private static bool Moving(Component target)
        {
            var body=target.GetComponent<Rigidbody>();
            return body&&!body.isKinematic&&body.linearVelocity.sqrMagnitude>0.0004f;
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
            if(finishFrames==0)
            {
                var session=DiwanSession.Instance;
                if(!Settled(session))
                {
                    if(Time.unscaledTime-start>SettleTimeoutSeconds)
                    {
                        var stalled=new Result();
                        stalled.tests.Add(new Check{name="Scene settles before it is judged",passed=false,detail="objects never came to rest"});
                        stalled.status="failed";
                        File.WriteAllText("Validation/play_mode_tests.json",JsonUtility.ToJson(stalled,true));
                        finishFrames=1;
                    }
                    return;
                }
                var result=new Result();
                Action<string,bool,string> check=(n,p,d)=>result.tests.Add(new Check{name=n,passed=p,detail=d});
                check("Session initialized",session!=null && !string.IsNullOrEmpty(session.report.sessionId),"");
                check("Camera at adult eye height",Camera.main && Camera.main.transform.position.y>1.4f && Camera.main.transform.position.y<1.9f,"");
                check("Letter remains on table",session && session.letter.transform.position.y>.79f && session.letter.transform.position.y<.88f,"");
                bool books=true;foreach(var b in session.ledgers)if(b.transform.position.y<.80f || b.transform.position.y>1.15f)books=false;
                check("Ledger stack survives physics",books,"");
                check("No spontaneous writing",session.ledgers[1].surface.Strokes.Count==0,"");
                check("No spontaneous punching",!session.letter.punchedCorrectly && !session.letter.mountedBinder,"");
                check("Cabinet blocks extraction",session.binders[2].IsBlockedByCabinet,"");
                bool stationeryStable=true,stationeryLocked=true,stationeryAccessible=true;int stationeryCount=0;
                var items=UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None);
                foreach(var item in items)
                    if(item.itemId.StartsWith("BLANK"))
                    {
                        stationeryCount++;stationeryStable &= item.transform.position.y>1.39f && !item.held;
                        stationeryLocked &= item.IsBlockedByStorage;
                    }
                session.cabinetDoor.SetAngle(session.cabinetDoor.openAngle);
                foreach(var item in items)if(item.itemId.StartsWith("BLANK"))stationeryAccessible &= !item.IsBlockedByStorage;
                session.cabinetDoor.SetAngle(0);
                check("Cabinet stationery remains stable",stationeryCount==7 && stationeryStable,"");
                check("Blank paper requires opening the glass door",stationeryLocked && stationeryAccessible,"");
                RaycastHit wall;
                check("Room wall blocks a body-sized movement",Physics.CapsuleCast(new Vector3(-2.5f,.22f,-2.5f),new Vector3(-2.5f,1.55f,-2.5f),.16f,Vector3.left,out wall,2f) && wall.collider.name=="LeftWall","");
                check("Baked lighting available",LightmapSettings.lightmaps.Length>0,"");
                PaperDocument blank=null;
                foreach(var document in UnityEngine.Object.FindObjectsByType<PaperDocument>(FindObjectsSortMode.None))if(document.documentId.StartsWith("BLANK_SHEET")){blank=document;break;}
                bool blankWriting=false,blankPunch=false;
                if(blank)
                {
                    var surface=blank.GetComponentInChildren<WritingSurface>();int selected=session.report.selectedLedger;
                    if(surface){surface.Touch(surface.transform.position);surface.Touch(surface.transform.position+surface.transform.right*.005f);surface.End();blankWriting=surface.Strokes.Count>0 && session.report.selectedLedger==selected;}
                    var punch=UnityEngine.Object.FindFirstObjectByType<PunchMachine>();Vector3 restPosition=blank.transform.position;Quaternion restRotation=blank.transform.rotation;
                    if(punch)
                    {
                        blank.transform.rotation=punch.feed.rotation;blank.transform.position=punch.feed.position-blank.transform.TransformVector(blank.feedReference.localPosition);
                        blankPunch=punch.FindAndPunch() && blank.punchedCorrectly && !session.letter.punchedCorrectly;
                        blank.transform.SetPositionAndRotation(restPosition,restRotation);
                    }
                }
                check("Blank paper accepts handwriting without register credit",blankWriting,"");
                check("Punch processes the sheet actually in its guide",blankPunch,"");
                session.Evaluate(false);check("Incomplete task cannot pass",!session.report.physicalTaskComplete,"");
                bool rendered=RenderProof.Capture(Camera.main,"Validation/play_mode.png",out string renderDetail);
                check("Camera renders a nonblank office image",rendered,renderDetail);
                result.status=result.tests.Exists(x=>!x.passed)?"failed":"passed";
                File.WriteAllText("Validation/play_mode_tests.json",JsonUtility.ToJson(result,true));
                finishFrames=1;
            }
            else if(++finishFrames>4)
            {
                SessionState.SetBool("DiwanPlayValidation",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            }
        }
    }
}
