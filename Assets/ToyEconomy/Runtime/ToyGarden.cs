using System;
using System.Collections.Generic;
using UnityEngine;
namespace ToyEconomy {
public sealed class ToyGarden : MonoBehaviour {
 readonly List<ToyDefinition> toys=new List<ToyDefinition>();
 public IReadOnlyList<ToyDefinition> Toys=>toys;
 public double IncomePerSecond {get;private set;}
 public EconomyManager Economy {get;set;}
 public event Action Changed;
 public void Add(ToyDefinition toy){if(toy==null || !toy.IsValid)throw new ArgumentException("Invalid toy");toys.Add(toy);IncomePerSecond+=toy.coinPerSecond;Changed?.Invoke();}
 void Update(){if(Economy!=null)Economy.Earn(IncomePerSecond*Time.deltaTime);}
}
}
