using UnityEngine;
namespace ToyEconomy {
[CreateAssetMenu(menuName="Toy Economy/Egg")]
public sealed class EggDefinition : ScriptableObject {
 public string eggId; public string displayName;
 [Min(0)] public int price=10;
 [Range(4,250)] public int fragmentCount=8;
 [Min(1)] public int hitsToBreak=3;
 public EggMaterialProfile materialProfile;
 public GameObject fracturedPrefab;
 public Color color=Color.white;
 public ToyDefinition[] possibleToys;
 public bool IsValid {
 get { if(fracturedPrefab==null || price<0 || fragmentCount<4 || fragmentCount>250 || hitsToBreak<1 || possibleToys==null || possibleToys.Length==0)return false;
 foreach(var toy in possibleToys)if(toy==null || !toy.IsValid)return false;return true; }
 }
 public ToyDefinition RollToy() => possibleToys[Random.Range(0,possibleToys.Length)];
}
}
