using UnityEngine;
namespace ToyEconomy {
[CreateAssetMenu(menuName="Toy Economy/Toy")]
public sealed class ToyDefinition : ScriptableObject {
 public string toyId; public string displayName;
 [Min(0.01f)] public float coinPerSecond=1;
 public Texture2D artwork;
 public Color color=Color.white;
 public bool IsValid => !string.IsNullOrWhiteSpace(toyId) && !string.IsNullOrWhiteSpace(displayName) && coinPerSecond>0 && !float.IsInfinity(coinPerSecond) && !float.IsNaN(coinPerSecond);
}
}
