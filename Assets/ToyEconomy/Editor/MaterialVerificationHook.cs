using UnityEditor;
using UnityEngine;
namespace ToyEconomy.Editor {
 [InitializeOnLoad]
 public static class MaterialVerificationHook {
  static MaterialVerificationHook(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ToyEconomy.MaterialVerify",false)){SessionState.SetBool("ToyEconomy.MaterialVerify",false);new GameObject("Material Gameplay Verification").AddComponent<MaterialGameplayVerification>();}};}
 }
 public static class MaterialGameplayBatch {
  static double started;static bool launched,testing;
  public static void Run(){try{MaterialGameplaySetup.Upgrade();started=EditorApplication.timeSinceStartup;EditorApplication.update+=Monitor;}catch(System.Exception e){System.IO.File.WriteAllText("/tmp/material-gameplay-failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
  static void Monitor(){if(!launched && EditorApplication.timeSinceStartup-started>3){launched=true;SessionState.SetBool("ToyEconomy.MaterialVerify",true);EditorApplication.isPlaying=true;}
   if(EditorApplication.isPlaying){testing=true;EditorApplication.QueuePlayerLoopUpdate();}
   if(testing && System.IO.File.Exists("/tmp/material-gameplay-verification.json")){var data=System.IO.File.ReadAllText("/tmp/material-gameplay-verification.json");EditorApplication.Exit(data.Contains("\"passed\": true")?0:1);}
   if(EditorApplication.timeSinceStartup-started>180){System.IO.File.WriteAllText("/tmp/material-gameplay-failure.txt","Play Mode timeout");EditorApplication.Exit(2);}
  }
 }
}
