using System.Collections.Generic;
using UnityEngine;

namespace DiwanVR
{
    public static class PaperMesh
    {
        private const float Radius=.0032f,Box=.004f;
        public static Mesh TwoHolePage(){return PerforatedPage(new Vector2(.093f,-.04f),new Vector2(.093f,.04f));}
        // Partition along the pin separation to retain the actual holes on a
        // rotated or misplaced sheet, instead of displaying a perfect punch.
        public static Mesh PerforatedPage(Vector2 first,Vector2 second)
        {
            bool turn=Mathf.Abs(first.x-second.x)>Mathf.Abs(first.y-second.y);
            float width=turn?.1485f:.105f,height=turn?.105f:.1485f;
            var centers=new List<Vector2>();
            foreach(var p in new[]{first,second})
            {
                Vector2 c=turn?new Vector2(p.y,-p.x):p;
                if(Mathf.Abs(c.x)+Box<width && Mathf.Abs(c.y)+Box<height)centers.Add(c);
            }
            centers.Sort((a,b)=>a.y.CompareTo(b.y));
            var vs=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float bottom=-height;
            foreach(var c in centers)
            {
                if(c.y-Box<bottom)continue;
                Rect(-width,bottom,width,c.y-Box,turn,vs,uv,triangles);
                Rect(-width,c.y-Box,c.x-Box,c.y+Box,turn,vs,uv,triangles);
                Rect(c.x+Box,c.y-Box,width,c.y+Box,turn,vs,uv,triangles);
                int start=vs.Count;const int segments=32;
                for(int i=0;i<=segments;i++)
                {
                    float a=i*Mathf.PI*2/segments;Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    float edge=Box/Mathf.Max(Mathf.Abs(direction.x),Mathf.Abs(direction.y));
                    Add(c+direction*Radius,turn,vs,uv);Add(c+direction*edge,turn,vs,uv);
                    if(i<segments){int k=start+i*2;triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}
                }
                bottom=c.y+Box;
            }
            Rect(-width,bottom,width,height,turn,vs,uv,triangles);
            // The mesh is generated at runtime and must never be written into a
            // scene or a prefab. Without this flag a later scene save would
            // persist a reference to a mesh that no longer exists.
            var mesh=new Mesh {name="Physically perforated A4",hideFlags=HideFlags.DontSave};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Add(Vector2 p,bool turn,List<Vector3> v,List<Vector2> uv)
        {
            Vector3 point=turn?new Vector3(-p.y,0,p.x):new Vector3(p.x,0,p.y);
            v.Add(point);uv.Add(new Vector2((point.x+.105f)/.21f,(point.z+.1485f)/.297f));
        }
        private static void Rect(float l,float b,float r,float t,bool turn,List<Vector3> v,List<Vector2> uv,List<int> tri)
        {
            if(r<=l || t<=b)return;
            int n=v.Count;Add(new Vector2(l,b),turn,v,uv);Add(new Vector2(l,t),turn,v,uv);Add(new Vector2(r,t),turn,v,uv);Add(new Vector2(r,b),turn,v,uv);tri.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
    }
}
