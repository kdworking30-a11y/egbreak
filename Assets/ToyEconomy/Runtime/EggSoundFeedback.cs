using UnityEngine;
namespace ToyEconomy {
 public sealed class EggSoundFeedback : MonoBehaviour {
  public bool Muted {get;set;}
  AudioSource source;AudioClip[] clips;EggCrackController cracker;
  public void Bind(EggCrackController target){cracker=target;source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.13f;clips=new AudioClip[3];
   for(int k=0;k<3;k++){int length=k==1?7000:4000;float[] samples=new float[length];var rng=new System.Random(31+k);
    for(int i=0;i<length;i++){float t=i/22050f;float envelope=Mathf.Exp(-t*(k==1?26:40));float tone=Mathf.Sin(t*Mathf.PI*2*(k==1?145:k==2?1300:480));samples[i]=(tone*.35f+((float)rng.NextDouble()*2-1)*.5f)*envelope;}
    clips[k]=AudioClip.Create("Material feedback "+k,length,1,22050,false);clips[k].SetData(samples,0);}
   target.Feedback+=Play;
  }
  void Play(EggInteractionKind kind,float progress){if(!Muted){source.pitch=Random.Range(.95f,1.05f);source.PlayOneShot(clips[(int)kind],progress>=1?1.2f:1);}}
  void OnDestroy(){if(cracker!=null)cracker.Feedback-=Play;if(clips!=null)foreach(var clip in clips)if(clip!=null)Destroy(clip);}
 }
}
