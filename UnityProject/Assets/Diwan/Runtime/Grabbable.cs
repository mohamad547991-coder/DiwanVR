using UnityEngine;

namespace DiwanVR
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Grabbable : MonoBehaviour
    {
        public string itemId;
        public bool canGrab=true;
        public bool held;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public Rigidbody Body { get; private set; }
        public HandInteractor holder;
        public Hinge accessDoor;
        public Transform storageAnchor;
        public bool IsBlockedByStorage => accessDoor && !accessDoor.IsOpen && storageAnchor && Vector3.Distance(transform.position,storageAnchor.position)<.20f;
        public bool CanBeGrabbed
        {
            get
            {
                if(!canGrab || held || IsBlockedByStorage)return false;
                var binder=GetComponent<Binder>();if(binder && binder.IsBlockedByCabinet)return false;
                var paper=GetComponent<PaperDocument>();
                return !paper || !paper.mountedBinder || paper.mountedBinder.CanMount;
            }
        }
        private void Awake() { Body=GetComponent<Rigidbody>(); spawnPosition=transform.position; spawnRotation=transform.rotation; }
        public bool TryGrab(HandInteractor hand)
        {
            if(!CanBeGrabbed || (hand && (hand.grabbed || hand.grabbedHinge)))return false;
            var paper=GetComponent<PaperDocument>();if(paper && paper.mountedBinder)paper.DetachFromBinder();
            Body=GetComponent<Rigidbody>();
            held=true; holder=hand;
            Body.isKinematic=false;
            Body.interpolation=RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            if(DiwanSession.Instance) DiwanSession.Instance.Record("pick_up",itemId);
            return true;
        }
        public void Release()
        {
            held=false;holder=null;
            if(DiwanSession.Instance) DiwanSession.Instance.Record("release",itemId);
        }
        private void FixedUpdate()
        {
            if(transform.position.y < -.65f) Restore();
        }
        public void Restore()
        {
            if(holder) holder.Release();
            var paper=GetComponent<PaperDocument>();
            if(paper && paper.mountedBinder){paper.RestoreToMount();return;}
            if(Body && !Body.isKinematic){Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;}
            transform.SetPositionAndRotation(spawnPosition,spawnRotation);
        }
        private void OnDisable(){if(holder)holder.Release();}
    }
}
