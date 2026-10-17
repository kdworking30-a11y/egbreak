using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ToyEconomy.Editor {
 [InitializeOnLoad] public static class IdleProgressionHook {
  static IdleProgressionHook(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ToyEconomy.IdleVerify",false)){SessionState.SetBool("ToyEconomy.IdleVerify",false);new GameObject("Idle Progression Verification").AddComponent<IdleProgressionVerification>();}};}
 }
 public static class IdleProgressionBatch {
  static double started;static bool launched;
  public static void Run(){try{File.Delete("/tmp/idle-progression-verification.json");EditorSceneManager.OpenScene(ToyEconomySetup.ScenePath);started=EditorApplication.timeSinceStartup;EditorApplication.update+=Monitor;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  static void Monitor(){if(!launched && EditorApplication.timeSinceStartup-started>3){launched=true;SessionState.SetBool("ToyEconomy.IdleVerify",true);EditorApplication.isPlaying=true;}if(EditorApplication.isPlaying)EditorApplication.QueuePlayerLoopUpdate();if(launched && File.Exists("/tmp/idle-progression-verification.json")){string json=File.ReadAllText("/tmp/idle-progression-verification.json");EditorApplication.Exit(json.Contains("\"passed\": true")?0:1);}if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Idle verification timeout");EditorApplication.Exit(2);}}
 }
}
