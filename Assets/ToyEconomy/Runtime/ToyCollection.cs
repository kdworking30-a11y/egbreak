using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
// imageai AchievementManager-style ID lookup and capped 3-star duplicate progress.
public sealed class ToyCollection : MonoBehaviour {
 readonly Dictionary<string,int> counts=new Dictionary<string,int>();
 public int UniqueCount=>counts.Count;
 public void Clear(){counts.Clear();}
 public int Count(ToyDefinition toy)=>toy!=null && counts.TryGetValue(toy.toyId,out int count)?count:0;
 public int Stars(ToyDefinition toy)=>Mathf.Min(3,Count(toy));
 public bool Unlock(ToyDefinition toy){bool isNew=Count(toy)==0;counts[toy.toyId]=Count(toy)+1;return isNew;}
}
}
