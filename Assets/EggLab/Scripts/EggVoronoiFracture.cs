using System;
using System.Collections.Generic;
using UnityEngine;

namespace EggLab
{
    // Spherical nearest-site Voronoi. Gnomonic projection turns each spherical
    // bisector into a line, so cells can be clipped with bounded 2D polygons.
    public static class EggVoronoiFracture
    {
        public sealed class Cell
        {
            public Vector3 seed;
            public Vector3[] corners;
            public Vector3[] edgePlanes;
        }
        public static List<Cell> Generate(int count,int randomSeed,float irregularity)
        {
            count=Mathf.Clamp(count,12,96);
            var rng=new System.Random(randomSeed);
            var sites=new Vector3[count];
            float spacing=Mathf.Sqrt(4*Mathf.PI/count),phase=(float)rng.NextDouble()*Mathf.PI*2;
            for(int i=0;i<count;i++)
            {
                float y=1-2*(i+.5f)/count,angle=i*2.39996323f+phase,r=Mathf.Sqrt(1-y*y);
                Vector3 p=new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                Vector3 x=Vector3.Cross(p,Mathf.Abs(y)>.9f?Vector3.right:Vector3.up).normalized;
                Vector3 z=Vector3.Cross(p,x);
                float j=spacing*.33f*Mathf.Clamp01(irregularity);
                sites[i]=(p+x*((float)rng.NextDouble()*2-1)*j+z*((float)rng.NextDouble()*2-1)*j).normalized;
            }
            var cells=new List<Cell>(count);
            for(int i=0;i<count;i++)
            {
                Vector3 origin=sites[i];
                Vector3 x=Vector3.Cross(origin,Mathf.Abs(origin.y)>.9f?Vector3.right:Vector3.up).normalized;
                Vector3 y=Vector3.Cross(origin,x);
                var polygon=new List<Vector2>{new Vector2(-4,-4),new Vector2(4,-4),new Vector2(4,4),new Vector2(-4,4)};
                for(int j=0;j<count&&polygon.Count>0;j++)
                {
                    if(i==j)continue;
                    Vector3 plane=origin-sites[j];
                    float a=Vector3.Dot(plane,x),b=Vector3.Dot(plane,y),c=Vector3.Dot(plane,origin);
                    var output=new List<Vector2>(polygon.Count+1);
                    Vector2 previous=polygon[polygon.Count-1];float pd=a*previous.x+b*previous.y+c;
                    foreach(Vector2 current in polygon)
                    {
                        float cd=a*current.x+b*current.y+c;
                        if((cd>=0)!=(pd>=0))
                        {
                            float t=pd/(pd-cd);output.Add(Vector2.LerpUnclamped(previous,current,t));
                        }
                        if(cd>=0)output.Add(current);
                        previous=current;pd=cd;
                    }
                    polygon=output;
                }
                if(polygon.Count<3)throw new InvalidOperationException("Voronoi cell has no area: "+i);
                var corners=new List<Vector3>();
                foreach(Vector2 p in polygon)
                {
                    Vector3 direction=(origin+x*p.x+y*p.y).normalized;
                    // All three adjacent cells use the same canonical triple intersection.
                    direction=CanonicalCorner(direction,i,sites);
                    if(corners.Count==0||(direction-corners[corners.Count-1]).sqrMagnitude>1e-10f)corners.Add(direction);
                }
                if(corners.Count>2&&(corners[0]-corners[corners.Count-1]).sqrMagnitude<1e-10f)corners.RemoveAt(corners.Count-1);
                if(corners.Count<3)throw new InvalidOperationException("Degenerate Voronoi cell: "+i);
                var planes=new Vector3[corners.Count];
                for(int k=0;k<corners.Count;k++)
                {
                    Vector3 n=Vector3.Cross(corners[k],corners[(k+1)%corners.Count]).normalized;
                    if(Vector3.Dot(n,origin)<0)n=-n;
                    planes[k]=n;
                }
                cells.Add(new Cell{seed=origin,corners=corners.ToArray(),edgePlanes=planes});
            }
            return cells;
        }
        static Vector3 CanonicalCorner(Vector3 point,int site,Vector3[] sites)
        {
            int first=-1,second=-1;float best=-2,next=-2;
            for(int j=0;j<sites.Length;j++)
            {
                if(j==site)continue;float dot=Vector3.Dot(point,sites[j]);
                if(dot>best){next=best;second=first;best=dot;first=j;}else if(dot>next){next=dot;second=j;}
            }
            int a=site,b=first,c=second;
            if(a>b){int t=a;a=b;b=t;}if(b>c){int t=b;b=c;c=t;}if(a>b){int t=a;a=b;b=t;}
            Vector3 intersection=Vector3.Cross(sites[a]-sites[b],sites[a]-sites[c]).normalized;
            return Vector3.Dot(intersection,point)<0?-intersection:intersection;
        }
        public static float BoundaryDistance(Cell cell,Vector3 direction)
        {
            float d=float.MaxValue;
            foreach(Vector3 plane in cell.edgePlanes)d=Mathf.Min(d,Vector3.Dot(plane,direction));
            return Mathf.Max(0,d)*.9f;
        }
    }
}
