using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DiwanVR.Editor
{
    public static partial class DiwanBuilder
    {
        private static void OptimizeReferenceBooks()
        {
            for(int i=0;i<12;i++)Mat("ReferenceAtlas"+i,Color.white,0,.20f,"ReferenceAtlas"+i);
            int count=0,renderers=0;
            foreach(var item in UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None))
            {
                if(!item.itemId.StartsWith("REF"))continue;
                var filters=item.GetComponentsInChildren<MeshFilter>();
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();int atlas=0;
                foreach(var filter in filters)
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer || !filter.sharedMesh)continue;
                    string material=renderer.sharedMaterial.name;
                    if(material.StartsWith("ReferenceSpine"))int.TryParse(material.Substring("ReferenceSpine".Length),out atlas);
                    else if(material.StartsWith("ReferenceCloth") && item.itemId.StartsWith("REF_FLAT"))int.TryParse(material.Substring("ReferenceCloth".Length),out atlas);
                }
                foreach(var filter in filters)
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer || !filter.sharedMesh)continue;
                    string material=renderer.sharedMaterial.name;
                    Rect region=material.StartsWith("ReferenceCover")?new Rect(0,0,.5f,.75f):material.StartsWith("ReferenceSpine")?new Rect(.5f,0,.25f,1):material.StartsWith("ReferenceCloth")?new Rect(0,.75f,.5f,.25f):new Rect(.75f,0,.25f,1);
                    // Inset each region to prevent neighbouring atlas colours bleeding into mipmaps.
                    region.xMin+=.0015f;region.xMax-=.0015f;region.yMin+=.0015f;region.yMax-=.0015f;
                    var source=filter.sharedMesh;var vs=source.vertices;var ns=source.normals;var uv=source.uv;
                    var matrix=item.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;int offset=vertices.Count;
                    for(int n=0;n<vs.Length;n++)
                    {
                        vertices.Add(matrix.MultiplyPoint3x4(vs[n]));normals.Add(matrix.MultiplyVector(ns[n]).normalized);
                        Vector2 t=n<uv.Length?uv[n]:Vector2.zero;uvs.Add(new Vector2(region.x+t.x*region.width,region.y+t.y*region.height));
                    }
                    foreach(int n in source.triangles)triangles.Add(offset+n);
                    UnityEngine.Object.DestroyImmediate(renderer);UnityEngine.Object.DestroyImmediate(filter);
                }
                var mesh=new Mesh {name="ReferenceBookCombined"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
                item.gameObject.AddComponent<MeshFilter>().sharedMesh=PersistMesh(mesh,"");item.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials["ReferenceAtlas"+atlas];
                count++;renderers+=item.GetComponentsInChildren<MeshRenderer>().Length;
            }
            File.WriteAllText("Validation/renderer_budget.json","{\"referenceBooks\":"+count+",\"referenceBookRenderers\":"+renderers+",\"headsetPerformanceMeasured\":false}");
        }
    }
}
