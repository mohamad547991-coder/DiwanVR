using System.Collections;
using UnityEngine;

namespace DiwanVR
{
    public sealed class PunchMachine : MonoBehaviour
    {
        public Transform feed;
        public Transform lever;
        public AudioClip punchSound;
        public bool cycling;
        public void Press(HandInteractor hand)
        {
            if(!cycling)StartCoroutine(Cycle(hand));
        }
        public bool FindAndPunch()
        {
            PaperDocument nearest=null;float best=float.PositiveInfinity;
            foreach(var paper in FindObjectsByType<PaperDocument>())
            {
                if(!paper.feedReference || paper.punchedCorrectly || paper.damaged || paper.mountedBinder)continue;
                Vector3 point=feed.InverseTransformPoint(paper.feedReference.position);
                if(Mathf.Abs(point.x)>.025f || Mathf.Abs(point.z)>.02f || Mathf.Abs(point.y)>.018f)continue;
                if(point.sqrMagnitude<best){nearest=paper;best=point.sqrMagnitude;}
            }
            if(!nearest)return false;
            Vector3 alignment=feed.InverseTransformPoint(nearest.feedReference.position);
            bool correct=Quaternion.Angle(nearest.transform.rotation,feed.rotation)<5 && Mathf.Abs(alignment.x)<.003f && Mathf.Abs(alignment.z)<.003f;
            Vector3 first=nearest.transform.InverseTransformPoint(feed.TransformPoint(new Vector3(0,0,-.04f)));
            Vector3 second=nearest.transform.InverseTransformPoint(feed.TransformPoint(new Vector3(0,0,.04f)));
            nearest.Punch(correct,new Vector2(first.x,first.z),new Vector2(second.x,second.z));return true;
        }
        private IEnumerator Cycle(HandInteractor hand)
        {
            cycling=true;
            Quaternion rest=lever.localRotation;
            for(float t=0;t<1;t+=Time.deltaTime*6) { lever.localRotation=rest*Quaternion.Euler(-25*Mathf.Sin(t*Mathf.PI*.5f),0,0);yield return null; }
            bool punched=FindAndPunch();
            if(punched)
            {
                if(punchSound)AudioSource.PlayClipAtPoint(punchSound,feed.position,.4f);
                if(hand)hand.Haptic(.25f,.06f);
            }
            for(float t=0;t<1;t+=Time.deltaTime*5) { lever.localRotation=Quaternion.Slerp(rest*Quaternion.Euler(-25,0,0),rest,t);yield return null; }
            lever.localRotation=rest;cycling=false;
        }
    }
}
