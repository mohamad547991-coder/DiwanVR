using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR;
namespace DiwanVR.Editor
{
    [InitializeOnLoad]
    public static class Regression041
    {
        [Serializable] private class Check {public string name;public bool passed;public string detail;}
        [Serializable] private class Result
        {
            public string version,status,kind="Play mode regression with simulated controller poses";
            public bool headsetTested=false;
            public List<Check> tests=new List<Check>();
            public List<string> skipped=new List<string>();
            public List<string> runtimeErrors=new List<string>();
        }
        private static IEnumerator routine;private static double next;private static Result result;private static string filename;
        static Regression041(){EditorApplication.playModeStateChanged+=Changed;}
        [MenuItem("Diwan VR/Diagnostics/Run 0.4.1 regressions")]
        public static void Begin(){Start("regression041_fixed.json");}
        public static void Baseline(){Start("regression041_baseline.json");}
        private static void Start(string file)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before starting diagnostics.");
            EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);SessionState.SetString("Diwan041Run",file);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;filename=SessionState.GetString("Diwan041Run","");if(string.IsNullOrEmpty(filename))return;
            result=new Result{version=Application.version};Application.logMessageReceived+=Log;
            Application.runInBackground=true;routine=Run();next=Time.unscaledTime+2;EditorApplication.update+=Tick;
        }
        private static void Log(string condition,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)result.runtimeErrors.Add(condition);}
        private static void Tick()
        {
            if(!EditorApplication.isPlaying){Finish();return;}if(Time.unscaledTime<next)return;
            try
            {
                if(!routine.MoveNext()){Finish();return;}
                next=Time.unscaledTime+(routine.Current is double delay?delay:.03);
            }
            catch(Exception error){result.tests.Add(new Check{name="Unhandled test failure: "+error,passed=false});Finish();}
        }
        private static void CheckIt(string name,bool passed,string detail=null){result.tests.Add(new Check{name=name,passed=passed,detail=detail});}
        // A check that cannot run must be reported, never silently dropped.
        private static void SkipIt(string name,string reason){result.skipped.Add(name+": "+reason);}
        private static void Finish()
        {
            if(result==null)return;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            result.status=result.tests.Exists(t=>!t.passed)||result.runtimeErrors.Count>0?"failed":"passed";
            string json=JsonUtility.ToJson(result,true);
            File.WriteAllText("Validation/"+filename,json);
            // Keep a dated copy so a later run never overwrites the history.
            Directory.CreateDirectory("Validation/history");
            File.WriteAllText("Validation/history/"+Path.GetFileNameWithoutExtension(filename)+"_"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json",json);
            result=null;routine=null;SessionState.SetString("Diwan041Run","");EditorApplication.isPlaying=false;
        }
        private static Grabbable Item(Transform parent,string name,Vector3 position,float size=.025f)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=Vector3.one*size;
            var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.mass=.1f;var grab=go.AddComponent<Grabbable>();grab.itemId="TEST_"+name;return grab;
        }
        private static void ResetBinder(Binder binder)
        {
            binder.cover.SetAngle(binder.cover.openAngle);if(binder.ringsOpen)binder.ToggleRings();binder.cover.SetAngle(0);binder.containedPaper=null;
            binder.transform.SetPositionAndRotation(binder.shelfAnchor.position,binder.shelfAnchor.rotation);binder.GetComponent<Rigidbody>().isKinematic=true;
        }
        private static IEnumerator Run()
        {
            var session=DiwanSession.Instance;CheckIt("Training scene initializes",session && session.letter && session.ledgers.Length==5 && session.binders.Length==8);
            var rig=UnityEngine.Object.FindFirstObjectByType<VRRig>();rig.enabled=false;
            var fixture=new GameObject("Regression041Fixtures").transform;Vector3 origin=new Vector3(0,1.1f,-2.55f);
            var handObject=new GameObject("SimulatedController");handObject.transform.SetParent(fixture,false);handObject.transform.position=origin;
            var hand=handObject.AddComponent<HandInteractor>();hand.ManualPreview=true;hand.node=XRNode.LeftHand;

            var pad=GameObject.Find("InkPadLid").GetComponent<Hinge>();Quaternion openPose=pad.transform.localRotation;float originalAngle=pad.angle;
            Quaternion closedPose=openPose*Quaternion.Inverse(Quaternion.AngleAxis(originalAngle,pad.axis));pad.SetAngle(0);
            CheckIt("Saved open lid closes to its physical rest pose",Quaternion.Angle(pad.transform.localRotation,closedPose)<.25f);
            pad.SetAngle(originalAngle);

            Grabbable supply=null;foreach(var g in UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None))if(g.itemId=="SUPPLY_SHEET_00"){supply=g;break;}
            var door=supply.accessDoor;door.SetAngle(Mathf.Sign(door.openAngle)*10);
            CheckIt("A tiny door opening cannot release stored A4 paper",supply.IsBlockedByStorage);door.SetAngle(door.openAngle);CheckIt("An open storage door permits pickup",!supply.IsBlockedByStorage);door.SetAngle(0);

            Grabbable a=Item(fixture,"FirstObject",origin),b=Item(fixture,"SecondObject",origin+Vector3.right*.045f);
            bool first=hand.Grab(a),second=hand.Grab(b);
            CheckIt("One controller cannot hold two different objects",first && !second && !b.held && hand.grabbed==a);
            hand.Release();if(a.held)a.Release();if(b.held)b.Release();a.GetComponent<Rigidbody>().isKinematic=true;b.GetComponent<Rigidbody>().isKinematic=true;
            yield return .10;

            var gate=new GameObject("StorageAccessGate").AddComponent<Hinge>();gate.transform.SetParent(fixture,false);gate.transform.position=origin+Vector3.up;
            a.accessDoor=gate;var anchor=new GameObject("StorageAnchor").transform;anchor.SetParent(fixture,false);anchor.position=a.transform.position;a.storageAnchor=anchor;
            Physics.SyncTransforms();hand.GrabNearest();
            CheckIt("A blocked nearby item cannot hide an accessible object",hand.grabbed==b);
            hand.Release();a.accessDoor=null;a.storageAnchor=null;
            UnityEngine.Object.Destroy(a.gameObject);UnityEngine.Object.Destroy(b.gameObject);yield return .10;

            var loose=Item(fixture,"TrackingLossObject",origin);
            if(InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid)
                SkipIt("Tracking loss releases the object and its joint","An XR device is connected, so a simulated tracking loss cannot be staged.");
            else
            {
                hand.Grab(loose);hand.ManualPreview=false;yield return .15;
                CheckIt("Tracking loss releases the object and its joint",!loose.held && !hand.grabbed && !hand.grabbedHinge);hand.ManualPreview=true;
            }
            hand.Release();UnityEngine.Object.Destroy(loose.gameObject);yield return .10;

            var binder=session.binders[2];var other=session.binders[3];session.cabinetDoor.SetAngle(session.cabinetDoor.openAngle);
            foreach(var folder in new[]{binder,other}){folder.cover.SetAngle(folder.cover.openAngle);if(!folder.ringsOpen)folder.ToggleRings();}
            binder.transform.SetPositionAndRotation(new Vector3(.2f,.86f,.24f),Quaternion.identity);
            var page=UnityEngine.Object.Instantiate(session.letter);page.name="RegressionMountedPaper";page.documentId="REGRESSION_PAPER";page.Punch(true);page.GetComponent<Rigidbody>().isKinematic=true;
            CheckIt("An open binder accepts a prepared paper",binder.TryMount(page));
            var paperCollider=page.GetComponentInChildren<Collider>();Collider binderCollider=null;
            foreach(var c in binder.GetComponentsInChildren<Collider>())if(c.attachedRigidbody==binder.GetComponent<Rigidbody>()){binderCollider=c;break;}
            CheckIt("Mounted paper cannot collide with its own binder",paperCollider && binderCollider && Physics.GetIgnoreCollision(paperCollider,binderCollider));
            bool transferred=other.TryMount(page);CheckIt("A page cannot be claimed by two binders",!transferred && page.mountedBinder==binder && binder.containedPaper==page && other.containedPaper==null);
            other.containedPaper=null;page.mountedBinder=binder;binder.containedPaper=page;page.transform.SetParent(binder.paperMount,false);page.transform.localPosition=Vector3.zero;page.transform.localRotation=Quaternion.identity;
            page.GetComponent<Grabbable>().Restore();
            CheckIt("Recovery keeps a filed page on its binder mount",page.mountedBinder==binder && Vector3.Distance(page.transform.position,binder.paperMount.position)<.001f);
            binder.ToggleRings();CheckIt("Closed rings reject pulling out a filed page",!hand.Grab(page.GetComponent<Grabbable>()) && page.mountedBinder==binder);binder.ToggleRings();
            hand.transform.position=page.transform.position;bool taken=hand.Grab(page.GetComponent<Grabbable>());
            CheckIt("Lifting an unfastened page detaches ownership immediately",taken && page.mountedBinder==null && binder.containedPaper==null && page.transform.parent==null);
            CheckIt("Detaching a page restores collisions with its former binder",!Physics.GetIgnoreCollision(paperCollider,binderCollider));
            hand.Release();UnityEngine.Object.Destroy(page.gameObject);ResetBinder(binder);ResetBinder(other);session.cabinetDoor.SetAngle(0);yield return .1;

            var correct=UnityEngine.Object.Instantiate(session.letter);var damaged=UnityEngine.Object.Instantiate(session.letter);correct.Punch(true);damaged.Punch(false);
            Vector3[] cv=correct.frontSurface.sharedMesh.vertices,dv=damaged.frontSurface.sharedMesh.vertices;bool same=cv.Length==dv.Length;
            if(same)for(int i=0;i<cv.Length;i++)if((cv[i]-dv[i]).sqrMagnitude>.00000001f){same=false;break;}
            CheckIt("A misaligned punch does not display perfectly aligned holes",!same && damaged.damaged && !damaged.punchedCorrectly);
            UnityEngine.Object.Destroy(correct.gameObject);UnityEngine.Object.Destroy(damaged.gameObject);yield return .1;

            var printer=UnityEngine.Object.FindFirstObjectByType<OfficePrinter>();var scanPaper=supply.GetComponent<PaperDocument>();Vector3 resting=scanPaper.transform.position;Quaternion rotation=scanPaper.transform.rotation;
            int before=printer.remainingPaper;printer.scannerLid.SetAngle(0);scanPaper.transform.SetPositionAndRotation(printer.scannerSurface.position,printer.scannerSurface.rotation*Quaternion.Euler(180,0,0));Physics.SyncTransforms();printer.Copy();
            CheckIt("Scanner starts with a face-down physical page",printer.Busy);scanPaper.transform.position+=Vector3.right*.6f;Physics.SyncTransforms();
            var scanFinder=typeof(OfficePrinter).GetMethod("FindDocument",BindingFlags.Instance|BindingFlags.NonPublic);
            string trace="immediate offset="+printer.scannerSurface.InverseTransformPoint(scanPaper.transform.position)+", found="+(scanFinder.Invoke(printer,null)==scanPaper);
            yield return .25;trace+="; after .25s offset="+printer.scannerSurface.InverseTransformPoint(scanPaper.transform.position)+", found="+(scanFinder.Invoke(printer,null)==scanPaper);
            yield return 2.5;
            CheckIt("Removing the page during scanning cancels the copy",printer.remainingPaper==before && !printer.Busy,"stock "+before+" -> "+printer.remainingPaper+", busy="+printer.Busy+"; "+trace);
            scanPaper.transform.SetPositionAndRotation(resting,rotation);

            var surfaceObject=new GameObject("WritingContactFixture");surfaceObject.transform.SetParent(fixture,false);surfaceObject.transform.position=origin;
            var collider=surfaceObject.AddComponent<BoxCollider>();collider.size=new Vector3(.21f,.0015f,.297f);
            var surface=surfaceObject.AddComponent<WritingSurface>();surface.inkMaterial=session.ledgers[1].surface.inkMaterial;surface.freeForm=false;
            surface.Touch(surface.transform.TransformPoint(new Vector3(-.103f,0,0)));surface.Touch(surface.transform.TransformPoint(new Vector3(-.102f,0,0)));surface.End();
            CheckIt("Writing in the margin does not fill a register field",surface.Strokes.Count==0);
            surface.Strokes.Clear();surface.freeForm=true;
            var penObject=new GameObject("ContactPenFixture");penObject.transform.SetParent(fixture,false);penObject.AddComponent<Rigidbody>().isKinematic=true;var penGrab=penObject.AddComponent<Grabbable>();penGrab.held=true;
            var pen=penObject.AddComponent<Pen>();pen.tip=penObject.transform;penObject.transform.position=origin-Vector3.up*.004f;Physics.SyncTransforms();
            var fixedUpdate=typeof(Pen).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);fixedUpdate.Invoke(pen,null);
            CheckIt("A pen below a page cannot write through its front",surface.Strokes.Count==0);
            penObject.transform.position=origin+Vector3.up*.001f;Physics.SyncTransforms();fixedUpdate.Invoke(pen,null);penObject.transform.position+=Vector3.right*.003f;Physics.SyncTransforms();fixedUpdate.Invoke(pen,null);
            CheckIt("Front-side pen contact produces real ink strokes",surface.Strokes.Count>0);penGrab.held=false;UnityEngine.Object.Destroy(penObject);yield return .1;

            // Positive path: all six fields, physical guide alignment, automatic
            // mounting, closed rings/cover, shelving and the closed glass door.
            var ledger=session.ledgers[1];ledger.cover.SetAngle(ledger.cover.openAngle);yield return .1;
            foreach(float u in new[]{.90f,.70f,.55f,.37f,.20f,.09f})
            {
                Vector3 point=ledger.surface.transform.TransformPoint(new Vector3((u-.5f)*ledger.surface.size.x,.001f,0));
                ledger.surface.Touch(point);ledger.surface.Touch(point+ledger.surface.transform.right*.002f);ledger.surface.End();
            }
            var punch=UnityEngine.Object.FindFirstObjectByType<PunchMachine>();var original=session.letter;
            original.GetComponent<Rigidbody>().isKinematic=true;original.transform.rotation=punch.feed.rotation;
            original.transform.position=punch.feed.position-original.transform.TransformVector(original.feedReference.localPosition);Physics.SyncTransforms();
            CheckIt("Original letter can be punched at the real guide",punch.FindAndPunch() && original.punchedCorrectly && !original.damaged);
            session.cabinetDoor.SetAngle(session.cabinetDoor.openAngle);binder.transform.SetPositionAndRotation(new Vector3(.2f,.86f,.24f),Quaternion.identity);
            binder.cover.SetAngle(binder.cover.openAngle);if(!binder.ringsOpen)binder.ToggleRings();
            original.transform.SetPositionAndRotation(binder.paperMount.position,binder.paperMount.rotation);Physics.SyncTransforms();yield return .12;
            CheckIt("Releasing an aligned original page mounts it automatically",original.mountedBinder==binder && binder.containedPaper==original);
            if(binder.ringsOpen)binder.ToggleRings();binder.cover.SetAngle(0);binder.transform.SetPositionAndRotation(binder.shelfAnchor.position,binder.shelfAnchor.rotation);session.cabinetDoor.SetAngle(0);Physics.SyncTransforms();yield return .1;
            session.Evaluate(false);
            CheckIt("The complete original workflow passes physical assessment",session.report.physicalTaskComplete && session.report.selectedLedger==1 && session.report.selectedBinder==2);
            CheckIt("Handwritten data still requires instructor review",session.report.handwritingNeedsInstructorReview && session.report.instructorDataScore==-1);
            session.Evaluate(true);int events=session.report.events.Count;
            session.Record("after_end","SHOULD_NOT_BE_ADDED");CheckIt("Ending an attempt stops further event recording",session.report.events.Count==events);
            session.Evaluate(false);CheckIt("Periodic review cannot reopen a finished attempt",session.report.ended);
            int selection=session.report.selectedLedger;session.ledgers[1].surface.Touch(session.ledgers[1].surface.transform.position);session.ledgers[1].surface.Touch(session.ledgers[1].surface.transform.position+Vector3.right*.004f);session.ledgers[1].surface.End();
            CheckIt("Writing after an ended attempt cannot alter its report",session.report.selectedLedger==selection);
            int savedPoints=session.report.handwriting[0].points.Count;ledger.surface.Strokes[0].points.Add(Vector2.one);
            CheckIt("Final handwriting is an independent immutable snapshot",session.report.handwriting[0].points.Count==savedPoints);
            binder.cover.SetAngle(binder.cover.openAngle);session.Evaluate(false);
            CheckIt("Moving props after ending cannot rewrite the final assessment",session.report.ended && session.report.physicalTaskComplete);
            session.ReviewHandwriting(20);
            CheckIt("Instructor can review handwriting without reopening an attempt",session.report.ended && !session.report.handwritingNeedsInstructorReview && session.report.instructorDataScore==20 && session.report.physicalTaskComplete);
            CheckIt("Final report is actually saved to disk",!string.IsNullOrEmpty(session.LastSavePath) && File.Exists(session.LastSavePath) && JsonUtility.FromJson<SessionReport>(File.ReadAllText(session.LastSavePath)).ended);
            UnityEngine.Object.Destroy(fixture.gameObject);yield return .1;
        }
    }
}
