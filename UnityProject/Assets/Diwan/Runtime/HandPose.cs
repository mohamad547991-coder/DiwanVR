using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace DiwanVR
{
    public sealed class HandPose : MonoBehaviour
    {
        public XRNode node;
        private readonly List<Transform> fingers=new List<Transform>();
        private readonly List<Quaternion> rest=new List<Quaternion>();
        private readonly List<Transform> jointTransforms=new List<Transform>();
        private readonly List<XRHandJointID> jointIds=new List<XRHandJointID>();
        private readonly List<Vector3> jointRestPositions=new List<Vector3>();
        private readonly List<Quaternion> jointRestRotations=new List<Quaternion>();
        private XRHand trackedHand;
        private Transform trackingOrigin;
        private Vector3 correction;
        private bool trackedJoints,initialized;
        private void Start()
        {
            Initialize();
        }
        private void Initialize()
        {
            if(initialized)return;
            initialized=true;
            foreach(Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n=t.name.ToLowerInvariant();
                if(n.Contains("proximal")||n.Contains("intermediate")||n.Contains("distal")){fingers.Add(t);rest.Add(t.localRotation);}
                string jointName=t.name;
                if(jointName.StartsWith("L_")||jointName.StartsWith("R_"))jointName=jointName.Substring(2);
                if(Enum.TryParse(jointName,true,out XRHandJointID id) && id!=XRHandJointID.Invalid && id!=XRHandJointID.EndMarker)
                {jointTransforms.Add(t);jointIds.Add(id);jointRestPositions.Add(t.localPosition);jointRestRotations.Add(t.localRotation);}
            }
        }
        public void SetTrackedJoints(XRHand hand,Transform origin,Vector3 offset)
        {
            Initialize();trackedHand=hand;trackingOrigin=origin;correction=offset;trackedJoints=true;
        }
        public void ClearTrackedJoints()
        {
            if(!trackedJoints)return;
            trackedJoints=false;
            for(int i=0;i<jointTransforms.Count;i++)
            {
                jointTransforms[i].localPosition=jointRestPositions[i];
                jointTransforms[i].localRotation=jointRestRotations[i];
            }
        }
        private void LateUpdate()
        {
            if(trackedJoints && trackingOrigin)
            {
                // The model contains the Unity XR Hands sample joint names.
                // Poses are relative to the tracking origin, while model bones
                // have a hierarchy, so write world-space joint poses in order.
                for(int i=0;i<jointTransforms.Count;i++)
                {
                    if(!trackedHand.GetJoint(jointIds[i]).TryGetPose(out Pose p))continue;
                    jointTransforms[i].SetPositionAndRotation(trackingOrigin.TransformPoint(p.position+correction),trackingOrigin.rotation*p.rotation);
                }
                return;
            }
            var d=InputDevices.GetDeviceAtXRNode(node);d.TryGetFeatureValue(CommonUsages.grip,out float grip);
            for(int i=0;i<fingers.Count;i++)fingers[i].localRotation=rest[i]*Quaternion.Euler(0,0,grip*55);
        }
    }
}
