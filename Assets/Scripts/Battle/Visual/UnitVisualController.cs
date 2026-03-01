using UnityEngine;

namespace LongLiveKhioyen
{
    public abstract class UnitVisualController : MonoBehaviour
    {
        [Header("Base Components")]
        public UnitOverheadUI overheadUI;
        public Transform modelContainer; // 模型容器

        protected Unit _ownerUnit;
        protected GameObject _activeFlag; // 持有旗帜的引用

        public virtual void Initialize(Unit unit)
        {
            _ownerUnit = unit;
            
            if (overheadUI != null) overheadUI.UpdateInfo(unit);
            
            CreateOrUpdateFlag();
            
            RefreshModel();
        }

        public virtual void RefreshVisuals()
        {
            if (_ownerUnit == null) return;
            
            if (overheadUI != null) overheadUI.UpdateInfo(_ownerUnit);
            
            CreateOrUpdateFlag();
            
            RefreshModel();
        }
        
        public void SetVisualState(bool selected, bool actionDone)
        {
            
            Color targetColor = Color.white;
            if (selected) targetColor = Color.green;
            else if (actionDone) targetColor = Color.gray;
            
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material.color = targetColor; 
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