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
        
        [Header("Fog of War")]
        public FogOfWarController fogOfWarController;
        
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
            
            if (CurrentActionStage == PlayerActionStage.SelectingTarget && availableTargetPositions.Contains(pos))
            {
                return;
            }

            if (fogMap[pos.x, pos.y] != FogState.Visible) return;
            
            int visualPlayerZOC = 0;
            int visualEnemyZOC = 0;
            var neighborTiles = GetAllTilesInRange(pos, 1);

            void AddVisualZOC(Unit unit)
            {
                if (unit != null && IsUnitVisibleToPlayer(unit))
                {
                    if (unit.faction == Faction.Player || unit.faction == Faction.Friend)
                    {
                        visualPlayerZOC += (int)unit.GetStat(StatType.ZocPower);
                    }
                    else if (unit.faction == Faction.Enemy)
                    {
                        visualEnemyZOC += (int)unit.GetStat(StatType.ZocPower);
                    }
                }
            }

            var tileOnPos = mapData[pos.x, pos.y];
            AddVisualZOC(tileOnPos.Battalion);
            AddVisualZOC(tileOnPos.Facility);
            
            int parity = pos.y & 1;
            foreach (var offset in neighborOffsets[parity])
            {
                Vector2Int neighborPos = pos + offset;

                if (!IsValidMapPosition(neighborPos)) continue;

                var tileOnNeighbor = mapData[neighborPos.x, neighborPos.y];
                AddVisualZOC(tileOnNeighbor.Battalion);
                AddVisualZOC(tileOnNeighbor.Facility);
            }


            ZOCState visualState = ZOCState.Neutral;
            int balance = visualPlayerZOC - visualEnemyZOC;
            Color zocColor = Color.clear;
            float intensity = Mathf.Min(Mathf.Abs(balance) * 0.2f, zocMaxAlpha);
            if (balance > 0) zocColor = zocPlayerColor;
            else if (balance < 0) zocColor = zocEnemyColor;
            zocColor.a = intensity;
            tileScript.SetOverlayColor(zocColor);
        }
        
        private void RefreshZOCVisualsAround(Unit unit)
        {
            if (unit == null) return;
            var tilesToUpdate = GetAllTilesInRange(unit.position, 1);
            foreach (var pos in tilesToUpdate)
            {
                UpdateTileZOCVisual(pos);
            }
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

        #region FogOfWar
        
        private void UpdateFogOfWar()
       {
           if (fogMap == null) return;
           Debug.Log($"--- Running UpdateFogOfWar ---");
           for (int x = 0; x < Size.x; x++)
           {
               for (int y = 0; y < Size.y; y++)
               {
                   if (fogMap[x, y] == FogState.Visible)
                   {
                       fogMap[x, y] = FogState.Explored;
                   }
               }
           }

           // 2. 根据视野源，计算新的可见格子
           HashSet<Vector2Int> currentlyVisibleTiles = new HashSet<Vector2Int>();
           Debug.Log($"Found {_playerVisionSourceTiles.Count} vision sources.");
           
           foreach (var sourcePos in _playerVisionSourceTiles)
           {
               // 获取提供视野的单位
               var tileData = mapData[sourcePos.x, sourcePos.y];
               Unit visionProvider = tileData.Battalion ?? (Unit)tileData.Facility;
               int visionRange = 0;

               if (visionProvider != null)
               {
                   visionRange = visionProvider.GetVisionRange();
               }
               else 
               {
                   if ((CurrentStage == Stage.Arrangement ||CurrentStage == Stage.Preparation)&& availableArrangementPositions.Contains(sourcePos))
                       visionRange = BattleParam.Instance.deployZoneVisionRange;
                   if (mapData[sourcePos.x, sourcePos.y].isExtractionPoint)
                       visionRange = BattleParam.Instance.extractionZoneVisionRange;
               }

               if (visionRange > 0)
               {
                   var tilesInRange = GetAllTilesInRange(sourcePos, visionRange);
                   currentlyVisibleTiles.UnionWith(tilesInRange);
               }
           }

           // 3. 更新 fogMap
           foreach (var pos in currentlyVisibleTiles)
           {
               fogMap[pos.x, pos.y] = FogState.Visible;
           }
           
           // 4. 特殊处理：撤离点提供历史视野
           if (levelPreset != null)
           {
               foreach (var pos in levelPreset.extractionPoints)
               {
                   var tilesInRange = GetAllTilesInRange(pos, BattleParam.Instance.extractionZoneVisionRange);
                   foreach (var tile in tilesInRange)
                   {
                       if (fogMap[tile.x, tile.y] == FogState.Concealed)
                       {
                           fogMap[tile.x, tile.y] = FogState.Explored;
                       }
                   }
               }
           }

           // 5. 根据新的 fogMap 更新所有单位的 IsVisible 状态
           RefreshAllUnitsVisuals();
           foreach (var factionUnits in factionActiveUnits.Values)
           {
               foreach (var unit in factionUnits)
               {
                   if (unit != null)
                   {
                       bool oldVisibility = unit.gameObject.activeSelf;
                       unit.OnUnitStateChanged();

                       // 如果一个单位的可见性刚刚发生了变化（从可见->不可见 或 不可见->可见）
                       if (oldVisibility != IsUnitVisibleToPlayer(unit))
                       {
                           // 刷新它周围的 ZOC 视觉
                           RefreshZOCVisualsAround(unit);
                       }
                   }
               }
           }

           // 6. 通知视觉控制器更新迷雾
           if (fogOfWarController != null)
           {
               fogOfWarController.UpdateFogVisuals(fogMap);
           }
       }
        
        private void UpdatePlayerVisionSources()
        {
            _playerVisionSourceTiles.Clear();
           
            // 添加玩家和友方单位
            foreach (var unit in factionActiveUnits[Faction.Player])
            {
                _playerVisionSourceTiles.Add(unit.position);
            }
            foreach (var unit in factionActiveUnits[Faction.Friend])
            {
                _playerVisionSourceTiles.Add(unit.position);
            }

            if (CurrentStage == Stage.Arrangement||CurrentStage == Stage.Preparation)
            {
                if (availableArrangementPositions != null)
                {
                    _playerVisionSourceTiles.UnionWith(availableArrangementPositions);
                }
            }
        }
        public bool IsUnitVisibleToPlayer(Unit unit)
        {
            if (unit == null) return false;

            // 己方单位，我们自己永远能看到
            if (unit.faction == Faction.Player || unit.faction == Faction.Friend)
            {
                return true;
            }

            // 对于敌方和中立单位
            if (fogMap == null) return true; // 如果没有迷雾系统，默认都可见

            // 必须在当前视野格子里
            if (fogMap[unit.position.x, unit.position.y] != FogState.Visible)
            {
                return false;
            }

            // 同时，单位本身不能是隐身状态
            if (!unit.IsVisible)
            {
                return false;
            }

            return true;
        }
        
        public void RefreshAllUnitsVisuals()
        {
            foreach (var factionUnits in factionActiveUnits.Values)
            {
                foreach (var unit in factionUnits)
                {
                    if (unit != null)
                    {
                        unit.OnUnitStateChanged();
                    }
                }
            }
        }
        #endregion
    }
}
