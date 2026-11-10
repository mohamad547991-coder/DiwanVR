using UnityEngine;

namespace DiwanVR
{
    public sealed class Pen : MonoBehaviour
    {
        public Transform tip;
        private WritingSurface touching;
        private readonly Collider[] hits=new Collider[32];
        private void FixedUpdate()
        {
            if(!tip)return;
            var grab=GetComponent<Grabbable>();
            if(!grab || !grab.held) { End();return; }
            int count=Physics.OverlapSphereNonAlloc(tip.position,.008f,hits,~0,QueryTriggerInteraction.Collide);
            WritingSurface found=null;float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var surface=hits[i].GetComponent<WritingSurface>();
                if(!surface || !surface.enabled)continue;
                Vector3 p=surface.transform.InverseTransformPoint(tip.position);
                if(p.y<-.00025f || p.y>.004f || Mathf.Abs(p.x)>surface.size.x*.5f || Mathf.Abs(p.z)>surface.size.y*.5f)continue;
                float distance=Mathf.Abs(p.y);if(distance<nearest){found=surface;nearest=distance;}
            }
            if(found!=touching)End();
            touching=found;
            if(touching)touching.Touch(tip.position);
        }
        private void End() { if(touching)touching.End();touching=null; }
        private void OnDisable(){End();}
    }
}
