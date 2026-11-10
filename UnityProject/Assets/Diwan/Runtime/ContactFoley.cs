using UnityEngine;

namespace DiwanVR
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ContactFoley : MonoBehaviour
    {
        public AudioClip contactClip;
        private AudioSource source;
        private float nextSound;
        private void Awake()
        {
            source=GetComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=.35f;source.maxDistance=5;
        }
        private void OnCollisionEnter(Collision collision)
        {
            float speed=collision.relativeVelocity.magnitude;
            if(!contactClip || speed<.22f || Time.time<nextSound || Time.time<.8f)return;
            nextSound=Time.time+.25f;
            source.pitch=Random.Range(.94f,1.06f);
            source.PlayOneShot(contactClip,Mathf.Clamp(speed*.16f,.045f,.32f));
        }
    }
}
