using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {

        #region Config

        [Header("Global Visual Config")]
        public GameObject globalFlagPrefab;
        public GameObject HextilePrefab;
        public GameObject extractionPointPrefab;
        public Material playerFactionMaterial;
        public Material enemyFactionMaterial;
        
        public Color movementHighlightColor = Color.green; 
        public Color arrangementHighlightColor = Color.blue;
        public Color attackHighlightColor = Color.red;
        
        [Header("UI Colors")]
        public Color playerUIColor = new Color(0.6f, 0.8f, 1f);
        public Color enemyUIColor = new Color(1f, 0.6f, 0.6f);
        public Color neutralUIColor = Color.white;
        #endregion

        #region Tiles

        public void HighlightTiles(HashSet<Vector2Int> positionsToHighlight, Color highloghtColor)
        {
            if (positionsToHighlight == null) return;

            foreach (Vector2Int position in positionsToHighlight)
            {
                if (hexTiles.TryGetValue(position, out HexTile tile))
                {
                    tile.Highlight(highloghtColor);
                }
            }
        }
        
        public void ClearAllHexHighlights()
        {
            foreach (HexTile tile in hexTiles.Values)
            {
                tile.UnHighlight();
            }
        }



        #endregion

        #region Units
        public void ChangeUnitVisibility(Unit unit, bool isVisible)
        {
            if (unit == null) return;

            if (unit.IsVisible == isVisible) return;

            unit.IsVisible = isVisible;

            if (isVisible)
            {
                if (!factionVisibleUnits[unit.faction].Contains(unit))
                {
                    factionVisibleUnits[unit.faction].Add(unit);
                }
                Debug.Log($"{unit.name} is now VISIBLE.");
            }
            else
            {
                if (factionVisibleUnits[unit.faction].Contains(unit))
                {
                    factionVisibleUnits[unit.faction].Remove(unit);
                }
                Debug.Log($"{unit.name} is now HIDDEN.");
            }
            unit.OnUnitStateChanged();
        }

        
        public void SetupUnitVisuals(Unit unit)
        {
            GameObject go = unit.gameObject;
            UnitVisualController visuals = null;

            // 1. 根据类型挂载不同的控制器
            if (unit is Battalion)
            {
                visuals = go.AddComponent<BattalionVisuals>();
            }
            else if (unit is Facility)
            {
                visuals = go.AddComponent<FacilityVisuals>();
            }

            if (visuals == null) return;

            // 2. 创建模型容器子物体
            // 检查是否已经有了，防止重复创建
            Transform containerTrans = go.transform.Find("ModelContainer");
            if (containerTrans == null)
            {
                containerTrans = new GameObject("ModelContainer").transform;
                containerTrans.SetParent(go.transform, false);
            }
            visuals.modelContainer = containerTrans;

            // 3. 加载并生成 UI
            // 这里的路径可以提取为常量，或者从 Battle 配置里读
            var uiPrefab = Resources.Load<GameObject>("Prefabs/Battle/UI/PF_UnitUI");
            if (uiPrefab)
            {
				
                var existingUI = go.GetComponentInChildren<UnitOverheadUI>();
                if (existingUI == null)
                {
                    var uiObj = Instantiate(uiPrefab);
                    var uiScript = uiObj.GetComponent<UnitOverheadUI>();
                    if (uiScript != null)
                    {
                        uiScript.Initialize(unit);
                        if (visuals != null) visuals.overheadUI = uiScript;
                    }
                }
                else
                {
                    visuals.overheadUI = existingUI;
                }
            }
    
            // 4. (可选) 如果你希望在这里就 Initialize，也可以
            // 但通常依靠 Unit.Start() 来调用 Initialize 更符合生命周期
        }



        #endregion

        #region Faction
        
        public Color GetFactionUIColor(Faction faction)
        {
            switch (faction)
            {
                case Faction.Player: return playerUIColor;
                case Faction.Enemy: return enemyUIColor;
                default: return neutralUIColor;
            }
        }
        
        public Material GetFactionFlagMaterial(Faction faction)
        {
            switch (faction)
            {
                case Faction.Player: return playerFactionMaterial;
                case Faction.Enemy: return enemyFactionMaterial;
                default: return null;
            }
        }
        #endregion
        #region Camera
        public Transform anchor;		
        [SerializeField] new CinemachineVirtualCamera camera;

        public Vector3 AnchorPosition
        {
            get => anchor.position;
            set => anchor.position = value;
        }
        public Vector3 AnchorEulers
        {
            get => anchor.eulerAngles;
            set => anchor.eulerAngles = value;
        }
        
        public void SetCameraLocked(bool locked)
        {
            if (inputController == null) inputController = GetComponent<BattleInputController>();
            if (inputController == null) inputController = FindObjectOfType<BattleInputController>();

            if (inputController != null)
            {
                inputController.cameraLocked = locked;
            }
        }
        
        public float CameraDistance
        {
            get => -camera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset.z;
            set
            {
                var composer = camera.GetCinemachineComponent<CinemachineTransposer>();
                var offset = composer.m_FollowOffset;
                offset.z = -value;
                composer.m_FollowOffset = offset;
            }
        }
		
        public bool RayToGround(Ray ray, out Vector3 ground)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if(!plane.Raycast(ray, out float t))
            {
                ground = default;
                return false;
            }
            ground = ray.GetPoint(t);
            return true;
        }

        public bool ScreenToGround(Vector3 screen, out Vector3 ground)
        {
            var ray = Camera.main.ScreenPointToRay(screen);
            return RayToGround(ray, out ground);
        }

        public void FocusCamera(Vector3 targetPos, float duration = 1.0f)
        {
            // 假设 Anchor 是摄像机的父物体或控制点
            // 如果是瞬间移动
            if (duration <= 0)
            {
                AnchorPosition = targetPos;
            }
            else
            {
                StartCoroutine(SmoothFocus(targetPos, duration));
            }
        }
        
        private IEnumerator SmoothFocus(Vector3 targetPos, float duration)
        {
            Vector3 startPos = AnchorPosition;
            float t = 0;
          
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                AnchorPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            AnchorPosition = targetPos;
        }

        #endregion

    }
}
