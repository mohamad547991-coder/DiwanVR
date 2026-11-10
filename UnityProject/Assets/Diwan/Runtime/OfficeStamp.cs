using UnityEngine;
using UnityEngine.Rendering;
namespace DiwanVR
{
    public sealed class OfficeStamp : MonoBehaviour
    {
        public Transform contact,inkPad;
        public Material imprintMaterial;
        public int inkCharges=8;
        private bool armed=true;
        private readonly Collider[] hits=new Collider[24];
        private void FixedUpdate()
        {
            var grab=GetComponent<Grabbable>();if(!grab || !grab.held || !contact)return;
            if(inkPad && Vector3.Distance(contact.position,inkPad.position)<.026f){inkCharges=8;armed=true;return;}
            WritingSurface target=null;Vector3 point=Vector3.zero;
            int count=Physics.OverlapSphereNonAlloc(contact.position,.018f,hits,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var surface=hits[i].GetComponent<WritingSurface>();if(!surface || !surface.enabled)continue;
                Vector3 p=surface.transform.InverseTransformPoint(contact.position);
                if(Mathf.Abs(p.y)>.006f || Mathf.Abs(p.x)>surface.size.x*.5f-.027f || Mathf.Abs(p.z)>surface.size.y*.5f-.016f)continue;
                if(Vector3.Dot(transform.up,surface.transform.up)<.88f)continue;
                target=surface;point=p;break;
            }
            if(!target){armed=true;return;}
            if(!armed || inkCharges<=0 || !imprintMaterial)return;
            armed=false;inkCharges--;
            var mark=new GameObject("DiwanStampImprint");mark.transform.SetParent(target.transform,false);
            mark.transform.localPosition=new Vector3(point.x,.00085f,point.z);
            Vector3 forward=Vector3.ProjectOnPlane(transform.forward,target.transform.up).normalized;
            mark.transform.rotation=Quaternion.LookRotation(forward,target.transform.up);
            var mesh=new Mesh{name="Office stamp imprint"};
            mesh.vertices=new[]{new Vector3(-.025f,0,-.014f),new Vector3(-.025f,0,.014f),new Vector3(.025f,0,.014f),new Vector3(.025f,0,-.014f)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            mark.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=mark.AddComponent<MeshRenderer>();renderer.sharedMaterial=imprintMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(DiwanSession.Instance)DiwanSession.Instance.Record("stamp_paper",target.GetComponentInParent<PaperDocument>()?.documentId??"REGISTER");
        }
    }
}
