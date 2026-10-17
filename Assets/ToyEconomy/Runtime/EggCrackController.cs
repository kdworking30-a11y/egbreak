using System;
using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
 public sealed class EggCrackController : MonoBehaviour {
  public Material shellMaterial;
  public EggDefinition ActiveEgg {get;private set;}
  public ToyDefinition PendingToy {get;private set;}
  public int Hits {get;private set;}
  public bool IsCracking=>ActiveEgg!=null;
  public int FragmentsRemaining=>fragments.Count;
  public int DetachedCount {get;private set;}
  public float Progress {get;private set;}
  public float Charge {get;private set;}
  public bool IsHolding {get;private set;}
  public bool SimpleControls {get;private set;}
  public EggInteractionKind Interaction=>ActiveEgg!=null && ActiveEgg.materialProfile!=null?ActiveEgg.materialProfile.interaction:EggInteractionKind.Tap;
  public string Instruction=>SimpleControls?"SIMPLE MODE: TAP TO OPEN":ActiveEgg!=null && ActiveEgg.materialProfile!=null?ActiveEgg.materialProfile.instruction:"TAP THE SHELL";
  public event Action Changed;
  public event Action<ToyDefinition> Prepared,Revealed;
  public event Action<EggInteractionKind,float> Feedback;
  readonly List<Transform> fragments=new List<Transform>();
  readonly List<GameObject> debris=new List<GameObject>();
  readonly Dictionary<Transform,float> warmth=new Dictionary<Transform,float>();
  readonly Dictionary<EggDefinition,Queue<ToyDefinition>> bags=new Dictionary<EggDefinition,Queue<ToyDefinition>>();
  readonly Dictionary<EggDefinition,ToyDefinition> lastRewards=new Dictionary<EggDefinition,ToyDefinition>();
  MaterialPropertyBlock block;
  void Awake(){block=new MaterialPropertyBlock();}
  OpticalCutFaceVisibility cutFaces;
  GameObject shellRoot;Material outside,inside;Vector2 gesturePoint;float carry;
  float gestureUpdated;float rubFrameBudget;int rubFrame=-1;
  public void SetSimpleControls(bool enabled){CancelGesture();SimpleControls=enabled;Changed?.Invoke();}
  public void Begin(EggDefinition egg){
   if(IsCracking || egg==null || !egg.IsValid)throw new InvalidOperationException("Cannot start egg");
   Cleanup();ActiveEgg=egg;PendingToy=Roll(egg);Hits=DetachedCount=0;Progress=Charge=carry=0;IsHolding=false;
   var profile=egg.materialProfile;
   outside=new Material(profile!=null && profile.outside!=null?profile.outside:shellMaterial);
   if(profile==null)outside.color=egg.color;
   inside=new Material(profile!=null && profile.inside!=null?profile.inside:outside);
   if(outside.HasProperty("_EggCenter"))outside.SetVector("_EggCenter",transform.position);
   if(inside.HasProperty("_EggCenter"))inside.SetVector("_EggCenter",transform.position);
   shellRoot=Instantiate(egg.fracturedPrefab,transform,false);shellRoot.name=egg.displayName+" Shell";
   foreach(var filter in shellRoot.GetComponentsInChildren<MeshFilter>()){
    var mats=new Material[filter.sharedMesh.subMeshCount];for(int i=0;i<mats.Length;i++)mats[i]=i==0?outside:inside;
    filter.GetComponent<Renderer>().sharedMaterials=mats;fragments.Add(filter.transform);warmth[filter.transform]=0;
   }
   if(outside.shader.name=="ToyEconomy/OpticalGlass"){cutFaces=shellRoot.AddComponent<OpticalCutFaceVisibility>();cutFaces.Initialize(fragments);}
   if(fragments.Count!=egg.fragmentCount)throw new InvalidOperationException("Baked fragment count mismatch");
   Prepared?.Invoke(PendingToy);Changed?.Invoke();
  }
  ToyDefinition Roll(EggDefinition egg){
   if(!bags.TryGetValue(egg,out var bag) || bag.Count==0){var pool=new List<ToyDefinition>(egg.possibleToys);
    for(int i=pool.Count-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);var swap=pool[i];pool[i]=pool[j];pool[j]=swap;}
    if(pool.Count>1 && lastRewards.TryGetValue(egg,out var last) && pool[0]==last){int j=pool.FindIndex(t=>t!=last);if(j>0){var swap=pool[0];pool[0]=pool[j];pool[j]=swap;}}
    bag=new Queue<ToyDefinition>(pool);bags[egg]=bag;
   }
   var reward=bag.Dequeue();lastRewards[egg]=reward;return reward;
  }
  public void Hit(){HitAtScreen(new Vector2(Screen.width*.5f,Screen.height*.53f));}
  public void HitAtScreen(Vector2 screen){
   if(!IsCracking || (!SimpleControls && Interaction!=EggInteractionKind.Tap))return;
   if(!TryImpact(screen,out Vector3 impact))return;
   Hits++;float progress=SimpleControls?Progress+1f/6:Mathf.Clamp01((float)Hits/ActiveEgg.hitsToBreak);
   BreakTo(Mathf.Min(1,progress+.00001f),impact,false);
  }
  public void BeginGesture(Vector2 screen){
   if(!IsCracking || SimpleControls || Interaction!=EggInteractionKind.PressBreak || !TryImpact(screen,out _))return;
   gesturePoint=screen;IsHolding=true;Charge=0;gestureUpdated=Time.unscaledTime;
  }
  void Update(){
   if(!IsHolding || !IsCracking)return;
   float now=Time.unscaledTime;float elapsed=Mathf.Max(0,now-gestureUpdated);gestureUpdated=now;
   Charge=Mathf.Clamp01(Charge+elapsed/ActiveEgg.materialProfile.chargeSeconds);
   if(shellRoot!=null)shellRoot.transform.localScale=new Vector3(1+Charge*.025f,1-Charge*.045f,1+Charge*.025f);
   ApplyStress(Charge);Changed?.Invoke();
  }
  public void EndGesture(Vector2 screen){
   if(!IsHolding || !IsCracking)return;
   float elapsed=Mathf.Max(0,Time.unscaledTime-gestureUpdated);
   Charge=Mathf.Clamp01(Charge+elapsed/ActiveEgg.materialProfile.chargeSeconds);
   float amount=Mathf.Lerp(ActiveEgg.materialProfile.shortPressProgress,1f/ActiveEgg.materialProfile.breaksToOpen,Charge);
   if(TryImpact(gesturePoint,out Vector3 impact)){Hits++;carry=Mathf.Min(1,Mathf.Max(carry,Progress)+amount);CancelGesture();BreakTo(carry,impact,false);}else CancelGesture();
  }
  public void CancelGesture(){IsHolding=false;Charge=0;if(shellRoot!=null)shellRoot.transform.localScale=Vector3.one;if(IsCracking)ApplyStress(0);Changed?.Invoke();}
  public void RubStroke(Vector2 from,Vector2 to,float seconds){
   if(!IsCracking || SimpleControls || Interaction!=EggInteractionKind.RubIce || seconds<=0 || !float.IsFinite(seconds))return;
   float distance=Vector2.Distance(from,to)/Mathf.Max(1,Screen.height);
   if(distance<.001f || !TryImpact(to,out Vector3 point))return;
   // A frame has a fixed thermal budget, regardless of input event frequency.
   if(rubFrame!=Time.frameCount){rubFrame=Time.frameCount;rubFrameBudget=Mathf.Min(.05f,Time.unscaledDeltaTime);}
   float budget=Mathf.Min(rubFrameBudget,Mathf.Min(seconds,.05f));if(budget<=0)return;rubFrameBudget-=budget;
   float energy=budget*ActiveEgg.materialProfile.meltPerSecond*Mathf.Clamp01(distance/.025f);
   float radius=ActiveEgg.materialProfile.meltRadius;
   var thawed=new List<Transform>();float total=DetachedCount;
   foreach(var part in fragments){
    // Surface projection reaches front/back layers at the same screen region.
    var delta=part.position-point;float dist=new Vector2(delta.x,delta.y).magnitude;
    float falloff=Mathf.Clamp01(1-dist/radius);
    warmth[part]=Mathf.Clamp01(warmth[part]+energy*falloff);
    var renderer=part.GetComponent<Renderer>();renderer.GetPropertyBlock(block);block.SetFloat("_Heat",warmth[part]);renderer.SetPropertyBlock(block);
    total+=warmth[part];if(warmth[part]>=.999f)thawed.Add(part);
   }
   foreach(var part in thawed){fragments.Remove(part);Release(part,true);}
   if(thawed.Count>0 && cutFaces!=null)cutFaces.Refresh();
   Progress=Mathf.Clamp01(total/ActiveEgg.fragmentCount);
   if(Progress>=ActiveEgg.materialProfile.autoFinishMelt){BreakTo(1,point,true);return;}
   Changed?.Invoke();
  }
  void BreakTo(float progress,Vector3 impact,bool melt){
   if(!IsCracking)return;Progress=Mathf.Max(Progress,Mathf.Clamp01(progress));
   int desired=ActiveEgg.fragmentCount-Mathf.CeilToInt(Progress*ActiveEgg.fragmentCount);
   fragments.Sort((a,b)=>(a.position-impact).sqrMagnitude.CompareTo((b.position-impact).sqrMagnitude));
   while(fragments.Count>desired){var part=fragments[0];fragments.RemoveAt(0);Release(part,melt);}
   if(cutFaces!=null)cutFaces.Refresh();
   Feedback?.Invoke(Interaction,Progress);
   if(Progress>=.99999f || fragments.Count==0){var reward=PendingToy;Progress=1;IsHolding=false;Charge=0;ActiveEgg=null;PendingToy=null;foreach(var item in debris)if(item!=null)foreach(var renderer in item.GetComponentsInChildren<Renderer>())renderer.enabled=false;Revealed?.Invoke(reward);}
   else ApplyStress(0);
   Changed?.Invoke();
  }
  public bool TryImpact(Vector2 screen,out Vector3 impact){
   impact=transform.position;var camera=Camera.main;if(camera==null || fragments.Count==0)return false;
   var ray=camera.ScreenPointToRay(screen);var whole=new Bounds(transform.position,new Vector3(2.7f,2.9f,2.7f));if(!whole.IntersectRay(ray))return false;
   float nearest=float.MaxValue;foreach(var part in fragments)if(part.GetComponent<Renderer>().bounds.IntersectRay(ray,out float distance) && distance<nearest){nearest=distance;impact=ray.GetPoint(distance);}
   if(nearest==float.MaxValue){float best=float.MaxValue;foreach(var part in fragments){float d=Vector3.Cross(part.position-ray.origin,ray.direction).sqrMagnitude;if(d<best){best=d;impact=part.position;}}}
   return true;
  }
  void ApplyStress(float stress){foreach(var part in fragments){var renderer=part.GetComponent<Renderer>();renderer.GetPropertyBlock(block);block.SetFloat("_Stress",stress);block.SetFloat("_Cracks",Progress);renderer.SetPropertyBlock(block);}}
  void Release(Transform part,bool melt){
   if(cutFaces!=null){cutFaces.Detach(part);var renderer=part.GetComponent<Renderer>();renderer.GetPropertyBlock(block);block.SetFloat("_Cracks",1);renderer.SetPropertyBlock(block);}
   part.SetParent(transform,true);part.name=melt?"Melting ice fragment":"Detached shell debris";
   float impulse=ActiveEgg!=null && ActiveEgg.materialProfile!=null?ActiveEgg.materialProfile.impulse:1.5f;
   if(melt){part.gameObject.AddComponent<MeltDebris>();}
   else {var body=part.gameObject.AddComponent<Rigidbody>();body.mass=Interaction==EggInteractionKind.PressBreak?.1f:.035f;body.useGravity=true;body.interpolation=RigidbodyInterpolation.Interpolate;
    body.AddForce((part.position-transform.position).normalized*impulse+Vector3.up*impulse*.6f+Vector3.back*.2f,ForceMode.VelocityChange);
    body.AddTorque(UnityEngine.Random.insideUnitSphere*(Interaction==EggInteractionKind.PressBreak?2:5),ForceMode.VelocityChange);}
   debris.Add(part.gameObject);DetachedCount++;Destroy(part.gameObject,melt?.45f:2);
  }
  void OnApplicationPause(bool pause){if(pause)CancelGesture();}
  void OnApplicationFocus(bool focus){if(!focus)CancelGesture();}
  void Cleanup(){foreach(var part in debris)if(part!=null)Destroy(part);debris.Clear();fragments.Clear();warmth.Clear();cutFaces=null;if(shellRoot!=null)Destroy(shellRoot);if(outside!=null)Destroy(outside);if(inside!=null)Destroy(inside);}
  void OnDestroy(){Cleanup();}
 }
}
