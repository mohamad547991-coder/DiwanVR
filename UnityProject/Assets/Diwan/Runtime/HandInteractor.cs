using UnityEngine;
using UnityEngine.XR;

namespace DiwanVR
{
    public sealed class HandInteractor : MonoBehaviour
    {
        public XRNode node;
        public Transform visuals;
        public Grabbable grabbed;
        public Hinge grabbedHinge;
        private FixedJoint joint;
        private Rigidbody handBody;
        private bool lastGrip,lastTrigger;
        private bool handTracking,pinching,controllerTracking;
        private readonly TrackedPinch pinchGesture=new TrackedPinch();
        private Collider[] overlaps=new Collider[64];
        public bool ManualPreview;
        private void Awake()
        {
            handBody=GetComponent<Rigidbody>();if(!handBody)handBody=gameObject.AddComponent<Rigidbody>();
            handBody.isKinematic=true;handBody.useGravity=false;
        }
        private void Update()
        {
            if(ManualPreview)return;
            if(handTracking)
            {
                if(visuals)visuals.gameObject.SetActive(true);
                if(pinching && !lastGrip)
                {
                    if(!TryPressNearest())GrabNearest();
                }
                if(!pinching && lastGrip)Release();
                lastGrip=pinching;lastTrigger=false;
                return;
            }
            InputDevice device=InputDevices.GetDeviceAtXRNode(node);
            bool tracked;
            if(!controllerTracking || !device.isValid || (device.characteristics & InputDeviceCharacteristics.Controller)==0 ||
               !device.TryGetFeatureValue(CommonUsages.isTracked,out tracked) || !tracked)
            {
                Release();lastGrip=false;lastTrigger=false;
                if(visuals)visuals.gameObject.SetActive(false);return;
            }
            if(visuals)visuals.gameObject.SetActive(true);
            bool grip=false,trigger=false;
            device.TryGetFeatureValue(CommonUsages.gripButton,out grip);
            device.TryGetFeatureValue(CommonUsages.triggerButton,out trigger);
            if(grip && !lastGrip)GrabNearest();
            if(!grip && lastGrip)Release();
            if(trigger && !lastTrigger)PressNearest();
            lastGrip=grip;lastTrigger=trigger;
        }
        // The same state is used for both hands. Different thresholds prevent
        // jitter at the pinch boundary from generating repeated interactions.
        public void SetHandTracking(bool tracked,float pinchDistance)
        {
            if(handTracking!=tracked)
            {
                Release();lastGrip=false;lastTrigger=false;pinching=false;pinchGesture.Reset();
                handTracking=tracked;
            }
            if(tracked)controllerTracking=false;
            pinching=pinchGesture.Update(tracked,pinchDistance);
        }
        public void SetControllerTracking(bool tracked)
        {
            SetHandTracking(false,0);
            controllerTracking=tracked;
            if(!tracked)
            {
                Release();lastGrip=false;lastTrigger=false;
                if(visuals)visuals.gameObject.SetActive(false);
            }
        }
        public void GrabNearest()
        {
            if(grabbed || grabbedHinge)return;
            int n=Nearby();
            float best=.08f,bestHandle=.11f;Grabbable candidate=null;Hinge hinge=null;
            for(int i=0;i<n;i++)
            {
                Collider c=overlaps[i];if(c.transform.IsChildOf(transform))continue;
                Vector3 closest=c.ClosestPoint(transform.position);float distance=Vector3.Distance(closest,transform.position);
                Hinge h=c.GetComponentInParent<Hinge>();
                if(h && h.handle)
                {
                    float handleDistance=Vector3.Distance(h.handle.position,transform.position);
                    var owner=h.GetComponentInParent<Binder>();
                    if((!owner || !owner.IsBlockedByCabinet) && handleDistance<bestHandle){hinge=h;bestHandle=handleDistance;}
                }
                var g=c.GetComponentInParent<Grabbable>();
                if(g && g.CanBeGrabbed && distance<best){candidate=g;best=distance;}
            }
            // Handles remain usable when their collider overlaps a cover or door.
            if(hinge && (!candidate || bestHandle<.045f) && hinge.TryGrab(this)){grabbedHinge=hinge;Haptic(.1f,.025f);return;}
            if(candidate)Grab(candidate);
        }
        public bool Grab(Grabbable g)
        {
            if(!g || grabbed || grabbedHinge || !g.TryGrab(this))return false;
            grabbed=g;
            joint=gameObject.AddComponent<FixedJoint>();joint.connectedBody=g.Body;
            joint.breakForce=Mathf.Infinity;joint.breakTorque=Mathf.Infinity;
            joint.enableCollision=false;
            Haptic(.1f,.025f);return true;
        }
        public void Release()
        {
            if(joint){joint.connectedBody=null;Destroy(joint);joint=null;}
            if(grabbed){var g=grabbed;grabbed=null;g.Release();}
            if(grabbedHinge){grabbedHinge.Release();grabbedHinge=null;}
        }
        public void PressNearest()
        {
            TryPressNearest();
        }
        private bool TryPressNearest()
        {
            int n=Nearby();
            float best=.08f;Pressable button=null;
            for(int i=0;i<n;i++)
            {
                var p=overlaps[i].GetComponentInParent<Pressable>();if(!p)continue;
                float d=Vector3.Distance(overlaps[i].ClosestPoint(transform.position),transform.position);
                if(d<best){best=d;button=p;}
            }
            if(button){button.Press(this);return true;}
            return false;
        }
        public void Haptic(float strength,float duration)
        {
            InputDevice d=InputDevices.GetDeviceAtXRNode(node);
            if(d.isValid && d.TryGetHapticCapabilities(out HapticCapabilities cap) && cap.supportsImpulse)d.SendHapticImpulse(0,strength,duration);
        }
        private int Nearby()
        {
            int count=Physics.OverlapSphereNonAlloc(transform.position,.075f,overlaps,~0,QueryTriggerInteraction.Collide);
            while(count==overlaps.Length && overlaps.Length<1024)
            {
                overlaps=new Collider[overlaps.Length*2];
                count=Physics.OverlapSphereNonAlloc(transform.position,.075f,overlaps,~0,QueryTriggerInteraction.Collide);
            }
            return count;
        }
        private void OnDisable(){Release();handTracking=false;controllerTracking=false;pinching=false;lastGrip=false;lastTrigger=false;pinchGesture.Reset();}
    }
}
