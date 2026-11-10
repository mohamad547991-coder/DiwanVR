using UnityEngine;
namespace DiwanVR
{
    public sealed class LandlineHandset : MonoBehaviour
    {
        public Transform cradle,cordStart,cordEnd;
        public Material cordMaterial;
        private LineRenderer cord;
        private Grabbable grab;
        private readonly Vector3[] points=new Vector3[180];
        private void Start()
        {
            grab=GetComponent<Grabbable>();var root=new GameObject("CoiledTelephoneCord");root.transform.SetParent(cradle.parent,false);
            cord=root.AddComponent<LineRenderer>();cord.useWorldSpace=true;cord.positionCount=points.Length;cord.sharedMaterial=cordMaterial;
            cord.startWidth=.0026f;cord.endWidth=.0026f;cord.numCornerVertices=1;
        }
        private void LateUpdate()
        {
            if(!cradle || !cordStart || !cordEnd || !cord)return;
            if(grab && !grab.held && Vector3.Distance(transform.position,cradle.position)<.045f)
            {
                transform.SetPositionAndRotation(cradle.position,cradle.rotation);grab.Body.isKinematic=true;
            }
            Vector3 a=cordStart.position,b=cordEnd.position,axis=(b-a).normalized;
            Vector3 side=Vector3.Cross(axis,Vector3.up);if(side.sqrMagnitude<.01f)side=Vector3.right;side.Normalize();Vector3 up=Vector3.Cross(axis,side);
            float distance=Vector3.Distance(a,b),radius=Mathf.Lerp(.006f,.002f,Mathf.Clamp01(distance/1.3f));
            for(int i=0;i<points.Length;i++)
            {
                float t=(float)i/(points.Length-1),edge=Mathf.Sin(t*Mathf.PI),angle=t*22*Mathf.PI*2;
                points[i]=Vector3.Lerp(a,b,t)+Vector3.down*(.018f+Mathf.Max(0,.055f-distance*.03f))*edge+(side*Mathf.Cos(angle)+up*Mathf.Sin(angle))*radius*edge;
                points[i].y=Mathf.Max(points[i].y,cradle.parent.position.y+.004f);
            }
            cord.SetPositions(points);
        }
    }
}
