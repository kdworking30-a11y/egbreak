using UnityEngine;
namespace ToyEconomy {
public sealed class EggShop : MonoBehaviour {
 public EconomyManager Economy {get;set;}
 public EggCrackController Cracker {get;set;}
 public IdleProgression Progression {get;set;}
 public bool RewardPending {get;set;}
 public bool CanBuy(EggDefinition egg)=>egg!=null && egg.IsValid && (Progression==null || Progression.IsUnlocked(egg)) && !RewardPending && !Cracker.IsCracking && Economy.Coins>=egg.price;
 public bool Buy(EggDefinition egg){if(!CanBuy(egg) || !Economy.TrySpend(egg.price))return false;Cracker.Begin(egg);return true;}
}
}
