using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
 // Voronoi cells share coplanar internal faces. Render a cut only after its neighbour leaves.
 public sealed class OpticalCutFaceVisibility : MonoBehaviour {
  sealed class Cell {
   public Transform part; public Mesh mesh; public int[] cuts; public Vector4Int[] planes;
  }
  // Integer plane keys tolerate the small float differences between imported cell meshes.
  struct Vector4Int : System.IEquatable<Vector4Int> {
   public int x,y,z,w;
   public bool Equals(Vector4Int b)=>x==b.x && y==b.y && z==b.z && w==b.w;
   public override bool Equals(object b)=>b is Vector4Int p && Equals(p);
   public override int GetHashCode()=>unchecked(((x*397^y)*397^z)*397^w);
  }
  readonly List<Cell> cells=new List<Cell>();
  readonly HashSet<Transform> attached=new HashSet<Transform>();
  readonly Dictionary<Vector4Int,HashSet<Transform>> neighbours=new Dictionary<Vector4Int,HashSet<Transform>>();
  public int HiddenTriangles {get;private set;}
  public void Initialize(List<Transform> parts){
   foreach(var part in parts){
    var filter=part.GetComponent<MeshFilter>();if(filter.sharedMesh.subMeshCount<2)continue;
    var mesh=Instantiate(filter.sharedMesh);mesh.name=filter.sharedMesh.name+" exposed cuts";
    filter.sharedMesh=mesh;part.gameObject.AddComponent<OpticalMeshLifetime>().owned=mesh;
    var cell=new Cell{part=part,mesh=mesh,cuts=mesh.GetTriangles(1)};
    cell.planes=new Vector4Int[cell.cuts.Length/3];var vertices=mesh.vertices;
    for(int t=0;t<cell.planes.Length;t++){
     var a=part.TransformPoint(vertices[cell.cuts[t*3]]);var b=part.TransformPoint(vertices[cell.cuts[t*3+1]]);var c=part.TransformPoint(vertices[cell.cuts[t*3+2]]);
     var n=Vector3.Cross(b-a,c-a).normalized;
     // Canonical orientation pairs the opposite sides of the same plane.
     if(Mathf.Abs(n.x)>.001f?n.x<0:Mathf.Abs(n.y)>.001f?n.y<0:n.z<0)n=-n;
     var key=new Vector4Int{x=Mathf.RoundToInt(n.x*500),y=Mathf.RoundToInt(n.y*500),z=Mathf.RoundToInt(n.z*500),w=Mathf.RoundToInt(Vector3.Dot(n,a)*500)};
     cell.planes[t]=key;if(!neighbours.TryGetValue(key,out var owners)){owners=new HashSet<Transform>();neighbours.Add(key,owners);}owners.Add(part);
    }
    cells.Add(cell);attached.Add(part);
   }
   Refresh();
  }
  public void Detach(Transform part){
   attached.Remove(part);
   foreach(var cell in cells)if(cell.part==part){cell.mesh.SetTriangles(cell.cuts,1,false);break;}
  }
  public void Refresh(){
   HiddenTriangles=0;
   foreach(var cell in cells){if(!attached.Contains(cell.part))continue;
    var visible=new List<int>();
    for(int t=0;t<cell.planes.Length;t++){
     bool covered=false;foreach(var other in neighbours[cell.planes[t]])if(other!=cell.part && attached.Contains(other)){covered=true;break;}
     if(covered){HiddenTriangles++;continue;}
     visible.Add(cell.cuts[t*3]);visible.Add(cell.cuts[t*3+1]);visible.Add(cell.cuts[t*3+2]);
    }
    cell.mesh.SetTriangles(visible,1,false);
   }
  }
 }
 public sealed class OpticalMeshLifetime : MonoBehaviour {
  public Mesh owned;
  void OnDestroy(){if(owned!=null)Destroy(owned);}
 }
}
