using UnityEngine;

namespace LongLiveKhioyen
{
    public class FogTile : MonoBehaviour
    {
        [Header("Visual References")]
        [Tooltip("负责显示黑云的 SpriteRenderer")]
        public SpriteRenderer cloudRenderer;
        
        [Tooltip("负责显示半透明六边形的 SpriteRenderer")]
        public SpriteRenderer overlayRenderer;
        
        [Tooltip("黑云的 Transform (用于缩放动画)")]
        public Transform cloudTransform;
    }
}