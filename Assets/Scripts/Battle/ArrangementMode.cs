using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace LongLiveKhioyen
{
    public class ArrangementMode : MonoBehaviour
    {
       Battle Battle =>Battle.Instance;
       
       #region Settings
       [Header("Settings")]
       public float panSpeed = 1;
       public float zoomSpeed = 1;
       public Vector2 zoomRange = new(2, 100);
       public float rotateSpeed = 1;
       public float minAzimuth = 10f;
       #endregion

       #region Input states
       Vector2 pointerScreenPosition;
       public Vector2 PointerScreenPosition => pointerScreenPosition;
       bool isPointerOverGameObjects;

       bool isPrimaryButtonDown, isSecondaryButtonDown;
       float lastPrimaryClickTime;
       Vector2 primaryStartScreenPosition;
       #endregion
       
       #region Life cycle
       void Update()
       {
           isPointerOverGameObjects = EventSystem.current?.IsPointerOverGameObject() ?? false;
       }
       #endregion
       
       #region Input handlers
       protected void OnPoint(InputValue value)
       {
           var raw = value.Get<Vector2>();
           pointerScreenPosition = raw;
       }

       protected void OnDrag(InputValue value)
       {
           var raw = value.Get<Vector2>();

           if(isPrimaryButtonDown)
               Pan(raw);
          // if(isSecondaryButtonDown)
            //   Rotate(raw);
       }

       protected void OnScroll(InputValue value)
       {
           var raw = value.Get<float>();
           Zoom(raw);
       }

       protected void OnPrimaryClick(InputValue value)
       {
           if(isPointerOverGameObjects)
               return;

           var raw = value.isPressed;
           isPrimaryButtonDown = raw;

           if(raw)
           {
               lastPrimaryClickTime = Time.realtimeSinceStartup;
               primaryStartScreenPosition = pointerScreenPosition;
           }
           else
           {
               float elapsedTime = Time.realtimeSinceStartup - lastPrimaryClickTime;
               Vector2 mouseMoved = primaryStartScreenPosition - pointerScreenPosition;
               if(
                   elapsedTime <= 0.3f &&
                   mouseMoved.magnitude <= 5 &&
                   !isPointerOverGameObjects
               )
                   Interact(pointerScreenPosition);
           }
       }

       protected void OnSecondaryClick(InputValue value)
       {
           if(isPointerOverGameObjects)
               return;
           var raw = value.isPressed;
           isSecondaryButtonDown = raw;
           
           if(raw)
           {
               lastPrimaryClickTime = Time.realtimeSinceStartup;
               primaryStartScreenPosition = pointerScreenPosition;
           }
           else
           {
               float elapsedTime = Time.realtimeSinceStartup - lastPrimaryClickTime;
               Vector2 mouseMoved = primaryStartScreenPosition - pointerScreenPosition;
               if(
                   elapsedTime <= 0.3f &&
                   mouseMoved.magnitude <= 5 &&
                   !isPointerOverGameObjects
               )
                   SecondaryInteract(pointerScreenPosition);
           }
       }
       #endregion
       
       #region Functions
       void Pan(Vector2 screenDelta)
       {
           if(!Battle.ScreenToGround(pointerScreenPosition - screenDelta, out var to))
               return;
           if(!Battle.ScreenToGround(pointerScreenPosition, out var from))
               return;
           Vector3 pos = Battle.AnchorPosition + (to - from) * panSpeed;
           Bounds bounds = new(default, new(Battle.Size.x * Battle.Xscale, 0, Battle.Size.y * Battle.Yscale));
           Vector3 boundedPos = Battle.transform.localToWorldMatrix.MultiplyPoint(
               bounds.ClosestPoint(
                   Battle.transform.worldToLocalMatrix.MultiplyPoint(pos)
               )
           );
           pos.x = boundedPos.x;
           pos.z = boundedPos.z;
           Battle.AnchorPosition = pos;
       }

       void Zoom(float scrollY)
       {
           float z = Battle.CameraDistance;
           z *= Mathf.Exp(-scrollY * zoomSpeed);
           z = Mathf.Clamp(z, zoomRange.x, zoomRange.y);
           Battle.CameraDistance = z;
       }

       void Rotate(Vector2 screenDelta)
       {
           screenDelta.y *= -1;
           screenDelta *= rotateSpeed;

           var euler = Battle.AnchorEulers;
           euler.x = Mathf.Clamp(euler.x + screenDelta.y, minAzimuth, 90);
           euler.y += screenDelta.x;

           Battle.AnchorEulers = euler;
       }

       protected void Interact(Vector2 screenPos)
       {
           var ray = Camera.main.ScreenPointToRay(screenPos);
           if(!Physics.Raycast(ray, out var hit, Mathf.Infinity))
               return;
           
           if (Battle.IsInArrangementStage)
           {
               if (Battle.IsReserveTeamSelected)
               {
                   Battle.arrangementModal.TryPlaceReserveTeam();
               }
               else if (Battle.IsBattalionSelected)
               {
                   Battle.arrangementModal.TryMoveBattalionArrangement();
               }
               else
               {
                   var hitBattalion = hit.collider.GetComponentInParent<Battalion>();
                   if(hitBattalion) Battle.SelectBattalion(hitBattalion);
               }
           }
           else if (Battle.IsInBattleStage)
           {
               if (Battle.CurrentActionStage == PlayerActionStage.None)
               {
                   {
                       Battle.ClearAllHexHighlights();
                       var hitBattalion = hit.collider.GetComponentInParent<Battalion>();
                       if(hitBattalion&&hitBattalion.actionDone==false) Battle.SelectBattalion(hitBattalion);
                   }
               }
               else if (Battle.CurrentActionStage==PlayerActionStage.MovingBattalion&&Battle.IsBattalionSelected)
               {
                   Battle.arrangementModal.TryMoveBattalionBattle();
               }
               else if (Battle.CurrentActionStage == PlayerActionStage.SelectingTarget && Battle.IsPreparingAction)
               {
                   Battle.arrangementModal.TryApplyCurrentAction();
               }
               
           }
       }
       
       protected void SecondaryInteract(Vector2 screenPos)
       {
           var ray = Camera.main.ScreenPointToRay(screenPos);
           if(!Physics.Raycast(ray, out var hit, Mathf.Infinity))
               return;
           
           if (Battle.IsInArrangementStage)
           {
               if (Battle.IsReserveTeamSelected)
               {
                   Battle.ClearReserveTeamSelection();
               }
               else if (Battle.IsBattalionSelected)
               {
                   Battle.ClearBattalionSelection();
               }
               
           }
           else if (Battle.IsInBattleStage)
           {
               if (Battle.IsBattalionSelected)
               {
                   if(Battle.CurrentActionStage!=PlayerActionStage.None) 
                       ReturnToPreviousActionStage();
               }
           }
       }

       protected void ReturnToPreviousActionStage()
       {
           if (!Battle.IsInBattleStage)
           {
               return;
           }

           switch (Battle.CurrentActionStage)
           {
               case PlayerActionStage.None:
                   return;
               
               case PlayerActionStage.MovingBattalion:
                   Battle.ClearBattalionSelection();
                   Battle.ChangeActionStage(PlayerActionStage.None);
                   break;
               
               case PlayerActionStage.SelectingAction:
                   Battle.CancelMovement();
                   Battle.ChangeActionStage(PlayerActionStage.MovingBattalion);
                   break;
               
               case PlayerActionStage.SelectingTarget:
                   Battle.CancelAction();
                   Battle.ChangeActionStage(PlayerActionStage.SelectingAction);
                   break;
               default:
                   break;
           }
           
       }
       #endregion
    }
}
