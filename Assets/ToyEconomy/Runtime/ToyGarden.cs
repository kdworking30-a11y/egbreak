using System;
using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
public sealed class ToyGarden : MonoBehaviour {
 readonly List<ToyDefinition> toys=new List<ToyDefinition>();
 public IReadOnlyList<ToyDefinition> Toys=>toys;
 double baseIncome;double multiplier=1;
 public double IncomePerSecond=>baseIncome*multiplier;
 public void SetMultiplier(double value){multiplier=Math.Max(1,value);Changed?.Invoke();}
 public void Clear(){toys.Clear();baseIncome=0;Changed?.Invoke();}
 public EconomyManager Economy {get;set;}
 public event Action Changed;
 public void Add(ToyDefinition toy){if(toy==null || !toy.IsValid)throw new ArgumentException("Invalid toy");toys.Add(toy);baseIncome+=toy.coinPerSecond;Changed?.Invoke();}
 void Update(){if(Economy!=null)Economy.Earn(IncomePerSecond*Time.deltaTime);}
}
}
