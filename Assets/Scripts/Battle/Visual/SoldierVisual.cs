using UnityEngine;

namespace LongLiveKhioyen
{
    public class SoldierVisual : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("负责显示图像的渲染器")]
        public SpriteRenderer spriteRenderer;

        [Header("State Sprites")]
        public Sprite idleSprite;
        public Sprite moveSprite;
        public Sprite prepareSprite;
        public Sprite attackSprite;
        public Sprite hitSprite;
        public Sprite castSprite;

        private SoldierState _currentState = SoldierState.Idle;

        private void Awake()
        {
            // 如果没有手动分配，尝试自动获取子物体上的 SpriteRenderer
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 供外界调用的核心接口：切换士兵状态
        /// </summary>
        public void SetState(SoldierState newState)
        {
            // 如果状态没有改变，直接跳过以节省性能
            if (_currentState == newState && spriteRenderer.sprite != null) return;

            _currentState = newState;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            // 根据当前状态赋予对应的差分贴图
            switch (_currentState)
            {
                case SoldierState.Idle:
                    if (idleSprite != null) spriteRenderer.sprite = idleSprite;
                    break;
                case SoldierState.Move:
                    if (moveSprite != null) spriteRenderer.sprite = moveSprite;
                    break;
                case SoldierState.Prepare:
                    if (prepareSprite != null) spriteRenderer.sprite = prepareSprite;
                    break;
                case SoldierState.Attack:
                    if (attackSprite != null) spriteRenderer.sprite = attackSprite;
                    break;
                case SoldierState.Hit:
                    if (hitSprite != null) spriteRenderer.sprite = hitSprite;
                    break;
                case SoldierState.Cast: 
                    if (castSprite != null) spriteRenderer.sprite = castSprite; 
                    break;
            }
        }

        /// <summary>
        /// 供 BattalionVisuals 在初始化时设置基础材质和朝向
        /// </summary>
        public void SetupInitialVisuals(Material mat, bool flipX)
        {
            if (spriteRenderer != null)
            {
                if (mat != null) spriteRenderer.material = mat;
                spriteRenderer.flipX = flipX;
            }
        }
    }
}