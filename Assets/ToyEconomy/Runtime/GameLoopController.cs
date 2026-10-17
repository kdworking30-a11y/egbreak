using UnityEngine;
namespace ToyEconomy {
public sealed class GameLoopController : MonoBehaviour {
 public enum PlayState { Shop, Cracking, Reward }
 public ToyDefinition starterToy; public EggDefinition[] eggs; public Material shellMaterial; public Shader rewardShader;
 public EconomyManager Economy {get;private set;}
 public ToyGarden Garden {get;private set;}
 public EggShop Shop {get;private set;}
 public EggCrackController Cracker {get;private set;}
 public ToyCollection Collection {get;private set;}
 public HUD Hud {get;private set;}
 public PlayState State {get;private set;}
 public int EggsOpened {get;private set;}
 public IdleProgression Progression {get;private set;}
 public IdleSave Persistence {get;private set;}
 public ToyDefinition LastReward {get;private set;}
 public string Message {get;private set;}="Your first toy is earning. Choose an egg!";
 GameObject toyPreview;Material previewMaterial;float revealStarted;float targetScale;
 void Awake(){
 Application.targetFrameRate=60;
 Economy=gameObject.AddComponent<EconomyManager>();Economy.Initialize(10);
 Garden=gameObject.AddComponent<ToyGarden>();Garden.Economy=Economy;
 Collection=gameObject.AddComponent<ToyCollection>();Collection.Unlock(starterToy);Garden.Add(starterToy);
 Cracker=new GameObject("ImageAI Prefractured Egg").AddComponent<EggCrackController>();Cracker.transform.SetParent(transform,false);Cracker.transform.localPosition=new Vector3(0,.23f,0);Cracker.shellMaterial=shellMaterial;
 gameObject.AddComponent<EggSoundFeedback>().Bind(Cracker);
 Shop=gameObject.AddComponent<EggShop>();Shop.Economy=Economy;Shop.Cracker=Cracker;
 Cracker.IsOwned=toy=>Collection.Count(toy)>0;
 Progression=gameObject.AddComponent<IdleProgression>();Progression.Initialize(this);Shop.Progression=Progression;
 Persistence=gameObject.AddComponent<IdleSave>();Persistence.Initialize(this);
 if(!string.IsNullOrEmpty(Persistence.WelcomeMessage))Message=Persistence.WelcomeMessage;
 Cracker.Prepared+=PrepareToy;Cracker.Revealed+=OnReveal;
 Hud=gameObject.AddComponent<HUD>();Hud.Initialize(this);ShowToy(starterToy,2.3f);
 }
 public void Buy(int index){
 if(index<0 || index>=eggs.Length)return;
 if(Shop.Buy(eggs[index])){Message=Cracker.Instruction;Persistence.Save();}
 else Message=State==PlayState.Reward?"Tap NEXT to continue.":Cracker.IsCracking?"Finish your current egg first.":"Your toys are earning. More coins needed.";
 Hud.Refresh();
 }
 public void SetNotice(string message){Message=message;Hud?.Refresh();}
 public void Hit(){Cracker.Hit();}
 public void HitAtScreen(Vector2 point){Cracker.HitAtScreen(point);}
 void PrepareToy(ToyDefinition toy){State=PlayState.Cracking;ShowToy(toy,1.5f);}
 void OnReveal(ToyDefinition toy){
 if(State!=PlayState.Cracking)return;
 bool isNew=Collection.Unlock(toy);Garden.Add(toy);EggsOpened++;Progression.OnEggOpened();LastReward=toy;
 State=PlayState.Reward;Shop.RewardPending=true;
 Message=(isNew?"NEW TOY UNLOCKED! ":"ANOTHER FRIEND! ")+toy.displayName+"  +"+toy.coinPerSecond.ToString("0.#")+" /sec";
 if(EggsOpened==3 || EggsOpened==6 || EggsOpened==10)Message+="  NEW EGG AVAILABLE!";
 if(EggsOpened%3==0 && EggsOpened<=30)Message+="  Garden income +10%!";
 targetScale=2.4f+Collection.Stars(toy)*.08f;revealStarted=Time.time;Persistence.Save();
 }
 public void Next(){if(State!=PlayState.Reward)return;State=PlayState.Shop;Shop.RewardPending=false;Message="Toy added to your garden. Choose the next egg!";Hud.Refresh();}
 void Update(){if(toyPreview==null || State!=PlayState.Reward)return;float t=Mathf.Clamp01((Time.time-revealStarted)/.4f);float scale=Mathf.Lerp(1.5f,targetScale,1-Mathf.Pow(1-t,3));toyPreview.transform.localScale=new Vector3(scale,scale,1);}
 void ShowToy(ToyDefinition toy,float scale){
 if(toyPreview!=null)Destroy(toyPreview);if(previewMaterial!=null)Destroy(previewMaterial);
 toyPreview=GameObject.CreatePrimitive(PrimitiveType.Quad);toyPreview.name="Hidden reward - "+toy.displayName;toyPreview.transform.SetParent(transform,false);toyPreview.transform.localPosition=new Vector3(0,0,.05f);toyPreview.transform.localScale=new Vector3(scale,scale,1);Destroy(toyPreview.GetComponent<Collider>());
 previewMaterial=new Material(rewardShader);previewMaterial.SetTexture("_BaseMap",toy.artwork);previewMaterial.SetFloat("_Reveal",1);previewMaterial.renderQueue=2450;previewMaterial.SetFloat("_ZWrite",1);toyPreview.GetComponent<Renderer>().sharedMaterial=previewMaterial;
 }
 public System.Collections.Generic.List<ToyDefinition> ToyCatalog(){var toys=new System.Collections.Generic.List<ToyDefinition>{starterToy};foreach(var egg in eggs)foreach(var toy in egg.possibleToys)if(!toys.Contains(toy))toys.Add(toy);return toys;}
 public void RestoreWorld(System.Collections.Generic.List<ToyDefinition> toys,int eggsOpened){Garden.Clear();Collection.Clear();foreach(var toy in toys){Garden.Add(toy);Collection.Unlock(toy);}EggsOpened=eggsOpened;}
 void OnDestroy(){if(previewMaterial!=null)Destroy(previewMaterial);}
}
}
