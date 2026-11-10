using System;
using System.Collections;
using UnityEngine;

namespace DiwanVR
{
    public sealed class OfficePrinter : MonoBehaviour
    {
        public Hinge scannerLid;
        public Transform scannerSurface,outputStart,outputEnd;
        public Renderer display;
        public Material ready,noDocument,printing,noPaper,screenOff,inkMaterial;
        public AudioClip motorSound;
        public int remainingPaper=50;
        public bool powered=true;
        public bool Busy {get;private set;}
        private AudioSource motor;
        private bool cancelRequested;
        private PaperDocument movingCopy;
        private void Awake()
        {
            motor=GetComponent<AudioSource>();
            if(!motor)motor=gameObject.AddComponent<AudioSource>();
            motor.playOnAwake=false;motor.spatialBlend=1;motor.minDistance=.4f;motor.maxDistance=5;
            SetDisplay(powered?ready:screenOff);
        }
        public void TogglePower()
        {
            powered=!powered;
            if(!powered)
            {
                StopAllCoroutines();if(motor)motor.Stop();ReleaseCopy();Busy=false;
            }
            SetDisplay(powered?(remainingPaper>0?ready:noPaper):screenOff);
        }
        public void Cancel(){cancelRequested=true;}
        public void Copy()
        {
            if(!powered || Busy)return;
            if(remainingPaper<=0){SetDisplay(noPaper);return;}
            var source=FindDocument();
            if(!source || !scannerLid || !scannerLid.IsClosed){SetDisplay(noDocument);return;}
            StartCoroutine(PrintCopy(source));
        }
        private PaperDocument FindDocument()
        {
            if(!scannerSurface)return null;
            PaperDocument nearest=null;float best=.08f;
            foreach(var paper in FindObjectsByType<PaperDocument>())
            {
                var grab=paper.GetComponent<Grabbable>();
                if(paper.mountedBinder || paper==movingCopy || (grab && grab.held))continue;
                Vector3 p=scannerSurface.InverseTransformPoint(paper.transform.position);
                if(Mathf.Abs(p.x)>.055f || Mathf.Abs(p.z)>.065f || Mathf.Abs(p.y)>.022f)continue;
                if(Vector3.Dot(paper.transform.up,scannerSurface.up)>-.85f)continue;
                if(p.magnitude<best){nearest=paper;best=p.magnitude;}
            }
            return nearest;
        }
        private IEnumerator PrintCopy(PaperDocument source)
        {
            Busy=true;cancelRequested=false;SetDisplay(printing);
            if(motorSound){motor.clip=motorSound;motor.loop=true;motor.volume=.16f;motor.Play();}
            yield return new WaitForSeconds(.65f);
            // Scan the sheet that is still physically on the glass, with the
            // lid closed. A page removed during scanning cannot be copied.
            bool valid=source && scannerLid && scannerLid.IsClosed && FindDocument()==source;
            if(!cancelRequested && valid)
            {
                movingCopy=Instantiate(source,outputStart.position,outputStart.rotation);
                movingCopy.name="Photocopy_"+source.documentId;
                movingCopy.documentId="COPY_"+Guid.NewGuid().ToString("N");
                movingCopy.punchedCorrectly=false;movingCopy.damaged=false;movingCopy.mountedBinder=null;
                var body=movingCopy.GetComponent<Rigidbody>();body.isKinematic=true;
                var grab=movingCopy.GetComponent<Grabbable>();
                grab.itemId=movingCopy.documentId;grab.canGrab=false;grab.held=false;grab.holder=null;grab.accessDoor=null;grab.storageAnchor=null;
                grab.spawnPosition=outputEnd.position;grab.spawnRotation=outputEnd.rotation;
                Mesh plain=PlainPaper();
                movingCopy.SetGeneratedMeshes(plain,plain);
                if(movingCopy.thicknessRenderer)movingCopy.thicknessRenderer.enabled=true;
                if(movingCopy.holeMarkers!=null)foreach(var hole in movingCopy.holeMarkers)if(hole)hole.SetActive(false);
                var front=movingCopy.frontSurface;
                if(front)
                {
                    var writable=front.GetComponent<WritingSurface>();
                    if(!writable)writable=front.gameObject.AddComponent<WritingSurface>();
                    writable.freeForm=true;writable.ledgerIndex=-1;writable.inkMaterial=inkMaterial;
                }
                remainingPaper--;
                for(float t=0;t<1;t+=Time.deltaTime/1.55f)
                {
                    if(!movingCopy)break;
                    movingCopy.transform.SetPositionAndRotation(Vector3.Lerp(outputStart.position,outputEnd.position,t),Quaternion.Slerp(outputStart.rotation,outputEnd.rotation,t));
                    yield return null;
                }
                if(movingCopy)movingCopy.transform.SetPositionAndRotation(outputEnd.position,outputEnd.rotation);
                ReleaseCopy();
                if(DiwanSession.Instance && source)DiwanSession.Instance.Record("photocopy_created",source.documentId);
            }
            motor.Stop();Busy=false;SetDisplay(!cancelRequested && !valid?noDocument:(remainingPaper>0?ready:noPaper));
        }
        private void ReleaseCopy()
        {
            if(!movingCopy)return;
            var body=movingCopy.GetComponent<Rigidbody>();if(body)body.isKinematic=false;
            var grab=movingCopy.GetComponent<Grabbable>();if(grab)grab.canGrab=true;
            movingCopy=null;
        }
        private void SetDisplay(Material material){if(display && material)display.sharedMaterial=material;}
        private static Mesh PlainPaper()
        {
            var mesh=new Mesh{name="Unpunched photocopy",hideFlags=HideFlags.DontSave};
            mesh.vertices=new[]{new Vector3(-.105f,0,-.1485f),new Vector3(-.105f,0,.1485f),new Vector3(.105f,0,.1485f),new Vector3(.105f,0,-.1485f)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private void OnDisable()
        {
            StopAllCoroutines();if(motor)motor.Stop();ReleaseCopy();Busy=false;
        }
    }
}
