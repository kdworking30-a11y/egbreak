using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ToyEconomy.Editor {
 public static class MaterialGameplaySetup {
  const string Root="Assets/ToyEconomy/";
  [MenuItem("Tools/Toy Economy/Upgrade Material Gameplay")]
  public static void Upgrade(){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
   var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save the current scene before material upgrade");
   // Modify loaded scene in the Editor, so no external scene replacement is needed.
   if(scene.path!=ToyEconomySetup.ScenePath)scene=EditorSceneManager.OpenScene(ToyEconomySetup.ScenePath);
   var game=UnityEngine.Object.FindFirstObjectByType<GameLoopController>();if(game==null)throw new InvalidOperationException("Game loop missing");
   Directory.CreateDirectory(Root+"Materials");
   var shader=Shader.Find("ToyEconomy/MaterialSurface");if(shader==null)throw new InvalidOperationException("Material shader missing");
   var shell=Profile("Shell",EggInteractionKind.Tap,0,new Color(.93f,.87f,.7f),new Color(.98f,.96f,.87f),"TAP THE SHELL TO CRACK IT",1.7f,shader);
   var chocolate=Profile("Chocolate",EggInteractionKind.PressBreak,1,new Color(.22f,.085f,.035f),new Color(.36f,.16f,.08f),"HOLD TO BUILD PRESSURE. RELEASE TO BREAK.",.65f,shader);
   var crystal=Profile("Crystal",EggInteractionKind.Tap,3,new Color(.27f,.74f,.9f),new Color(.57f,.87f,.96f),"TAP THE CRYSTAL TO SHATTER IT",2,shader);
   var ice=Profile("Ice",EggInteractionKind.RubIce,2,new Color(.8f,.94f,1),new Color(.52f,.82f,.93f),"RUB ACROSS THE ICE TO MELT IT",.4f,shader);
   game.eggs[0].materialProfile=shell;game.eggs[0].hitsToBreak=6;
   game.eggs[1].materialProfile=chocolate;game.eggs[1].hitsToBreak=4;
   game.eggs[2].materialProfile=crystal;game.eggs[2].hitsToBreak=12;
   var iceEgg=AssetDatabase.LoadAssetAtPath<EggDefinition>(Root+"Data/egg_ice.asset");if(iceEgg==null){iceEgg=ScriptableObject.CreateInstance<EggDefinition>();AssetDatabase.CreateAsset(iceEgg,Root+"Data/egg_ice.asset");}
   iceEgg.eggId="ice";iceEgg.displayName="Ice";iceEgg.price=100;iceEgg.fragmentCount=game.eggs[2].fragmentCount;iceEgg.fracturedPrefab=game.eggs[2].fracturedPrefab;iceEgg.hitsToBreak=6;iceEgg.materialProfile=ice;iceEgg.possibleToys=game.eggs[2].possibleToys;
   game.eggs=new[]{game.eggs[0],game.eggs[1],game.eggs[2],iceEgg};
   foreach(var egg in game.eggs)EditorUtility.SetDirty(egg);
   EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText("/tmp/material-gameplay-upgrade.txt","SUCCESS: Basic tap, Chocolate hold/release, Crystal tap, Ice rub/melt");
   Debug.Log("[Material Gameplay] Four eggs, three interactions. Shared coin/reward/Next flow preserved.");
  }
  static EggMaterialProfile Profile(string name,EggInteractionKind kind,int look,Color outside,Color inside,string instruction,float impulse,Shader shader){
   string path=Root+"Materials/"+name+".asset";var profile=AssetDatabase.LoadAssetAtPath<EggMaterialProfile>(path);if(profile==null){profile=ScriptableObject.CreateInstance<EggMaterialProfile>();AssetDatabase.CreateAsset(profile,path);}
   profile.interaction=kind;profile.outside=Surface(name+"Outside",look,0,outside,shader);profile.inside=Surface(name+"Inside",look,1,inside,shader);profile.instruction=instruction;profile.impulse=impulse;profile.chargeSeconds=.65f;profile.shortPressProgress=.06f;profile.breaksToOpen=4;profile.meltRadius=.92f;profile.meltPerSecond=3.1f;profile.autoFinishMelt=.76f;EditorUtility.SetDirty(profile);return profile;
  }
  static Material Surface(string name,int look,int interior,Color color,Shader shader){string path=Root+"Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;mat.SetColor("_BaseColor",color);mat.SetFloat("_Kind",look);mat.SetFloat("_Interior",interior);EditorUtility.SetDirty(mat);return mat;}
 }
}
