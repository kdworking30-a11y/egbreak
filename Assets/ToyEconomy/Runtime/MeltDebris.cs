using UnityEngine;
namespace ToyEconomy {
 public sealed class MeltDebris : MonoBehaviour {
  float time;Vector3 initial;
  void Awake(){initial=transform.localScale;}
  void Update(){time+=Time.deltaTime;transform.localScale=initial*Mathf.Clamp01(1-time/.45f);transform.position+=Vector3.down*Time.deltaTime*.6f;}
 }
}
