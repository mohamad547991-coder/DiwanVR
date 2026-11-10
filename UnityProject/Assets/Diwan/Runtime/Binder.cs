using UnityEngine;

namespace DiwanVR
{
    public sealed class Binder : MonoBehaviour
    {
        public int index;
        public string arabicName;
        public Hinge cover;
        public bool ringsOpen;
        public Transform[] ringHalves;
        public Transform paperMount;
        public Transform shelfAnchor;
        public Hinge cabinetDoor;
        public PaperDocument containedPaper;
        public bool IsBlockedByCabinet => InShelfPosition && cabinetDoor && !cabinetDoor.IsOpen;
        public bool InShelfPosition => shelfAnchor && Vector3.Distance(transform.position,shelfAnchor.position)<.085f && Quaternion.Angle(transform.rotation,shelfAnchor.rotation)<18;
        public bool CanMount => cover && cover.IsOpen && ringsOpen && !IsBlockedByCabinet;
        public void ToggleRings()
        {
            if(IsBlockedByCabinet)return;
            if(!cover || !cover.IsOpen)return;
            ringsOpen=!ringsOpen;
            for(int i=0;i<ringHalves.Length;i++)if(ringHalves[i])ringHalves[i].localRotation=Quaternion.Euler(0,0,ringsOpen?(i%2==0?-27:27):0);
            if(DiwanSession.Instance)DiwanSession.Instance.Record(ringsOpen?"rings_open":"rings_closed","B"+index);
            if(containedPaper)containedPaper.SetFastened(!ringsOpen);
        }
        public bool TryMount(PaperDocument paper)
        {
            if(!paper || !paperMount || !CanMount || !paper.punchedCorrectly || paper.damaged || containedPaper || paper.mountedBinder)return false;
            paper.AttachToBinder(this);
            if(DiwanSession.Instance)DiwanSession.Instance.Record("paper_mounted","B"+index);
            return true;
        }
        private void FixedUpdate()
        {
            var grab=GetComponent<Grabbable>();
            if(!grab || grab.held || !shelfAnchor)return;
            if(cabinetDoor && !cabinetDoor.IsOpen && !InShelfPosition)return;
            if(Vector3.Distance(transform.position,shelfAnchor.position)<.055f && Quaternion.Angle(transform.rotation,shelfAnchor.rotation)<12)
            {
                var body=GetComponent<Rigidbody>();body.isKinematic=true;
                transform.SetPositionAndRotation(shelfAnchor.position,shelfAnchor.rotation);
            }
        }
    }
}
