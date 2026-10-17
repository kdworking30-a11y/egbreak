using UnityEngine;
namespace ToyEconomy {
 public enum EggInteractionKind { Tap, PressBreak, RubIce }
 [CreateAssetMenu(menuName="Toy Economy/Material Profile")]
 public sealed class EggMaterialProfile : ScriptableObject {
  public EggInteractionKind interaction;
  public Material outside,inside;
  [Min(.1f)] public float chargeSeconds=.65f;
  [Range(.01f,.5f)] public float shortPressProgress=.06f;
  [Range(2,8)] public int breaksToOpen=4;
  [Min(.1f)] public float meltRadius=.72f;
  [Min(.1f)] public float meltPerSecond=2.4f;
  [Range(.5f,1)] public float autoFinishMelt=.8f;
  public float impulse=1.5f;
  public string instruction;
 }
}
