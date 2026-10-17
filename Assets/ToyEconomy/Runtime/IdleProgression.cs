using System;
using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
 public enum ToolKind { Hammer, Gloves, WarmCloth }
 public sealed class IdleProgression : MonoBehaviour {
  public const int MaxToolLevel=5;
  public static readonly string[] ToolNames={"Soft Hammer","Cocoa Gloves","Warm Cloth"};
  static readonly int[] Costs={20,45,90,160,260};
  readonly int[] levels=new int[3];GameLoopController game;
#if UNITY_EDITOR
  public bool VerificationBypass;
#endif
  public event Action Changed;
  public void Initialize(GameLoopController owner){game=owner;Apply();}
  public int Level(ToolKind kind)=>levels[(int)kind];
  public int RequiredEggs(ToolKind kind)=>kind==ToolKind.Gloves?3:kind==ToolKind.WarmCloth?6:0;
  public int EggGate(EggDefinition egg)=>egg.eggId=="chocolate"?3:egg.eggId=="ice"?6:egg.eggId=="crystal"?10:0;
  public bool IsUnlocked(EggDefinition egg){
#if UNITY_EDITOR
   if(VerificationBypass)return true;
#endif
   return game.EggsOpened>=EggGate(egg);
  }
  public double ToolPrice(ToolKind kind)=>levels[(int)kind]>=MaxToolLevel?0:Math.Ceiling(Costs[levels[(int)kind]]*(kind==ToolKind.Gloves?1.5:kind==ToolKind.WarmCloth?2:1));
  public bool CanUpgrade(ToolKind kind)=>game.State==GameLoopController.PlayState.Shop && !game.Cracker.IsCracking && game.EggsOpened>=RequiredEggs(kind) && Level(kind)<MaxToolLevel && game.Economy.Coins>=ToolPrice(kind);
  public bool BuyUpgrade(ToolKind kind){
   if(!CanUpgrade(kind) || !game.Economy.TrySpend(ToolPrice(kind)))return false;
   levels[(int)kind]++;Apply();Changed?.Invoke();game.Persistence?.Save();return true;
  }
  public double GardenMultiplier=>1+Math.Min(10,game.EggsOpened/3)*.1;
  public int NextGate=>game.EggsOpened<3?3:game.EggsOpened<6?6:game.EggsOpened<10?10:((game.EggsOpened/3)+1)*3;
  public string GoalText=>game.EggsOpened>=30?"Garden fully grown! Collect more friends":game.EggsOpened<3?$"Chocolate: {game.EggsOpened}/3 eggs":game.EggsOpened<6?$"Ice: {game.EggsOpened}/6 eggs":game.EggsOpened<10?$"Crystal: {game.EggsOpened}/10 eggs":$"Garden boost: {game.EggsOpened}/{NextGate} eggs";
  public string Effect(ToolKind kind,int level)=>kind==ToolKind.Hammer?$"Tap power x{1+level*.25f:0.##}":kind==ToolKind.Gloves?$"Charge speed x{1+level*.2f:0.##}":$"Melting speed x{1+level*.25f:0.##}";
  public void OnEggOpened(){Apply();Changed?.Invoke();}
  public int[] CopyLevels()=>(int[])levels.Clone();
  public void RestoreLevels(int[] restored){Array.Copy(restored,levels,3);Apply();Changed?.Invoke();}
  void Apply(){if(game==null)return;game.Garden.SetMultiplier(GardenMultiplier);game.Cracker.TapPower=1+levels[0]*.25f;game.Cracker.ChargeSpeed=1+levels[1]*.2f;game.Cracker.MeltPower=1+levels[2]*.25f;}
 }
}
