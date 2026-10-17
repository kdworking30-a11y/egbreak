using System;
using UnityEngine;
namespace ToyEconomy {
public sealed class EconomyManager : MonoBehaviour {
 public double Coins {get; private set;}
 public event Action Changed;
 public void Initialize(double startingCoins){Coins=Math.Max(0,startingCoins);Changed?.Invoke();}
 public void Earn(double amount){if(amount<=0 || double.IsNaN(amount) || double.IsInfinity(amount))return; Coins+=amount;Changed?.Invoke();}
 public bool TrySpend(double amount){if(amount<0 || double.IsNaN(amount) || double.IsInfinity(amount) || Coins<amount)return false;Coins-=amount;Changed?.Invoke();return true;}
}
}
