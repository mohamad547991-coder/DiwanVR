using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwanVR.Editor
{
    public static partial class DiwanBuilder
    {
        public const float RoomWidth=7.2f,RoomDepth=6.6f,RoomHeight=3.15f;

        private static void SurfaceDetail(string material,string normal,float strength=1,string smoothness=null)
        {
            var m=materials[material];
            if(normal!=null)
            {
                string path="Assets/Diwan/Textures/"+normal+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer && importer.textureType!=TextureImporterType.NormalMap)
                {
                    importer.textureType=TextureImporterType.NormalMap;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.SaveAndReimport();
                }
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));m.SetFloat("_BumpScale",strength);m.EnableKeyword("_NORMALMAP");
            }
            if(smoothness!=null)
            {
                m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Diwan/Textures/"+smoothness+".png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(m);
        }
        private static void MakeRealismMaterials()
        {
            Mat("Plaster",Color.white,0,.12f,"Plaster_Albedo");
            Mat("LowerWall",new Color(.63f,.66f,.62f),0,.15f,"Plaster_Albedo");
            Mat("TrimPaint",new Color(.82f,.83f,.78f),0,.28f);
            Mat("CleanPaperEdges",Color.white,0,.08f,"PaperEdgesClean");
            Mat("ArchiveCard",new Color(.58f,.53f,.42f),0,.10f);
            Mat("Cork",Color.white,0,.06f,"Cork");
            Mat("Upholstery",new Color(.74f,.78f,.71f),0,.18f,"Cloth");
            Mat("OutdoorMasonry",new Color(.63f,.61f,.55f),0,.08f);
            Mat("OutdoorWindow",new Color(.25f,.31f,.33f),.05f,.55f);
            Mat("CalendarPage",Color.white,0,.05f,"OfficeCalendar");
            Mat("BookcaseLabel",Color.white,0,.10f,"BookcaseLabel");
            Mat("NoticeSheet",Color.white,0,.05f,"NoticeSheet");
            Mat("LampShade",new Color(.16f,.19f,.17f),.18f,.48f);
            materials["Grout"].color=new Color(.61f,.62f,.58f);
            Mat("Walnut",new Color(.90f,.88f,.84f),0,.33f,"Walnut_Albedo");
            SurfaceDetail("Walnut","Walnut_Normal",.28f,"WoodSmoothness");
            SurfaceDetail("Plaster","Plaster_Normal",.18f);SurfaceDetail("LowerWall","Plaster_Normal",.14f);
            materials["Plaster"].mainTextureScale=new Vector2(3,2);
            materials["LowerWall"].mainTextureScale=new Vector2(3,1);
            SurfaceDetail("Floor","Floor_Normal",.12f);
            for(int i=0;i<4;i++)
            {
                float tint=.985f+i*.008f;
                Mat("FloorTile"+i,new Color(tint,tint,tint),0,.25f,"Floor");SurfaceDetail("FloorTile"+i,"Floor_Normal",.12f);
            }
            Color[] bookColors={new Color(.17f,.23f,.22f),new Color(.26f,.13f,.12f),new Color(.16f,.20f,.29f),new Color(.35f,.29f,.18f),new Color(.27f,.29f,.25f),new Color(.18f,.15f,.12f)};
            for(int i=0;i<6;i++)Mat("ReferenceCloth"+i,bookColors[i],0,.22f,"BookCloth");
            for(int i=0;i<12;i++)
            {
                Mat("ReferenceSpine"+i,Color.white,0,.18f,"BookSpine"+i);
                Mat("ReferenceCover"+i,Color.white,0,.18f,"BookCover"+i);
            }
            materials["PaperEdges"].color=new Color(.91f,.89f,.83f);
            materials["Glass"].color=new Color(.87f,.93f,.94f,.075f);materials["Glass"].SetFloat("_Metallic",0);
            materials["Lamp"].shader=Shader.Find("Universal Render Pipeline/Unlit");
            materials["Lamp"].shaderKeywords=Array.Empty<string>();
            materials["Lamp"].SetColor("_BaseColor",new Color(1,.99f,.95f));
        }
        private static void BuildExpandedOffice()
        {
            var room=new GameObject("OfficeInterior").transform;
            Box("Subfloor",room,new Vector3(0,-.08f,0),new Vector3(RoomWidth,.16f,RoomDepth),"Grout");
            for(int x=0;x<12;x++)for(int z=0;z<11;z++)
            {
                var tile=Box("PorcelainTile",room,new Vector3(-3.3f+x*.6f,.003f,-3f+z*.6f),new Vector3(.596f,.012f,.596f),"FloorTile"+((x*3+z*7)%4),false,.001f);
                tile.transform.localRotation=Quaternion.Euler(0,((x+z*3)%4)*90,0);
            }
            Box("BackWall",room,new Vector3(0,RoomHeight*.5f,3.3f),new Vector3(RoomWidth,RoomHeight,.16f),"Plaster");
            Box("LeftWall",room,new Vector3(-3.6f,RoomHeight*.5f,0),new Vector3(.16f,RoomHeight,RoomDepth),"Plaster");
            Box("RightWallLower",room,new Vector3(3.6f,.54f,0),new Vector3(.16f,1.08f,RoomDepth),"Plaster");
            Box("RightWallUpper",room,new Vector3(3.6f,2.825f,0),new Vector3(.16f,.65f,RoomDepth),"Plaster");
            Box("RightWallFront",room,new Vector3(3.6f,1.79f,-2.075f),new Vector3(.16f,1.42f,2.45f),"Plaster");
            Box("RightWallBack",room,new Vector3(3.6f,1.79f,2.575f),new Vector3(.16f,1.42f,1.45f),"Plaster");
            Box("FrontWallLeft",room,new Vector3(-2.035f,RoomHeight*.5f,-3.3f),new Vector3(3.13f,RoomHeight,.16f),"Plaster");
            Box("FrontWallRight",room,new Vector3(2.035f,RoomHeight*.5f,-3.3f),new Vector3(3.13f,RoomHeight,.16f),"Plaster");
            Box("FrontDoorLintel",room,new Vector3(0,2.75f,-3.3f),new Vector3(.94f,.8f,.16f),"Plaster");
            Box("Ceiling",room,new Vector3(0,3.20f,0),new Vector3(RoomWidth,.10f,RoomDepth),"Ceiling");
            foreach(float x in new[]{-3.505f,3.505f})
            {
                Box("WashableLowerWall",room,new Vector3(x,.48f,0),new Vector3(.012f,.94f,6.43f),"LowerWall",false,.001f);
                Box("WallDado",room,new Vector3(x,.965f,0),new Vector3(.026f,.035f,6.43f),"TrimPaint",false,.002f);
                Box("Skirting",room,new Vector3(x,.065f,0),new Vector3(.035f,.13f,6.43f),"WoodEdge",false);
                Box("CeilingCornice",room,new Vector3(x,3.09f,0),new Vector3(.065f,.11f,6.43f),"Ceiling",false);
            }
            Box("BackLowerWall",room,new Vector3(0,.48f,3.208f),new Vector3(7.02f,.94f,.012f),"LowerWall",false);
            Box("BackDado",room,new Vector3(0,.965f,3.192f),new Vector3(7.02f,.035f,.026f),"TrimPaint",false);
            Box("BackSkirting",room,new Vector3(0,.065f,3.19f),new Vector3(7.02f,.13f,.035f),"WoodEdge",false);
            foreach(float z in new[]{-3.19f,3.19f})Box("Cornice",room,new Vector3(0,3.09f,z),new Vector3(7.02f,.11f,.065f),"Ceiling",false);
            BuildEntryDoor(room);BuildOfficeWindow(room);BuildWallClock(room,new Vector3(.22f,2.48f,3.175f));
            Box("PlaqueFrame",room,new Vector3(-1.04f,2.25f,3.178f),new Vector3(1.21f,.255f,.025f),"WoodEdge",false,.005f);
            Page("DepartmentPlaque",room,new Vector3(-1.04f,2.25f,3.162f),new Vector2(1.15f,.20f),"DepartmentSign",Quaternion.LookRotation(Vector3.up,Vector3.back));
            var sun=new GameObject("Daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.08f;sun.color=new Color(1,.97f,.91f);sun.transform.rotation=Quaternion.Euler(34,-67,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.18f;
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)
            {
                var fixture=Empty("RecessedOfficeLuminaire",room,new Vector3(-1.7f+x*3.4f,3.12f,-1.55f+z*3.1f));
                Box("Housing",fixture,Vector3.zero,new Vector3(1.18f,.045f,.32f),"TrimPaint",false,.005f);
                Box("Diffuser",fixture,new Vector3(0,-.025f,0),new Vector3(1.10f,.006f,.255f),"Lamp",false,.002f);
                for(int n=0;n<10;n++)Box("DiffuserRib",fixture,new Vector3(-.48f+n*.106f,-.029f,0),new Vector3(.006f,.004f,.252f),"Ceiling",false,.0003f);
                var light=Empty("CeilingFill",fixture,new Vector3(0,-.065f,0)).gameObject.AddComponent<Light>();light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=135;light.innerSpotAngle=100;light.range=5.2f;light.intensity=5.0f;light.bounceIntensity=1.4f;light.color=new Color(1,.99f,.96f);light.shadows=LightShadows.None;
            }
            var probe=new GameObject("OfficeReflection").AddComponent<ReflectionProbe>();probe.size=new Vector3(7.0f,3.0f,6.4f);probe.transform.position=new Vector3(0,1.5f,0);probe.mode=ReflectionProbeMode.Baked;probe.resolution=128;probe.boxProjection=true;probe.intensity=.6f;probe.clearFlags=ReflectionProbeClearFlags.Skybox;
            var probes=Empty("OfficeLightProbes",room,Vector3.zero).gameObject.AddComponent<LightProbeGroup>();var positions=new List<Vector3>();
            foreach(float x in new[]{-2.85f,-1.4f,0,1.4f,2.85f})foreach(float z in new[]{-2.65f,-1.3f,0,1.3f,2.65f})foreach(float y in new[]{.45f,1.25f,2.15f})positions.Add(new Vector3(x,y,z));probes.probePositions=positions.ToArray();
        }
        private static void BuildEntryDoor(Transform room)
        {
            var door=Empty("EntryDoor",room,new Vector3(0,1.155f,-3.23f));
            Box("DoorLeaf",door,Vector3.zero,new Vector3(.90f,2.27f,.055f),"Walnut",true,.006f);
            foreach(float x in new[]{-.487f,.487f})Box("DoorJamb",room,new Vector3(x,1.175f,-3.24f),new Vector3(.07f,2.35f,.095f),"WoodEdge");
            Box("DoorHeader",room,new Vector3(0,2.35f,-3.24f),new Vector3(1.04f,.08f,.095f),"WoodEdge");
            for(int i=0;i<2;i++)
            {
                Box("RecessedDoorPanel",door,new Vector3(0,-.53f+i*1.06f,.032f),new Vector3(.73f,.84f,.008f),"WoodEdge",false,.006f);
                Box("PanelVeneer",door,new Vector3(0,-.53f+i*1.06f,.038f),new Vector3(.69f,.80f,.006f),"Walnut",false,.004f);
            }
            Cylinder("HandleRose",door,new Vector3(.34f,-.15f,.045f),.027f,.008f,"Brass",Quaternion.Euler(90,0,0));
            Cylinder("LeverHandle",door,new Vector3(.295f,-.15f,.078f),.009f,.105f,"Brass",Quaternion.Euler(0,0,90));
            Page("DoorNameplate",door,new Vector3(0,.39f,.045f),new Vector2(.27f,.10f),"DoorSign",Quaternion.LookRotation(Vector3.up,Vector3.forward));
            Box("Threshold",room,new Vector3(0,.016f,-3.20f),new Vector3(.94f,.016f,.13f),"Steel",true,.002f);
        }
        private static void BuildOfficeWindow(Transform room)
        {
            var win=Empty("OfficeWindow",room,new Vector3(3.49f,1.79f,.5f));
            Box("ClearWindowGlass",win,Vector3.zero,new Vector3(.009f,1.42f,2.70f),"Glass");
            foreach(float z in new[]{-1.37f,0,1.37f})Box("WindowMullion",win,new Vector3(0,0,z),new Vector3(.075f,1.50f,.045f),"TrimPaint");
            foreach(float y in new[]{-.745f,.745f})Box("WindowFrame",win,new Vector3(0,y,0),new Vector3(.075f,.045f,2.80f),"TrimPaint");
            Box("DeepWindowSill",win,new Vector3(-.035f,-.79f,0),new Vector3(.27f,.05f,2.86f),"Floor",true,.007f);
            Box("BlindHeadRail",win,new Vector3(-.06f,.72f,0),new Vector3(.10f,.075f,2.76f),"TrimPaint",false);
            for(int i=0;i<18;i++)Box("BlindSlat",win,new Vector3(-.065f,.62f-i*.069f,0),new Vector3(.07f,.009f,2.65f),"Ceiling",false,.001f).transform.localRotation=Quaternion.Euler(0,0,-29);
            foreach(float z in new[]{-1.11f,1.11f})Cylinder("BlindCord",win,new Vector3(-.083f,.015f,z),.0012f,1.28f,"Ceiling",Quaternion.identity);
            Cylinder("BlindWand",win,new Vector3(-.12f,.39f,-1.28f),.003f,.53f,"TrimPaint",Quaternion.identity);
            Box("WindowLatch",win,new Vector3(-.07f,-.1f,0),new Vector3(.04f,.10f,.018f),"Steel",false);
            var exterior=Empty("CourtyardBeyondWindow",room,Vector3.zero);
            Box("CourtyardFloor",exterior,new Vector3(7.2f,-.09f,.5f),new Vector3(7.1f,.12f,9),"OutdoorMasonry",false);
            Box("OppositeOfficeBuilding",exterior,new Vector3(10.5f,3,.5f),new Vector3(.30f,6.1f,9.4f),"OutdoorMasonry",false);
            for(int row=0;row<2;row++)for(int col=0;col<4;col++)
            {
                var frame=Empty("DistantWindow",exterior,new Vector3(10.32f,1.75f+row*2.25f,-2.7f+col*2.1f));
                Box("Glazing",frame,Vector3.zero,new Vector3(.025f,1.4f,1.12f),"OutdoorWindow",false);
                Box("WindowCentreBar",frame,new Vector3(-.02f,0,0),new Vector3(.025f,1.4f,.035f),"TrimPaint",false);
                foreach(float z in new[]{-.58f,.58f})Box("StoneWindowSurround",frame,new Vector3(-.02f,0,z),new Vector3(.04f,1.49f,.05f),"TrimPaint",false);
                foreach(float y in new[]{-.72f,.72f})Box("StoneWindowSurround",frame,new Vector3(-.02f,y,0),new Vector3(.04f,.05f,1.20f),"TrimPaint",false);
            }
        }
        private static void BuildWallClock(Transform parent,Vector3 location)
        {
            var root=Empty("OfficeWallClock",parent,location);
            Cylinder("ClockBezel",root,Vector3.zero,.185f,.033f,"Steel",Quaternion.Euler(90,0,0));
            Page("ClockFace",root,new Vector3(0,0,-.019f),new Vector2(.345f,.345f),"ClockFace",Quaternion.LookRotation(Vector3.up,Vector3.back));
            var clock=root.gameObject.AddComponent<WallClock>();
            clock.hourHand=Empty("HourHand",root,new Vector3(0,0,-.022f));Box("HourNeedle",clock.hourHand,new Vector3(0,.034f,0),new Vector3(.008f,.082f,.001f),"BlackRubber",false,.0003f);
            clock.minuteHand=Empty("MinuteHand",root,new Vector3(0,0,-.024f));Box("MinuteNeedle",clock.minuteHand,new Vector3(0,.05f,0),new Vector3(.005f,.115f,.001f),"BlackRubber",false,.0003f);
            clock.secondHand=Empty("SecondHand",root,new Vector3(0,0,-.026f));Box("SecondNeedle",clock.secondHand,new Vector3(0,.045f,0),new Vector3(.002f,.13f,.001f),"Brass",false,.0002f);
        }
        private static void BuildLibrary()
        {
            var shelf=Empty("AdministrativeBookcase",null,new Vector3(1.61f,0,2.985f));
            Box("BookcaseBack",shelf,new Vector3(0,1.13f,.195f),new Vector3(1.83f,2.10f,.018f),"Walnut");
            foreach(float x in new[]{-.93f,.93f})Box("BookcaseSide",shelf,new Vector3(x,1.13f,0),new Vector3(.045f,2.18f,.43f),"Walnut");
            foreach(float y in new[]{.12f,.57f,.98f,1.39f,1.80f,2.22f})Box("BookcaseShelf",shelf,new Vector3(0,y,0),new Vector3(1.91f,.035f,.43f),"Walnut");
            Box("BookcaseCornice",shelf,new Vector3(0,2.25f,0),new Vector3(1.99f,.08f,.48f),"WoodEdge",true,.008f);
            Box("BookcasePlinth",shelf,new Vector3(0,.065f,0),new Vector3(1.99f,.13f,.48f),"WoodEdge");
            Box("BottomDoorLeft",shelf,new Vector3(-.455f,.34f,-.232f),new Vector3(.895f,.39f,.025f),"Walnut");
            Box("BottomDoorRight",shelf,new Vector3(.455f,.34f,-.232f),new Vector3(.895f,.39f,.025f),"Walnut");
            foreach(float x in new[]{-.11f,.11f})Cylinder("BookcaseKnob",shelf,new Vector3(x,.37f,-.255f),.011f,.027f,"Brass",Quaternion.Euler(90,0,0));
            var random=new System.Random(2711);int index=0;
            for(int row=0;row<4;row++)
            {
                float cursor=-.79f;
                for(int n=0;n<12;n++)
                {
                    float thickness=.042f+(float)random.NextDouble()*.022f,height=.255f+(float)random.NextDouble()*.077f;
                    if(cursor+thickness>.82f)break;
                    var book=Empty("ReferenceBook_"+index,shelf,new Vector3(cursor+thickness*.5f,.591f+row*.41f+height*.5f,-.045f));
                    string cloth="ReferenceCloth"+(index%6);Body(book.gameObject,.35f,true);
                    var grab=book.gameObject.AddComponent<Grabbable>();grab.itemId="REF"+index;
                    Box("BookPages",book,Vector3.zero,new Vector3(thickness-.006f,height-.009f,.19f),"CleanPaperEdges",true,.001f);
                    foreach(float x in new[]{-thickness*.5f,thickness*.5f})Box("HardCover",book,new Vector3(x,0,-.003f),new Vector3(.003f,height,.204f),cloth,true,.001f);
                    Box("ClothSpine",book,new Vector3(0,0,-.105f),new Vector3(thickness+.003f,height,.008f),cloth,true,.003f);
                    Page("PrintedBookSpine",book,new Vector3(0,0,-.1095f),new Vector2(thickness-.003f,height-.01f),"ReferenceSpine"+(index%12),Quaternion.LookRotation(Vector3.up,Vector3.back));
                    Page("PrintedBookCover",book,new Vector3(thickness*.5f+.0017f,0,-.003f),new Vector2(.192f,height-.009f),"ReferenceCover"+(index%12),Quaternion.LookRotation(Vector3.up,Vector3.right));
                    cursor+=thickness+.009f;index++;
                    if(n==8 && row%2==0)cursor+=.19f;
                }
                Box("MetalBookend",shelf,new Vector3(cursor+.018f,.69f+row*.41f,-.045f),new Vector3(.008f,.20f,.19f),"Steel",true,.002f);
                for(int n=0;n<2;n++)
                {
                    var flat=Empty("HorizontalReference_"+row+"_"+n,shelf,new Vector3(.59f,.608f+row*.41f+n*.040f,-.035f));
                    flat.localRotation=Quaternion.Euler(0,n==0?-3:2,0);Body(flat.gameObject,.42f,true);
                    flat.gameObject.AddComponent<Grabbable>().itemId="REF_FLAT"+row+n;
                    Box("PageBlock",flat,Vector3.zero,new Vector3(.285f,.029f,.205f),"CleanPaperEdges");
                    foreach(float y in new[]{-.016f,.016f})Box("Cover",flat,new Vector3(0,y,0),new Vector3(.298f,.003f,.217f),"ReferenceCloth"+((row+n+2)%6));
                    Box("Spine",flat,new Vector3(0,0,-.108f),new Vector3(.298f,.034f,.006f),"ReferenceCloth"+((row+n+2)%6));
                }
            }
            Page("LibraryNameplate",shelf,new Vector3(0,2.25f,-.246f),new Vector2(.36f,.052f),"BookcaseLabel",Quaternion.LookRotation(Vector3.up,Vector3.back));
        }
        private static void BuildCabinetPaper(Transform cabinet,Hinge door)
        {
            for(int i=0;i<3;i++)
            {
                var anchor=Empty("WhitePaperAnchor"+i,cabinet,new Vector3(-.36f+i*.36f,1.429f,-.012f));
                var stack=Empty("BlankPaperReam"+i,cabinet,anchor.localPosition);Body(stack.gameObject,.75f,true);
                var grab=stack.gameObject.AddComponent<Grabbable>();grab.itemId="BLANK_REAM"+i;grab.accessDoor=door;grab.storageAnchor=anchor;
                Box("HundredsOfPaperEdges",stack,Vector3.zero,new Vector3(.210f,.056f,.297f),"CleanPaperEdges",true,.0004f);
                Page("BlankTopSheet",stack,new Vector3(0,.0285f,0),new Vector2(.210f,.297f),"Paper");
                for(int n=0;n<9;n++)Box("VisibleSheetEdge",stack,new Vector3(0,-.023f+n*.0055f,-.149f),new Vector3(.208f,.0004f,.0005f),"PaperEdges",false,.00005f);
                if(i<2)for(int p=0;p<2;p++)
                {
                    var sheetAnchor=Empty("BlankSheetAnchor"+i+p,cabinet,new Vector3(anchor.localPosition.x,1.46f+p*.0025f,-.004f));
                    var sheet=Empty("LooseBlankSheet"+i+p,cabinet,sheetAnchor.localPosition);sheet.localRotation=Quaternion.Euler(0,p==0?-2.5f:2.0f,0);Body(sheet.gameObject,.018f,true);
                    var sg=sheet.gameObject.AddComponent<Grabbable>();sg.itemId="BLANK_SHEET"+i+p;sg.accessDoor=door;sg.storageAnchor=sheetAnchor;
                    var thickness=Box("ThinPaper",sheet,Vector3.zero,new Vector3(.210f,.00018f,.297f),"Paper",true,.00003f);thickness.GetComponent<BoxCollider>().size=new Vector3(.210f,.002f,.297f);
                    var front=Page("BlankFront",sheet,new Vector3(0,.00013f,0),new Vector2(.210f,.297f),"Paper",null,true);
                    var back=Page("BlankBack",sheet,new Vector3(0,-.00013f,0),new Vector2(.210f,.297f),"Paper",Quaternion.Euler(180,0,0));
                    var document=sheet.gameObject.AddComponent<PaperDocument>();document.documentId=sg.itemId;document.feedReference=Empty("PunchEdgeReference",sheet,new Vector3(.093f,0,0));document.frontSurface=front.GetComponent<MeshFilter>();document.backSurface=back.GetComponent<MeshFilter>();document.thicknessRenderer=thickness.GetComponent<MeshRenderer>();
                    var writable=front.AddComponent<WritingSurface>();writable.freeForm=true;writable.ledgerIndex=-1;writable.inkMaterial=materials["WritingInk"];writable.size=new Vector2(.210f,.297f);
                }
            }
        }
        private static void BuildOfficeDetails()
        {
            var details=Empty("OfficeDetails",null,Vector3.zero);
            var console=Empty("SideCredenza",details,new Vector3(-1.96f,0,2.88f));
            Box("CredenzaBody",console,new Vector3(0,.43f,0),new Vector3(1.35f,.76f,.51f),"Walnut");
            Box("CredenzaTop",console,new Vector3(0,.834f,0),new Vector3(1.43f,.052f,.57f),"Walnut",true,.008f);
            Box("CredenzaBase",console,new Vector3(0,.045f,0),new Vector3(1.29f,.09f,.46f),"WoodEdge");
            foreach(float x in new[]{-.333f,.333f})
            {
                Box("CredenzaDoor",console,new Vector3(x,.44f,-.266f),new Vector3(.649f,.69f,.025f),"Walnut");
                Cylinder("CredenzaPull",console,new Vector3(x<0?-.10f:.10f,.54f,-.292f),.007f,.115f,"Brass",Quaternion.identity);
            }
            for(int i=0;i<2;i++)
            {
                var tray=Empty("DocumentTray"+i,console,new Vector3(-.35f,.88f+i*.095f,0));
                Box("TrayBase",tray,Vector3.zero,new Vector3(.26f,.008f,.34f),"InkBlue",true,.003f);
                foreach(float x in new[]{-.127f,.127f})Box("TraySide",tray,new Vector3(x,.025f,0),new Vector3(.008f,.055f,.34f),"InkBlue");
                Box("TrayBack",tray,new Vector3(0,.025f,.164f),new Vector3(.26f,.055f,.008f),"InkBlue");
                Box("SpareStationery",tray,new Vector3(0,.015f,0),new Vector3(.21f,.014f,.297f),"CleanPaperEdges");
            }
            var calendar=Empty("PaperDeskCalendar",console,new Vector3(.31f,.96f,0));calendar.localRotation=Quaternion.Euler(12,0,0);
            Box("CalendarBoard",calendar,Vector3.zero,new Vector3(.21f,.19f,.008f),"Leather",false);
            Page("PrintedCalendar",calendar,new Vector3(0,0,-.005f),new Vector2(.202f,.182f),"CalendarPage",Quaternion.LookRotation(Vector3.up,Vector3.back));
            Cylinder("CalendarBinding",calendar,new Vector3(0,.093f,-.007f),.004f,.21f,"Steel",Quaternion.Euler(0,0,90));
            Box("CalendarRearFoot",calendar,new Vector3(0,-.05f,.045f),new Vector3(.18f,.09f,.009f),"Leather",false);
            var board=Empty("NoticeBoard",details,new Vector3(-3.49f,1.77f,2.11f));board.localRotation=Quaternion.Euler(0,-90,0);
            Box("NoticeBoardFrame",board,Vector3.zero,new Vector3(.83f,.73f,.035f),"Walnut",false,.007f);
            Box("NoticeBoardCork",board,new Vector3(0,0,-.022f),new Vector3(.77f,.67f,.012f),"Cork",false);
            Page("PinnedOfficeNotice",board,new Vector3(.10f,.025f,-.030f),new Vector2(.25f,.36f),"NoticeSheet",Quaternion.LookRotation(Vector3.up,Vector3.back));
            Cylinder("BrassDrawingPin",board,new Vector3(.10f,.19f,-.033f),.004f,.006f,"Brass",Quaternion.Euler(90,0,0));
            var bench=Empty("WaitingBench",details,new Vector3(3.08f,0,-1.68f));bench.localRotation=Quaternion.Euler(0,90,0);
            Box("BenchSeat",bench,new Vector3(0,.44f,0),new Vector3(1.35f,.065f,.45f),"Walnut",true,.012f);
            Box("BenchCushion",bench,new Vector3(0,.487f,0),new Vector3(1.28f,.055f,.40f),"Upholstery",true,.016f);
            Box("BenchBack",bench,new Vector3(0,.75f,.19f),new Vector3(1.33f,.39f,.045f),"Walnut",true,.012f);
            Box("BenchBackPad",bench,new Vector3(0,.76f,.159f),new Vector3(1.24f,.29f,.03f),"Upholstery",true,.012f);
            foreach(float x in new[]{-.57f,.57f})foreach(float z in new[]{-.15f,.15f})Box("BenchLeg",bench,new Vector3(x,.22f,z),new Vector3(.05f,.44f,.05f),"Walnut");
            for(int i=0;i<3;i++)
            {
                var socket=Empty("WallSocket",details,new Vector3(-2.9f+i*2.6f,.31f,3.18f));
                Box("SocketPlate",socket,Vector3.zero,new Vector3(.105f,.083f,.012f),"TrimPaint",false,.004f);
                foreach(float x in new[]{-.016f,.016f})Cylinder("SocketRecess",socket,new Vector3(x,0,-.008f),.0035f,.001f,"BlackRubber",Quaternion.Euler(90,0,0));
            }
            var desk=GameObject.Find("SolidWoodDesk").transform;
            var lamp=Empty("DeskTaskLamp",desk,new Vector3(-.76f,.806f,.67f));
            Cylinder("LampBase",lamp,new Vector3(0,.012f,0),.066f,.022f,"LampShade",Quaternion.identity);
            Cylinder("LampStem",lamp,new Vector3(.018f,.19f,.006f),.009f,.35f,"Steel",Quaternion.Euler(0,0,-9));
            var shade=Empty("TaskLightHead",lamp,new Vector3(-.02f,.37f,-.01f));shade.localRotation=Quaternion.Euler(0,0,12);
            Box("LampHood",shade,Vector3.zero,new Vector3(.19f,.036f,.085f),"LampShade",true,.013f);
            Box("LampDiffuser",shade,new Vector3(0,-.020f,0),new Vector3(.16f,.003f,.06f),"Lamp",false,.001f);
            var light=Empty("TaskLight",shade,new Vector3(0,-.025f,0)).gameObject.AddComponent<Light>();light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=95;light.innerSpotAngle=65;light.range=1.2f;light.intensity=.22f;light.color=new Color(1,.98f,.93f);light.shadows=LightShadows.None;
        }
        private static void ConfigureContactSounds()
        {
            foreach(var item in UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None))
            {
                var foley=item.GetComponent<ContactFoley>();if(!foley)foley=item.gameObject.AddComponent<ContactFoley>();
                string clip=item.itemId.StartsWith("BLANK") || item.itemId=="DOC001"?"PaperRustle":item.itemId=="PEN" || item.itemId=="STAPLER" || item.itemId=="COMPUTER_MOUSE"?"SmallObjectTap":"BookContact";
                foley.contactClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Diwan/Audio/"+clip+".wav");
            }
        }
        private static void MarkEnvironmentStatic()
        {
            foreach(string name in new[]{"OfficeInterior","SolidWoodDesk","OfficeChair","WoodAndGlassCabinet","AdministrativeBookcase","OfficeDetails","OfficeHardware030"})
            {
                var root=GameObject.Find(name);if(!root)continue;
                foreach(var tr in root.GetComponentsInChildren<Transform>())
                {
                    if(tr.GetComponentInParent<WallClock>() || tr.GetComponentInParent<Grabbable>() || tr.GetComponentInParent<Hinge>())continue;
                    GameObjectUtility.SetStaticEditorFlags(tr.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic);
                }
            }
        }
    }
}
