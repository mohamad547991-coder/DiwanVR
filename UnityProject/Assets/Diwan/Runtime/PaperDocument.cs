using UnityEngine;

namespace DiwanVR
{
    public sealed class PaperDocument : MonoBehaviour
    {
        public string documentId="DOC001";
        public bool punchedCorrectly;
        public bool damaged;
        public Binder mountedBinder;
        public GameObject[] holeMarkers;
        public Transform feedReference;
        public MeshFilter frontSurface;
        public MeshFilter backSurface;
        public MeshRenderer thicknessRenderer;
        private Collider[] paperColliders,binderColliders;
        // Own only meshes generated for this document, never imported assets.
        private Mesh generatedFront,generatedBack;
        public void Punch(bool correct)
        {
            Punch(correct,correct?new Vector2(.093f,-.04f):new Vector2(.078f,-.025f),correct?new Vector2(.093f,.04f):new Vector2(.084f,.055f));
        }
        public void Punch(bool correct,Vector2 firstHole,Vector2 secondHole)
        {
            if(punchedCorrectly || damaged)return;
            punchedCorrectly=correct;damaged=!correct;
            SetGeneratedMeshes(frontSurface?PaperMesh.PerforatedPage(firstHole,secondHole):null,
                backSurface?PaperMesh.PerforatedPage(new Vector2(firstHole.x,-firstHole.y),new Vector2(secondHole.x,-secondHole.y)):null);
            if(thicknessRenderer)thicknessRenderer.enabled=false;
            if(DiwanSession.Instance)DiwanSession.Instance.Record(correct?"paper_punched":"paper_damaged",documentId);
        }
        public void SetFastened(bool fastened)
        {
            var grab=GetComponent<Grabbable>();if(grab)grab.canGrab=!fastened;
        }
        // The printer passes one shared mesh for both sides. Take ownership
        // once so replacing it with punched geometry also releases it.
        public void SetGeneratedMeshes(Mesh front,Mesh back)
        {
            ReleaseGeneratedMeshes();
            generatedFront=front;generatedBack=back;
            if(frontSurface)frontSurface.sharedMesh=front;
            if(backSurface)backSurface.sharedMesh=back;
        }
        public void AttachToBinder(Binder binder)
        {
            if(!binder || mountedBinder || !binder.paperMount)return;
            var grab=GetComponent<Grabbable>();if(grab && grab.holder)grab.holder.Release();
            var body=GetComponent<Rigidbody>();
            if(body){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}
            mountedBinder=binder;binder.containedPaper=this;RestoreToMount();
            paperColliders=GetComponentsInChildren<Collider>();
            var own=new System.Collections.Generic.List<Collider>();var binderBody=binder.GetComponent<Rigidbody>();
            foreach(var c in binder.GetComponentsInChildren<Collider>())if(c.attachedRigidbody==binderBody)own.Add(c);
            binderColliders=own.ToArray();SetBinderCollisions(true);SetFastened(!binder.ringsOpen);
        }
        public void RestoreToMount()
        {
            if(!mountedBinder || !mountedBinder.paperMount)return;
            var body=GetComponent<Rigidbody>();if(body)body.isKinematic=true;
            transform.SetParent(mountedBinder.paperMount,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
        }
        public void DetachFromBinder()
        {
            SetBinderCollisions(false);paperColliders=null;binderColliders=null;
            if(mountedBinder && mountedBinder.containedPaper==this)mountedBinder.containedPaper=null;
            mountedBinder=null;transform.SetParent(null,true);SetFastened(false);
        }
        private void SetBinderCollisions(bool ignore)
        {
            if(paperColliders==null || binderColliders==null)return;
            foreach(var a in paperColliders)foreach(var b in binderColliders)if(a && b && a!=b)Physics.IgnoreCollision(a,b,ignore);
        }
        private void Update()
        {
            var grab=GetComponent<Grabbable>();
            if(mountedBinder && grab && grab.held)
            {
                if(!mountedBinder.ringsOpen)return;
                DetachFromBinder();
            }
        }
        private void OnDestroy()
        {
            SetBinderCollisions(false);
            if(mountedBinder && mountedBinder.containedPaper==this)mountedBinder.containedPaper=null;
            ReleaseGeneratedMeshes();
        }
        private void ReleaseGeneratedMeshes()
        {
            if(generatedBack==generatedFront)generatedBack=null;
            ReleaseMesh(ref generatedFront);ReleaseMesh(ref generatedBack);
        }
        private static void ReleaseMesh(ref Mesh mesh)
        {
            if(!mesh)return;
            if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);
            mesh=null;
        }
        private void FixedUpdate()
        {
            if(mountedBinder || !punchedCorrectly || damaged)return;
            var grab=GetComponent<Grabbable>();if(grab && grab.held)return;
            if(!DiwanSession.Instance)return;
            foreach(var b in DiwanSession.Instance.binders)
            {
                if(!b || !b.CanMount)continue;
                if(Vector3.Distance(transform.position,b.paperMount.position)<.025f && Quaternion.Angle(transform.rotation,b.paperMount.rotation)<14) { b.TryMount(this);break; }
            }
        }
    }
}
