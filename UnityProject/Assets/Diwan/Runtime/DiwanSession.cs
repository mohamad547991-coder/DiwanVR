using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DiwanVR
{
    [Serializable] public class Evidence { public string action; public string subject; public float time; }
    [Serializable] public class InkStroke { public int field; public List<Vector2> points = new List<Vector2>(); }
    [Serializable] public class SessionReport
    {
        public string sessionId;
        public string startedUtc;
        public string scenario = "القيود والسجلات";
        public string buildVersion = "0.4.1";
        public bool ended;
        public bool physicalTaskComplete;
        public bool handwritingNeedsInstructorReview;
        public string resultArabic;
        public List<string> issuesArabic = new List<string>();
        public List<Evidence> events = new List<Evidence>();
        public List<InkStroke> handwriting = new List<InkStroke>();
        public int selectedLedger = -1;
        public int selectedBinder = -1;
        public int instructorDataScore = -1;
    }
    public sealed class DiwanSession : MonoBehaviour
    {
        public static DiwanSession Instance { get; private set; }
        public LedgerBook[] ledgers;
        public Binder[] binders;
        public PaperDocument letter;
        public Hinge cabinetDoor;
        public Vector3 shelfLocalPosition;
        public SessionReport report = new SessionReport();
        private float nextSave;
        public string LastSavePath { get; private set; }
        private void Awake()
        {
            Instance = this;
            report = new SessionReport();
            report.sessionId = Guid.NewGuid().ToString("N");
            report.startedUtc = DateTime.UtcNow.ToString("o");
            report.buildVersion = Application.version;
        }
        private void Update() { if (!report.ended && Time.unscaledTime > nextSave) { nextSave=Time.unscaledTime+10; Evaluate(false); } }
        public void Record(string action, string subject)
        {
            if (report == null || report.ended) return;
            report.events.Add(new Evidence { action=action, subject=subject, time=Time.time });
        }
        public void Evaluate(bool end)
        {
            if(report.ended)return;
            report.issuesArabic.Clear();
            report.handwritingNeedsInstructorReview=false;report.handwriting.Clear();
            int bestLedger = -1;
            for(int i=0;i<ledgers.Length;i++) if(ledgers[i] && ledgers[i].surface && ledgers[i].surface.Strokes.Count>0) { if(bestLedger<0 || ledgers[i].surface.Strokes.Count>ledgers[bestLedger].surface.Strokes.Count) bestLedger=i; }
            report.selectedLedger=bestLedger;
            if(bestLedger!=1) report.issuesArabic.Add("لم يسجل الكتاب في سجل الرسائل الواردة.");
            if(bestLedger>=0)
            {
                foreach(var stroke in ledgers[bestLedger].surface.Strokes)
                    report.handwriting.Add(new InkStroke{field=stroke.field,points=new List<Vector2>(stroke.points)});
                report.handwritingNeedsInstructorReview=report.handwriting.Count>0 && report.instructorDataScore<0;
                var fields=new HashSet<int>();
                foreach(var stroke in report.handwriting)if(stroke.points.Count>1)fields.Add(stroke.field);
                if(fields.Count<6)report.issuesArabic.Add("توجد حقول في سطر القيد لم يكتب فيها المتدرب.");
            }
            else report.issuesArabic.Add("لا توجد بيانات مكتوبة بالقلم على السجل.");
            report.selectedBinder=letter && letter.mountedBinder ? letter.mountedBinder.index : -1;
            if(report.selectedBinder!=2) report.issuesArabic.Add("لم يحفظ الكتاب في مصنف الرسائل الواردة.");
            if(letter && !letter.punchedCorrectly) report.issuesArabic.Add("الكتاب غير مثقوب بثقبين صحيحين.");
            if(letter && letter.damaged) report.issuesArabic.Add("أصابت أداة الثقب متن الكتاب أو الحافة غير المناسبة.");
            Binder target=letter ? letter.mountedBinder : null;
            if(target)
            {
                if(target.ringsOpen) report.issuesArabic.Add("العلاقة المعدنية لا تزال مفتوحة.");
                if(target.cover && !target.cover.IsClosed) report.issuesArabic.Add("غلاف المصنف لا يزال مفتوحاً.");
                if(!target.InShelfPosition) report.issuesArabic.Add("المصنف لم يعد إلى مكانه على الرف.");
            }
            if(cabinetDoor && !cabinetDoor.IsClosed) report.issuesArabic.Add("باب الخزانة لا يزال مفتوحاً.");
            report.physicalTaskComplete=report.issuesArabic.Count==0;
            report.ended=end;
            if(end)foreach(var ledger in ledgers)if(ledger && ledger.surface)ledger.surface.End();
            UpdateResult();
            Save(false);
        }
        public void ReviewHandwriting(int score)
        {
            report.instructorDataScore=Mathf.Clamp(score,0,30);
            if(!report.ended){Evaluate(false);return;}
            report.handwritingNeedsInstructorReview=false;UpdateResult();Save(false);
        }
        private void UpdateResult()
        {
            report.resultArabic=report.physicalTaskComplete
                ?(report.handwritingNeedsInstructorReview?"اكتمل الإجراء العملي وينتظر تدقيق بيانات السجل من المدرب.":"اكتمل الإجراء العملي. درجة بيانات السجل: "+report.instructorDataScore+"/30.")
                :"المحاولة غير مكتملة. تُراجع الملاحظات خارج عرض المتدرب.";
        }
        public void Save(bool evaluate)
        {
            if(evaluate) { Evaluate(false); return; }
            if(string.IsNullOrEmpty(report.sessionId)) return;
            string dir=Path.Combine(Application.persistentDataPath,"Sessions");
            Directory.CreateDirectory(dir);
            LastSavePath=Path.Combine(dir,report.sessionId+".json");
            string temp=LastSavePath+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(report,true));
            if(File.Exists(LastSavePath)) File.Delete(LastSavePath);
            File.Move(temp,LastSavePath);
        }
        private void OnApplicationPause(bool paused) { if(paused) Save(false); }
        private void OnApplicationQuit() { Evaluate(true); }
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
