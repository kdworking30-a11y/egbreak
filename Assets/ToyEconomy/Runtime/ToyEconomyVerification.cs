#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ToyEconomy.Editor {
public sealed class ToyEconomyVerification : MonoBehaviour {
 [Serializable] public class Report {public bool passed;public List<string> checks=new List<string>();public List<string> errors=new List<string>();public double finalCoins;public double finalIncome;public int toyCount;}
 readonly Report report=new Report();
 void OnEnable(){Application.logMessageReceived+=Log;}
 void OnDisable(){Application.logMessageReceived-=Log;}
 void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)report.errors.Add(message+"\n"+stack);}
 void Check(bool value,string message){if(!value)throw new Exception("FAIL: "+message);report.checks.Add(message);File.WriteAllText("/tmp/imageai-fracture-live-checks.json",JsonUtility.ToJson(report,true));}
 void CapturePortrait(string path){
 var camera=Camera.main;var canvas=FindFirstObjectByType<Canvas>();var priorTarget=camera.targetTexture;var priorActive=RenderTexture.active;var priorMode=canvas.renderMode;var priorCamera=canvas.worldCamera;float priorPlane=canvas.planeDistance;
 var target=new RenderTexture(480,900,24);var texture=new Texture2D(480,900,TextureFormat.RGB24,false);
 try{camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,480,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
 finally{camera.targetTexture=priorTarget;RenderTexture.active=priorActive;canvas.renderMode=priorMode;canvas.worldCamera=priorCamera;canvas.planeDistance=priorPlane;Canvas.ForceUpdateCanvases();target.Release();Destroy(target);Destroy(texture);}
 }
 void Press(Button button){
 Canvas.ForceUpdateCanvases();
 var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center)),button=PointerEventData.InputButton.Left};
 var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
 Check(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Canvas raycast reaches "+button.name);
 ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 IEnumerator Start(){
 Application.runInBackground=true;
 yield return null;
 var routine=Verify();bool running=true;
 while(running){object current=null;try{running=routine.MoveNext();if(running)current=routine.Current;}catch(Exception e){report.errors.Add(e.ToString());running=false;}if(running)yield return current;}
 report.passed=report.errors.Count==0;File.WriteAllText("/tmp/imageai-fracture-verification.json",JsonUtility.ToJson(report,true));Debug.Log("[Toy Economy Verification] "+(report.passed?"PASS":"FAIL"));
 }
 IEnumerator Verify(){
 var game=FindFirstObjectByType<GameLoopController>();Check(game!=null,"MVP scene bootstraps in Play Mode");
 Check(game.Garden.Toys.Count==1 && game.Garden.IncomePerSecond==1,"Starter toy earns 1 coin/sec");
 Check(game.Hud.ShopButtons[0].interactable && !game.Hud.ShopButtons[2].interactable,"HUD enables affordable eggs and disables unaffordable eggs");
 double before=game.Economy.Coins;Check(!game.Shop.Buy(game.eggs[2]) && game.Economy.Coins==before,"Insufficient funds never charges coins");
 var invalid=ScriptableObject.CreateInstance<EggDefinition>();invalid.possibleToys=Array.Empty<ToyDefinition>();Check(!game.Shop.Buy(invalid) && game.Economy.Coins==before,"Invalid reward pool rejected without charging");Destroy(invalid);
 float start=Time.time;yield return new WaitForSeconds(1.1f);double earned=game.Economy.Coins-before;Check(Math.Abs(earned-(Time.time-start))<.12,"Real Update accrues coins at 1/sec");
 for(int tier=0;tier<game.eggs.Length;tier++){
 var egg=game.eggs[tier];if(game.Economy.Coins<egg.price){float wait=(float)((egg.price-game.Economy.Coins)/game.Garden.IncomePerSecond)+.15f;yield return new WaitForSeconds(wait);}
 Check(game.Hud.ShopButtons[tier].interactable,"Tier "+tier+" becomes affordable through toy income");
 before=game.Economy.Coins;double oldIncome=game.Garden.IncomePerSecond;int oldCount=game.Garden.Toys.Count;
 Press(game.Hud.ShopButtons[tier]);Check(game.Cracker.IsCracking && Math.Abs(game.Economy.Coins-(before-egg.price))<.001,"Tier "+tier+" UI purchase deducts exact price");
 Check(game.Cracker.PendingToy!=null,"Reward chosen before cracking");
 Check(game.Cracker.FragmentsRemaining==egg.fragmentCount,"Tier "+tier+" creates "+egg.fragmentCount+" predefined fragments");
 before=game.Economy.Coins;Check(!game.Shop.Buy(game.eggs[0]) && game.Economy.Coins==before,"Busy cracker rejects another purchase");
 for(int hit=1;hit<=egg.hitsToBreak;hit++){
 Press(game.Hud.CrackButton);int expected=egg.fragmentCount-Mathf.CeilToInt((float)hit/egg.hitsToBreak*egg.fragmentCount);
 Check(game.Cracker.FragmentsRemaining==expected,"Tier "+tier+" hit "+hit+" leaves "+expected+" fragments");
 if(hit<egg.hitsToBreak)Check(game.Garden.Toys.Count==oldCount,"No early reward");
 if(tier==0 && hit==egg.hitsToBreak/2)CapturePortrait("/tmp/imageai-fracture-cracking.png");yield return null;
 }
 Check(game.Cracker.DetachedCount==egg.fragmentCount,"Every source chunk detached exactly once");
 Check(game.State==GameLoopController.PlayState.Reward && game.Shop.RewardPending,"Reward screen waits for Next");
 Check(game.Collection.Stars(game.LastReward)>0,"Toy ID unlocked in collection with stars");
 Check(!game.Hud.ShopButtons[0].gameObject.activeSelf && game.Hud.NextButton.gameObject.activeSelf,"Next replaces shop on reward screen");
 Check(!game.Cracker.IsCracking && game.Garden.Toys.Count==oldCount+1,"Tier "+tier+" reveals exactly one toy");
 double newRate=game.Garden.IncomePerSecond;Check(newRate>oldIncome,"Tier "+tier+" increases income/sec");
 game.Hit();Check(game.Garden.Toys.Count==oldCount+1,"Extra tap cannot duplicate reward");
 if(tier==2){yield return new WaitForSeconds(.5f);CapturePortrait("/tmp/imageai-fracture-unlock.png");yield return null;}
 before=game.Economy.Coins;start=Time.time;yield return new WaitForSeconds(1);Check(Math.Abs((game.Economy.Coins-before)-newRate*(Time.time-start))<newRate*.12,"Tier "+tier+" new income runs in real frames");
 Press(game.Hud.NextButton);Check(game.State==GameLoopController.PlayState.Shop && !game.Shop.RewardPending,"Next returns to shop without extra reward");
 }
 Press(game.Hud.CollectionButton);Check(game.Hud.CloseCollectionButton.gameObject.activeInHierarchy,"Collection opens from HUD");yield return null;CapturePortrait("/tmp/imageai-fracture-collection.png");Press(game.Hud.CloseCollectionButton);
 yield return new WaitForSeconds(3.1f);
 int beforeDuplicates=game.Garden.Toys.Count;
 for(int cycle=0;cycle<6;cycle++){
 if(game.Economy.Coins<10)yield return new WaitForSeconds(.6f);
 Press(game.Hud.ShopButtons[0]);for(int tap=0;tap<game.eggs[0].hitsToBreak;tap++)game.Hit();
 Check(game.Garden.Toys.Count==beforeDuplicates+cycle+1,"Duplicate still adds productive toy "+cycle);
 yield return null;Press(game.Hud.NextButton);yield return null;
 }
 foreach(var toy in game.eggs[0].possibleToys)Check(game.Collection.Stars(toy)==3,"Duplicate stars cap at three: "+toy.displayName);
 yield return new WaitForSeconds(2.2f);Check(game.Cracker.GetComponentsInChildren<Rigidbody>().Length==0,"Detached physics debris cleaned after two seconds");
 report.finalCoins=game.Economy.Coins;report.finalIncome=game.Garden.IncomePerSecond;report.toyCount=game.Garden.Toys.Count;
 CapturePortrait("/tmp/imageai-fracture-playmode.png");yield return null;
 }
}
}

#endif
