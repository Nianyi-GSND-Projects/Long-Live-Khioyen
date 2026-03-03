using UnityEngine;
using UnityEngine.InputSystem;

namespace LongLiveKhioyen
{
    public class BattleCameraController : MonoBehaviour
    {
        [Header("Settings")]
        public float panSpeed = 1f;
        public float zoomSpeed = 0.01f; // 调整缩放速度，因为 Scroll 值通常很大
        public Vector2 zoomRange = new(2, 100);
        public float rotateSpeed = 1f;
        public float minAzimuth = 10f;

        // 状态
        private Vector2 _pointerScreenPos;
        private bool _isDragging;
        private Vector2 _dragStartPos;

        // 引用
        private Battle Battle => Battle.Instance;

        // 供 InputController 调用
        public void UpdatePointerPos(Vector2 screenPos)
        {
            _pointerScreenPos = screenPos;
        }

        public void StartDrag()
        {
            _isDragging = true;
            _dragStartPos = _pointerScreenPos;
        }

        public void EndDrag()
        {
            _isDragging = false;
        }

        public void ProcessDrag(Vector2 delta)
        {
            if (_isDragging) Pan(delta);
        }

        public void ProcessZoom(float scrollY)
        {
            Zoom(scrollY);
        }

        void Pan(Vector2 screenDelta)
        {
            if (Battle == null) return;

            Vector2 prevScreenPos = _pointerScreenPos - screenDelta;

            if(!Battle.ScreenToGround(prevScreenPos, out var to))
               return;
            
            if(!Battle.ScreenToGround(_pointerScreenPos, out var from))
               return;
            
            Vector3 pos = Battle.AnchorPosition + (to - from) * panSpeed;
            
            Vector3 totalSize = new Vector3(Battle.Size.x * Battle.Xscale, 0, Battle.Size.y * Battle.Yscale);
            Bounds bounds = new(totalSize*0.5f, totalSize);
            
            Vector3 localPos = Battle.transform.worldToLocalMatrix.MultiplyPoint(pos);
            Vector3 boundedLocalPos = bounds.ClosestPoint(localPos);
            Vector3 boundedWorldPos = Battle.transform.localToWorldMatrix.MultiplyPoint(boundedLocalPos);

            pos.x = boundedWorldPos.x;
            pos.z = boundedWorldPos.z;
            
            Battle.AnchorPosition = pos;
        }

        void Zoom(float scrollY)
        {
            if (Battle == null) return;
            float delta = Mathf.Clamp(scrollY, -10f, 10f) * zoomSpeed;
            float z = Battle.CameraDistance;
            z *= Mathf.Exp(-delta);
            z = Mathf.Clamp(z, zoomRange.x, zoomRange.y);
            Battle.CameraDistance = z;
        }

        void Rotate(Vector2 screenDelta)
        {
            if (Battle == null) return;

            screenDelta.y *= -1;
            screenDelta *= rotateSpeed;

            var euler = Battle.AnchorEulers;
            euler.x = Mathf.Clamp(euler.x + screenDelta.y, minAzimuth, 90);
            euler.y += screenDelta.x;

            Battle.AnchorEulers = euler;
        }
    }
}