using UnityEngine;

namespace DiwanVR
{
    public sealed class Pressable : MonoBehaviour
    {
        public enum Kind { BinderLever, PunchLever, LedgerCover, MonitorPower, PrinterCopy, PrinterPower, PrinterCancel }
        public Kind kind;
        public Binder binder;
        public PunchMachine punch;
        public LedgerBook ledger;
        public PhysicalMonitor monitor;
        public OfficePrinter printer;
        private float nextUse;
        public void Press(HandInteractor hand)
        {
            if(Time.time<nextUse)return;
            nextUse=Time.time+.35f;
            if(kind==Kind.BinderLever && binder) binder.ToggleRings();
            if(kind==Kind.PunchLever && punch) punch.Press(hand);
            if(kind==Kind.LedgerCover && ledger) ledger.ToggleCover();
            if(kind==Kind.MonitorPower && monitor)monitor.TogglePower();
            if(kind==Kind.PrinterCopy && printer)printer.Copy();
            if(kind==Kind.PrinterPower && printer)printer.TogglePower();
            if(kind==Kind.PrinterCancel && printer)printer.Cancel();
            if((int)kind>=3 && hand)hand.Haptic(.08f,.025f);
        }
    }
}
