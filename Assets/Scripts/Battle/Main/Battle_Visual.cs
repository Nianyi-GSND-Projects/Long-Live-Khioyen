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
        public Material playerFactionMaterial;
        public Material enemyFactionMaterial;
        
        [Header("UI Colors")]
        public Color playerUIColor = new Color(0.6f, 0.8f, 1f);
        public Color enemyUIColor = new Color(1f, 0.6f, 0.6f);
        public Color neutralUIColor = Color.white;
        
        [Header("Tile Visual Colors - Ring")]
        public Color deployRingColor = new Color(0, 0, 0.8f, 1f); // 深蓝环
        public Color extractRingColor = new Color(0.4f, 0.6f, 1f, 1f); // 淡蓝环
        public Color moveRingColor = new Color(0, 0.8f, 0, 1f); // 深绿环
    
        [Header("Tile Visual Colors - Overlay")]
        public Color targetEnemyColor = new Color(0.8f, 0, 0, 0.6f); // 深红实心 (攻击目标)
        public Color targetFriendColor = new Color(0.4f, 0.6f, 1f, 0.6f); // 淡蓝实心 (增益目标)
        public Color targetNeutralColor = new Color(0.4f, 1f, 0.4f, 0.6f); // 淡绿实心 (建造/空地)
    
        [Header("Tile Visual Colors - ZOC")]
        public Color zocPlayerColor = new Color(0, 0, 1f, 1f);
        public Color zocEnemyColor = new Color(1f, 0, 0, 1f);
        public float zocMaxAlpha = 0.6f;
        #endregion

        #region Tiles

        public void HighlightTiles(HashSet<Vector2Int> positions, Color color)
        {
            if (positions == null) return;
            foreach (Vector2Int pos in positions)
            {
                if (hexTiles.TryGetValue(pos, out HexTile tile))
                {
                    tile.SetRingColor(color);
                }
            }
        }
        
        public void HighlightTargets(HashSet<Vector2Int> positions, Color color)
        {
            if (positions == null) return;
            foreach (Vector2Int pos in positions)
            {
                if (hexTiles.TryGetValue(pos, out HexTile tile))
                {
                    tile.SetOverlayColor(color);
                }
            }
        }
        
        public void ClearAllHexHighlights()
        {
            foreach (var kvp in hexTiles)
            {
                HexTile tile = kvp.Value;
                tile.SetRingColor(Color.clear);
            
                UpdateTileZOCVisual(kvp.Key); 
            }
        }
        
        public void UpdateTileZOCVisual(Vector2Int pos)
        {
            if (!hexTiles.TryGetValue(pos, out HexTile tileScript)) return;
        
            // 如果当前处于 Target 选择阶段，不要覆盖 Target 高亮
            // 这需要检查 CurrentActionStage
            if (CurrentActionStage == PlayerActionStage.SelectingTarget && availableTargetPositions.Contains(pos))
            {
                return; // 保持 Target 高亮
            }

            TileData data = mapData[pos.x, pos.y];
            int balance = data.PlayerZOC - data.EnemyZOC;
        
            Color c = Color.clear;
            float intensity = Mathf.Min(Mathf.Abs(balance) * 0.2f, zocMaxAlpha);

            if (balance > 0) c = zocPlayerColor;
            else if (balance < 0) c = zocEnemyColor;
        
            c.a = intensity;
            tileScript.SetOverlayColor(c);
        
        }



        #endregion

        #region Units
        public void ChangeUnitVisibility(Unit unit, bool isVisible)
        {
            if (unit == null) return;

            if (unit.IsVisible == isVisible) return;
            
            bool wasVisible = unit.IsVisible;
            if (wasVisible) UpdateZOC(unit, false);

            unit.IsVisible = isVisible;
            
            if (isVisible) UpdateZOC(unit, true);
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

        
        public UnitVisualController SetupUnitVisuals(Unit unit)
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
            if(visuals != null)
            visuals.Initialize(unit);
            Transform containerTrans = go.transform.Find("ModelContainer");
            if (containerTrans == null)
            {
                containerTrans = new GameObject("ModelContainer").transform;
                containerTrans.SetParent(go.transform, false);
            }
            visuals.modelContainer = containerTrans;

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
    
            return visuals;
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
