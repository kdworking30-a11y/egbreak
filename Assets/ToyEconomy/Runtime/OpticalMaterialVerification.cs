#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
namespace ToyEconomy.Editor {
 public sealed class OpticalMaterialVerification : MonoBehaviour {
  [Serializable] public class Report {public bool passed;public List<string> checks=new List<string>();public List<string> errors=new List<string>();public float iceThawDelta;public float iceReflectionDelta,iceRefractionDelta,crystalReflectionDelta,crystalRefractionDelta;}
  Report report=new Report();GameLoopController game;
  void OnEnable(){Application.logMessageReceived+=Log;}
  void OnDisable(){Application.logMessageReceived-=Log;}
  void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)report.errors.Add(text+"\n"+stack);}
  void Check(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);report.checks.Add(message);}
  IEnumerator Start(){Application.runInBackground=true;yield return null;var routine=Verify();bool running=true;while(running){object next=null;try{running=routine.MoveNext();if(running)next=routine.Current;}catch(Exception e){report.errors.Add(e.ToString());running=false;}if(running)yield return next;}report.passed=report.errors.Count==0;File.WriteAllText("/tmp/optical-material-verification.json",JsonUtility.ToJson(report,true));Debug.Log("[Optical Verification] "+(report.passed?"PASS":"FAIL"));}
  IEnumerator Verify(){
   game=FindFirstObjectByType<GameLoopController>();game.Progression.VerificationBypass=true;var camera=Camera.main;var data=camera.GetComponent<UniversalAdditionalCameraData>();
   Check(data!=null && data.requiresColorTexture,"Main camera generates URP opaque color for refraction");
   game.Economy.Earn(1000);yield return null;
   for(int index=0;index<game.eggs.Length;index++){
    var egg=game.eggs[index];game.Buy(index);yield return null;
    Check(game.Cracker.IsCracking,"Buy "+egg.displayName+" into material showcase");
    var cube=egg.materialProfile.outside.GetTexture("_EnvironmentCube") as Cubemap;Check(cube!=null && cube.mipmapCount>1,"HDR reflection cubemap and roughness mip chain: "+egg.displayName);
    Color32[] baseline=Capture("/tmp/optical-"+egg.eggId+".png");
    if(index>=2){
     Check(egg.materialProfile.outside.shader.name=="ToyEconomy/OpticalGlass","Transparent optical shader: "+egg.displayName);
     Check(egg.materialProfile.outside.GetFloat("_IOR")>1,"Physical IOR set: "+egg.displayName);
     float reflection=egg.materialProfile.outside.GetFloat("_ReflectionStrength");float refraction=egg.materialProfile.outside.GetFloat("_RefractionStrength");
     Set("_ReflectionStrength",0);var noReflection=Capture("/tmp/optical-"+egg.eggId+"-no-reflection.png");Set("_ReflectionStrength",reflection);
     float reflectionDelta=Delta(baseline,noReflection);Check(reflectionDelta>.002f,"Rendered pixels change when cubemap reflection is disabled: "+egg.displayName+" "+reflectionDelta);
     Set("_RefractionStrength",0);var noRefraction=Capture("/tmp/optical-"+egg.eggId+"-no-refraction.png");Set("_RefractionStrength",refraction);
     float refractionDelta=Delta(baseline,noRefraction);Check(refractionDelta>.002f,"Rendered pixels change when screen refraction is disabled: "+egg.displayName+" "+refractionDelta);
     if(egg.eggId=="ice"){
      Heat(.85f);var thawed=Capture("/tmp/optical-ice-thawed.png");Heat(0);
      report.iceThawDelta=Delta(baseline,thawed);Check(report.iceThawDelta>.004f,"Local thaw visibly clears frost: "+report.iceThawDelta);
     }
     if(index==2){report.crystalReflectionDelta=reflectionDelta;report.crystalRefractionDelta=refractionDelta;}else{report.iceReflectionDelta=reflectionDelta;report.iceRefractionDelta=refractionDelta;}
    }
    if(index>=2){
     Check(game.Cracker.GetComponentInChildren<OpticalCutFaceVisibility>().HiddenTriangles>100,"Bonded internal cut triangles are hidden: "+egg.displayName);
     Check(egg.materialProfile.outside.GetFloat("_Density")>.5f,"Optical volume density hides intact reward: "+egg.displayName);
     float density=egg.materialProfile.outside.GetFloat("_Density");Set("_Density",0);var thin=Capture("/tmp/optical-"+egg.eggId+"-thin.png");Set("_Density",density);
     Check(Delta(baseline,thin)>.02f,"Dense shell visibly suppresses reward compared with thin glass: "+egg.displayName);
     game.Cracker.SetSimpleControls(true);game.Hit();game.Hit();
     yield return new WaitForSeconds(.2f);
     Capture("/tmp/optical-"+egg.eggId+"-partial.png");
     yield return new WaitForSeconds(2);Capture("/tmp/optical-"+egg.eggId+"-partial-settled.png");
     // Reverse every fragment's sortingOrder: nearest surfaces must still win.
     var before=Capture("/tmp/optical-"+egg.eggId+"-depth-before.png");
     var renderers=game.Cracker.GetComponentsInChildren<MeshRenderer>();
     for(int r=0;r<renderers.Length;r++)renderers[r].sortingOrder=r+1;
     var after=Capture("/tmp/optical-"+egg.eggId+"-depth-reversed.png");
     Check(Delta(before,after)<.002f,"Fragment draw order cannot overwrite front shell or reward: "+egg.displayName+" "+Delta(before,after));
     foreach(var renderer in renderers)renderer.sortingOrder=0;
    }
    int owned=game.Garden.Toys.Count;game.Cracker.SetSimpleControls(true);for(int n=0;n<6 && game.Cracker.IsCracking;n++)game.Hit();
    Check(game.Garden.Toys.Count==owned+1 && game.State==GameLoopController.PlayState.Reward,"Optical shell still grants exactly one productive reward: "+egg.displayName);
    if(index>=2){bool clear=true;foreach(var renderer in game.Cracker.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled && renderer.gameObject.name=="Detached shell debris")clear=false;Check(clear,"Reward screen disables obstructing shell debris: "+egg.displayName);}
    yield return new WaitForSeconds(.45f);Capture("/tmp/optical-"+egg.eggId+"-reward.png");game.Next();game.Cracker.SetSimpleControls(false);yield return null;
   }
  }
  void Heat(float value){var block=new MaterialPropertyBlock();foreach(var renderer in game.Cracker.GetComponentsInChildren<MeshRenderer>()){renderer.GetPropertyBlock(block);block.SetFloat("_Heat",value);renderer.SetPropertyBlock(block);}}
  void Set(string name,float value){var materials=new HashSet<Material>();foreach(var renderer in game.Cracker.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)materials.Add(material);foreach(var material in materials)if(material.HasProperty(name))material.SetFloat(name,value);}
  float Delta(Color32[] a,Color32[] b){double sum=0;int count=0;for(int y=285;y<605;y++)for(int x=110;x<370;x++){int i=y*480+x;sum+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);count+=3;}return (float)(sum/(count*255));}
  Color32[] Capture(string path){
   var camera=Camera.main;var canvas=FindFirstObjectByType<Canvas>();var priorTarget=camera.targetTexture;var priorActive=RenderTexture.active;var priorMode=canvas.renderMode;var priorCamera=canvas.worldCamera;float priorPlane=canvas.planeDistance;
   var target=new RenderTexture(480,900,24);var texture=new Texture2D(480,900,TextureFormat.RGB24,false);
   try{camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,480,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());return texture.GetPixels32();}
   finally{camera.targetTexture=priorTarget;RenderTexture.active=priorActive;canvas.renderMode=priorMode;canvas.worldCamera=priorCamera;canvas.planeDistance=priorPlane;Canvas.ForceUpdateCanvases();target.Release();Destroy(target);Destroy(texture);}
  }
 }
}
#endif
