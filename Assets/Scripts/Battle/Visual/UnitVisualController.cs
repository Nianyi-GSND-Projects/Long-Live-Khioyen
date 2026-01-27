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
            // 这里实现具体的高亮逻辑
            // 方案 A: 替换材质颜色
            // 方案 B: 开启/关闭某个高亮光圈物体
            // 方案 C: 使用 Shader 变色
            
            // 示例：简单粗暴地改颜色
            Color targetColor = Color.white;
            if (selected) targetColor = Color.green;
            else if (actionDone) targetColor = Color.gray;
            
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                // 注意：不要覆盖了阵营颜色，建议用 Emission 或专门的高亮材质
                r.material.color = targetColor; 
            }
        }
        
        protected abstract void RefreshModel();

        protected void CreateOrUpdateFlag()
        {
            if (Battle.Instance == null || Battle.Instance.globalFlagPrefab == null) return;
            
            if (_activeFlag == null)
            {
                _activeFlag = Instantiate(Battle.Instance.globalFlagPrefab, modelContainer);
                _activeFlag.transform.localPosition = new Vector3(0f,0.0f,.5f);
                _activeFlag.transform.localRotation = Quaternion.identity;
            }
            
            Material factionMat = Battle.Instance.GetFactionMaterial(_ownerUnit.faction);
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