using UnityEngine;
using UnityEngine.EventSystems;
namespace ToyEconomy {
 // A single pointer owns a gesture. Lost focus, overlays and pointer exit cancel it safely.
 public sealed class EggTapTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
  IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerExitHandler {
  public GameLoopController Game;
  int owner=int.MinValue;Vector2 previous;bool dragged;
  bool Ready=>Game!=null && Game.Cracker!=null && Game.Cracker.IsCracking && !Game.Hud.CollectionVisible;
  public void OnPointerDown(PointerEventData data){
   if(!Ready || data.button!=PointerEventData.InputButton.Left || owner!=int.MinValue)return;
   owner=data.pointerId;previous=data.position;dragged=false;Game.Cracker.BeginGesture(data.position);
  }
  public void OnPointerUp(PointerEventData data){if(data.pointerId!=owner)return;if(Ready)Game.Cracker.EndGesture(data.position);Reset();}
  public void OnPointerClick(PointerEventData data){if(Ready && !dragged && data.button==PointerEventData.InputButton.Left)Game.Cracker.HitAtScreen(data.position);}
  public void OnBeginDrag(PointerEventData data){if(data.pointerId==owner)dragged=true;}
  public void OnDrag(PointerEventData data){if(data.pointerId!=owner || !Ready)return;Game.Cracker.RubStroke(previous,data.position,Time.unscaledDeltaTime);previous=data.position;}
  public void OnEndDrag(PointerEventData data){if(data.pointerId==owner)Cancel();}
  public void OnPointerExit(PointerEventData data){if(data.pointerId==owner)Cancel();}
  void Reset(){owner=int.MinValue;}
  public void Cancel(){if(Game!=null && Game.Cracker!=null)Game.Cracker.CancelGesture();Reset();dragged=false;}
  void OnDisable(){Cancel();}
  void OnApplicationFocus(bool focus){if(!focus)Cancel();}
  void OnApplicationPause(bool pause){if(pause)Cancel();}
 }
}
