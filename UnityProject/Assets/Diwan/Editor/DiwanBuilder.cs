using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace DiwanVR.Editor
{
    public static partial class DiwanBuilder
    {
        public const string ScenePath="Assets/Diwan/Scenes/Diwan.unity";
        // The version of the source tree. Reports read this instead of a
        // hand-written string that silently goes stale.
        public const string SourceVersion="0.4.4";
        private static Dictionary<string,Material> materials;
        private static int meshIndex;
        private static Dictionary<string,Mesh> boxCache;
        private static readonly string[] regNames={"سجل البرقيات الوارد","سجل الرسائل الواردة","سجل الضبوط","سجل الرسائل الصادرة","دفتر الذمة"};
        private static readonly string[] binderNames={"مصنف نسخ الضبوط","مصنف الرسائل السرية","مصنف الرسائل الواردة","مصنف رؤساء السلك","مصنف تقارير التفتيش","مصنف البرقيات","مصنف الاوامر العامة","مصنف احتياطي"};

        [MenuItem("Diwan VR/Build training room")]
        public static void Bootstrap()
        {
            Directory.CreateDirectory("Assets/Diwan/Meshes");Directory.CreateDirectory("Assets/Diwan/Materials");Directory.CreateDirectory("Validation");
            meshIndex=0;materials=new Dictionary<string,Material>();boxCache=new Dictionary<string,Mesh>();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            MakeMaterials();MakeRealismMaterials();
            BuildExpandedOffice();BuildDesk();BuildLibrary();OptimizeReferenceBooks();BuildOfficeDetails();
            var sessionObject=new GameObject("TrainingSession");var session=sessionObject.AddComponent<DiwanSession>();
            session.ledgers=new LedgerBook[5];session.binders=new Binder[8];
            Hinge cabinet=BuildCabinet(session);session.cabinetDoor=cabinet;
            for(int i=0;i<5;i++) session.ledgers[i]=BuildLedger(i,new Vector3(.64f,.819f+(4-i)*.036f,.26f));
            session.letter=BuildLetter();BuildPen();BuildPunch();BuildStapler();BuildRig();BuildProfessionalOffice030();ConfigureContactSounds();
            ConfigureSettings();
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.52f,.56f);
            RenderSettings.ambientEquatorColor=new Color(.37f,.38f,.37f);
            RenderSettings.ambientGroundColor=new Color(.22f,.21f,.19f);
            RenderSettings.ambientIntensity=.75f;RenderSettings.reflectionIntensity=.48f;
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Diwan/Materials/DaySky.mat");
            if(!sky){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,"Assets/Diwan/Materials/DaySky.mat");}
            sky.SetColor("_SkyTint",new Color(.57f,.65f,.74f));sky.SetColor("_GroundColor",new Color(.47f,.44f,.38f));sky.SetFloat("_Exposure",1.15f);RenderSettings.skybox=sky;
            MarkEnvironmentStatic();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
            ApplyOffice040();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            File.WriteAllText("Validation/bootstrap_complete.json","{\"scene\":\"Diwan\",\"ledgers\":5,\"binders\":8,\"traineeHUD\":false}");
            Debug.Log("DIWAN_BOOTSTRAP_COMPLETE");
        }
        private static Material Mat(string name,Color color,float metal=0,float smooth=.25f,string texture=null)
        {
            string path="Assets/Diwan/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.shader=Shader.Find("Universal Render Pipeline/Lit");
            m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Diwan/Textures/"+texture+".png"));
            materials[name]=m;return m;
        }
        private static void MakeMaterials()
        {
            Mat("Walnut",Color.white,0,.34f,"Walnut_Albedo");Mat("WoodEdge",new Color(.20f,.12f,.065f),0,.35f);
            Mat("Plaster",new Color(.78f,.775f,.72f),0,.1f);Mat("Ceiling",new Color(.88f,.87f,.82f),0,.07f);
            Mat("Floor",Color.white,0,.18f,"Floor");Mat("Grout",new Color(.39f,.38f,.35f),0,.05f);
            Mat("Brass",new Color(.51f,.36f,.16f),.78f,.72f);Mat("Steel",new Color(.58f,.61f,.63f),.86f,.76f);
            Mat("InkBlue",new Color(.045f,.10f,.19f),.06f,.45f);Mat("BinderCloth",Color.white,0,.20f,"Cloth");
            Mat("WritingInk",new Color(.045f,.12f,.28f),0,.08f);
            Mat("Paper",Color.white,0,.05f,"Paper");Mat("PaperEdges",new Color(.79f,.76f,.68f),0,.08f);
            Mat("BlackRubber",new Color(.035f,.038f,.040f),0,.25f);Mat("Leather",new Color(.15f,.115f,.078f),0,.30f);
            Mat("HandSkin",new Color(.62f,.40f,.28f),0,.3f);
            var glass=Mat("Glass",new Color(.65f,.77f,.81f,.12f),.05f,.9f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            var light=Mat("Lamp",new Color(.97f,.94f,.80f),0,.3f);light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(1,.91f,.69f)*2);
            for(int i=0;i<5;i++){Mat("RegLabel"+i,Color.white,0,.05f,"RegisterLabel"+i);Mat("RegisterPage"+i,Color.white,0,.04f,"RegisterPage"+i);}
            for(int i=0;i<8;i++)Mat("BinderLabel"+i,Color.white,0,.05f,"BinderLabel"+i);
            foreach(string n in new[]{"IncomingLetter","RegisterPage","PreviousLetter","ReceiptSlip","DoorSign","DepartmentSign"})Mat(n,Color.white,0,.04f,n);
            var clock=Mat("ClockFace",Color.white,0,.08f,"ClockFace");clock.SetFloat("_AlphaClip",1);clock.SetFloat("_Cutoff",.4f);clock.EnableKeyword("_ALPHATEST_ON");
        }
        private static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,string mat,bool collider=true,float bevel=.003f)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            var mf=g.AddComponent<MeshFilter>();var renderer=g.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=materials[mat];
            mf.sharedMesh=BevelBox(scale,Mathf.Min(bevel,Mathf.Min(scale.x,Mathf.Min(scale.y,scale.z))*.22f));
            if(collider){var c=g.AddComponent<BoxCollider>();c.size=scale;}
            return g;
        }
        private static Mesh BevelBox(Vector3 size,float radius)
        {
            string key=size.ToString("F4")+radius.ToString("F4");
            if(boxCache.TryGetValue(key,out Mesh cached))return cached;
            // A rounded box with planar centres and smoothly chamfered edges.
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            Vector3 ext=size*.5f;Vector3 inner=ext-Vector3.one*radius;
            Vector3[] ns={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var n in ns)
            {
                Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n);
                Vector3 v=Vector3.Cross(n,u);
                float ue=Vector3.Dot(Abs(u),ext),ve=Vector3.Dot(Abs(v),ext),ne=Vector3.Dot(Abs(n),ext);
                float[] us={-ue,-ue+radius,ue-radius,ue};float[] vs={-ve,-ve+radius,ve-radius,ve};
                int start=vertices.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 raw=n*ne+u*us[x]+v*vs[y];Vector3 q=new Vector3(Mathf.Clamp(raw.x,-inner.x,inner.x),Mathf.Clamp(raw.y,-inner.y,inner.y),Mathf.Clamp(raw.z,-inner.z,inner.z));
                    Vector3 normal=(raw-q).normalized;vertices.Add(q+normal*radius);normals.Add(normal);uv.Add(new Vector2((us[x]/ue+1)*.5f,(vs[y]/ve+1)*.5f));
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=start+y*4+x;tris.Add(a);tris.Add(a+1);tris.Add(a+5);tris.Add(a);tris.Add(a+5);tris.Add(a+4);}
            }
            var mesh=new Mesh {name="BevelledBox"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();
            mesh=PersistMesh(mesh,"Assets/Diwan/Meshes/box_"+(meshIndex++)+".asset");boxCache[key]=mesh;return mesh;
        }
        private static Mesh PersistMesh(Mesh mesh,string path)
        {
            // Mesh assets are immutable and identified by geometry. Rebuilding
            // must not alter a mesh that an existing object still references.
            var key=new System.Text.StringBuilder();
            foreach(var v in mesh.vertices)key.Append(v.ToString("F6"));
            foreach(var n in mesh.normals)key.Append(n.ToString("F6"));
            foreach(var uv in mesh.uv)key.Append(uv.ToString("F6"));
            foreach(int i in mesh.triangles)key.Append(i).Append(',');
            path="Assets/Diwan/Meshes/generated_"+Hash128.Compute(key.ToString())+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){UnityEngine.Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        private static GameObject Cylinder(string name,Transform parent,Vector3 pos,float radius,float height,string mat,Quaternion rotation)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localRotation=rotation;g.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);g.GetComponent<Renderer>().sharedMaterial=materials[mat];return g;
        }
        private static GameObject Page(string name,Transform parent,Vector3 pos,Vector2 size,string mat,Quaternion? rotation=null,bool collider=false)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;if(rotation.HasValue)g.transform.localRotation=rotation.Value;
            var mesh=new Mesh {name="PrintedPaperSurface"};float x=size.x*.5f,z=size.y*.5f;
            mesh.vertices=new[]{new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            mesh=PersistMesh(mesh,"Assets/Diwan/Meshes/page_"+(meshIndex++)+".asset");
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=materials[mat];
            if(collider){var c=g.AddComponent<BoxCollider>();c.size=new Vector3(size.x,.002f,size.y);}
            return g;
        }
        private static Rigidbody Body(GameObject g,float mass,bool kinematic=false)
        {
            var rb=g.AddComponent<Rigidbody>();rb.mass=mass;rb.isKinematic=kinematic;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=kinematic?CollisionDetectionMode.Discrete:CollisionDetectionMode.ContinuousDynamic;rb.maxAngularVelocity=8;return rb;
        }
        private static Transform Empty(string name,Transform parent,Vector3 pos)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;return g.transform;
        }
        private static void BuildRoom()
        {
            var room=new GameObject("OfficeInterior").transform;
            Box("Subfloor",room,new Vector3(0,-.08f,0),new Vector3(4.6f,.16f,4.6f),"Grout");
            for(int x=0;x<8;x++)for(int z=0;z<8;z++)Box("CeramicTile",room,new Vector3(-2.01f+x*.575f,.003f,-2.01f+z*.575f),new Vector3(.571f,.012f,.571f),"Floor",false,.001f);
            Box("BackWall",room,new Vector3(0,1.45f,2.27f),new Vector3(4.6f,2.9f,.16f),"Plaster");
            Box("LeftWall",room,new Vector3(-2.27f,1.45f,0),new Vector3(.16f,2.9f,4.6f),"Plaster");
            Box("RightWallLower",room,new Vector3(2.27f,.53f,0),new Vector3(.16f,1.06f,4.6f),"Plaster");
            Box("RightWallUpper",room,new Vector3(2.27f,2.6f,0),new Vector3(.16f,.6f,4.6f),"Plaster");
            Box("RightWallFront",room,new Vector3(2.27f,1.68f,-1.68f),new Vector3(.16f,1.24f,1.25f),"Plaster");
            Box("RightWallBack",room,new Vector3(2.27f,1.68f,1.68f),new Vector3(.16f,1.24f,1.25f),"Plaster");
            Box("FrontWallLeft",room,new Vector3(-1.37f,1.45f,-2.27f),new Vector3(1.75f,2.9f,.16f),"Plaster");
            Box("FrontWallRight",room,new Vector3(1.37f,1.45f,-2.27f),new Vector3(1.75f,2.9f,.16f),"Plaster");
            Box("FrontDoorLintel",room,new Vector3(0,2.59f,-2.27f),new Vector3(1.0f,.62f,.16f),"Plaster");
            Box("Ceiling",room,new Vector3(0,2.93f,0),new Vector3(4.6f,.12f,4.6f),"Ceiling");
            Box("SkirtingBack",room,new Vector3(0,.065f,2.16f),new Vector3(4.4f,.13f,.035f),"WoodEdge");
            Box("SkirtingLeft",room,new Vector3(-2.16f,.065f,0),new Vector3(.035f,.13f,4.4f),"WoodEdge");
            Box("SkirtingRight",room,new Vector3(2.16f,.065f,0),new Vector3(.035f,.13f,4.4f),"WoodEdge");
            var door=Empty("EntryDoor",room,new Vector3(0,1.12f,-2.2f));
            Box("DoorPanel",door,Vector3.zero,new Vector3(.92f,2.20f,.045f),"Walnut");
            for(float xx=-.34f;xx<.5f;xx+=.68f)Box("DoorInsetMoulding",door,new Vector3(xx,0,.028f),new Vector3(.018f,1.9f,.013f),"WoodEdge");
            Page("DoorNameplate",door,new Vector3(0,.25f,.031f),new Vector2(.28f,.11f),"DoorSign",Quaternion.LookRotation(Vector3.up,Vector3.forward));
            Cylinder("DoorHandle",door,new Vector3(-.34f,-.1f,.07f),.015f,.12f,"Brass",Quaternion.Euler(0,0,90));
            var win=Empty("Window",room,new Vector3(2.18f,1.7f,0));
            Box("WindowGlass",win,Vector3.zero,new Vector3(.012f,1.17f,2.1f),"Glass");
            foreach(float zz in new[]{-1.08f,0,1.08f})Box("Frame",win,new Vector3(0,0,zz),new Vector3(.07f,1.24f,.04f),"Ceiling");
            foreach(float yy in new[]{-.62f,.62f})Box("Frame",win,new Vector3(0,yy,0),new Vector3(.07f,.045f,2.2f),"Ceiling");
            Box("Sill",win,new Vector3(-.04f,-.65f,0),new Vector3(.24f,.055f,2.23f),"Floor");
            for(int i=0;i<16;i++)Box("VenetianBlindSlat",win,new Vector3(-.07f,.5f-i*.066f,0),new Vector3(.045f,.012f,2.06f),"Ceiling",false,.001f).transform.localRotation=Quaternion.Euler(0,0,-18);
            var sun=new GameObject("Daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.color=new Color(1,.93f,.80f);sun.transform.rotation=Quaternion.Euler(38,-67,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.03f;
            for(int i=0;i<2;i++)
            {
                var fixture=Empty("CeilingLuminaire",room,new Vector3(0,2.84f,-.8f+i*1.6f));
                Box("Housing",fixture,Vector3.zero,new Vector3(1.15f,.065f,.24f),"Ceiling");
                Box("Diffuser",fixture,new Vector3(0,-.04f,0),new Vector3(1.07f,.01f,.19f),"Lamp",false);
                var lamp=Empty("WarmCeilingFill",fixture,new Vector3(0,-.1f,0)).gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=4;lamp.intensity=1.8f;lamp.color=new Color(1,.94f,.84f);lamp.shadows=LightShadows.None;
            }
            Page("DepartmentPlaque",room,new Vector3(0,1.94f,2.17f),new Vector2(.73f,.18f),"DepartmentSign",Quaternion.LookRotation(Vector3.up,Vector3.back));
            var clockRoot=Empty("OfficeWallClock",room,new Vector3(1.05f,2.14f,2.14f));
            Cylinder("ClockBezel",clockRoot,Vector3.zero,.18f,.036f,"Steel",Quaternion.Euler(90,0,0));
            Page("ClockFace",clockRoot,new Vector3(0,0,-.019f),new Vector2(.34f,.34f),"ClockFace",Quaternion.LookRotation(Vector3.up,Vector3.back));
            var clock=clockRoot.gameObject.AddComponent<WallClock>();
            clock.hourHand=Empty("HourHand",clockRoot,new Vector3(0,0,-.021f));Box("HourNeedle",clock.hourHand,new Vector3(0,.033f,0),new Vector3(.009f,.082f,.001f),"BlackRubber",false,.0004f);
            clock.minuteHand=Empty("MinuteHand",clockRoot,new Vector3(0,0,-.023f));Box("MinuteNeedle",clock.minuteHand,new Vector3(0,.048f,0),new Vector3(.006f,.114f,.001f),"BlackRubber",false,.0003f);
            clock.secondHand=Empty("SecondHand",clockRoot,new Vector3(0,0,-.025f));Box("SecondNeedle",clock.secondHand,new Vector3(0,.045f,0),new Vector3(.002f,.13f,.001f),"Brass",false,.0002f);
            var probe=new GameObject("OfficeReflection").AddComponent<ReflectionProbe>();probe.size=new Vector3(4.2f,2.7f,4.2f);probe.transform.position=new Vector3(0,1.4f,0);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.resolution=128;probe.clearFlags=ReflectionProbeClearFlags.Skybox;
        }
        private static void BuildDesk()
        {
            var desk=new GameObject("SolidWoodDesk").transform;
            Box("Desktop",desk,new Vector3(0,.77f,.35f),new Vector3(1.86f,.064f,.9f),"Walnut",true,.013f);
            Box("DesktopLip",desk,new Vector3(0,.733f,.35f),new Vector3(1.78f,.025f,.84f),"WoodEdge");
            foreach(float x in new[]{-.77f,.77f})foreach(float z in new[]{.04f,.67f})Box("TaperedLeg",desk,new Vector3(x,.36f,z),new Vector3(.072f,.72f,.072f),"Walnut",true,.006f);
            Box("ModestyPanel",desk,new Vector3(0,.48f,.70f),new Vector3(1.52f,.40f,.04f),"Walnut");
            Box("DrawerPedestal",desk,new Vector3(.60f,.52f,.36f),new Vector3(.33f,.41f,.63f),"Walnut");
            for(int i=0;i<3;i++)
            {
                Box("DrawerFront",desk,new Vector3(.60f,.40f+i*.124f,.026f),new Vector3(.30f,.111f,.027f),"Walnut");
                Cylinder("DrawerPull",desk,new Vector3(.60f,.40f+i*.124f,-.003f),.008f,.11f,"Brass",Quaternion.Euler(0,0,90));
            }
            // A stationery tray and a tangible date-of-receipt slip.
            Box("InTray",desk,new Vector3(-.06f,.81f,.40f),new Vector3(.265f,.012f,.345f),"Leather",true,.004f);
            Page("ReceiptSlip",desk,new Vector3(-.09f,.82f,.65f),new Vector2(.20f,.05f),"ReceiptSlip");
            var chair=Empty("OfficeChair",null,new Vector3(0,0,1.32f));
            Box("SeatFrame",chair,new Vector3(0,.43f,0),new Vector3(.44f,.05f,.43f),"Walnut",true,.012f);
            Box("SeatCushion",chair,new Vector3(0,.468f,0),new Vector3(.41f,.048f,.40f),"BinderCloth",true,.013f);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.16f,.16f})Box("ChairLeg",chair,new Vector3(x,.215f,z),new Vector3(.036f,.43f,.036f),"Walnut",true,.003f);
            foreach(float x in new[]{-.17f,.17f})Box("BackUpright",chair,new Vector3(x,.66f,.17f),new Vector3(.035f,.46f,.035f),"Walnut",true,.003f);
            Box("BackRest",chair,new Vector3(0,.76f,.17f),new Vector3(.39f,.25f,.04f),"Walnut",true,.012f);
            Box("BackPad",chair,new Vector3(0,.76f,.143f),new Vector3(.33f,.20f,.025f),"BinderCloth",true,.009f);
        }
        private static Hinge BuildCabinet(DiwanSession session)
        {
            var cabinet=Empty("WoodAndGlassCabinet",null,new Vector3(-3.20f,0,.59f));cabinet.localRotation=Quaternion.Euler(0,90,0);
            Box("Back",cabinet,new Vector3(0,1.0f,-.19f),new Vector3(1.20f,1.86f,.035f),"Walnut");
            foreach(float x in new[]{-.60f,.60f})Box("Side",cabinet,new Vector3(x,1,0),new Vector3(.045f,1.92f,.43f),"Walnut");
            foreach(float y in new[]{.08f,.80f,1.38f,1.94f})Box("Shelf",cabinet,new Vector3(0,y,0),new Vector3(1.23f,.036f,.43f),"Walnut");
            Box("Plinth",cabinet,new Vector3(0,.05f,0),new Vector3(1.27f,.10f,.47f),"WoodEdge");
            // Lower files are a closed cabinet, leaving the working shelf clear.
            Box("LowerPanel",cabinet,new Vector3(0,.43f,.228f),new Vector3(1.15f,.67f,.025f),"Walnut");
            var pivot=Empty("GlassDoorHinge",cabinet,new Vector3(-.57f,.83f,.245f));
            var hinge=pivot.gameObject.AddComponent<Hinge>();hinge.axis=Vector3.up;hinge.minAngle=-112;hinge.maxAngle=0;hinge.openAngle=-108;hinge.itemId="CABINET_DOOR";
            foreach(float x in new[]{.018f,1.12f})Box("DoorStile",pivot,new Vector3(x,.525f,0),new Vector3(.046f,1.10f,.035f),"Walnut");
            foreach(float y in new[]{0f,1.075f})Box("DoorRail",pivot,new Vector3(.57f,y,0),new Vector3(1.14f,.047f,.035f),"Walnut");
            Box("GlassPane",pivot,new Vector3(.57f,.537f,0),new Vector3(1.06f,1.02f,.007f),"Glass");
            hinge.handle=Cylinder("CabinetHandle",pivot,new Vector3(1.06f,.49f,.035f),.012f,.13f,"Brass",Quaternion.identity).transform;
            var handleHit=hinge.handle.GetComponent<Collider>();
            for(int i=0;i<8;i++)
            {
                var anchor=Empty("BinderShelfAnchor"+i,cabinet,new Vector3(-.425f+i*.119f,.984f,.012f));
                anchor.localRotation=Quaternion.LookRotation(Vector3.up,Vector3.right);
                var b=BuildBinder(i,anchor.position,anchor.rotation);b.shelfAnchor=anchor;b.cabinetDoor=hinge;session.binders[i]=b;
            }
            BuildCabinetPaper(cabinet,hinge);
            foreach(float yy in new[]{.98f,1.75f})
            {
                Box("DoorHingePlate",cabinet,new Vector3(-.575f,yy,.265f),new Vector3(.045f,.075f,.008f),"Brass",false,.001f);
                Cylinder("DoorHingePin",cabinet,new Vector3(-.598f,yy,.268f),.005f,.09f,"Brass",Quaternion.identity);
            }
            return hinge;
        }
        private static LedgerBook BuildLedger(int i,Vector3 position)
        {
            var root=Empty("Ledger_"+i+"_"+regNames[i],null,position);Body(root.gameObject,.75f);
            var grab=root.gameObject.AddComponent<Grabbable>();grab.itemId="REG"+i;
            var ledger=root.gameObject.AddComponent<LedgerBook>();ledger.index=i;ledger.arabicName=regNames[i];
            Box("BottomCover",root,new Vector3(0,-.013f,0),new Vector3(.31f,.006f,.285f),"BinderCloth");
            Box("PageBlock",root,Vector3.zero,new Vector3(.294f,.022f,.27f),"PaperEdges");
            var surface=Page("WritableLedgerPage",root,new Vector3(0,.0128f,0),new Vector2(.292f,.267f),"RegisterPage"+i,null,true);
            ledger.surface=surface.AddComponent<WritingSurface>();ledger.surface.size=new Vector2(.292f,.267f);ledger.surface.ledgerIndex=i;
            ledger.surface.inkMaterial=materials["WritingInk"];
            var pivot=Empty("LedgerCoverHinge",root,new Vector3(-.15f,.015f,0));
            ledger.cover=pivot.gameObject.AddComponent<Hinge>();ledger.cover.axis=Vector3.forward;ledger.cover.minAngle=0;ledger.cover.maxAngle=174;ledger.cover.openAngle=170;ledger.cover.itemId="REG_COVER"+i;
            var cover=Box("TopCover",pivot,new Vector3(.15f,0,0),new Vector3(.31f,.006f,.285f),"BinderCloth");
            ledger.cover.handle=Empty("CoverGripEdge",pivot,new Vector3(.30f,0,-.08f));
            Page("CoverLabel",pivot,new Vector3(.15f,.004f,0),new Vector2(.25f,.062f),"RegLabel"+i);
            Box("Spine",root,new Vector3(-.153f,0,0),new Vector3(.011f,.032f,.285f),"Leather");
            Page("StackSpineLabel",root,new Vector3(0,0,-.143f),new Vector2(.25f,.029f),"RegLabel"+i,Quaternion.LookRotation(Vector3.up,Vector3.back));
            return ledger;
        }
        private static Binder BuildBinder(int index,Vector3 pos,Quaternion rotation)
        {
            var root=Empty("Binder_"+index+"_"+binderNames[index],null,pos);root.rotation=rotation;Body(root.gameObject,.6f,true);
            var grab=root.gameObject.AddComponent<Grabbable>();grab.itemId="B"+index;
            var binder=root.gameObject.AddComponent<Binder>();binder.index=index;binder.arabicName=binderNames[index];
            Box("BackBoard",root,new Vector3(0,-.026f,0),new Vector3(.278f,.004f,.325f),"BinderCloth");
            Box("Spine",root,new Vector3(.140f,.001f,0),new Vector3(.004f,.058f,.325f),"BinderCloth");
            Page("SpineLabel",root,new Vector3(.143f,0,.01f),new Vector2(.26f,.043f),"BinderLabel"+index,Quaternion.LookRotation(Vector3.down,Vector3.right));
            var pivot=Empty("FrontCoverHinge",root,new Vector3(.14f,.03f,0));
            binder.cover=pivot.gameObject.AddComponent<Hinge>();binder.cover.axis=Vector3.forward;binder.cover.minAngle=-174;binder.cover.maxAngle=0;binder.cover.openAngle=-170;binder.cover.itemId="BINDER_COVER"+index;
            Box("FrontBoard",pivot,new Vector3(-.14f,0,0),new Vector3(.282f,.004f,.325f),"BinderCloth");
            binder.cover.handle=Empty("CoverGripEdge",pivot,new Vector3(-.275f,0,-.08f));
            Page("FrontLabel",pivot,new Vector3(-.14f,.003f,0),new Vector2(.235f,.06f),"BinderLabel"+index);
            Box("PreviousPages",root,new Vector3(-.005f,-.017f,0),new Vector3(.21f,.010f,.297f),"PaperEdges");
            Page("LastFiledLetter",root,new Vector3(-.005f,-.0115f,0),new Vector2(.21f,.297f),"PreviousLetter");
            Box("RingBasePlate",root,new Vector3(.1f,-.008f,0),new Vector3(.027f,.009f,.197f),"Steel");
            var halves=new List<Transform>();
            foreach(float z in new[]{-.04f,.04f})
            {
                halves.Add(RingHalf(root,new Vector3(.10f,-.007f,z),-90,90));halves.Add(RingHalf(root,new Vector3(.10f,-.007f,z),90,270));
            }
            binder.ringHalves=halves.ToArray();
            var lever=Box("RingReleaseLever",root,new Vector3(.10f,.002f,.119f),new Vector3(.024f,.012f,.038f),"Steel");
            var press=lever.AddComponent<Pressable>();press.kind=Pressable.Kind.BinderLever;press.binder=binder;
            binder.paperMount=Empty("NewLetterMount",root,new Vector3(.007f,-.008f,0));
            return binder;
        }
        private static Transform RingHalf(Transform parent,Vector3 pos,float from,float to)
        {
            var root=Empty("SteelRingHalf",parent,pos);
            int segments=16,sides=6;float radius=.014f,tube=.0018f;
            var vs=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=segments;i++)for(int j=0;j<sides;j++)
            {
                float a=Mathf.Lerp(from,to,(float)i/segments)*Mathf.Deg2Rad,b=j*Mathf.PI*2/sides;
                vs.Add(new Vector3(Mathf.Cos(a)*(radius+tube*Mathf.Cos(b)),radius+Mathf.Sin(a)*(radius+tube*Mathf.Cos(b)),tube*Mathf.Sin(b)));
                if(i<segments){int v=i*sides+j,n=i*sides+(j+1)%sides;triangles.AddRange(new[]{v,v+sides,n,n,v+sides,n+sides});}
            }
            var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh=PersistMesh(mesh,"Assets/Diwan/Meshes/ring_"+(meshIndex++)+".asset");
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials["Steel"];
            return root;
        }
        private static PaperDocument BuildLetter()
        {
            var root=Empty("IncomingLetter_DOC001",null,new Vector3(-.06f,.825f,.34f));Body(root.gameObject,.018f);
            var grab=root.gameObject.AddComponent<Grabbable>();grab.itemId="DOC001";
            var thickness=Box("PaperThickness",root,Vector3.zero,new Vector3(.210f,.00015f,.297f),"Paper",true,.00002f);
            thickness.GetComponent<BoxCollider>().size=new Vector3(.210f,.0015f,.297f);
            var front=Page("LetterFront",root,new Vector3(0,.00012f,0),new Vector2(.210f,.297f),"IncomingLetter");
            var back=Page("LetterBack",root,new Vector3(0,-.00012f,0),new Vector2(.210f,.297f),"Paper",Quaternion.Euler(180,0,0));
            var paper=root.gameObject.AddComponent<PaperDocument>();paper.feedReference=Empty("PunchEdgeReference",root,new Vector3(.093f,0,0));
            paper.frontSurface=front.GetComponent<MeshFilter>();paper.thicknessRenderer=thickness.GetComponent<MeshRenderer>();
            paper.backSurface=back.GetComponent<MeshFilter>();
            paper.holeMarkers=new GameObject[2];
            for(int i=0;i<2;i++)
            {
                var hole=Cylinder("PunchedHole",root,new Vector3(.093f,.0011f,-.04f+i*.08f),.0032f,.0002f,"BlackRubber",Quaternion.identity);
                UnityEngine.Object.DestroyImmediate(hole.GetComponent<Collider>());hole.SetActive(false);paper.holeMarkers[i]=hole;
            }
            return paper;
        }
        public static void RepairRingGeometry()
        {
            EditorSceneManager.OpenScene(ScenePath);
            if(materials==null)materials=new Dictionary<string,Material>();
            materials["Steel"]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Diwan/Materials/Steel.mat");
            foreach(var binder in UnityEngine.Object.FindObjectsByType<Binder>(FindObjectsSortMode.None))
            {
                foreach(var half in binder.ringHalves)if(half)UnityEngine.Object.DestroyImmediate(half.gameObject);
                var halves=new List<Transform>();
                foreach(float z in new[]{-.04f,.04f})
                {
                    halves.Add(RingHalf(binder.transform,new Vector3(.10f,-.007f,z),-90,90));
                    halves.Add(RingHalf(binder.transform,new Vector3(.10f,-.007f,z),90,270));
                }
                binder.ringHalves=halves.ToArray();binder.ringsOpen=false;EditorUtility.SetDirty(binder);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        private static void BuildPen()
        {
            var root=Empty("BallpointPen",null,new Vector3(.20f,.819f,-.006f));root.rotation=Quaternion.Euler(90,0,30);Body(root.gameObject,.018f);
            var grab=root.gameObject.AddComponent<Grabbable>();grab.itemId="PEN";
            Cylinder("Barrel",root,Vector3.zero,.0045f,.125f,"InkBlue",Quaternion.identity);
            Cylinder("Grip",root,new Vector3(0,-.043f,0),.0049f,.032f,"BlackRubber",Quaternion.identity);
            Cylinder("TipMetal",root,new Vector3(0,-.069f,0),.0022f,.014f,"Steel",Quaternion.identity);
            Box("PocketClip",root,new Vector3(.0048f,.045f,0),new Vector3(.0015f,.034f,.003f),"Steel",false,.0003f);
            var pen=root.gameObject.AddComponent<Pen>();pen.tip=Empty("InkTip",root,new Vector3(0,-.077f,0));
        }
        private static void BuildPunch()
        {
            var root=Empty("TwoHolePaperPunch",null,new Vector3(-.57f,.803f,.44f));
            Box("RubberFoot",root,new Vector3(0,.005f,0),new Vector3(.145f,.010f,.095f),"BlackRubber");
            Box("DieBase",root,new Vector3(0,.014f,0),new Vector3(.14f,.015f,.09f),"Steel");
            foreach(float xx in new[]{-.04f,.04f})Cylinder("PunchPin",root,new Vector3(xx,.028f,.005f),.0032f,.025f,"Steel",Quaternion.identity);
            var lever=Empty("PunchLever",root,new Vector3(0,.052f,.035f));
            var top=Box("PressedHandle",lever,new Vector3(0,0,-.035f),new Vector3(.15f,.018f,.105f),"InkBlue",true,.008f);
            var punch=root.gameObject.AddComponent<PunchMachine>();punch.lever=lever;
            punch.punchSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Diwan/Audio/Punch.wav");
            // To engage the tool the right margin of the sheet is slid into the die.
            punch.feed=Empty("PaperEdgeGuide",root,new Vector3(0,.025f,-.012f));punch.feed.localRotation=Quaternion.Euler(0,90,0);
            var button=top.AddComponent<Pressable>();button.kind=Pressable.Kind.PunchLever;button.punch=punch;
            Box("GuideStop",root,new Vector3(0,.021f,.025f),new Vector3(.13f,.012f,.007f),"Steel");
        }
        private static void BuildStapler()
        {
            var root=Empty("OfficeStapler",null,new Vector3(-.70f,.81f,.12f));Body(root.gameObject,.20f);var grab=root.gameObject.AddComponent<Grabbable>();grab.itemId="STAPLER";
            Box("StaplerBase",root,new Vector3(0,.008f,0),new Vector3(.035f,.014f,.125f),"BlackRubber",true,.005f);
            Box("SteelTrack",root,new Vector3(0,.024f,0),new Vector3(.024f,.013f,.112f),"Steel");
            Box("StaplerCover",root,new Vector3(0,.04f,.004f),new Vector3(.033f,.026f,.115f),"InkBlue",true,.006f);
        }
        private static void BuildRig()
        {
            var root=Empty("TraineeRig",null,new Vector3(0,0,-.82f));var rig=root.gameObject.AddComponent<VRRig>();
            var camera=Empty("HeadCamera",root,new Vector3(0,1.65f,0)).gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.025f;camera.farClipPlane=18;camera.fieldOfView=72;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.62f,.67f,.70f);camera.stereoTargetEye=StereoTargetEyeMask.Both;camera.gameObject.AddComponent<AudioListener>();rig.head=camera;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;
            for(int i=0;i<2;i++)
            {
                var hand=Empty(i==0?"LeftHand":"RightHand",root,new Vector3(i==0?-.20f:.20f,1.15f,.3f));
                var interactor=hand.gameObject.AddComponent<HandInteractor>();interactor.node=i==0?XRNode.LeftHand:XRNode.RightHand;
                var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Diwan/Models/"+(i==0?"Left":"Right")+"Hand.fbx");
                if(model)
                {
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(hand,false);visual.transform.localRotation=Quaternion.Euler(0,180,0);
                    foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=materials["HandSkin"];
                    var pose=visual.AddComponent<HandPose>();pose.node=interactor.node;interactor.visuals=visual.transform;
                }
                if(i==0)rig.left=interactor;else rig.right=interactor;
            }
        }
        public static void ConfigureSettings()
        {
            PlayerSettings.companyName="DiwanTraining";PlayerSettings.productName="القيود والسجلات";
            PlayerSettings.SplashScreen.show=false;
            PlayerSettings.SplashScreen.showUnityLogo=false;
            PlayerSettings.bundleVersion=SourceVersion;PlayerSettings.Android.bundleVersionCode=8;PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"org.diwan.training.records");
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.GameActivity;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=4;QualitySettings.shadowDistance=8;
            var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;so.ApplyModifiedPropertiesWithoutUndo();}
            ConfigureXR(BuildTargetGroup.Standalone);ConfigureXR(BuildTargetGroup.Android);
            QuestRenderCompatibility.Apply();
            foreach(string guid in AssetDatabase.FindAssets("t:ScriptableRendererData",new[]{"Assets/Settings"}))
            {
                var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var feature in renderer.rendererFeatures)
                    if(feature && feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))feature.SetActive(false);
                EditorUtility.SetDirty(renderer);
            }
        }
        private static void ConfigureXR(BuildTargetGroup group)
        {
            var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if(!general)
            {
                XRGeneralSettingsPerBuildTarget perTarget;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out perTarget);
                if(!perTarget){perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(perTarget,"Assets/Diwan/XRGeneralSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);}
                general=ScriptableObject.CreateInstance<XRGeneralSettings>();general.name=group+"XRSettings";AssetDatabase.AddObjectToAsset(general,perTarget);
                var manager=ScriptableObject.CreateInstance<XRManagerSettings>();manager.name=group+"XRManager";AssetDatabase.AddObjectToAsset(manager,perTarget);general.Manager=manager;general.InitManagerOnStart=true;perTarget.SetSettingsForBuildTarget(group,general);
            }
            var loaders=new List<XRLoader>(general.Manager.activeLoaders);
            if(!loaders.Exists(x=>x is OpenXRLoader))
            {
                var loader=ScriptableObject.CreateInstance<OpenXRLoader>();loader.name=group+"OpenXRLoader";AssetDatabase.CreateAsset(loader,"Assets/Diwan/"+group+"OpenXRLoader.asset");loaders.Add(loader);general.Manager.TrySetLoaders(loaders);
            }
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if(settings)
            {
                var serialized=new SerializedObject(settings);var features=serialized.FindProperty("features");
                if(features!=null)
                {
                    for(int i=features.arraySize-1;i>=0;i--)
                    {
                        if(features.GetArrayElementAtIndex(i).objectReferenceValue!=null)continue;
                        int oldCount=features.arraySize;features.DeleteArrayElementAtIndex(i);
                        if(features.arraySize==oldCount)features.DeleteArrayElementAtIndex(i);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
                settings.latencyOptimization=OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
                foreach(var feature in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
                {
                    string type=feature.GetType().Name;
                    if(type.Contains("OculusTouchControllerProfile")||type.Contains("MetaQuestTouchPlusControllerProfile")|| (group==BuildTargetGroup.Android && type.Contains("MetaQuestFeature")))feature.enabled=true;
                }
                EditorUtility.SetDirty(settings);
            }
            EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
        }
    }
}
