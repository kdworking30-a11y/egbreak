using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ToyEconomy.Editor {
public static class ImageAIBatch {
 static double started;static bool testing;static bool launched;
 public static void Run(){
 try{ImageAIIntegration.Upgrade();started=EditorApplication.timeSinceStartup;EditorApplication.update+=Monitor;}
 catch(Exception ex){File.WriteAllText("/tmp/imageai-fracture-batch-failure.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
 }
 static void Monitor(){
 if(!launched && EditorApplication.timeSinceStartup-started>3){launched=true;SessionState.SetBool("ToyEconomy.Verify",true);EditorApplication.isPlaying=true;}
 if(EditorApplication.isPlaying){testing=true;EditorApplication.QueuePlayerLoopUpdate();}
 if(testing && File.Exists("/tmp/imageai-fracture-verification.json")){string result=File.ReadAllText("/tmp/imageai-fracture-verification.json");EditorApplication.Exit(result.Contains("\"passed\": true")?0:1);}
 if(EditorApplication.timeSinceStartup-started>240){File.WriteAllText("/tmp/imageai-fracture-batch-failure.txt","Timed out waiting for Play Mode verification");EditorApplication.Exit(2);}
 }
}
}
