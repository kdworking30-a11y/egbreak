#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ToyEconomy.Editor {
 public sealed class IdleProgressionVerification : MonoBehaviour {
  [Serializable] public class Report {public bool passed;public List<string> checks=new List<string>();public List<string> errors=new List<string>();}
  readonly Report report=new Report();GameLoopController game;
  void OnEnable(){Application.logMessageReceived+=Log;}
  void OnDisable(){Application.logMessageReceived-=Log;}
  void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)report.errors.Add(message+"\n"+stack);}
  void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);report.checks.Add(text);}
  void Click(Button b){Canvas.ForceUpdateCanvases();var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.TransformPoint(b.GetComponent<RectTransform>().rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);Check(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==b.gameObject,"Canvas reaches "+b.name);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);}
  IEnumerator Start(){Application.runInBackground=true;yield return null;var routine=Verify();while(true){object next;try{if(!routine.MoveNext())break;next=routine.Current;}catch(Exception e){report.errors.Add(e.ToString());break;}yield return next;}report.passed=report.errors.Count==0;File.WriteAllText("/tmp/idle-progression-verification.json",JsonUtility.ToJson(report,true));Debug.Log("[Idle Progression] "+(report.passed?"PASS":"FAIL"));}
  IEnumerator Verify(){
   game=FindFirstObjectByType<GameLoopController>();Check(game!=null,"Game initialized");Check(!game.Persistence.Enabled,"Batch verification cannot read or overwrite the child's progress");
   Check(game.EggsOpened==0 && game.Garden.Toys.Count==1,"Starter and zero completed eggs");
   var basic=Array.Find(game.eggs,e=>e.eggId=="basic");if(basic==null)basic=game.eggs[0];var chocolate=Array.Find(game.eggs,e=>e.eggId=="chocolate");var ice=Array.Find(game.eggs,e=>e.eggId=="ice");var crystal=Array.Find(game.eggs,e=>e.eggId=="crystal");
   Check(game.Progression.IsUnlocked(basic) && !game.Progression.IsUnlocked(chocolate) && !game.Progression.IsUnlocked(ice) && !game.Progression.IsUnlocked(crystal),"Only Basic unlocked at start");
   game.Economy.Earn(10000);Check(!game.Shop.Buy(crystal),"Money alone cannot bypass progression gates");
   Check(game.Garden.IncomePerSecond==game.starterToy.coinPerSecond,"Initial income is toy production only");
   var before=game.Economy.Coins;yield return new WaitForSeconds(.3f);Check(game.Economy.Coins>before,"Idle toys earn on real frames");
   Click(game.Hud.ToolsButton);yield return null;Capture("/tmp/idle-tools-start.png");
   Check(game.Hud.ToolButtons[0].interactable && !game.Hud.ToolButtons[1].interactable && !game.Hud.ToolButtons[2].interactable,"Tool shop shows gate states");
   double cost=game.Progression.ToolPrice(ToolKind.Hammer);before=game.Economy.Coins;Click(game.Hud.ToolButtons[0]);Check(game.Progression.Level(ToolKind.Hammer)==1 && game.Economy.Coins==before-cost,"Tool purchase deducts its shown price exactly once");
   Check(game.Cracker.TapPower==1.25f,"Hammer upgrade applies immediately");Click(game.Hud.CloseToolsButton);yield return null;
   game.Buy(Array.IndexOf(game.eggs,basic));Check(!game.Progression.BuyUpgrade(ToolKind.Hammer),"No upgrades during an active egg");
   Check(game.Collection.Count(game.Cracker.PendingToy)==0,"First Basic prioritizes a new friend over starter duplicate");
   game.Hit();Check(game.Cracker.Progress>.20f,"Hammer increases progress per real tap");
   int taps=1;while(game.Cracker.IsCracking && taps++<12)game.Hit();Check(game.State==GameLoopController.PlayState.Reward && taps==5,"Level-one hammer reduces Basic from six to five taps");
   Check(!game.Progression.BuyUpgrade(ToolKind.Hammer),"Reward screen also blocks upgrade purchases");
   int opened=game.EggsOpened;var count=game.Garden.Toys.Count;game.Hit();Check(game.EggsOpened==opened && game.Garden.Toys.Count==count,"Repeated reveal input cannot duplicate rewards or milestones");game.Next();yield return null;
   for(int n=2;n<=10;n++){
    int owned=game.Garden.Toys.Count;game.Buy(Array.IndexOf(game.eggs,basic));game.Cracker.SetSimpleControls(true);for(int h=0;h<6;h++)game.Hit();
    Check(game.EggsOpened==n && game.Garden.Toys.Count==owned+1,"Egg completion grants one productive friend: "+n);
    Check(game.Progression.IsUnlocked(chocolate)==(n>=3),"Chocolate gate at three: "+n);
    Check(game.Progression.IsUnlocked(ice)==(n>=6),"Ice gate at six: "+n);
    Check(game.Progression.IsUnlocked(crystal)==(n>=10),"Crystal gate at ten: "+n);
    double baseIncome=0;foreach(var toy in game.Garden.Toys)baseIncome+=toy.coinPerSecond;
    Check(Math.Abs(game.Garden.IncomePerSecond-baseIncome*(1+(n/3)*.1))<.00001,"Milestone multiplies toy income once: "+n);
    game.Next();game.Cracker.SetSimpleControls(false);yield return null;
   }
   Click(game.Hud.ToolsButton);yield return null;Check(game.Hud.ToolButtons[1].interactable && game.Hud.ToolButtons[2].interactable,"Gloves and cloth become purchasable at their gates");
   Click(game.Hud.ToolButtons[1]);Click(game.Hud.ToolButtons[2]);Check(game.Cracker.ChargeSpeed==1.2f && game.Cracker.MeltPower==1.25f,"Specialist upgrades increase their own mechanics");
   for(int level=1;level<5;level++)Check(game.Progression.BuyUpgrade(ToolKind.Hammer),"Buy next Hammer level "+(level+1));
   before=game.Economy.Coins;Check(!game.Progression.BuyUpgrade(ToolKind.Hammer) && game.Economy.Coins==before,"Max level cannot spend more coins");
   Capture("/tmp/idle-tools-upgraded.png");Click(game.Hud.CloseToolsButton);yield return null;
   game.Buy(Array.IndexOf(game.eggs,chocolate));var point=(Vector2)Camera.main.WorldToScreenPoint(game.Cracker.transform.position);game.Cracker.BeginGesture(point);yield return new WaitForSeconds(.3f);Check(game.Cracker.Charge>.5f,"Upgraded gloves fill pressure faster on real frames");game.Cracker.CancelGesture();game.Cracker.SetSimpleControls(true);for(int h=0;h<6;h++)game.Hit();game.Next();game.Cracker.SetSimpleControls(false);yield return null;
   game.Persistence.VerificationSavePath="/tmp/egbreak-idle-save-verification/progress.json";game.Persistence.Enabled=true;game.Persistence.Save();
   Check(File.Exists(game.Persistence.SavePath),"Progress file written in isolated verification directory");double diskCoins=game.Economy.Coins;game.Economy.Initialize(17);game.Persistence.Save();Check(File.Exists(game.Persistence.SavePath+".bak"),"Atomic replacement retains a backup save");
   File.WriteAllText(game.Persistence.SavePath,"broken-json");Check(game.Persistence.Load() && game.Economy.Coins>=diskCoins,"Corrupt current save recovers from backup");game.Persistence.Enabled=false;
   var snapshot=game.Persistence.Snapshot(100000);string json=JsonUtility.ToJson(snapshot);snapshot=JsonUtility.FromJson<IdleSaveData>(json);double savedIncome=game.Garden.IncomePerSecond;
   Check(game.Persistence.Restore(snapshot,100060),"Progress round-trip restores valid save");
   Check(Math.Abs(game.Economy.Coins-snapshot.coins-savedIncome*60)<.001,"Offline reward equals sixty seconds of saved toy income");
   Check(game.Progression.Level(ToolKind.Hammer)==5 && game.Progression.Level(ToolKind.Gloves)==1 && game.EggsOpened==11,"Tool levels and egg milestones survive restore");
   Check(game.Persistence.Restore(snapshot,200000),"Restore after long absence");Check(Math.Abs(game.Economy.Coins-snapshot.coins-savedIncome*14400)<.001,"Offline production capped at four hours");
   Check(game.Persistence.Restore(snapshot,90000) && Math.Abs(game.Economy.Coins-snapshot.coins)<.001,"Backward clock never removes coins or grants negative income");
   var invalid=JsonUtility.FromJson<IdleSaveData>(json);invalid.toolLevels[0]=99;before=game.Economy.Coins;Check(!game.Persistence.Restore(invalid,100000) && before==game.Economy.Coins,"Invalid save is rejected without partial wallet changes");
   invalid=JsonUtility.FromJson<IdleSaveData>(json);invalid.coins=double.NaN;Check(!game.Persistence.Restore(invalid,100000),"Non-finite wallet is rejected");
   invalid=JsonUtility.FromJson<IdleSaveData>(json);invalid.toys[0].id="unknown";Check(!game.Persistence.Restore(invalid,100000),"Unknown reward IDs cannot corrupt collection");
   game.Buy(Array.IndexOf(game.eggs,ice));Check(game.Cracker.IsCracking,"Ice can be bought after progression");var pending=game.Persistence.Snapshot(100000);Check(pending.pendingRefund==ice.price,"Unfinished egg records exact paid cost for recovery");
   game.Cracker.SetSimpleControls(true);for(int h=0;h<6;h++)game.Hit();game.Next();Check(game.Persistence.Restore(pending,100000),"Restore unfinished purchase snapshot");Check(Math.Abs(game.Economy.Coins-pending.coins-ice.price)<.001 && game.State==GameLoopController.PlayState.Shop,"Unfinished egg refunded and returned to shop without a free toy");
   game.Economy.Initialize(0);Check(!game.Progression.BuyUpgrade(ToolKind.Gloves),"Insufficient coins cannot buy upgrade");
   game.Economy.Earn(250);game.SetNotice("Every friend earns coins. Choose your next egg!");game.Hud.Refresh();yield return null;Capture("/tmp/idle-main.png");
  }
  void Capture(string path){var camera=Camera.main;var canvas=FindFirstObjectByType<Canvas>();var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var rt=new RenderTexture(480,900,24);var image=new Texture2D(480,900,TextureFormat.RGB24,false);try{camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;rt.Release();Destroy(rt);Destroy(image);}}
 }
}
#endif
