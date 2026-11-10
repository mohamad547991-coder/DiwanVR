using System.Collections.Generic;
using UnityEngine;

namespace DiwanVR
{
    public sealed class WritingSurface : MonoBehaviour
    {
        public Vector2 size=new Vector2(.21f,.297f);
        public int ledgerIndex;
        public bool freeForm;
        public Material inkMaterial;
        public List<InkStroke> Strokes=new List<InkStroke>();
        private LineRenderer active;
        private InkStroke activeData;
        private readonly List<Vector3> positions=new List<Vector3>();
        private Material ink;
        public void Touch(Vector3 world)
        {
            if(DiwanSession.Instance && DiwanSession.Instance.report.ended){End();return;}
            Vector3 local=transform.InverseTransformPoint(world);
            float u=local.x/size.x+.5f, v=.5f-local.z/size.y;
            if(u<(freeForm?0:60f/1240f)||u>(freeForm?1:1180f/1240f)||v<(freeForm?0:.30f)||v>(freeForm?1:.92f)) { End();return; }
            int field=freeForm?0:GetField(u);
            if(activeData!=null && activeData.field!=field)End();
            if(active==null)
            {
                var go=new GameObject("InkStroke");go.transform.SetParent(transform,false);
                active=go.AddComponent<LineRenderer>();active.useWorldSpace=false;
                if(!ink) { ink=inkMaterial; if(!ink){ink=new Material(Shader.Find("Universal Render Pipeline/Lit"));ink.color=new Color(.045f,.12f,.28f);} }
                active.sharedMaterial=ink;active.startWidth=.0007f;active.endWidth=.0007f;active.numCapVertices=3;active.numCornerVertices=3;active.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                positions.Clear();activeData=new InkStroke { field=field };Strokes.Add(activeData);
                if(DiwanSession.Instance)
                {
                    var document=GetComponentInParent<PaperDocument>();
                    DiwanSession.Instance.Record(freeForm?"write_paper":"write_field",freeForm && document?document.documentId:ledgerIndex+":"+field);
                }
            }
            Vector3 point=new Vector3(local.x,.0006f,local.z);
            if(positions.Count==0 || Vector3.Distance(point,positions[positions.Count-1])>.00045f)
            {
                positions.Add(point);activeData.points.Add(new Vector2(u,v));
                active.positionCount=positions.Count;active.SetPositions(positions.ToArray());
                if(!freeForm && DiwanSession.Instance)DiwanSession.Instance.report.selectedLedger=ledgerIndex;
            }
        }
        public static int GetField(float u)
        {
            float px=u*1240f;
            if(px>1060)return 0;
            if(px>790)return 1;
            if(px>580)return 2;
            if(px>345)return 3;
            if(px>160)return 4;
            return 5;
        }
        public void End() { active=null;activeData=null;positions.Clear(); }
    }
}
