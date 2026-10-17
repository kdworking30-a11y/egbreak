using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace ToyEconomy.Editor {
[InitializeOnLoad]
public static class ToyEconomySetup {
 public const string ScenePath="Assets/ToyEconomy/Scenes/ToyEconomy.unity";
 static readonly string CommandPath="/tmp/toy-economy-command.txt";
 static ToyEconomySetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath))return;var command=File.ReadAllText(CommandPath).Trim();File.Delete(CommandPath);try{
 if(command=="build"){Build();File.WriteAllText("/tmp/toy-economy-build.txt","SUCCESS");}
 else if(command=="upgrade"){ImageAIIntegration.Upgrade();File.WriteAllText("/tmp/toy-economy-upgrade.txt","SUCCESS");}
 else if(command=="materials"){MaterialGameplaySetup.Upgrade();}
 else if(command=="material-test"){SessionState.SetBool("ToyEconomy.MaterialVerify",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
 else if(command=="state"){File.WriteAllText("/tmp/toy-economy-state.txt","playing="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused);}
 else if(command=="test"){SessionState.SetBool("ToyEconomy.MaterialVerify",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
 else if(command=="play")EditorApplication.isPlaying=true;
 else if(command=="stop")EditorApplication.isPlaying=false;
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("/tmp/toy-economy-build.txt",e.ToString());}}
 [MenuItem("Tools/Toy Economy/Create or Open MVP")]
 public static void Build(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before setup.");
 if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before setup.");
 if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
 Directory.CreateDirectory("Assets/ToyEconomy/Data");Directory.CreateDirectory("Assets/ToyEconomy/Scenes");
 var bunny=Toy("Bunny",1,new Color(.95f,.75f,.82f));var chick=Toy("Chick",2,new Color(1,.85f,.32f));var bear=Toy("Chocolate Bear",5,new Color(.50f,.28f,.18f));var fox=Toy("Crystal Fox",15,new Color(.42f,.81f,.93f));
 var basic=Egg("Basic",10,8,3,new Color(.98f,.79f,.44f),new[]{bunny,chick});var chocolate=Egg("Chocolate",50,24,5,new Color(.55f,.29f,.17f),new[]{bear});var crystal=Egg("Crystal",200,64,7,new Color(.34f,.77f,.87f),new[]{fox});
 var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new InvalidOperationException("URP Lit shader missing");
 var material=new Material(shader);material.SetFloat("_Smoothness",.38f);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,"Assets/ToyEconomy/Data/EggShell.mat");
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-8);
 var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=4.6f;cam.backgroundColor=new Color(.73f,.85f,.79f);cam.clearFlags=CameraClearFlags.SolidColor;
 var light=new GameObject("Soft sunlight",typeof(Light));light.transform.rotation=Quaternion.Euler(30,-30,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;
 RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.75f,.8f,.85f);
 var loop=new GameObject("Toy Economy Game Loop").AddComponent<GameLoopController>();loop.starterToy=bunny;loop.eggs=new[]{basic,chocolate,crystal};loop.shellMaterial=material;
 PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
 EditorSceneManager.SaveScene(scene,ScenePath);
 var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};foreach(var s in EditorBuildSettings.scenes)if(s.path!=ScenePath)scenes.Add(s);EditorBuildSettings.scenes=scenes.ToArray();
 AssetDatabase.SaveAssets();Debug.Log("[Toy Economy] Scene created. Press Play.");
 }
 static ToyDefinition Toy(string name,float rate,Color color){var toy=ScriptableObject.CreateInstance<ToyDefinition>();toy.toyId=name.ToLowerInvariant().Replace(' ','_');toy.displayName=name;toy.coinPerSecond=rate;toy.color=color;AssetDatabase.CreateAsset(toy,"Assets/ToyEconomy/Data/"+toy.toyId+".asset");return toy;}
 static EggDefinition Egg(string name,int price,int pieces,int hits,Color color,ToyDefinition[] toys){var egg=ScriptableObject.CreateInstance<EggDefinition>();egg.eggId=name.ToLowerInvariant();egg.displayName=name;egg.price=price;egg.fragmentCount=pieces;egg.hitsToBreak=hits;egg.color=color;egg.possibleToys=toys;AssetDatabase.CreateAsset(egg,"Assets/ToyEconomy/Data/egg_"+egg.eggId+".asset");return egg;}
}
}
