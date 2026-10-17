using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace ToyEconomy {
public sealed class HUD : MonoBehaviour {
 GameLoopController game; RectTransform safe; Text coins,income,status,progress,garden;
 public bool CollectionVisible=>collectionPanel!=null && collectionPanel.activeSelf;
 public Button SimpleButton {get;private set;}
 public Button SoundButton {get;private set;}
 Text gestureHint;Image chargeFill;
 public Button NextButton {get;private set;}
 public Button CollectionButton {get;private set;}
 public Button CloseCollectionButton {get;private set;}
 GameObject collectionPanel;Text collectionText;RawImage[] collectionArt;Text[] collectionLabels;
 public Button CrackButton {get;private set;}
 public List<Button> ShopButtons {get;}=new List<Button>();
 readonly List<Text> shopLabels=new List<Text>(); Font font; Rect lastSafe; int lastWidth,lastHeight;
 static Color Ink=new Color(.13f,.21f,.24f), Cream=new Color(.96f,.96f,.90f), Green=new Color(.22f,.56f,.40f);
 public void Initialize(GameLoopController loop){
 game=loop;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
 var canvas=new GameObject("Mobile HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
 var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(480,900);scaler.matchWidthOrHeight=1;
 safe=Panel(canvas.transform,"Safe Area",Vector2.zero,Vector2.one,Color.clear);safe.GetComponent<Image>().raycastTarget=false;
 Panel(safe,"Header",new Vector2(0,.78f),Vector2.one,Cream);
 Label(safe,"TOY GARDEN",new Vector2(.06f,.92f),new Vector2(.68f,.99f),26);
 coins=Label(safe,"",new Vector2(.06f,.85f),new Vector2(.56f,.92f),28);
 income=Label(safe,"",new Vector2(.56f,.85f),new Vector2(.96f,.92f),20);
 CollectionButton=Button(safe,"Collection",new Vector2(.71f,.935f),new Vector2(.96f,.987f),Green,"TOYS");CollectionButton.onClick.AddListener(()=>{game.Cracker.CancelGesture();collectionPanel.SetActive(true);RefreshCollection();});
 
 progress=Label(safe,"",new Vector2(.06f,.69f),new Vector2(.94f,.77f),22);
 progress.alignment=TextAnchor.MiddleCenter;
 CrackButton=Button(safe,"Crack Egg",new Vector2(.13f,.39f),new Vector2(.87f,.69f),new Color(1,1,1,0),"");var target=CrackButton.gameObject.AddComponent<EggTapTarget>();target.Game=game;
 status=Label(safe,"",new Vector2(.06f,.335f),new Vector2(.94f,.405f),16);status.alignment=TextAnchor.MiddleCenter;
 Panel(safe,"Shop",new Vector2(0,.17f),new Vector2(1,.32f),Cream);
 for(int i=0;i<game.eggs.Length;i++){
 int index=i;float width=.92f/game.eggs.Length;float left=.04f+i*width;var button=Button(safe,"Buy "+game.eggs[i].displayName,new Vector2(left,.185f),new Vector2(left+width-.018f,.305f),Green,"");
 button.onClick.AddListener(()=>game.Buy(index));ShopButtons.Add(button);shopLabels.Add(button.GetComponentInChildren<Text>());
 }
 Panel(safe,"Garden",Vector2.zero,new Vector2(1,.16f),new Color(.83f,.91f,.82f));
 garden=Label(safe,"",new Vector2(.06f,.01f),new Vector2(.94f,.155f),18);
 NextButton=Button(safe,"Next Egg",new Vector2(.2f,.195f),new Vector2(.8f,.30f),Green,"NEXT EGG");NextButton.onClick.AddListener(game.Next);
 SimpleButton=Button(safe,"Simple Controls",new Vector2(.06f,.777f),new Vector2(.45f,.827f),Green,"SIMPLE: OFF");SimpleButton.onClick.AddListener(()=>{game.Cracker.SetSimpleControls(!game.Cracker.SimpleControls);SimpleButton.GetComponentInChildren<Text>().text=game.Cracker.SimpleControls?"SIMPLE: ON":"SIMPLE: OFF";});
 SoundButton=Button(safe,"Sound",new Vector2(.55f,.777f),new Vector2(.94f,.827f),Green,"SOUND: ON");SoundButton.onClick.AddListener(()=>{var sound=game.GetComponent<EggSoundFeedback>();sound.Muted=!sound.Muted;SoundButton.GetComponentInChildren<Text>().text=sound.Muted?"SOUND: OFF":"SOUND: ON";});
 gestureHint=Label(safe,"",new Vector2(.06f,.32f),new Vector2(.94f,.365f),15);gestureHint.alignment=TextAnchor.MiddleCenter;
 var track=Panel(safe,"Gesture Progress",new Vector2(.2f,.675f),new Vector2(.8f,.687f),new Color(.55f,.68f,.6f));
 chargeFill=Panel(track,"Fill",Vector2.zero,Vector2.one,Green).GetComponent<Image>();chargeFill.type=Image.Type.Simple;
 BuildCollection();
 if(FindFirstObjectByType<EventSystem>()==null){var events=new GameObject("Touch EventSystem",typeof(EventSystem));events.transform.SetParent(transform,false);
#if ENABLE_INPUT_SYSTEM
 events.AddComponent<InputSystemUIInputModule>();
#else
 events.AddComponent<StandaloneInputModule>();
#endif
 }
 game.Economy.Changed+=Refresh;game.Garden.Changed+=Refresh;game.Cracker.Changed+=Refresh;ApplySafeArea();Refresh();
 }
 void Update(){if(lastSafe!=Screen.safeArea || lastWidth!=Screen.width || lastHeight!=Screen.height)ApplySafeArea();}
 void ApplySafeArea(){lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;if(Screen.width<=0 || Screen.height<=0)return;safe.anchorMin=lastSafe.min/new Vector2(Screen.width,Screen.height);safe.anchorMax=lastSafe.max/new Vector2(Screen.width,Screen.height);}
 public void Refresh(){if(game==null || coins==null)return;
 coins.text=game.Economy.Coins.ToString("0.0")+" coins";income.text="+"+game.Garden.IncomePerSecond.ToString("0.#")+" / sec";
 var crack=game.Cracker;CrackButton.interactable=crack.IsCracking;
 NextButton.gameObject.SetActive(game.State==GameLoopController.PlayState.Reward);
 progress.text=game.State==GameLoopController.PlayState.Reward?"TOY UNLOCKED!\n"+new string('★',game.Collection.Stars(game.LastReward))+new string('☆',3-game.Collection.Stars(game.LastReward)):crack.IsCracking?crack.ActiveEgg.displayName+"\n"+Mathf.RoundToInt(crack.Progress*100)+"% OPEN   |   "+crack.FragmentsRemaining+" pieces":"BUY AN EGG\nYour next toy is waiting";
 status.text=game.State==GameLoopController.PlayState.Reward?game.Message:crack.IsCracking?crack.Instruction:game.Message;
 gestureHint.text=crack.IsCracking && crack.Interaction==EggInteractionKind.PressBreak && !crack.SimpleControls?(crack.IsHolding?"PRESSURE "+Mathf.RoundToInt(crack.Charge*100)+"% — RELEASE TO SNAP":"HOLD THE SHELL, THEN RELEASE"):"";
 float fill=crack.IsHolding?crack.Charge:crack.Progress;chargeFill.rectTransform.anchorMax=new Vector2(fill,1);chargeFill.transform.parent.gameObject.SetActive(crack.IsCracking);
 for(int i=0;i<ShopButtons.Count;i++){
 var egg=game.eggs[i];ShopButtons[i].gameObject.SetActive(game.State!=GameLoopController.PlayState.Reward);ShopButtons[i].interactable=game.Shop.CanBuy(egg);
 shopLabels[i].text=egg.displayName+"\n"+egg.price+" coins\n"+(egg.materialProfile==null || egg.materialProfile.interaction==EggInteractionKind.Tap?"TAP":egg.materialProfile.interaction==EggInteractionKind.PressBreak?"HOLD / RELEASE":"RUB TO MELT");
 }
 var counts=new Dictionary<ToyDefinition,int>();foreach(var toy in game.Garden.Toys){if(!counts.ContainsKey(toy))counts[toy]=0;counts[toy]++;}
 string list="GARDEN  |  "+game.Collection.UniqueCount+" collected  |  "+game.EggsOpened+" eggs\n";foreach(var pair in counts)list+=pair.Key.displayName+" x"+pair.Value+"   +"+(pair.Value*pair.Key.coinPerSecond).ToString("0.#")+" /s\n";garden.text=list;if(collectionPanel.activeSelf)RefreshCollection();
 }
 void BuildCollection(){
 collectionPanel=Panel(safe,"Toy Collection",new Vector2(.02f,.17f),new Vector2(.98f,.92f),Cream).gameObject;
 collectionText=Label(collectionPanel.transform,"",new Vector2(.05f,.9f),new Vector2(.75f,.99f),24);
 CloseCollectionButton=Button(collectionPanel.transform,"Close Collection",new Vector2(.79f,.92f),new Vector2(.98f,.99f),Green,"BACK");CloseCollectionButton.onClick.AddListener(()=>collectionPanel.SetActive(false));
 var toys=Catalog();collectionArt=new RawImage[toys.Count];collectionLabels=new Text[toys.Count];
 for(int i=0;i<toys.Count;i++){float x=.04f+(i%2)*.49f;float y=.48f-(i/2)*.44f;
 var card=Panel(collectionPanel.transform,"Toy card",new Vector2(x,y),new Vector2(x+.43f,y+.40f),new Color(.85f,.91f,.85f));
 var art=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage));art.transform.SetParent(card,false);var rt=art.GetComponent<RectTransform>();rt.anchorMin=new Vector2(.06f,.25f);rt.anchorMax=new Vector2(.94f,.98f);rt.offsetMin=rt.offsetMax=Vector2.zero;collectionArt[i]=art.GetComponent<RawImage>();collectionArt[i].raycastTarget=false;
 collectionLabels[i]=Label(card,"",new Vector2(.03f,.01f),new Vector2(.97f,.25f),16);collectionLabels[i].alignment=TextAnchor.MiddleCenter;
 }collectionPanel.SetActive(false);
 }
 List<ToyDefinition> Catalog(){var toys=new List<ToyDefinition>{game.starterToy};foreach(var egg in game.eggs)foreach(var toy in egg.possibleToys)if(!toys.Contains(toy))toys.Add(toy);return toys;}
 void RefreshCollection(){var toys=Catalog();collectionText.text="TOYS  "+game.Collection.UniqueCount+" / "+toys.Count;
 for(int i=0;i<toys.Count;i++){var toy=toys[i];bool unlocked=game.Collection.Count(toy)>0;collectionArt[i].texture=toy.artwork;collectionArt[i].color=unlocked?Color.white:new Color(0,0,0,.45f);collectionLabels[i].text=unlocked?toy.displayName+"  "+new string('★',game.Collection.Stars(toy))+"\nOwned "+game.Collection.Count(toy)+"  |  +"+toy.coinPerSecond+" /sec":"LOCKED\nFind inside an egg";}
 }
 void OnDestroy(){if(game==null)return;if(game.Economy!=null)game.Economy.Changed-=Refresh;if(game.Garden!=null)game.Garden.Changed-=Refresh;if(game.Cracker!=null)game.Cracker.Changed-=Refresh;}
 RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color){var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);var rt=obj.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;obj.GetComponent<Image>().color=color;return rt;}
 Text Label(Transform parent,string value,Vector2 min,Vector2 max,int size){var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);var rt=obj.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;var text=obj.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=Ink;text.alignment=TextAnchor.MiddleLeft;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.raycastTarget=false;return text;}
 Button Button(Transform parent,string name,Vector2 min,Vector2 max,Color color,string title){var rt=Panel(parent,name,min,max,color);var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=rt.GetComponent<Image>();var text=Label(rt,title,new Vector2(.04f,.04f),new Vector2(.96f,.96f),18);text.color=color.a==0?Ink:Color.white;text.alignment=TextAnchor.MiddleCenter;return button;}
}
}
