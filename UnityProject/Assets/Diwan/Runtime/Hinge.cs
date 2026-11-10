using UnityEngine;

namespace DiwanVR
{
    public sealed class Hinge : MonoBehaviour
    {
        public Vector3 axis=Vector3.up;
        public float minAngle=0,maxAngle=105;
        public float angle;
        public float openAngle=105;
        public string itemId;
        public Transform handle;
        public float ClosedThreshold=4;
        private Quaternion rest;
        private bool initialized;
        private HandInteractor hand;
        private Vector3 lastVector;
        public bool IsClosed => Mathf.Abs(angle)<ClosedThreshold;
        public bool IsOpen => Mathf.Abs(angle)>50;
        private void Awake() { Init(); }
        public void Init()
        {
            if(initialized)return;
            if(axis.sqrMagnitude<.0001f)axis=Vector3.up;
            axis.Normalize();
            // A scene may be saved with this hinge already open.
            rest=transform.localRotation*Quaternion.Inverse(Quaternion.AngleAxis(angle,axis));
            initialized=true;
        }
        private Vector3 WorldAxis => transform.parent ? transform.parent.TransformDirection(rest*axis) : rest*axis;
        public bool TryGrab(HandInteractor who)
        {
            if(!who || hand || who.grabbed || who.grabbedHinge) return false;
            var binder=GetComponentInParent<Binder>();if(binder && binder.IsBlockedByCabinet)return false;
            Init();hand=who;
            lastVector=Vector3.ProjectOnPlane(who.transform.position-transform.position,WorldAxis);
            return true;
        }
        public void Release()
        {
            if(hand && DiwanSession.Instance) DiwanSession.Instance.Record(IsClosed?"hinge_closed":"hinge_open",itemId);
            hand=null;
        }
        private void LateUpdate()
        {
            if(!hand)return;
            Vector3 worldAxis=WorldAxis;
            Vector3 current=Vector3.ProjectOnPlane(hand.transform.position-transform.position,worldAxis);
            if(current.sqrMagnitude>.001 && lastVector.sqrMagnitude>.001)
            {
                float change=Vector3.SignedAngle(lastVector,current,worldAxis);
                angle=Mathf.Clamp(angle+change,minAngle,maxAngle);
                transform.localRotation=rest*Quaternion.AngleAxis(angle,axis);
            }
            lastVector=current;
        }
        public void SetAngle(float value) { Init();angle=Mathf.Clamp(value,minAngle,maxAngle);transform.localRotation=rest*Quaternion.AngleAxis(angle,axis); }
        private void OnDisable(){if(hand)hand.Release();}
    }
}
