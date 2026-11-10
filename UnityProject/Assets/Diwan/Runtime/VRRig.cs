using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace DiwanVR
{
    [DefaultExecutionOrder(-100)]
    public sealed class VRRig : MonoBehaviour
    {
        public Camera head;
        public HandInteractor left,right;
        public float walkingSpeed=1.0f;
        public float standingEyeHeight=1.65f;
        private CharacterController capsule;
        private float yaw,pitch=21,nextTurn;
        private bool headset,previousHeadset;
        private readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
        private readonly List<XRInputSubsystem> trackingSystems=new List<XRInputSubsystem>();
        private readonly List<XRHandSubsystem> handSystems=new List<XRHandSubsystem>();
        private Vector3 trackingOffset;
        // Guardian coordinates can have their origin metres away from the wearer.
        // Anchor the first valid head sample at the authored spawn point, then
        // retain all subsequent physical movement relative to that sample.
        private Vector3 horizontalTrackingOrigin;
        private bool hasTrackingOrigin;
        private float nextOriginCheck;
        private void Awake()
        {
            capsule=gameObject.AddComponent<CharacterController>();capsule.radius=.20f;capsule.height=1.65f;capsule.center=new Vector3(0,.825f,0);capsule.skinWidth=.015f;
            Application.targetFrameRate=72;
            Physics.defaultSolverIterations=12;Physics.defaultSolverVelocityIterations=4;
            Time.fixedDeltaTime=1f/90f;
        }
        private void Update()
        {
            displays.Clear();SubsystemManager.GetSubsystems(displays);
            headset=false;foreach(var d in displays)if(d.running)headset=true;
            if(headset!=previousHeadset)
            {
                left.Release();right.Release();
                if(headset)hasTrackingOrigin=false;
                previousHeadset=headset;
            }
            if(headset)XRUpdate();else DesktopUpdate();
        }
        private void XRUpdate()
        {
            left.ManualPreview=false;right.ManualPreview=false;
            if(Time.time>=nextOriginCheck)
            {
                nextOriginCheck=Time.time+1;trackingSystems.Clear();SubsystemManager.GetSubsystems(trackingSystems);
                trackingOffset=Vector3.up*standingEyeHeight;
                foreach(var system in trackingSystems)
                {
                    if(!system.running)continue;
                    system.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    if((system.GetTrackingOriginMode()&TrackingOriginModeFlags.Floor)!=0)trackingOffset=Vector3.zero;
                }
            }
            var hmd=InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool tracked=hmd.isValid;
            if(tracked && hmd.TryGetFeatureValue(CommonUsages.isTracked,out bool isTracked))tracked=isTracked;
            if(tracked && hmd.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 hp) && IsFinite(hp))
            {
                if(!hasTrackingOrigin){horizontalTrackingOrigin=new Vector3(hp.x,0,hp.z);hasTrackingOrigin=true;}
                head.transform.localPosition=hp+trackingOffset-horizontalTrackingOrigin;
            }
            if(hmd.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion hq))head.transform.localRotation=hq;
            handSystems.Clear();SubsystemManager.GetSubsystems(handSystems);
            XRHandSubsystem handSystem=null;
            foreach(var system in handSystems)if(system.running){handSystem=system;break;}
            Track(left,XRNode.LeftHand,handSystem);Track(right,XRNode.RightHand,handSystem);
            var l=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);var r=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            l.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 move);
            Vector3 f=Vector3.ProjectOnPlane(head.transform.forward,Vector3.up).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,f);
            capsule.Move((f*move.y+side*move.x)*walkingSpeed*Time.deltaTime+Vector3.down*2*Time.deltaTime);
            r.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 turn);
            if(Mathf.Abs(turn.x)>.7f && Time.time>nextTurn){transform.RotateAround(head.transform.position,Vector3.up,Mathf.Sign(turn.x)*30);nextTurn=Time.time+.3f;}
            if(Mathf.Abs(turn.x)<.2f)nextTurn=0;
            capsule.height=Mathf.Clamp(head.transform.localPosition.y,1,2);capsule.center=new Vector3(head.transform.localPosition.x,capsule.height*.5f,head.transform.localPosition.z);
        }
        private static bool IsFinite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
                   !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
                   !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        }
        private void Track(HandInteractor hand,XRNode node,XRHandSubsystem system)
        {
            if(hasTrackingOrigin && system!=null)
            {
                XRHand tracked=node==XRNode.LeftHand?system.leftHand:system.rightHand;
                if(tracked.isTracked && tracked.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist) &&
                   tracked.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb) &&
                   tracked.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index) &&
                   IsFinite(wrist.position) && IsFinite(thumb.position) && IsFinite(index.position))
                {
                    Vector3 correction=trackingOffset-horizontalTrackingOrigin;
                    hand.transform.localPosition=(thumb.position+index.position)*.5f+correction;
                    hand.transform.localRotation=wrist.rotation;
                    hand.SetHandTracking(true,Vector3.Distance(thumb.position,index.position));
                    var pose=hand.visuals?hand.visuals.GetComponent<HandPose>():null;
                    if(pose)pose.SetTrackedJoints(tracked,transform,correction);
                    return;
                }
            }
            var controllerPose=hand.visuals?hand.visuals.GetComponent<HandPose>():null;
            if(controllerPose)controllerPose.ClearTrackedJoints();
            var device=InputDevices.GetDeviceAtXRNode(node);
            bool validController=hasTrackingOrigin && device.isValid &&
                (device.characteristics & InputDeviceCharacteristics.Controller)!=0 &&
                device.TryGetFeatureValue(CommonUsages.isTracked,out bool controllerTracked) && controllerTracked;
            if(validController && device.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p) && IsFinite(p) &&
               device.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))
            {
                hand.transform.localPosition=p+trackingOffset-horizontalTrackingOrigin;
                hand.transform.localRotation=q;
                hand.SetControllerTracking(true);
            }
            else hand.SetControllerTracking(false);
        }
        private void DesktopUpdate()
        {
            // Clear XR state once on the transition, without releasing mouse grabs each frame.
            if(!left.ManualPreview)left.SetControllerTracking(false);
            if(!right.ManualPreview)right.SetControllerTracking(false);
            left.ManualPreview=true;right.ManualPreview=true;
            if(left.visuals)left.visuals.gameObject.SetActive(false);
            if(right.visuals)right.visuals.gameObject.SetActive(false);
            head.transform.localPosition=new Vector3(0,standingEyeHeight,0);
            var kb=Keyboard.current;var mouse=Mouse.current;
            if(kb==null||mouse==null)return;
            if(mouse.rightButton.isPressed)
            {
                Vector2 delta=mouse.delta.ReadValue();yaw+=delta.x*.13f;pitch=Mathf.Clamp(pitch-delta.y*.13f,-80,80);
            }
            head.transform.localRotation=Quaternion.Euler(pitch,yaw,0);
            Vector2 axes=new Vector2((kb.dKey.isPressed?1:0)-(kb.aKey.isPressed?1:0),(kb.wKey.isPressed?1:0)-(kb.sKey.isPressed?1:0));
            Vector3 f=Vector3.ProjectOnPlane(head.transform.forward,Vector3.up).normalized;
            capsule.Move((f*axes.y+Vector3.Cross(Vector3.up,f)*axes.x)*walkingSpeed*Time.deltaTime+Vector3.down*2*Time.deltaTime);
            if(mouse.leftButton.wasPressedThisFrame)
            {
                var ray=head.ScreenPointToRay(mouse.position.ReadValue());
                if(Physics.Raycast(ray,out RaycastHit hit,2.5f))
                {
                    right.transform.position=hit.point;
                    var press=hit.collider.GetComponentInParent<Pressable>();
                    if(press)press.Press(right);
                    else
                    {
                        var hinge=hit.collider.GetComponentInParent<Hinge>();
                        if(hinge)hinge.SetAngle(hinge.IsClosed?hinge.openAngle:0);
                        else {var grab=hit.collider.GetComponentInParent<Grabbable>();if(grab)right.Grab(grab);}
                    }
                }
            }
            if(right.grabbed)
            {
                var ray=head.ScreenPointToRay(mouse.position.ReadValue());
                right.transform.position=ray.GetPoint(.65f);
                if(mouse.scroll.ReadValue().y!=0)right.transform.Rotate(Vector3.up,mouse.scroll.ReadValue().y*.1f,Space.World);
                if(mouse.leftButton.wasReleasedThisFrame)right.Release();
            }
            if(kb.escapeKey.wasPressedThisFrame){right.Release();Cursor.lockState=CursorLockMode.None;}
        }
    }
}
