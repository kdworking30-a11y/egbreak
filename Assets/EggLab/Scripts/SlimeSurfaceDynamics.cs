using System.Collections.Generic;
using UnityEngine;

namespace EggLab
{
    // Cached welded topology for coupled surface displacements; no per-frame allocations.
    public sealed class SlimeSurfaceDynamics
    {
        readonly int[] weld;
        readonly int[][] neighbors;
        readonly Vector3[] displacement, speed, normalSums;
        readonly int[] triangles;
        public SlimeSurfaceDynamics(Vector3[] packed,int[] meshTriangles)
        {
            int count=packed.Length;weld=new int[count];neighbors=new int[count][];
            displacement=new Vector3[count];speed=new Vector3[count];normalSums=new Vector3[count];triangles=meshTriangles;
            var map=new Dictionary<Vector3Int,int>();var adjacency=new HashSet<int>[count];
            for(int i=0;i<count;i++)
            {
                Vector3 p=packed[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000));
                if(!map.TryGetValue(key,out int canonical)){canonical=i;map.Add(key,i);adjacency[i]=new HashSet<int>();}
                weld[i]=canonical;
            }
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=weld[triangles[i]],b=weld[triangles[i+1]],c=weld[triangles[i+2]];
                Link(adjacency,a,b);Link(adjacency,b,c);Link(adjacency,c,a);
            }
            for(int i=0;i<count;i++)if(weld[i]==i){neighbors[i]=new int[adjacency[i].Count];adjacency[i].CopyTo(neighbors[i]);}
            for(int i=0;i<count;i++)neighbors[i]=neighbors[weld[i]];
        }
        static void Link(HashSet<int>[] adjacency,int a,int b)
        {if(a==b)return;adjacency[a].Add(b);adjacency[b].Add(a);}
        public void Snapshot(Vector3[] vertices,Vector3[] goals,Vector3[] velocity)
        {for(int i=0;i<vertices.Length;i++){displacement[i]=vertices[i]-goals[i];speed[i]=velocity[i];}}
        public Vector3 Coupling(int index,float tension,float viscosity)
        {
            Vector3 d=Vector3.zero,v=Vector3.zero;var linked=neighbors[index];
            if(linked.Length==0)return Vector3.zero;
            foreach(int n in linked){d+=displacement[n];v+=speed[n];}
            return (d/linked.Length-displacement[index])*tension+(v/linked.Length-speed[index])*viscosity;
        }
        public void Weld(Vector3[] vertices,Vector3[] velocity)
        {
            for(int i=0;i<vertices.Length;i++)if(weld[i]!=i){vertices[i]=vertices[weld[i]];velocity[i]=velocity[weld[i]];}
        }
        public void SmoothNormals(List<Vector3> normals)
        {
            System.Array.Clear(normalSums,0,normalSums.Length);
            for(int i=0;i<normals.Count;i++)normalSums[weld[i]]+=normals[i];
            for(int i=0;i<normals.Count;i++)normals[i]=normalSums[weld[i]].normalized;
        }
        public float Volume(Vector3[] vertices)
        {
            double volume=0;
            for(int i=0;i<triangles.Length;i+=3)
                volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
            return Mathf.Abs((float)volume);
        }
    }
}
