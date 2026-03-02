using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace LongLiveKhioyen
{
    public class BattleInputController : MonoBehaviour
    {
        [Header("References")]
        public BattleCameraController cameraController;

        private Battle Battle => Battle.Instance;
        
        // 状态
        private Vector2 _pointerPos;
        private bool _isPrimaryDown;
        private float _primaryDownTime;
        private Vector2 _primaryDownPos;
        private bool _isValidClick;
        
        private bool _isPointerOverUI;
        
        public bool inputBlocked = false;
        public bool cameraLocked = false;
        
        private float _lastClickTime;
        private const float CLICK_COOLDOWN = 0.2f;
        // --- Input System Callbacks (通过 SendMessage 或 Unity Events 绑定) ---
        private void Update()
        {
            // [新增] 每帧更新 UI 状态
            _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
        public void OnPoint(InputValue value)
        {
            _pointerPos = value.Get<Vector2>();
            if (cameraController) cameraController.UpdatePointerPos(_pointerPos);
        }

        public void OnPrimaryClick(InputValue value)
        {
            bool isPressed = value.isPressed;
            
            if (inputBlocked) return;
            
            if (isPressed)
            {
                if (_isPointerOverUI)
                {
                    _isValidClick = false;
                    return;
                }
                // 按下
                _isPrimaryDown = true;
                _primaryDownTime = Time.realtimeSinceStartup;
                _primaryDownPos = _pointerPos;
                _isValidClick = true;
                
                if (cameraController) cameraController.StartDrag();
            }
            else
            {
                // 抬起
                _isPrimaryDown = false;
                if (cameraController) cameraController.EndDrag();

                if (_isValidClick)
                {
                    float timeDelta = Time.realtimeSinceStartup - _primaryDownTime;
                    float distDelta = Vector2.Distance(_pointerPos, _primaryDownPos);

                    // 判定为点击 (非拖拽)
                    if (timeDelta < 0.3f && distDelta < 10f)
                    {
                        HandleClick(_pointerPos);
                    }
                }
            }
        }

        public void OnDrag(InputValue value)
        {
            if (inputBlocked|| cameraLocked) return;
            if (_isPrimaryDown)
            {
                Vector2 delta = value.Get<Vector2>();
                if (cameraController) cameraController.ProcessDrag(delta);
            }
        }

        public void OnScroll(InputValue value)
        {
            if (inputBlocked|| cameraLocked) return;
            if (_isPointerOverUI) return;
            if (cameraController) cameraController.ProcessZoom(value.Get<float>());
        }

        public void OnSecondaryClick(InputValue value)
        {
            if (value.isPressed)
            {
                // 右键直接触发取消/回退
                Battle.HandleCancelInput();
            }
        }

        // --- 逻辑分发 ---
        private void HandleClick(Vector2 screenPos)
        {
            if (Time.time - _lastClickTime < CLICK_COOLDOWN) return;
            _lastClickTime = Time.time;
            if (Battle.ScreenToGround(screenPos, out Vector3 groundPos))
            {
                Vector2Int gridPos = Battle.WorldToMapInt(groundPos);
                Battle.HandleGridInput(gridPos);
            }
        }
    }
}