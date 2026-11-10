namespace DiwanVR
{
    // Requires an observed open hand after acquiring tracking. A closed hand
    // returning from occlusion must not produce an unintended new grab.
    public sealed class TrackedPinch
    {
        public const float CloseDistance = .022f;
        public const float OpenDistance = .035f;
        public bool IsPinching { get; private set; }
        private bool armed;

        public bool Update(bool tracked, float distance)
        {
            if (!tracked || float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0)
            {
                Reset();
                return false;
            }
            if (!armed)
            {
                armed = distance >= OpenDistance;
                return false;
            }
            IsPinching = IsPinching ? distance < OpenDistance : distance < CloseDistance;
            return IsPinching;
        }

        public void Reset() { armed = false; IsPinching = false; }
    }
}
