using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public abstract class UnitVisualController : MonoBehaviour
    {
        [Header("Base Components")]
        public UnitOverheadUI overheadUI;
        public Transform modelContainer; // 模型容器

        protected Unit _ownerUnit;
        protected GameObject _activeFlag; // 持有旗帜的引用
        private CanvasGroup _canvasGroup;
        
        private readonly List<Renderer> _cachedRenderers = new List<Renderer>();
        private bool _renderersCached = false;
        
        private MaterialPropertyBlock _propBlock;
        
        protected virtual void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }
        
        public virtual void Initialize(Unit unit)
        {
            _ownerUnit = unit;
            
            if (overheadUI != null) overheadUI.UpdateInfo(unit);
           // CreateOrUpdateFlag();
        }

        public virtual void RefreshVisuals()
        {
            if (_ownerUnit == null) return;
         //   Debug.Log($"Visibility:{_ownerUnit.IsVisible}");
            bool isPlayerSide = _ownerUnit.faction == Faction.Player || _ownerUnit.faction == Faction.Friend;
            
            if (!_ownerUnit.IsVisible)
            {
                if (isPlayerSide)
                {
                    gameObject.SetActive(true);
                    SetOverallVisibility(true, BattleParam.Instance.invisibleAllyAlpha);
                    if (overheadUI != null) overheadUI.SetAlpha(BattleParam.Instance.invisibleAllyAlpha);
                }
                else
                {
                    gameObject.SetActive(false);
                    if (overheadUI != null) overheadUI.SetAlpha(0f);
                    return;
                }
            }
            else
            {
                gameObject.SetActive(true);
                SetOverallVisibility(true, 1.0f);
                if (overheadUI != null) overheadUI.SetAlpha(1.0f);
            }
            
            if (overheadUI != null) overheadUI.UpdateInfo(_ownerUnit);
            
            //CreateOrUpdateFlag();
            RefreshModel();
        }
        
        public void SetVisualState(bool selected, bool actionDone)
        {
            Color tintColor = Color.white;
            if (selected) tintColor = Color.green;
            else if (actionDone) tintColor = Color.gray;
            SetTintColor(tintColor);
        }
        
        private void SetTintColor(Color color)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                // 保留当前的 alpha，只修改 RGB
                Color current = r.material.color;
                current.r = color.r;
                current.g = color.g;
                current.b = color.b;
                r.material.color = current;
            }
            
        }

        
        protected void CacheRenderers()
        {
            _cachedRenderers.Clear();
            GetComponentsInChildren(true, _cachedRenderers);
            _renderersCached = true;
        }
        
        private void SetOverallVisibility(bool visible, float alpha)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.enabled = visible;
                
                // 保留当前的 RGB，只修改 alpha
                Color current = r.material.color;
                current.a = alpha;
               
                r.material.color = current;
            }
        }
        
        protected abstract void RefreshModel();

        protected void CreateOrUpdateFlag()
        {
            
            if (Battle.Instance == null || Battle.Instance.globalFlagPrefab == null) return;
            
            if (_ownerUnit.faction != Faction.Player && _ownerUnit.faction != Faction.Enemy)
            {
                if (_activeFlag != null)
                {
                    Destroy(_activeFlag);
                    _activeFlag = null;
                }
                return;
            }
            
            if (_activeFlag == null)
            {
                _activeFlag = Instantiate(Battle.Instance.globalFlagPrefab, modelContainer);
                _activeFlag.transform.localPosition = new Vector3(0f,0.0f,.5f);
                _activeFlag.transform.localRotation = Quaternion.identity;
            }
            
            Material factionMat = Battle.Instance.GetFactionFlagMaterial(_ownerUnit.faction);
            var colorHandlers = _activeFlag.GetComponentsInChildren<IFactionColored>();
            foreach (var handler in colorHandlers)
            {
                handler.SetFactionMaterial(factionMat);
            }
            
            if (colorHandlers.Length == 0)
            {
                var renderers = _activeFlag.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {

                    if (factionMat != null)
                    {
                        r.material = factionMat; 
                    }
                }
            }
            
            
        }
    }
}