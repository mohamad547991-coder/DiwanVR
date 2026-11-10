using UnityEngine;

namespace DiwanVR
{
    public sealed class PhysicalMonitor : MonoBehaviour
    {
        public Renderer display;
        public Renderer powerLED;
        public Material screenOn,screenOff,ledOn,ledOff;
        public bool powered=true;
        private void Awake(){Apply();}
        public void TogglePower()
        {
            powered=!powered;Apply();
            if(DiwanSession.Instance)DiwanSession.Instance.Record("monitor_power",powered?"on":"off");
        }
        private void Apply()
        {
            if(display)display.sharedMaterial=powered?screenOn:screenOff;
            if(powerLED)powerLED.sharedMaterial=powered?ledOn:ledOff;
        }
    }
}
