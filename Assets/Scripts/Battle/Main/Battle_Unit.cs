using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {

        #region Data
        [Header("Construction")]
        public List<FacilityDefinition> buildableFacilities = new List<FacilityDefinition>();
        
        #region Unit Container
		
        public List<BattalionDescriptor> playerReserveTeam;
		
        private Dictionary<Faction,HashSet<Unit>> factionActiveUnits;
        private Dictionary<Faction,HashSet<Unit>> factionVisibleUnits;
        public List<Unit> retreatedUnits = new List<Unit>();
        public List<Unit> deadUnits = new List<Unit>();
        private HashSet<Unit> dirtyUnits = new HashSet<Unit>();
		
        private Dictionary<int, Unit> _instanceIdMap = new Dictionary<int, Unit>();
        private int _nextInstanceId = 1000;
        #endregion
        
        public Unit GetUnitByInstanceId(int id)
        {
            if (_instanceIdMap.TryGetValue(id, out Unit unit))
            {
                return unit;
            }
            
            foreach (var u in retreatedUnits)
            {
                if (u.InstanceId == id) return u;
            }
            return null;
        }
        
        public HashSet<Unit> GetUnitsByFaction(Faction faction)
        {
            if (factionActiveUnits.TryGetValue(faction, out var units))
            {
                return units;
            }
            return new HashSet<Unit>();
        }
        
        public HashSet<Unit> GetVisibleUnitsByFaction(Faction faction)
        {
            if (factionVisibleUnits.TryGetValue(faction, out var units))
            {
                return units;
            }
            return new HashSet<Unit>();
        }
        
        private int GenerateUniqueId()
        {
            while (_instanceIdMap.ContainsKey(_nextInstanceId))
            {
                _nextInstanceId++;
            }
            return _nextInstanceId++;
        }

        #endregion
        
        #region Spawn
        
        public BattalionDescriptor GenerateBattalionDescriptorFromBattalionStatus(BattalionStatus battalionStatus)
        {
            if(battalionStatus==null) Debug.Log("Null battalion");
            BattalionDescriptor battalionDescriptor = new BattalionDescriptor();
            battalionDescriptor.Definition = battalionStatus.battalionDefinition;
			
            battalionDescriptor.armyId = battalionStatus.battalionId;

            battalionDescriptor.faction = Faction.Player;
            if(battalionStatus.battalionCommander != null) 
                battalionDescriptor.battalionCommander = battalionStatus.battalionCommander;
            if(battalionStatus.battalionDefinition == null)
                Debug.Log("Null battalion definition");
            battalionDescriptor.zocPower = battalionStatus.battalionDefinition.defaultZocPower;
            battalionDescriptor.visionRange = battalionStatus.battalionDefinition.defaultVisionRange;
            battalionDescriptor.maxSolider = battalionStatus.MaxSolider;
            battalionDescriptor.maxMorale = battalionStatus.MaxMorale;
            battalionDescriptor.currentSoliders = battalionStatus.currentSolider;
            battalionDescriptor.currentMorale = battalionStatus.currentMorale;
            battalionDescriptor.currentExp = battalionStatus.currentExp;
            battalionDescriptor.placed = false;
			
            return battalionDescriptor;
        }
        
        public Unit RegisterUnitToBattle(UnitDescriptor descriptor, Vector2Int pos)
        {
            return RegisterUnitToBattle(descriptor, pos, descriptor.isVisible);
        }
        public Unit RegisterUnitToBattle(UnitDescriptor descriptor, Vector2Int pos, bool isVisible)
        {
            
            if (descriptor == null) return null;
            if (!IsValidMapPosition(pos))
            {
                Debug.LogError($"尝试在无效位置 {pos} 注册单位！");
                return null;
            }

            if (!CanDescriptorPlaceOnTile(descriptor, pos, true))
            {
                Debug.Log("部署失败！");
                return null;
            }
            
            if (descriptor.instanceId == -1)
            {
                descriptor.instanceId = GenerateUniqueId();
            }
            
            else if (_instanceIdMap.ContainsKey(descriptor.instanceId))
            {
                Debug.LogWarning($"Instance ID {descriptor.instanceId} conflict! Generating new ID.");
                descriptor.instanceId = GenerateUniqueId();
            }
            
            descriptor.placed = true;
            Unit unit = null;

            if (descriptor is BattalionDescriptor batDesc)
            {
                unit = SpawnUnit<Battalion, BattalionDefinition, BattalionDescriptor>(batDesc, pos);
            
                Battalion bat = unit as Battalion;
                bat.battalionCommander = batDesc.battalionCommander;
                bat.currentSoliders = batDesc.currentSoliders;
                bat.currentMorale = batDesc.currentMorale;
                bat.currentExp = batDesc.currentExp;
                bat.ArmyId = batDesc.armyId;
            }
            else if (descriptor is FacilityDescriptor facDesc)
            {
                unit = SpawnUnit<Facility, FacilityDefinition, FacilityDescriptor>(facDesc, pos);
                Facility fac = unit as Facility;
                fac.isConstructed = facDesc.isConstructed;
                fac.currentDurability = facDesc.currentDurability;
            }
            
            if (unit != null)
            {
                unit.IsVisible = isVisible;
                
                InitializeUnitActions(unit);
                
                if (!factionActiveUnits.ContainsKey(unit.faction))
                {
                    factionActiveUnits[unit.faction] = new HashSet<Unit>();
                }
                if (!factionVisibleUnits.ContainsKey(unit.faction))
                {
                    factionVisibleUnits[unit.faction] = new HashSet<Unit>();
                }
                factionActiveUnits[unit.faction].Add(unit);
                if (isVisible)
                {
                    factionVisibleUnits[unit.faction].Add(unit);
                }
                _instanceIdMap[unit.InstanceId] = unit;
                OnUnitPlaced?.Invoke();
                unit.UpdateVisualState();
                unit.OnUnitStateChanged();
                if(CurrentStage == Stage.Battle)
                RefreshAllZOCAndVision(unit);
                Debug.Log($"Unit {unit.name} (ID:{unit.InstanceId}) registered to battle at {pos}.");
            }

            return unit;
        }
        
        

        
        public TUnit SpawnUnit<TUnit, TDef, TDesc>(TDesc descriptor, Vector2Int pos) 
            where TUnit : Unit<TDef>
            where TDef : UnitDefinition
            where TDesc : UnitDescriptor
        {
            var go = new GameObject($"{typeof(TUnit).Name}_{descriptor.Definition.unitName}");

            var unit = go.AddComponent<TUnit>();
            
            unit.Definition = (TDef)descriptor.Definition; 
            unit.InstanceId = descriptor.instanceId;
            unit.faction = descriptor.faction;
            unit.position = pos;
            unit.IsVisible = descriptor.isVisible;
            
            unit.currentHealth = descriptor.currentHealth;
            unit.CalculateEntryStats(descriptor);
			
            var controller = SetupUnitVisuals(unit);
            unit.SetVisualController(controller);
            PlaceUnitOnMap(unit, pos);
            
            unit.transform.SetParent(transform, false);
            unit.transform.localPosition = MapToLocal(pos);
            
            if (CurrentStage == Stage.Battle)
            {
                unit.actionDone = true;
            }
            else
            {
                unit.actionDone = false;
            }
            Debug.Log($"SpawnUnit: {unit.name}, Stage: {CurrentStage}, ActionDone: {unit.actionDone}");
            return unit;
        }
        
        public void PlaceUnitOnMap(Unit unit, Vector2Int pos)
        {
            if (!IsValidMapPosition(pos)) return;
            
            TileData tile = mapData[pos.x, pos.y];

            if (unit is Battalion bat)
            {
                if (tile.Battalion != null) Debug.LogError($"位置 {pos} 已有部队，覆盖逻辑需谨慎处理！");
                tile.Battalion = bat;
            }
            else if (unit is Facility fac)
            {
                if (tile.Facility != null) Debug.LogError($"位置 {pos} 已有设施！");
                tile.Facility = fac;
            }
            //unit.position = pos;
        }
        
        public void PlacingPlayerBattalion(BattalionDescriptor battalionDescriptor, Vector2Int mapPosition)
        {
            if (!playerReserveTeam.Contains(battalionDescriptor))
            {
                Debug.Log("Battalion name: " + battalionDescriptor.Definition.unitName + "Don't exist in your reserve teams.");
                return;
            }

            if (battalionDescriptor.placed)
            {
                Debug.Log("Battalion name: " + battalionDescriptor.Definition.unitName + "already placed.");
                return;
            }

            RegisterUnitToBattle(battalionDescriptor, mapPosition,battalionDescriptor.isVisible);
            
            ClearReserveTeamSelection();
        }
        #endregion
        
        #region Initialization
        public void InitializeUnitActions(Unit unit)
        {
            
            unit.DefaultAttack = unit.unitDefinition.defaultAttack;
            unit.DefaultRetreat = unit.unitDefinition.defaultRetreat;
            unit.DefaultInteract = unit.unitDefinition.defaultInteract;
			
            if (unit.unitDefinition.unitUniqueActions != null)
            {
                unit.runtimeUnitActions.Clear();
                foreach (var action in unit.unitDefinition.unitUniqueActions)
                {
                    if (action != null && action.CheckDisplayConditions(unit))
                    {
                        unit.runtimeUnitActions.Add(action);
                    }
                }
				
            }
            if (unit is Battalion bat)
            {
                if (bat.battalionCommander != null)
                {
                    unit.runtimeCommanderActions.Clear();
                    var allCommanderActions = bat.battalionCommander.GetAllActions();
                    foreach (var action in allCommanderActions)
                    {
                        if (action != null && action.CheckDisplayConditions(unit))
                        {
                            unit.runtimeCommanderActions.Add(action);
                        }
                    }
                }
            }
        }
        

        #endregion
        
        #region Remove
        
        public void RemoveUnitFromBattle(Unit unit)
        {
            if(unit == null) return;
            
            RefreshZOCVisualsAround(unit);
            RemoveUnitFromMap(unit);
			
            if (SelectedUnit == unit) 
            {
                ClearAllSelection();
            }
			
            if (factionActiveUnits.ContainsKey(unit.faction))
            {
                if (factionActiveUnits[unit.faction].Contains(unit))
                {
                    factionActiveUnits[unit.faction].Remove(unit);
                }
            }
            if (factionVisibleUnits.ContainsKey(unit.faction))
            {
                factionVisibleUnits[unit.faction].Remove(unit);
            }
            
            if (_instanceIdMap.ContainsKey(unit.InstanceId))
            {
                _instanceIdMap.Remove(unit.InstanceId);
            }
            
            if (CurrentStage == Stage.Battle) UpdateZOCAroundUnit(unit);
            
            var visualController = unit.GetComponent<UnitVisualController>();
            if (visualController != null)
            {
                visualController.CleanupVisuals();
            }
            unit.gameObject.SetActive(false);
            unit.transform.SetParent(null);
			
            if (!deadUnits.Contains(unit))
            {
                deadUnits.Add(unit);
            }
            
            if (unit.faction == Faction.Player || unit.faction == Faction.Friend)
            {
                UpdatePlayerVisionSources();
                UpdateFogOfWar();
            }
            
        }
        
        public void RemoveUnitFromMap(Unit unit)
        {
            if (!IsValidMapPosition(unit.position)) return;

            TileData tile = mapData[unit.position.x, unit.position.y];
            
            if (unit is Battalion && tile.Battalion == unit)
            {
                tile.Battalion = null;
            }
            else if (unit is Facility && tile.Facility == unit)
            {
                tile.Facility = null;
            }
        }
        
        #endregion

        #region Operation
        
        public IEnumerator MoveUnit(Unit unit, List<Vector2Int> path,System.Action<bool> onComplete)
        {
            if (unit == null || path == null || path.Count == 0)
            {
                onComplete?.Invoke(false);
                yield break;
            }
            
            unit.hasMovedThisTurn = true;
            bool interrupted = false;
            // 2. 逐步移动
            foreach (var pos in path)
            {
                RemoveUnitFromMap(unit);
				
                yield return StartCoroutine(unit.OnEnterNewTileRoutine(pos, (res) => {
                    interrupted = res;
                }));
				
                PlaceUnitOnMap(unit, pos);
				
                Vector3 startPos = unit.transform.localPosition;
                Vector3 endPos = MapToLocal(pos);
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * 5f; // 移动速度
                    unit.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                unit.transform.localPosition = endPos;
                if (interrupted)
                {
                    Debug.Log($"{unit.name} 的移动被陷阱打断！");
                    
                    break;
                }
            }
            CheckDeath(unit);
            unit.OnUnitStateChanged();
            onComplete?.Invoke(interrupted);
        }
        
        

        public IEnumerator ForceMoveUnitRoutine(Unit unit, Vector2Int newPos)
        {
            if (unit == null || !IsValidMapPosition(newPos)) yield break;
            
            UpdateZOCAroundUnit(unit);
            RefreshZOCVisualsAround(unit);
            
            RemoveUnitFromMap(unit);
            TileData newTile = mapData[newPos.x, newPos.y];
            if (unit is Battalion bat) newTile.Battalion = bat;
            else if (unit is Facility fac) newTile.Facility = fac;
            
            yield return StartCoroutine(unit.ReceiveForcedMoveRoutine(newPos));
            
            if (unit.faction == Faction.Player || unit.faction == Faction.Friend)
            {
                UpdatePlayerVisionSources();
                UpdateFogOfWar();
            }
            
        }
        
        public void WithdrawUnit(Unit unit)
        {
            if (factionActiveUnits.ContainsKey(unit.faction))
            {
                factionActiveUnits[unit.faction].Remove(unit);
            }
            if (factionVisibleUnits.ContainsKey(unit.faction))
            {
                factionVisibleUnits[unit.faction].Remove(unit);
            }
            
            RemoveUnitFromMap(unit);
            
            ClearAllSelection();
            RefreshZOCVisualsAround(unit);
            if (CurrentStage == Stage.Battle) UpdateZOCAroundUnit(unit);
            
            unit.gameObject.SetActive(false);
            unit.transform.SetParent(null);
            Debug.Log($"[Withdraw] Unit {unit.name} has successfully retreated.");
            if (unit.faction == Faction.Player || unit.faction == Faction.Friend)
            {
                UpdatePlayerVisionSources(); 
                UpdateFogOfWar(); 
            }
            if (!retreatedUnits.Contains(unit))
            {
                retreatedUnits.Add(unit);
            }
            CheckBattleEnd();
        }
        
        public void ResolveDirtyUnits()
        {
            if (dirtyUnits.Count == 0) return;

            List<Unit> unitsToCheck = new List<Unit>(dirtyUnits);
            
            foreach (var unit in unitsToCheck)
            {
                CheckDeath(unit);
                unit.OnUnitStateChanged();
            }
            dirtyUnits.Clear();
        }
        
        public void MarkUnitDirty(Unit unit)
        {
            if (unit != null && !dirtyUnits.Contains(unit))
            {
                dirtyUnits.Add(unit);
            }
        }
        
        public void CheckDeath(Unit unit)
        {
            if (unit == null) return;
            bool isDead = false;
            
            if(unit is Battalion battalion && battalion.currentSoliders <= 0)
            {
                isDead = true;
                Debug.Log($"Battalion {battalion.InstanceId} die off!");
            }
            else if (unit is Facility facility && facility.currentDurability <= 0)
            {
                isDead = true;
                Debug.Log($"Facility {facility.InstanceId} destroyed!");
            }
			
            if (isDead)
            {
                Debug.Log($"[CheckDeath] Unit {unit.name} is dead. Processing loot...");
                HandleUnitDeathLoot(unit);

                // 3. 触发死亡事件 (Event System)
                // 这允许剧情脚本响应特定单位的死亡
                if (BattleEventManager.Instance != null)
                {
                    BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnUnitDeath, unit);
                }

                // 4. 移除单位 (Cleanup)
                RemoveUnitFromBattle(unit);
                CheckBattleEnd();
            }

            return;
        }
        
        private void HandleUnitDeathLoot(Unit victim)
        {
            // 1. 收集所有掉落物
            List<inBattleItem> allLoot = new List<inBattleItem>();

            // A. 从掉落规则生成
            if (victim.unitDefinition != null && victim.unitDefinition.lootRules != null)
            {
                foreach (var rule in victim.unitDefinition.lootRules)
                {
                    if (rule.lootTable != null && UnityEngine.Random.Range(0, 100) < rule.dropChance)
                    {
                        var item = rule.lootTable.Roll();
                        if (item != null) allLoot.Add(item);
                    }
                }
            }

            // B. 从背包继承 (全掉落)
            if (victim.inventory != null)
            {
                allLoot.AddRange(victim.inventory);
            }

            if (allLoot.Count == 0) 
            {
                Debug.Log("Nothing To Loot");
                return; // 没有东西可掉
            }

            // 2. 确定接收者
            Unit receiver = null;
            Unit killer = victim.LastAttacker;
            bool isMeleeKill = false;

            if (killer != null && killer is Battalion)
            {
                // 判断是否相邻 (近战)
                int dist = GetHexDistance(victim.position, killer.position);
                if (dist <= 1)
                {
                    isMeleeKill = true;
                    receiver = killer;
                    Debug.Log("Loot: Melee kill, loot goes to killer.");
                }
            }
            

            if (!isMeleeKill)
            {
                // 远程/陷阱击杀 -> 掉落在地上
                Debug.Log("Loot: Ranged/Trap kill, loot drops on ground.");
                
                TileData tile = mapData[victim.position.x, victim.position.y];
                
                // 检查是否有非陷阱设施
                if (tile.Facility != null &&tile.Facility != victim && !(tile.Facility.Definition is TrapFacilityDefinition))
                {
                    receiver = tile.Facility;
                    Debug.Log("Loot: Added to existing facility.");
                }
                else
                {
                    // 需要生成宝箱
                    // 首先，如果这里有陷阱，先移除陷阱 (因为我们要放宝箱了)
                    if (tile.Facility != null)
                    {
                        RemoveUnitFromBattle(tile.Facility);
                    }

                    // 生成宝箱
                    if (BattleParam.Instance.droppedLootChestDefinition != null)
                    {
                        FacilityDescriptor chestDesc = new FacilityDescriptor
                        {
                            Definition = BattleParam.Instance.droppedLootChestDefinition,
                            faction = Faction.Neutral,
                            isVisible = true,
                            maxDurability = BattleParam.Instance.droppedLootChestDefinition.defaultMaxDurability,
                            currentDurability = BattleParam.Instance.droppedLootChestDefinition.defaultMaxDurability,
                            isConstructed = true
                        };
                        
                        RemoveUnitFromMap(victim); 
                        
                        receiver = RegisterUnitToBattle(chestDesc, victim.position, true);
                    }
                    else
                    {
                        Debug.LogWarning("BattleParam.droppedLootChestDefinition is null! Loot lost.");
                    }
                }
            }

            if (receiver != null)
            {
                foreach (var item in allLoot)
                {
                    receiver.AddItem(item.definition, item.amount);
                }
            }
        }
        #endregion
        
        
    }
}
