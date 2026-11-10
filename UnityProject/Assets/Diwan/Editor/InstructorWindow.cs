using UnityEditor;
using UnityEngine;

namespace DiwanVR.Editor
{
    public sealed class InstructorWindow : EditorWindow
    {
        private Vector2 scroll;
        [MenuItem("Diwan VR/Instructor console")]
        public static void Open(){GetWindow<InstructorWindow>("Diwan Instructor");}
        private void OnGUI()
        {
            GUILayout.Label("Diwan training control",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This console is an Editor window. It is never rendered by the trainee camera or in the headset.",MessageType.Info);
            if(!Application.isPlaying)
            {
                if(GUILayout.Button("Open training room"))UnityEditor.SceneManagement.EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);
                if(GUILayout.Button("Start training")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(DiwanBuilder.ScenePath);EditorApplication.isPlaying=true;}
                return;
            }
            var session=DiwanSession.Instance;if(!session)return;
            if(GUILayout.Button("Review current physical state"))session.Evaluate(false);
            if(GUILayout.Button("End and save attempt"))session.Evaluate(true);
            EditorGUILayout.Space();
            GUILayout.Label("Handwriting review",EditorStyles.boldLabel);
            GUILayout.Label("Compare handwritten fields with the physical letter. No automatic OCR score is assumed.",EditorStyles.wordWrappedLabel);
            int score=session.report.instructorDataScore;
            score=EditorGUILayout.IntSlider("Data entry score",Mathf.Max(0,score),0,30);
            if(GUILayout.Button("Confirm reviewed data score"))session.ReviewHandwriting(score);
            GUILayout.Label("Attempt ended: "+session.report.ended);
            GUILayout.Label("Ledger written: "+session.report.selectedLedger);
            GUILayout.Label("Binder used: "+session.report.selectedBinder);
            GUILayout.Label("Physical task complete: "+session.report.physicalTaskComplete);
            GUILayout.Label("Handwriting awaiting review: "+session.report.handwritingNeedsInstructorReview);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(string issue in session.report.issuesArabic)EditorGUILayout.SelectableLabel(issue,EditorStyles.wordWrappedLabel,GUILayout.MinHeight(36));
            GUILayout.Label("Recorded events: "+session.report.events.Count);
            if(!string.IsNullOrEmpty(session.LastSavePath))EditorGUILayout.SelectableLabel(session.LastSavePath,EditorStyles.wordWrappedLabel,GUILayout.MinHeight(45));
            EditorGUILayout.EndScrollView();
            if(GUILayout.Button("Stop play mode"))EditorApplication.isPlaying=false;
        }
        private void Update(){if(EditorApplication.isPlaying)Repaint();}
    }
}
