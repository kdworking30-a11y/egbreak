using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace ToyEconomy {
 [Serializable] public sealed class IdleSaveData {
  public int version=1,eggsOpened;public double coins,pendingRefund;public long savedUtc;
  public int[] toolLevels;public List<OwnedToySave> toys=new List<OwnedToySave>();
 }
 [Serializable] public sealed class OwnedToySave {public string id;public int count;}
 public sealed class IdleSave : MonoBehaviour {
  GameLoopController game;float nextSave;long pausedUtc;public bool Enabled {get;set;}
  public string WelcomeMessage {get;private set;}="";
  #if UNITY_EDITOR
  public string VerificationSavePath;
#endif
  public string SavePath {
   get {
#if UNITY_EDITOR
    if(!string.IsNullOrEmpty(VerificationSavePath))return VerificationSavePath;
#endif
    return Path.Combine(Application.persistentDataPath,"egbreak-progress-v1.json");
   }
  }
  public void Initialize(GameLoopController owner){game=owner;Enabled=!Application.isBatchMode;nextSave=Time.unscaledTime+15;if(Enabled)Load();}
  public IdleSaveData Snapshot(long utc){
   var data=new IdleSaveData{coins=game.Economy.Coins,eggsOpened=game.EggsOpened,savedUtc=utc,toolLevels=game.Progression.CopyLevels(),pendingRefund=game.Cracker.IsCracking?game.Cracker.ActiveEgg.price:0};
   var counts=new Dictionary<string,int>();foreach(var toy in game.Garden.Toys){if(!counts.ContainsKey(toy.toyId))counts[toy.toyId]=0;counts[toy.toyId]++;}
   foreach(var pair in counts)data.toys.Add(new OwnedToySave{id=pair.Key,count=pair.Value});return data;
  }
  public bool Restore(IdleSaveData data,long now){
   if(data==null || data.version!=1 || !Finite(data.coins) || data.coins<0 || data.coins>1e12 || !Finite(data.pendingRefund) || data.pendingRefund<0 || data.pendingRefund>10000 || data.eggsOpened<0 || data.eggsOpened>1000000 || data.savedUtc<0 || data.savedUtc>253402300799 || data.toolLevels==null || data.toolLevels.Length!=3 || data.toys==null || data.toys.Count==0 || data.toys.Count>100)return false;
   foreach(int level in data.toolLevels)if(level<0 || level>IdleProgression.MaxToolLevel)return false;
   var catalog=game.ToyCatalog();var restored=new List<ToyDefinition>();var ids=new HashSet<string>();
   foreach(var item in data.toys){if(item==null || item.count<1 || item.count>10000 || !ids.Add(item.id))return false;var toy=catalog.Find(t=>t.toyId==item.id);if(toy==null || restored.Count+item.count>10000)return false;for(int i=0;i<item.count;i++)restored.Add(toy);}
   if(game.Cracker.IsCracking || game.State!=GameLoopController.PlayState.Shop)return false;
   game.RestoreWorld(restored,data.eggsOpened);game.Progression.RestoreLevels(data.toolLevels);
   double seconds=Math.Max(0,Math.Min(4*60*60,now-data.savedUtc));double offline=seconds*game.Garden.IncomePerSecond;
   game.Economy.Initialize(Math.Min(1e12,data.coins+data.pendingRefund+offline));
   WelcomeMessage=(offline>=1?$"Welcome back! Your toys earned {offline:0} coins.":"")+(data.pendingRefund>0?" Unfinished egg refunded.":"");return true;
  }
  static bool Finite(double value)=>!double.IsNaN(value) && !double.IsInfinity(value);
  public void Save(){if(!Enabled || game==null)return;try{
   var json=JsonUtility.ToJson(Snapshot(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),true);Directory.CreateDirectory(Path.GetDirectoryName(SavePath));File.WriteAllText(SavePath+".tmp",json);
   if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,SavePath+".bak");else File.Move(SavePath+".tmp",SavePath);
  }catch(Exception e){Debug.LogWarning("Progress could not be saved: "+e.Message);}}
  public bool Load(){foreach(var path in new[]{SavePath,SavePath+".bak"}){if(!File.Exists(path))continue;try{if(Restore(JsonUtility.FromJson<IdleSaveData>(File.ReadAllText(path)),DateTimeOffset.UtcNow.ToUnixTimeSeconds())){Save();return true;}}catch(Exception e){Debug.LogWarning("Cannot read progress: "+e.Message);}}return false;}
  void Update(){if(Enabled && Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+15;Save();}}
  void OnApplicationPause(bool paused){if(!Enabled || game==null)return;
   if(paused){pausedUtc=DateTimeOffset.UtcNow.ToUnixTimeSeconds();Save();}
   else if(pausedUtc>0){double seconds=Math.Max(0,Math.Min(14400,DateTimeOffset.UtcNow.ToUnixTimeSeconds()-pausedUtc));pausedUtc=0;double earned=seconds*game.Garden.IncomePerSecond;game.Economy.Earn(earned);if(earned>=1)game.SetNotice($"Welcome back! Your toys earned {earned:0} coins.");Save();}
  }
  void OnApplicationQuit(){Save();}
 }
}
