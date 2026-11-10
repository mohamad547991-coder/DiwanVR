using System;
using UnityEngine;
namespace DiwanVR
{
    public sealed class WallClock : MonoBehaviour
    {
        public Transform hourHand,minuteHand,secondHand;
        private void Update()
        {
            DateTime t=DateTime.Now;
            if(hourHand)hourHand.localRotation=Quaternion.Euler(0,0,-(t.Hour%12*30+t.Minute*.5f));
            if(minuteHand)minuteHand.localRotation=Quaternion.Euler(0,0,-(t.Minute*6+t.Second*.1f));
            if(secondHand)secondHand.localRotation=Quaternion.Euler(0,0,-t.Second*6);
        }
    }
}
