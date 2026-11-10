using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace DiwanVR.Editor
{
    public static partial class DiwanBuilder
    {
        public const string Accessories040Marker="OfficeAccessories040";
        public static void PolishGeometry040()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            foreach(string name in new[]{"BlueInkPad","DiwanOfficeStamp","LeatherPenHolder","DeskLandlineTelephone"})
            {var item=GameObject.Find(name);if(item){var p=item.transform.position;p.y=.803f;item.transform.position=p;}}
            var tray=GameObject.Find("DesktopLetterOrganizer");if(tray){var p=tray.transform.position;p.y=.801f;tray.transform.position=p;}
            foreach(string name in new[]{"HandsetDock","TelephoneHandset"}){var item=GameObject.Find(name);if(item){var p=item.transform.localPosition;p.y=.104f;item.transform.localPosition=p;}}
            for(int i=0;i<5;i++){var item=GameObject.Find("SpareWritingPen"+i);if(item){var p=item.transform.position;p.y=.885f;item.transform.position=p;}}
            for(int i=0;i<2;i++){var hinge=GameObject.Find("CredenzaDoorHinge"+i)?.GetComponent<Hinge>();if(hinge){hinge.minAngle=i==0?0:-112;hinge.maxAngle=i==0?112:0;hinge.openAngle=i==0?108:-108;hinge.SetAngle(0);}}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            File.WriteAllText("Validation/geometry040_polished.json","{\"revision\":2,\"savedInUnity\":true}");
        }
        public static void ApplyOffice040()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before modifying the office.");
            if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
            if(!GameObject.Find(Hardware030Marker))ApplyProfessionalOffice030();
            PrepareOffice030();
            if(GameObject.Find(Accessories040Marker))return;
            var root=Empty(Accessories040Marker,null,Vector3.zero);
            Mat("OfficeDeskLeather",new Color(.055f,.084f,.079f),0,.34f);
            Mat("OfficeInkPad",new Color(.022f,.045f,.12f),0,.03f,"Office030/UpholsteryAlbedo");
            Mat("OfficePenBlue",new Color(.025f,.12f,.29f),.10f,.55f);
            Mat("OfficePenBurgundy",new Color(.22f,.035f,.045f),.1f,.5f);
            Mat("OfficePencil",new Color(.60f,.38f,.10f),0,.3f);
            Mat("OfficeLens",new Color(.008f,.025f,.035f),.45f,.92f);
            Mat("OfficeTelephoneLCD",Color.white,0,.2f,"Office040/PhoneLCD");
            Mat("OfficePhoneKeys",Color.white,0,.2f,"Office040/TelephoneKeys");
            Mat("OfficeStampTop",Color.white,0,.25f,"Office040/StampLabel");
            var seal=Mat("OfficeStampImprint",Color.white,0,.05f,"Office040/StampImprint");
            seal.shader=Shader.Find("Universal Render Pipeline/Unlit");seal.SetFloat("_Surface",1);seal.SetFloat("_Blend",0);seal.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);seal.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);seal.SetFloat("_ZWrite",0);seal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");seal.renderQueue=3002;
            ArrangeArchives040();BuildCredenzaStorage040();BuildStamp040(root);BuildPenCup040(root);BuildTelephone040(root);BuildLetterTrays040(root);BuildSecurityCamera040(root);
            var letter=UnityEngine.Object.FindFirstObjectByType<DiwanSession>().letter;
            if(!letter.frontSurface.GetComponent<WritingSurface>())
            {
                var surface=letter.frontSurface.gameObject.AddComponent<WritingSurface>();surface.freeForm=true;surface.ledgerIndex=-1;surface.inkMaterial=materials["WritingInk"];
                OfficeBox(surface.transform,Vector3.zero,new Vector3(.21f,.0015f,.297f));
            }
            ConfigureContactSounds();MarkEnvironmentStatic();
            foreach(var tr in root.GetComponentsInChildren<Transform>())
                if(!tr.GetComponentInParent<Grabbable>() && !tr.GetComponentInParent<Hinge>())GameObjectUtility.SetStaticEditorFlags(tr.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic);
            ConfigureSettings();PlayerSettings.bundleVersion="0.4.1";PlayerSettings.Android.bundleVersionCode=5;
            Lightmapping.Clear();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/office040_applied.json","{\"sourceVersion\":\"0.4.0\",\"appliedInUnity\":true}");
            Debug.Log("DIWAN_OFFICE_040_APPLIED");
        }
        private static void ArrangeArchives040()
        {
            var session=UnityEngine.Object.FindFirstObjectByType<DiwanSession>();
            var cabinet=GameObject.Find("WoodAndGlassCabinet").transform;cabinet.position=new Vector3(-3.20f,0,.39f);cabinet.rotation=Quaternion.Euler(0,90,0);
            foreach(var binder in session.binders)if(binder && binder.shelfAnchor)binder.transform.SetPositionAndRotation(binder.shelfAnchor.position,binder.shelfAnchor.rotation);
            var books=GameObject.Find("AdministrativeBookcase").transform;books.position=new Vector3(-3.20f,0,2.10f);books.rotation=Quaternion.Euler(0,-90,0);
            var board=GameObject.Find("NoticeBoard");if(board){board.transform.position=new Vector3(-1.96f,1.69f,3.17f);board.transform.rotation=Quaternion.identity;}
        }
        private static void BuildCredenzaStorage040()
        {
            var c=GameObject.Find("SideCredenza").transform;
            var remove=new List<GameObject>();foreach(Transform child in c)if(child.name=="CredenzaBody" || child.name=="CredenzaDoor" || child.name=="CredenzaPull")remove.Add(child.gameObject);
            foreach(var item in remove)UnityEngine.Object.DestroyImmediate(item);
            Box("CredenzaBack",c,new Vector3(0,.43f,.242f),new Vector3(1.35f,.76f,.025f),"Walnut");
            foreach(float x in new[]{-.663f,0f,.663f})Box("StorageUpright",c,new Vector3(x,.43f,0),new Vector3(.026f,.76f,.48f),"Walnut");
            foreach(float y in new[]{.075f,.405f,.79f})Box("StorageShelf",c,new Vector3(0,y,0),new Vector3(1.30f,.023f,.48f),"Walnut");
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;
                var pivot=Empty("CredenzaDoorHinge"+i,c,new Vector3(sign*.657f,.095f,-.266f));
                var hinge=pivot.gameObject.AddComponent<Hinge>();hinge.axis=Vector3.up;hinge.minAngle=i==0?0:-112;hinge.maxAngle=i==0?112:0;hinge.openAngle=-sign*108;hinge.itemId="CREDENZA_DOOR_"+i;
                Box("CredenzaDoorLeaf",pivot,new Vector3(-sign*.324f,.345f,0),new Vector3(.649f,.69f,.025f),"Walnut",true,.004f);
                hinge.handle=Cylinder("CredenzaDoorHandle",pivot,new Vector3(-sign*.56f,.445f,-.026f),.007f,.115f,"Brass",Quaternion.identity).transform;
                foreach(float y in new[]{.12f,.57f})Cylinder("CabinetHingePin",pivot,new Vector3(0,y,.012f),.005f,.055f,"Brass",Quaternion.identity);
                for(int row=0;row<2;row++)
                {
                    float y=row==0?.119f:.449f;
                    var anchor=Empty("A4ReamAnchor"+i+row,c,new Vector3(sign*.33f,y,-.02f));
                    var ream=Empty("CredenzaA4Ream"+i+row,c,anchor.localPosition);Body(ream.gameObject,.75f,true);
                    var grab=ream.gameObject.AddComponent<Grabbable>();grab.itemId="SUPPLY_REAM_"+i+row;grab.accessDoor=hinge;grab.storageAnchor=anchor;
                    Box("A4SheetEdges",ream,Vector3.zero,new Vector3(.21f,.061f,.297f),"CleanPaperEdges",true,.0004f);
                    Page("A4BlankTop",ream,new Vector3(0,.0307f,0),new Vector2(.21f,.297f),"Paper");
                    for(int n=0;n<8;n++)Box("FineSheetEdge",ream,new Vector3(0,-.025f+n*.007f,-.149f),new Vector3(.208f,.00025f,.0004f),"PaperEdges",false,.00002f);
                    var sheetAnchor=Empty("SupplySheetAnchor"+i+row,c,new Vector3(sign*.33f,y+.0325f,-.02f));
                    SupplySheet040(c,sheetAnchor,hinge,"SUPPLY_SHEET_"+i+row);
                }
            }
        }
        private static void SupplySheet040(Transform parent,Transform anchor,Hinge door,string id)
        {
            var sheet=Empty("LooseA4_"+id,parent,anchor.localPosition);Body(sheet.gameObject,.018f,true);
            var grab=sheet.gameObject.AddComponent<Grabbable>();grab.itemId=id;grab.accessDoor=door;grab.storageAnchor=anchor;
            var thick=Box("A4Thickness",sheet,Vector3.zero,new Vector3(.21f,.00018f,.297f),"Paper",true,.00002f);thick.GetComponent<BoxCollider>().size=new Vector3(.21f,.0015f,.297f);
            var front=Page("A4Front",sheet,new Vector3(0,.00013f,0),new Vector2(.21f,.297f),"Paper",null,true);
            var back=Page("A4Back",sheet,new Vector3(0,-.00013f,0),new Vector2(.21f,.297f),"Paper",Quaternion.Euler(180,0,0));
            var paper=sheet.gameObject.AddComponent<PaperDocument>();paper.documentId=id;paper.frontSurface=front.GetComponent<MeshFilter>();paper.backSurface=back.GetComponent<MeshFilter>();paper.thicknessRenderer=thick.GetComponent<MeshRenderer>();paper.feedReference=Empty("PunchEdgeReference",sheet,new Vector3(.093f,0,0));
            var writable=front.AddComponent<WritingSurface>();writable.freeForm=true;writable.ledgerIndex=-1;writable.inkMaterial=materials["WritingInk"];
        }
        private static void BuildStamp040(Transform parent)
        {
            var pad=Empty("BlueInkPad",parent,new Vector3(-.30f,.803f,.41f));
            Box("InkPadCase",pad,new Vector3(0,.008f,0),new Vector3(.094f,.016f,.073f),"OfficeDeskLeather",true,.005f);
            Box("BlueFeltPad",pad,new Vector3(0,.017f,0),new Vector3(.080f,.004f,.058f),"OfficeInkPad",true,.003f);
            var lid=Empty("InkPadLid",pad,new Vector3(0,.016f,.035f));var hinge=lid.gameObject.AddComponent<Hinge>();hinge.axis=Vector3.right;hinge.minAngle=0;hinge.maxAngle=120;hinge.openAngle=110;hinge.itemId="STAMP_PAD_LID";
            Box("InkPadCover",lid,new Vector3(0,.003f,-.035f),new Vector3(.094f,.008f,.073f),"OfficeDeskLeather",true,.004f);hinge.handle=Empty("LidGrip",lid,new Vector3(0,0,-.068f));hinge.SetAngle(110);
            var stamp=Empty("DiwanOfficeStamp",parent,new Vector3(-.29f,.803f,.29f));Body(stamp.gameObject,.12f,true);stamp.gameObject.AddComponent<Grabbable>().itemId="DIWAN_STAMP";
            Box("RubberDie",stamp,new Vector3(0,.003f,0),new Vector3(.054f,.006f,.032f),"BlackRubber",true,.002f);
            Box("StampPlate",stamp,new Vector3(0,.009f,0),new Vector3(.064f,.008f,.042f),"Walnut",true,.004f);
            Cylinder("TurnedWoodGrip",stamp,new Vector3(0,.040f,0),.012f,.057f,"Walnut",Quaternion.identity);
            var knob=GameObject.CreatePrimitive(PrimitiveType.Sphere);knob.name="RoundedStampGrip";knob.transform.SetParent(stamp,false);knob.transform.localPosition=new Vector3(0,.068f,0);knob.transform.localScale=new Vector3(.036f,.027f,.032f);knob.GetComponent<Renderer>().sharedMaterial=materials["Walnut"];
            Page("StampIdentification",stamp,new Vector3(0,.014f,.0f),new Vector2(.058f,.034f),"OfficeStampTop");
            var tool=stamp.gameObject.AddComponent<OfficeStamp>();tool.contact=Empty("RubberContact",stamp,Vector3.zero);tool.inkPad=Empty("InkContact",pad,new Vector3(0,.020f,0));tool.imprintMaterial=materials["OfficeStampImprint"];
            MergeVisuals040(stamp);
        }
        private static void BuildPenCup040(Transform parent)
        {
            var cup=Empty("LeatherPenHolder",parent,new Vector3(-.27f,.803f,.66f));
            Cylinder("CupBase",cup,new Vector3(0,.005f,0),.044f,.01f,"OfficeDeskLeather",Quaternion.identity);
            OfficeTube040("LeatherCupWall",cup,new Vector3(0,.059f,0),.044f,.040f,.108f,"OfficeDeskLeather");
            OfficeTube040("BrassRim",cup,new Vector3(0,.113f,0),.0444f,.0398f,.003f,"Brass");
            OfficeTube040("BrassFoot",cup,new Vector3(0,.009f,0),.0445f,.040f,.003f,"Brass");
            for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;var wall=Empty("CupWallCollision",cup,new Vector3(Mathf.Sin(a)*.042f,.059f,Mathf.Cos(a)*.042f));wall.localRotation=Quaternion.Euler(0,i*45,0);OfficeBox(wall,Vector3.zero,new Vector3(.0348f,.108f,.004f));}
            for(int i=0;i<5;i++)
            {
                float a=i*2.4f;var pen=Empty("SpareWritingPen"+i,parent,cup.localPosition+new Vector3(Mathf.Cos(a)*.020f,.082f,Mathf.Sin(a)*.020f));pen.localRotation=Quaternion.Euler(i%2==0?7:-6,0,i%2==0?-7:9);
                Body(pen.gameObject,.012f,true);pen.gameObject.AddComponent<Grabbable>().itemId="DESK_PEN_"+i;
                string mat=i<3?"OfficePenBlue":"OfficePenBurgundy";
                Cylinder("PenBarrel",pen,new Vector3(0,.022f,0),.0038f,.115f,mat,Quaternion.identity);
                Cylinder("SteelTip",pen,new Vector3(0,-.039f,0),.0018f,.012f,"Steel",Quaternion.identity);
                Cylinder("PushButton",pen,new Vector3(0,.083f,0),.0027f,.009f,"Steel",Quaternion.identity);
                Box("PocketClip",pen,new Vector3(.0048f,.060f,0),new Vector3(.0017f,.036f,.0026f),"Steel",false,.0003f);
                var tool=pen.gameObject.AddComponent<Pen>();tool.tip=Empty("WritingNib",pen,new Vector3(0,-.0455f,0));MergeVisuals040(pen);
            }
            MergeVisuals040(cup);
        }
        private static void BuildTelephone040(Transform parent)
        {
            var phone=Empty("DeskLandlineTelephone",parent,new Vector3(-.51f,.803f,.585f));
            Box("TelephoneBase",phone,new Vector3(0,.025f,0),new Vector3(.23f,.05f,.19f),"OfficePlasticDark",true,.016f);
            var top=Empty("SlopingTelephonePanel",phone,new Vector3(.023f,.052f,0));top.localRotation=Quaternion.Euler(-12,0,0);
            Box("PhonePanel",top,Vector3.zero,new Vector3(.166f,.018f,.172f),"OfficePlasticDark",true,.009f);
            Page("PhoneLCD",top,new Vector3(.018f,.010f,.052f),new Vector2(.095f,.032f),"OfficeTelephoneLCD");
            for(int row=0;row<4;row++)for(int col=0;col<3;col++)
            {
                Box("DialKey",top,new Vector3(-.013f+col*.027f,.013f,.015f-row*.022f),new Vector3(.023f,.009f,.017f),"OfficePlasticLight",true,.003f);
                var label=Page("DialKeyLegend",top,new Vector3(-.013f+col*.027f,.0181f,.015f-row*.022f),new Vector2(.020f,.015f),"OfficePhoneKeys");
                var mesh=UnityEngine.Object.Instantiate(label.GetComponent<MeshFilter>().sharedMesh);float l=col/3f,r=(col+1)/3f,b=1-(row+1)/4f,t=1-row/4f;
                mesh.uv=new[]{new Vector2(l,b),new Vector2(l,t),new Vector2(r,t),new Vector2(r,b)};label.GetComponent<MeshFilter>().sharedMesh=PersistMesh(mesh,"telephone_key");
            }
            for(int i=0;i<6;i++)Box("SpeakerGrille",top,new Vector3(-.062f,.010f,-.041f+i*.011f),new Vector3(.021f,.003f,.002f),"BlackRubber",false,.0002f);
            foreach(float z in new[]{-.06f,.065f})Box("ReceiverCradle",phone,new Vector3(-.079f,.065f,z),new Vector3(.048f,.036f,.026f),"OfficePlasticDark",true,.006f);
            var dock=Empty("HandsetDock",phone,new Vector3(-.079f,.104f,0));
            var handset=Empty("TelephoneHandset",phone,dock.localPosition);Body(handset.gameObject,.19f,true);handset.gameObject.AddComponent<Grabbable>().itemId="PHONE_HANDSET";
            Box("ReceiverBridge",handset,new Vector3(0,.009f,0),new Vector3(.033f,.026f,.104f),"OfficePlasticDark",true,.012f);
            foreach(float z in new[]{-.07f,.07f})
            {
                Box("ReceiverEarpiece",handset,new Vector3(0,0,z),new Vector3(.057f,.040f,.061f),"OfficePlasticDark",true,.014f);
                for(int i=0;i<5;i++)Cylinder("AcousticHole",handset,new Vector3((i-2)*.006f,-.0205f,z),.0013f,.001f,"BlackRubber",Quaternion.identity);
            }
            var receiver=handset.gameObject.AddComponent<LandlineHandset>();receiver.cradle=dock;receiver.cordStart=Empty("CordSocket",phone,new Vector3(-.110f,.035f,-.064f));receiver.cordEnd=Empty("HandsetCordSocket",handset,new Vector3(-.016f,-.004f,-.089f));receiver.cordMaterial=materials["BlackRubber"];
            var phoneLine=new[]{new Vector3(.08f,.025f,.095f),new Vector3(.085f,-.10f,.19f),new Vector3(.09f,-.50f,.29f),new Vector3(-.1f,-.50f,.31f)};
            TubePath040("TelephoneLine",phone,phoneLine,.002f,"BlackRubber");
            MergeVisuals040(handset);MergeVisuals040(phone,true);
        }
        private static void BuildLetterTrays040(Transform parent)
        {
            var tray=Empty("DesktopLetterOrganizer",parent,new Vector3(.63f,.801f,.605f));
            for(int i=0;i<2;i++)
            {
                float y=.018f+i*.085f;
                Box("LetterTrayBase",tray,new Vector3(0,y,0),new Vector3(.252f,.006f,.319f),"OfficeDeskLeather",true,.003f);
                foreach(float x in new[]{-.124f,.124f})Box("TraySide",tray,new Vector3(x,y+.018f,0),new Vector3(.006f,.038f,.319f),"OfficeDeskLeather",true,.002f);
                Box("TrayBack",tray,new Vector3(0,y+.019f,.157f),new Vector3(.25f,.04f,.006f),"OfficeDeskLeather",true,.002f);
                Box("BrassFrontLip",tray,new Vector3(0,y+.004f,-.158f),new Vector3(.248f,.009f,.004f),"Brass",false,.001f);
                if(i==0){Box("BlankTrayPaperEdges",tray,new Vector3(0,y+.010f,0),new Vector3(.21f,.011f,.297f),"CleanPaperEdges",true,.0002f);Page("TrayBlankPaper",tray,new Vector3(0,y+.0157f,0),new Vector2(.21f,.297f),"Paper");}
            }
            foreach(float x in new[]{-.121f,.121f})foreach(float z in new[]{-.11f,.11f})Cylinder("OrganizerSupport",tray,new Vector3(x,.055f,z),.003f,.108f,"Brass",Quaternion.identity);
            MergeVisuals040(tray);
        }
        private static void BuildSecurityCamera040(Transform parent)
        {
            var mount=Empty("WallCCTVAssembly",parent,new Vector3(-3.493f,2.73f,-2.80f));
            Box("CameraWallPlate",mount,Vector3.zero,new Vector3(.025f,.12f,.10f),"OfficePlasticLight",true,.012f);
            foreach(float z in new[]{-.035f,.035f})foreach(float y in new[]{-.045f,.045f})Cylinder("MountScrew",mount,new Vector3(.015f,y,z),.003f,.003f,"Steel",Quaternion.Euler(0,0,90));
            Box("CameraBracket",mount,new Vector3(.08f,-.018f,0),new Vector3(.16f,.028f,.028f),"OfficePlasticLight",true,.008f);
            var camera=Empty("SecurityCameraHead",mount,new Vector3(.18f,-.025f,.025f));camera.rotation=Quaternion.LookRotation(new Vector3(.1f,1.05f,.4f)-camera.position);
            Cylinder("CameraHousing",camera,Vector3.zero,.045f,.16f,"OfficePlasticLight",Quaternion.Euler(90,0,0));
            Cylinder("FrontBezel",camera,new Vector3(0,0,.084f),.042f,.012f,"OfficePlasticDark",Quaternion.Euler(90,0,0));
            Cylinder("CameraGlass",camera,new Vector3(0,0,.091f),.035f,.002f,"OfficeLens",Quaternion.Euler(90,0,0));
            Cylinder("OpticalLens",camera,new Vector3(0,0,.093f),.014f,.002f,"BlackRubber",Quaternion.Euler(90,0,0));
            Cylinder("LensHighlight",camera,new Vector3(-.003f,.004f,.0945f),.004f,.001f,"OfficeLens",Quaternion.Euler(90,0,0));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Cylinder("IRLamp",camera,new Vector3(Mathf.Cos(a)*.026f,Mathf.Sin(a)*.026f,.093f),.003f,.002f,"OfficeMetalSteel",Quaternion.Euler(90,0,0));}
            Box("WeatherHood",camera,new Vector3(0,.046f,.014f),new Vector3(.105f,.012f,.19f),"OfficePlasticLight",true,.006f);
            TubePath040("CameraServiceCable",mount,new[]{new Vector3(.12f,-.025f,0),new Vector3(.02f,-.08f,0),new Vector3(.012f,-.30f,0),new Vector3(.012f,-.50f,0)},.003f,"OfficePlasticDark");
            MergeVisuals040(mount);
        }
        private static void OfficeTube040(string name,Transform parent,Vector3 pos,float outer,float inner,float height,string mat)
        {
            const int n=48;var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int wall=0;wall<4;wall++)for(int i=0;i<=n;i++)for(int edge=0;edge<2;edge++)
            {
                float a=i*2*Mathf.PI/n,r=wall==0?outer:wall==1?inner:edge==0?inner:outer;
                float y=wall<2?(edge==0?-height*.5f:height*.5f):wall==2?height*.5f:-height*.5f;
                vertices.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));uv.Add(new Vector2((float)i/n,edge));
                if(i<n && edge==0){int v=wall*(n+1)*2+i*2;bool flip=wall==0 || wall==3;if(flip)tris.AddRange(new[]{v,v+1,v+2,v+1,v+3,v+2});else tris.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3});}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var root=Empty(name,parent,pos);root.gameObject.AddComponent<MeshFilter>().sharedMesh=PersistMesh(mesh,"cup");root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials[mat];
        }
        private static void TubePath040(string name,Transform parent,Vector3[] points,float radius,string mat)
        {
            for(int i=1;i<points.Length;i++){Vector3 d=points[i]-points[i-1];Cylinder(name,parent,(points[i]+points[i-1])*.5f,radius,d.magnitude,mat,Quaternion.FromToRotation(Vector3.up,d.normalized));}
        }
        private static void MergeVisuals040(Transform root,bool excludeGrabbableChildren=false)
        {
            var groups=new Dictionary<Material,List<MeshFilter>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(excludeGrabbableChildren && filter.GetComponentInParent<Grabbable>())continue;
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer || !filter.sharedMesh || renderer.sharedMaterials.Length!=1)continue;
                var mat=renderer.sharedMaterial;if(!groups.ContainsKey(mat))groups[mat]=new List<MeshFilter>();groups[mat].Add(filter);
            }
            foreach(var pair in groups)
            {
                if(pair.Value.Count<2)continue;
                var combines=new List<CombineInstance>();foreach(var f in pair.Value)combines.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
                var mesh=new Mesh{name=root.name+"_"+pair.Key.name};mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();
                var child=Empty("Combined_"+pair.Key.name,root,Vector3.zero);child.gameObject.AddComponent<MeshFilter>().sharedMesh=PersistMesh(mesh,"combined");child.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
                foreach(var f in pair.Value){UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(f);}
            }
        }
    }
}
