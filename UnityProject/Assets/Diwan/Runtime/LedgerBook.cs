using UnityEngine;

namespace DiwanVR
{
    public sealed class LedgerBook : MonoBehaviour
    {
        public int index;
        public string arabicName;
        public Hinge cover;
        public WritingSurface surface;
        public bool Opened => cover && cover.IsOpen;
        public void ToggleCover() { if(cover)cover.SetAngle(Opened?0:170); }
        private void Update() { if(surface)surface.enabled=Opened; }
    }
}
