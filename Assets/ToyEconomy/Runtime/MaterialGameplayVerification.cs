#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ToyEconomy.Editor {
 public sealed class MaterialGameplayVerification : MonoBehaviour {
  [Serializable] public class Report {public bool passed;public string environment="Unity Editor Play Mode, Canvas pointer events; 1000 test coins added to exercise every egg";public List<string> checks=new List<string>();public List<string> errors=new List<string>();public int toys;public double income;}
  Report report=new Report();GameLoopController game;bool iceCaptured;
  void OnEnable(){Application.logMessageReceived+=Log;}
  void OnDisable(){Application.logMessageReceived-=Log;}
  void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)report.errors.Add(text+"\n"+stack);}
  void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);report.checks.Add(message);File.WriteAllText("/tmp/material-gameplay-live.json",JsonUtility.ToJson(report,true));}
  PointerEventData Pointer(Vector2 point,int id=0)=>new PointerEventData(EventSystem.current){pointerId=id,position=point,button=PointerEventData.InputButton.Left};
  Vector2 Center(Button b)=>RectTransformUtility.WorldToScreenPoint(null,b.GetComponent<RectTransform>().TransformPoint(b.GetComponent<RectTransform>().rect.center));
  void Click(Button button){Canvas.ForceUpdateCanvases();var data=Pointer(Center(button));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Canvas reaches "+button.name);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);}
  void Down(PointerEventData data)=>ExecuteEvents.Execute(game.Hud.CrackButton.gameObject,data,ExecuteEvents.pointerDownHandler);
  void Up(PointerEventData data)=>ExecuteEvents.Execute(game.Hud.CrackButton.gameObject,data,ExecuteEvents.pointerUpHandler);
  IEnumerator Start(){Application.runInBackground=true;yield return null;var routine=Verify();bool running=true;while(running){object next=null;try{running=routine.MoveNext();if(running)next=routine.Current;}catch(Exception e){report.errors.Add(e.ToString());running=false;}if(running)yield return next;}
   report.passed=report.errors.Count==0;if(game!=null){report.toys=game.Garden.Toys.Count;report.income=game.Garden.IncomePerSecond;}File.WriteAllText("/tmp/material-gameplay-verification.json",JsonUtility.ToJson(report,true));Debug.Log("[Material Gameplay Verification] "+(report.passed?"PASS":"FAIL"));}
  IEnumerator Verify(){
   game=FindFirstObjectByType<GameLoopController>();Check(game!=null && game.eggs.Length==4,"Four eggs retain previous Crystal and add Ice");
   double coins=game.Economy.Coins;yield return new WaitForSeconds(.4f);Check(game.Economy.Coins>coins,"Toy income continues in real frames");
   game.Economy.Earn(1000);yield return null;
   for(int tier=0;tier<4;tier++){
    int owned=game.Garden.Toys.Count;double oldIncome=game.Garden.IncomePerSecond;double before=game.Economy.Coins;var egg=game.eggs[tier];Click(game.Hud.ShopButtons[tier]);
    Check(game.Cracker.ActiveEgg==egg && game.Economy.Coins==before-egg.price,"Purchase deducts exact price: "+egg.displayName);
    Check(game.Cracker.FragmentsRemaining==egg.fragmentCount,"Baked shell count: "+egg.fragmentCount);
    var material=game.Cracker.GetComponentInChildren<MeshRenderer>().sharedMaterials;Check(material.Length>1 && material[0]!=material[1],"Outside and cut face have separate materials");
    Check(!game.Shop.Buy(game.eggs[0]),"Busy cracker blocks another egg");
    var kind=game.Cracker.Interaction;var point=Pointer(Center(game.Hud.CrackButton));
    if(kind==EggInteractionKind.Tap){
     Capture("/tmp/material-gameplay-"+egg.eggId+"-intact.png");
     Click(game.Hud.CrackButton);Check(game.Cracker.Progress>0,"Tap detaches a shell cluster");
     int guard=0;while(game.Cracker.IsCracking && guard++<20){Click(game.Hud.CrackButton);yield return null;}
    }else if(kind==EggInteractionKind.PressBreak){
     ExecuteEvents.Execute(game.Hud.CrackButton.gameObject,point,ExecuteEvents.pointerClickHandler);Check(game.Cracker.Progress==0,"Chocolate ignores click without a press gesture");
     Down(point);yield return new WaitForSeconds(.25f);Check(game.Cracker.Charge>0 && game.Cracker.IsHolding,"Chocolate charges while held");
     var other=Pointer(point.position,8);Down(other);Up(other);Check(game.Cracker.IsHolding,"Second pointer cannot release first gesture");
     Capture("/tmp/material-gameplay-chocolate-pressure.png");
     Click(game.Hud.CollectionButton);Check(!game.Cracker.IsHolding && game.Cracker.Progress==0,"Opening collection cancels pressure without phantom break");yield return null;Click(game.Hud.CloseCollectionButton);Up(point);
     Down(point);Up(point);Check(game.Cracker.Progress>0 && game.Cracker.Progress<.1f,"Early release retains a small amount of progress");
     Click(game.Hud.SimpleButton);Click(game.Hud.CrackButton);float switched=game.Cracker.Progress;Click(game.Hud.SimpleButton);
     Down(point);yield return new WaitForSeconds(.7f);Up(point);Check(game.Cracker.Progress>switched,"Switching simple controls back to pressure preserves forward progress");yield return null;
     int guard=0;while(game.Cracker.IsCracking && guard++<5){Down(point);yield return new WaitForSeconds(.7f);Check(game.Cracker.Charge>=.99f,"Pressure caps safely at full");Up(point);yield return null;}
    }else{
     Click(game.Hud.CrackButton);Check(game.Cracker.Progress==0,"Ice cannot be opened by tapping");
     game.Cracker.RubStroke(point.position,point.position,.05f);Check(game.Cracker.Progress==0,"Stationary finger does not melt ice");
     Vector2 start=Camera.main.WorldToScreenPoint(new Vector3(-.85f,0,0));point=Pointer(start);Down(point);ExecuteEvents.Execute(game.Hud.CrackButton.gameObject,point,ExecuteEvents.beginDragHandler);
     float started=Time.unscaledTime;int frame=0;
     while(game.Cracker.IsCracking && Time.unscaledTime-started<50){float t=Time.unscaledTime-started;
      point.position=Camera.main.WorldToScreenPoint(new Vector3(Mathf.Sin(t*9)*.9f,Mathf.Sin(t*1.6f)*1.05f,0));
      ExecuteEvents.Execute(game.Hud.CrackButton.gameObject,point,ExecuteEvents.dragHandler);
      if(game.Cracker.IsCracking && frame++==30){float progress=game.Cracker.Progress;game.Cracker.RubStroke(start,point.position,1);Check(Mathf.Abs(game.Cracker.Progress-progress)<.00001f,"Extra rub events cannot exceed one frame's melt budget");}
      if(game.Cracker.Progress>.55f && !iceCaptured){iceCaptured=true;Capture("/tmp/material-gameplay-ice-melting.png");}
      yield return null;
     }Up(point);Check(iceCaptured,"Rubbing creates visible persistent thaw progress");
    }
    Check(!game.Cracker.IsCracking && game.State==GameLoopController.PlayState.Reward,"Material completes into shared reveal: "+egg.displayName);
    Check(game.Garden.Toys.Count==owned+1 && game.Garden.IncomePerSecond>oldIncome,"Exactly one productive toy awarded: "+egg.displayName);
    game.Hit();game.Cracker.EndGesture(point.position);game.Cracker.RubStroke(point.position,point.position+Vector2.one*20,.03f);Check(game.Garden.Toys.Count==owned+1,"Post-reveal gestures cannot duplicate reward");
    yield return new WaitForSeconds(.45f);Capture("/tmp/material-gameplay-"+egg.eggId+"-reward.png");Click(game.Hud.NextButton);Check(game.State==GameLoopController.PlayState.Shop,"Next returns to shop: "+egg.displayName);yield return null;
   }
   Click(game.Hud.SimpleButton);Click(game.Hud.ShopButtons[3]);int count=game.Garden.Toys.Count;
   for(int i=0;i<6;i++){Click(game.Hud.CrackButton);yield return null;}
   Check(game.State==GameLoopController.PlayState.Reward && game.Garden.Toys.Count==count+1,"Simple controls open Ice with same reward in six taps");
   Click(game.Hud.NextButton);Click(game.Hud.SimpleButton);Click(game.Hud.SoundButton);Check(game.GetComponent<EggSoundFeedback>().Muted,"Sound toggle mutes feedback");
   yield return new WaitForSeconds(2.2f);Check(game.Cracker.GetComponentsInChildren<Rigidbody>().Length==0 && game.Cracker.GetComponentsInChildren<MeltDebris>().Length==0,"Debris and melt objects clean up");
  }
 void Capture(string path){
 var camera=Camera.main;var canvas=FindFirstObjectByType<Canvas>();var priorTarget=camera.targetTexture;var priorActive=RenderTexture.active;var priorMode=canvas.renderMode;var priorCamera=canvas.worldCamera;float priorPlane=canvas.planeDistance;
 var target=new RenderTexture(480,900,24);var texture=new Texture2D(480,900,TextureFormat.RGB24,false);
 try{camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,480,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
 finally{camera.targetTexture=priorTarget;RenderTexture.active=priorActive;canvas.renderMode=priorMode;canvas.worldCamera=priorCamera;canvas.planeDistance=priorPlane;Canvas.ForceUpdateCanvases();target.Release();Destroy(target);Destroy(texture);}
 }
 }
}
#endif
