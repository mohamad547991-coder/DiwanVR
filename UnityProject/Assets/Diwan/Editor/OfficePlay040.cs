using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class OfficePlay040
    {
        private const float SettleTimeoutSeconds=15f;
        private const float CopyTimeoutSeconds=45f;
        private const float StampTimeoutSeconds=20f;
        [Serializable] private class Check {public string name;public bool passed;public string detail;}
        [Serializable] private class Result {public string version,status;public bool headsetTested=false;public List<Check> tests=new List<Check>();}
        private static double start;private static int stage;private static Result result;
        private static PaperDocument paper;private static Vector3 paperPosition;private static Quaternion paperRotation;
        private static OfficePrinter printer;private static OfficeStamp stamp;private static Vector3 stampPosition;private static Quaternion stampRotation;
        static OfficePlay040(){EditorApplication.playModeStateChanged+=Changed;}
        [MenuItem("Diwan VR/0.4 - Test office interactions")]
        public static void Begin(){EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);SessionState.SetBool("DiwanOfficePlay040",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("DiwanOfficePlay040",false))return;
            Application.runInBackground=true;start=Time.unscaledTime;stage=0;result=new Result{version=Application.version};EditorApplication.update+=Tick;
        }
        private static void CheckIt(string name,bool passed){result.tests.Add(new Check{name=name,passed=passed,detail=""});}
        private static bool Moving(Component target)
        {
            var body=target.GetComponent<Rigidbody>();
            return body&&!body.isKinematic&&body.linearVelocity.sqrMagnitude>0.0004f;
        }
        private static bool Settled(DiwanSession session)
        {
            if(Time.unscaledTime-start<1f)return false;
            if(!session||!session.letter)return true;
            if(Moving(session.letter))return false;
            foreach(var binder in session.binders)if(binder&&Moving(binder))return false;
            return true;
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
            double elapsed=Time.unscaledTime-start;
            try
            {
                if(stage==0)
                {
                    var session=DiwanSession.Instance;
                    if(!Settled(session))
                    {
                        if(elapsed>SettleTimeoutSeconds)
                        {
                            CheckIt("Scene settles before the office interactions are judged",false);
                            result.status="failed";Finish();
                        }
                        return;
                    }
                    CheckIt("Original mission is present",session && session.ledgers.Length==5 && session.binders.Length==8 && session.letter.documentId=="DOC001");
                    int supplies=0;bool locked=true;foreach(var grab in UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None))if(grab.itemId.StartsWith("SUPPLY_")){supplies++;locked &= grab.IsBlockedByStorage;}
                    CheckIt("Credenza contains eight physical A4 supplies",supplies==8);CheckIt("Credenza doors prevent reaching stored supplies",locked);
                    var left=GameObject.Find("CredenzaDoorHinge0").GetComponent<Hinge>();var right=GameObject.Find("CredenzaDoorHinge1").GetComponent<Hinge>();left.SetAngle(left.openAngle);right.SetAngle(right.openAngle);
                    bool accessible=true;foreach(var grab in UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None))if(grab.itemId.StartsWith("SUPPLY_"))accessible &= !grab.IsBlockedByStorage;
                    CheckIt("Opening credenza doors releases A4 access",accessible);
                    foreach(var binder in session.binders)CheckIt("Binder "+binder.index+" remains at its moved shelf anchor",Vector3.Distance(binder.transform.position,binder.shelfAnchor.position)<.01f);
                    var monitor=UnityEngine.Object.FindFirstObjectByType<PhysicalMonitor>();bool was=monitor.powered;monitor.TogglePower();CheckIt("Physical monitor button changes the display",monitor.powered!=was && monitor.display.sharedMaterial==monitor.screenOff);monitor.TogglePower();
                    foreach(var p in UnityEngine.Object.FindObjectsByType<PaperDocument>(FindObjectsSortMode.None))if(p.documentId=="SUPPLY_SHEET_00"){paper=p;break;}
                    if(!paper)throw new Exception("A4 scanner test document is missing.");
                    paperPosition=paper.transform.position;paperRotation=paper.transform.rotation;
                    printer=UnityEngine.Object.FindFirstObjectByType<OfficePrinter>();printer.scannerLid.SetAngle(0);paper.transform.SetPositionAndRotation(printer.scannerSurface.position,printer.scannerSurface.rotation*Quaternion.Euler(180,0,0));Physics.SyncTransforms();printer.Copy();CheckIt("Copy begins only with a physical sheet on scanner",printer.Busy);
                    stage=1;start=Time.unscaledTime;
                }
                else if(stage==1)
                {
                    // Wait for the printer to finish instead of assuming a duration.
                    if(printer.Busy)
                    {
                        if(elapsed>CopyTimeoutSeconds)
                        {
                            CheckIt("Printer finishes the copy within the timeout",false);
                            result.status="failed";Finish();
                        }
                        return;
                    }
                    int copies=0;PaperDocument copy=null;foreach(var p in UnityEngine.Object.FindObjectsByType<PaperDocument>(FindObjectsSortMode.None))if(p.documentId.StartsWith("COPY_")){copies++;copy=p;}
                    CheckIt("Printer creates one separate copy",copies==1 && copy && copy.documentId!=paper.documentId);
                    CheckIt("Printed copy is available for physical pickup",copy && copy.GetComponent<Grabbable>().canGrab && !copy.GetComponent<Rigidbody>().isKinematic);
                    CheckIt("Copy does not inherit punched holes",copy && !copy.punchedCorrectly && !copy.damaged);
                    CheckIt("Paper supply decreases once",printer.remainingPaper==49 && !printer.Busy);
                    paper.transform.SetPositionAndRotation(paperPosition,paperRotation);
                    stamp=UnityEngine.Object.FindFirstObjectByType<OfficeStamp>();stampPosition=stamp.transform.position;stampRotation=stamp.transform.rotation;
                    var surface=paper.frontSurface.transform;stamp.transform.rotation=surface.rotation;stamp.transform.position=surface.position-stamp.transform.TransformVector(stamp.contact.localPosition)+surface.up*.001f;stamp.GetComponent<Grabbable>().held=true;Physics.SyncTransforms();stage=2;start=Time.unscaledTime;
                }
                else if(stage==2)
                {
                    bool marked=false;foreach(Transform child in paper.frontSurface.transform)if(child.name=="DiwanStampImprint")marked=true;
                    if(!marked)
                    {
                        if(elapsed>StampTimeoutSeconds)
                        {
                            CheckIt("Rubber stamp leaves an imprint on paper by contact",false);
                            result.status="failed";Finish();
                        }
                        return;
                    }
                    CheckIt("Rubber stamp leaves an imprint on paper by contact",marked);stamp.GetComponent<Grabbable>().held=false;stamp.transform.SetPositionAndRotation(stampPosition,stampRotation);
                    CheckIt("Phone handset has physical pickup and cord",UnityEngine.Object.FindFirstObjectByType<LandlineHandset>()?.GetComponent<Grabbable>()!=null && GameObject.Find("CoiledTelephoneCord")!=null);
                    var session=DiwanSession.Instance;session.Evaluate(false);CheckIt("Added stationery cannot complete original assessment",!session.report.physicalTaskComplete && !session.letter.punchedCorrectly && session.letter.documentId=="DOC001");
                    result.status=result.tests.Exists(x=>!x.passed)?"failed":"passed";Finish();
                }
            }
            catch(Exception error){CheckIt(error.ToString(),false);result.status="failed";Debug.LogException(error);Finish();}
        }
        private static void Finish(){File.WriteAllText("Validation/office040_play_tests.json",JsonUtility.ToJson(result,true));SessionState.SetBool("DiwanOfficePlay040",false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
