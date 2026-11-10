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
        public const string Hardware030Marker="OfficeHardware030";
        private const string Model030Path="Assets/Diwan/Models/Office030/";
        [Serializable] private class OfficeVertex {public Vector3 p,n;public Vector2 uv;}
        [Serializable] private class OfficeSurface {public string material;public int[] triangles;}
        [Serializable] private class OfficeGeometry {public string name;public OfficeVertex[] vertices;public OfficeSurface[] surfaces;}
        private static readonly Dictionary<string,Mesh> officeMeshes=new Dictionary<string,Mesh>();
        private static readonly Dictionary<string,Material[]> officeMeshMaterials=new Dictionary<string,Material[]>();
        private static Material OfficeSurfaceMaterial(string name)
        {
            switch(name)
            {
                case "Walnut":return materials["Walnut"];
                case "Glass":return materials["Glass"];
                case "Paper":return materials["CleanPaperEdges"];
                case "Screen":return materials["OfficeScreenOn"];
                case "PrinterLCD":return materials["OfficePrinterReady"];
                default:return materials["Office"+name];
            }
        }
        private static void OfficeUnlit(string name,string texture,Color color)
        {
            var material=Mat(name,color,0,0,texture);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.shaderKeywords=Array.Empty<string>();material.SetColor("_BaseColor",color);
        }
        private static void PrepareOffice030()
        {
            if(materials==null)materials=new Dictionary<string,Material>();
            if(boxCache==null)boxCache=new Dictionary<string,Mesh>();
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Diwan/Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(material)materials[material.name]=material;
            }
            Mat("OfficePlasticDark",new Color(.045f,.052f,.058f),0,.33f);
            Mat("OfficePlasticLight",new Color(.68f,.71f,.70f),0,.30f);
            Mat("OfficeMetalDark",new Color(.055f,.065f,.068f),.70f,.42f);
            Mat("OfficeMetalSteel",new Color(.45f,.49f,.51f),.82f,.65f);
            Mat("OfficeClothSage",new Color(.17f,.23f,.21f),0,.14f,"Office030/UpholsteryAlbedo");
            Mat("OfficeClothCharcoal",new Color(.075f,.088f,.093f),0,.12f,"Office030/UpholsteryAlbedo");
            SurfaceDetail("OfficeClothSage","Office030/UpholsteryNormal",.25f);SurfaceDetail("OfficeClothCharcoal","Office030/UpholsteryNormal",.22f);
            Mat("OfficeStitch",new Color(.30f,.36f,.32f),0,.08f);
            Mat("OfficeRubber",new Color(.018f,.020f,.022f),0,.12f);
            Mat("OfficeKeyboardLegends",Color.white,0,.24f,"Office030/KeyboardLegends");
            OfficeUnlit("OfficeScreenOn","Office030/ComputerDesktop",Color.white);
            Mat("OfficeScreenOff",new Color(.007f,.012f,.016f),.10f,.65f);
            OfficeUnlit("OfficeLED",null,new Color(.12f,.60f,.21f));
            foreach(string state in new[]{"Ready","NoDocument","Busy","NoPaper"})OfficeUnlit("OfficePrinter"+state,"Office030/Printer"+state,Color.white);
            officeMeshes.Clear();officeMeshMaterials.Clear();
            foreach(string name in new[]{"ComputerWorkstation","MonitorScreen","MonitorLED","ComputerMouse","PrinterBody","PrinterLid","PrinterDisplay","EmployeeBase","EmployeeSeat","VisitorChair","WaitingBench"})
            {
                LoadOfficeGeometry(name);if(File.Exists(Model030Path+name+"_LOD.json"))LoadOfficeGeometry(name+"_LOD");
            }
        }
        private static void LoadOfficeGeometry(string name)
        {
            var text=AssetDatabase.LoadAssetAtPath<TextAsset>(Model030Path+name+".json");
            if(!text)throw new InvalidOperationException("Office geometry is not imported: "+name);
            var data=JsonUtility.FromJson<OfficeGeometry>(text.text);
            if(data==null || data.vertices==null || data.vertices.Length<3 || data.surfaces==null)throw new InvalidOperationException("Invalid office geometry: "+name);
            var mesh=new Mesh{name=name};if(data.vertices.Length>65535)mesh.indexFormat=IndexFormat.UInt32;
            var vertices=new Vector3[data.vertices.Length];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=data.vertices[i].p;normals[i]=data.vertices[i].n;uv[i]=data.vertices[i].uv;}
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=data.surfaces.Length;
            var mats=new Material[data.surfaces.Length];
            for(int i=0;i<mats.Length;i++){mesh.SetTriangles(data.surfaces[i].triangles,i);mats[i]=OfficeSurfaceMaterial(data.surfaces[i].material);}
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            // Include submesh boundaries in the asset key; materials depend on them.
            string path="Assets/Diwan/Meshes/office030_"+name+"_"+Hash128.Compute(text.text)+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            officeMeshes[name]=mesh;officeMeshMaterials[name]=mats;
        }
        private static Transform OfficeModel(string objectName,string model,Transform parent,Vector3 position,Quaternion rotation,bool lod=true)
        {
            var root=Empty(objectName,parent,position);root.localRotation=rotation;
            Func<string,string,MeshRenderer> add=(child,key)=>
            {
                var visual=Empty(child,root,Vector3.zero);visual.gameObject.AddComponent<MeshFilter>().sharedMesh=officeMeshes[key];
                var renderer=visual.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=officeMeshMaterials[key];return renderer;
            };
            var high=add("DetailNear",model);
            if(lod && officeMeshes.ContainsKey(model+"_LOD"))
            {
                var low=add("DetailFar",model+"_LOD");var group=root.gameObject.AddComponent<LODGroup>();
                group.SetLODs(new[]{new LOD(.16f,new Renderer[]{high}),new LOD(.015f,new Renderer[]{low})});group.RecalculateBounds();group.fadeMode=LODFadeMode.None;
            }
            return root;
        }
        private static void OfficeBox(Transform root,Vector3 center,Vector3 size)
        {
            var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;
        }
        private static void OfficeLeg(Transform root,float x,float z,float height,float radius,float floorOffset=0)
        {
            var leg=Empty("LegCollision",root,new Vector3(x,floorOffset+height*.5f,z));var collider=leg.gameObject.AddComponent<CapsuleCollider>();collider.direction=1;collider.radius=radius;collider.height=height;
        }
        private static Pressable OfficeButton(Transform parent,string name,Vector3 position,Pressable.Kind kind)
        {
            var button=Empty(name,parent,position);OfficeBox(button,Vector3.zero,new Vector3(.018f,.012f,.018f));
            var press=button.gameObject.AddComponent<Pressable>();press.kind=kind;return press;
        }
        private static void BuildProfessionalOffice030()
        {
            PrepareOffice030();
            var previous=GameObject.Find(Hardware030Marker);if(previous)UnityEngine.Object.DestroyImmediate(previous);
            var hardware=Empty(Hardware030Marker,null,Vector3.zero);
            var workstation=OfficeModel("ComputerWorkstation","ComputerWorkstation",hardware,new Vector3(2.85f,0,.35f),Quaternion.Euler(0,90,0));
            OfficeBox(workstation,new Vector3(0,.785f,0),new Vector3(1.30f,.048f,.76f));
            OfficeBox(workstation,new Vector3(0,.47f,.29f),new Vector3(1.12f,.41f,.024f));
            foreach(float x in new[]{-.56f,.56f})foreach(float z in new[]{-.28f,.28f})OfficeLeg(workstation,x,z,.76f,.026f);
            OfficeBox(workstation,new Vector3(.43f,.29f,.035f),new Vector3(.19f,.46f,.36f));
            OfficeBox(workstation,new Vector3(0,1.161f,.18f),new Vector3(.567f,.337f,.042f));
            OfficeBox(workstation,new Vector3(-.008f,.835f,-.15f),new Vector3(.455f,.040f,.178f));
            var screen=OfficeModel("ComputerDisplay","MonitorScreen",workstation,Vector3.zero,Quaternion.identity,false);
            var led=OfficeModel("MonitorPowerLED","MonitorLED",workstation,Vector3.zero,Quaternion.identity,false);
            var monitor=workstation.gameObject.AddComponent<PhysicalMonitor>();monitor.display=screen.GetComponentInChildren<Renderer>();monitor.powerLED=led.GetComponentInChildren<Renderer>();
            monitor.screenOn=materials["OfficeScreenOn"];monitor.screenOff=materials["OfficeScreenOff"];monitor.ledOn=materials["OfficeLED"];monitor.ledOff=materials["OfficePlasticDark"];
            OfficeButton(workstation,"MonitorPowerButton",new Vector3(.245f,.995f,.147f),Pressable.Kind.MonitorPower).monitor=monitor;
            var mouse=OfficeModel("WirelessOfficeMouse","ComputerMouse",workstation,new Vector3(.354f,.812f,-.147f),Quaternion.identity);
            Body(mouse.gameObject,.085f,true);mouse.gameObject.AddComponent<Grabbable>().itemId="COMPUTER_MOUSE";OfficeBox(mouse,new Vector3(0,.020f,0),new Vector3(.062f,.040f,.108f));

            var printerRoot=OfficeModel("OfficeLaserPrinter","PrinterBody",hardware,new Vector3(-1.65f,.860f,2.88f),Quaternion.identity);
            OfficeBox(printerRoot,new Vector3(0,.155f,.075f),new Vector3(.48f,.29f,.25f));
            foreach(float x in new[]{-.216f,.216f})OfficeBox(printerRoot,new Vector3(x,.15f,-.133f),new Vector3(.048f,.28f,.166f));
            OfficeBox(printerRoot,new Vector3(0,.044f,-.134f),new Vector3(.404f,.065f,.163f));
            OfficeBox(printerRoot,new Vector3(0,.242f,-.133f),new Vector3(.404f,.083f,.163f));
            OfficeBox(printerRoot,new Vector3(0,.334f,-.004f),new Vector3(.355f,.004f,.290f));
            OfficeBox(printerRoot,new Vector3(0,.097f,-.248f),new Vector3(.265f,.012f,.18f));
            var lid=OfficeModel("ScannerLidHinge","PrinterLid",printerRoot,new Vector3(0,.352f,.185f),Quaternion.identity);
            OfficeBox(lid,new Vector3(0,.016f,-.185f),new Vector3(.485f,.03f,.4f));
            var hinge=lid.gameObject.AddComponent<Hinge>();hinge.axis=Vector3.right;hinge.minAngle=0;hinge.maxAngle=105;hinge.openAngle=85;hinge.itemId="SCANNER_LID";hinge.handle=Empty("LidGrip",lid,new Vector3(0,.010f,-.382f));
            var lcd=OfficeModel("PrinterPhysicalLCD","PrinterDisplay",printerRoot,Vector3.zero,Quaternion.identity,false);
            var printer=printerRoot.gameObject.AddComponent<OfficePrinter>();printer.scannerLid=hinge;printer.scannerSurface=Empty("ScannerA4Reference",printerRoot,new Vector3(0,.337f,-.004f));
            printer.outputStart=Empty("OutputStart",printerRoot,new Vector3(0,.157f,-.09f));printer.outputEnd=Empty("OutputEnd",printerRoot,new Vector3(0,.112f,-.28f));printer.display=lcd.GetComponentInChildren<Renderer>();
            printer.ready=materials["OfficePrinterReady"];printer.noDocument=materials["OfficePrinterNoDocument"];printer.printing=materials["OfficePrinterBusy"];printer.noPaper=materials["OfficePrinterNoPaper"];printer.screenOff=materials["OfficeScreenOff"];printer.inkMaterial=materials["WritingInk"];
            printer.motorSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Diwan/Audio/OfficePrinterMotor.wav");
            foreach(var pair in new[]{new KeyValuePair<float,Pressable.Kind>(.125f,Pressable.Kind.PrinterPower),new KeyValuePair<float,Pressable.Kind>(.158f,Pressable.Kind.PrinterCopy),new KeyValuePair<float,Pressable.Kind>(.192f,Pressable.Kind.PrinterCancel)})OfficeButton(printerRoot,pair.Value.ToString(),new Vector3(pair.Key,.323f,-.25f),pair.Value).printer=printer;

            var employee=Empty("ErgonomicEmployeeChair",hardware,new Vector3(2.04f,.008f,.35f));employee.localRotation=Quaternion.Euler(0,-90,0);
            Body(employee.gameObject,8,true);employee.gameObject.AddComponent<Grabbable>().itemId="EMPLOYEE_CHAIR";
            OfficeModel("FiveCasterBase","EmployeeBase",employee,Vector3.zero,Quaternion.identity);OfficeBox(employee,new Vector3(0,.105f,0),new Vector3(.61f,.206f,.61f));
            OfficeLeg(employee,0,0,.32f,.044f,.11f);
            var seat=OfficeModel("SwivelSeat","EmployeeSeat",employee,new Vector3(0,.44f,0),Quaternion.identity);
            OfficeBox(seat,new Vector3(0,.04f,0),new Vector3(.487f,.09f,.47f));OfficeBox(seat,new Vector3(0,.43f,.21f),new Vector3(.47f,.60f,.075f));
            foreach(float x in new[]{-.255f,.255f})OfficeBox(seat,new Vector3(x,.22f,.01f),new Vector3(.069f,.045f,.267f));
            var swivel=seat.gameObject.AddComponent<Hinge>();swivel.axis=Vector3.up;swivel.minAngle=-180;swivel.maxAngle=180;swivel.openAngle=45;swivel.itemId="CHAIR_SWIVEL";swivel.handle=Empty("SeatGrip",seat,new Vector3(0,.043f,-.235f));
            var visitor=OfficeModel("VisitorChair030","VisitorChair",hardware,new Vector3(0,.001f,1.32f),Quaternion.identity);
            Body(visitor.gameObject,6.5f,true);visitor.gameObject.AddComponent<Grabbable>().itemId="VISITOR_CHAIR";
            OfficeBox(visitor,new Vector3(0,.462f,0),new Vector3(.466f,.10f,.455f));OfficeBox(visitor,new Vector3(0,.80f,.22f),new Vector3(.445f,.31f,.065f));
            foreach(float x in new[]{-.20f,.20f})foreach(float z in new[]{-.18f,.18f})OfficeLeg(visitor,x,z,.422f,.025f,.008f);
            foreach(float x in new[]{-.208f,.208f})OfficeBox(visitor,new Vector3(x,.681f,.008f),new Vector3(.05f,.04f,.345f));
            var bench=OfficeModel("WaitingBench030","WaitingBench",hardware,new Vector3(3.08f,.005f,-1.68f),Quaternion.Euler(0,90,0));
            OfficeBox(bench,new Vector3(0,.47f,0),new Vector3(1.39f,.13f,.47f));OfficeBox(bench,new Vector3(0,.76f,.205f),new Vector3(1.39f,.35f,.08f));
            foreach(float x in new[]{-.59f,.59f})foreach(float z in new[]{-.17f,.17f})OfficeLeg(bench,x,z,.43f,.027f);
            foreach(float x in new[]{-.704f,.704f})OfficeBox(bench,new Vector3(x,.68f,0),new Vector3(.055f,.06f,.33f));
            var calendar=GameObject.Find("PaperDeskCalendar");if(calendar){calendar.transform.SetParent(hardware,true);calendar.transform.position=new Vector3(.25f,.895f,.64f);}
            var oldChair=GameObject.Find("OfficeChair");if(oldChair)UnityEngine.Object.DestroyImmediate(oldChair);
            var oldBench=GameObject.Find("WaitingBench");if(oldBench)UnityEngine.Object.DestroyImmediate(oldBench);
            visitor.name="OfficeChair";bench.name="WaitingBench";
        }
        public static void ApplyProfessionalOffice030()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before upgrading the scene.");
            if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory("Assets/Diwan/Meshes");Directory.CreateDirectory("Validation");
            BuildProfessionalOffice030();ConfigureContactSounds();MarkEnvironmentStatic();ConfigureSettings();
            Lightmapping.Clear();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            File.WriteAllText("Validation/office030_applied.json","{\"version\":\"0.3.0\",\"appliedInUnityEditor\":true,\"apkBuilt\":false}");
        }
    }
}
