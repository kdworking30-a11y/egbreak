using UnityEditor;
using UnityEngine;
namespace ToyEconomy.Editor {
 [InitializeOnLoad] public static class OpticalVerificationHook {
  static OpticalVerificationHook(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ToyEconomy.OpticalVerify",false)){SessionState.SetBool("ToyEconomy.OpticalVerify",false);new GameObject("Optical Render Verification").AddComponent<OpticalMaterialVerification>();}};}
 }
 public static class OpticalBatch {
  static double started;static bool launched;static bool regression;
  public static void RunRegression(){regression=true;System.IO.File.Delete("/tmp/material-gameplay-verification.json");Run();}
  public static void Run(){try{OpticalMaterialSetup.Upgrade();started=EditorApplication.timeSinceStartup;EditorApplication.update+=Monitor;}catch(System.Exception e){System.IO.File.WriteAllText("/tmp/optical-material-failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
  static void Monitor(){if(!launched && EditorApplication.timeSinceStartup-started>3){launched=true;SessionState.SetBool(regression?"ToyEconomy.MaterialVerify":"ToyEconomy.OpticalVerify",true);EditorApplication.isPlaying=true;}
   if(EditorApplication.isPlaying)EditorApplication.QueuePlayerLoopUpdate();
   string report=regression?"/tmp/material-gameplay-verification.json":"/tmp/optical-material-verification.json";
   if(launched && System.IO.File.Exists(report)){var data=System.IO.File.ReadAllText(report);EditorApplication.Exit(data.Contains("\"passed\": true")?0:1);}
   if(EditorApplication.timeSinceStartup-started>150){System.IO.File.WriteAllText("/tmp/optical-material-failure.txt","Render verification timeout");EditorApplication.Exit(2);}
  }
 }
}
