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
            
            if (descriptor.instanceId == -1)
            {
                descriptor.instanceId = GenerateUniqueId();
            }
            else if (_instanceIdMap.ContainsKey(descriptor.instanceId))
            {
                Debug.LogWarning($"Instance ID {descriptor.instanceId} conflict! Generating new ID.");
                descriptor.instanceId = GenerateUniqueId();
            }

            Unit unit = null;

            if (descriptor is BattalionDescriptor batDesc)
            {
                unit = SpawnUnit<Battalion, BattalionDefinition, BattalionDescriptor>(batDesc, pos);
            
                Battalion bat = unit as Battalion;
                bat.battalionCommander = batDesc.battalionCommander;
                bat.currentSoliders = batDesc.currentSoliders;
                bat.currentMurale = batDesc.currentMurale;
                bat.currentTraining = batDesc.currentTraining;
                bat.ArmyId = batDesc.armyId;
                InitializeUnitActions(bat);
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
                descriptor.placed = true;

                OnUnitPlaced?.Invoke();
            
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
            
            unit.currentHealth = descriptor.currentHealth;
			
            unit.CalculateEntryStats(descriptor);
			
            SetupUnitVisuals(unit);
            
            PlaceUnitOnMap(unit, pos);
            
            unit.transform.SetParent(transform, false);
            unit.transform.localPosition = MapToLocal(pos);
            
            InitializeUnitActions(unit);
            
            unit.actionDone = true;
            unit.UpdateVisualState();

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
                if (bat.battalionCommander != null && bat.battalionCommander.commanderActions != null)
                {
                    unit.runtimeCommanderActions.Clear();
                    foreach (var action in bat.battalionCommander.commanderActions)
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
            unit.gameObject.SetActive(false);
            unit.transform.SetParent(null);
			
            if (!deadUnits.Contains(unit))
            {
                deadUnits.Add(unit);
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
        
        public IEnumerator MoveUnit(Unit unit, List<Vector2Int> path)
        {
            if (unit == null || path == null || path.Count == 0) yield break;

            // 1. 标记移动开始
            unit.hasMovedThisTurn = true;
			
            // 2. 逐步移动
            foreach (var pos in path)
            {
                RemoveUnitFromMap(unit);
				
                unit.position = pos;
				
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

                bool interrupted = CheckTileEffectOnEnter(unit, pos);
                
                if (interrupted)
                {
                    Debug.Log($"{unit.name} 的移动被陷阱打断！");
                    yield break; // 提前结束协程
                }
                
            }
            
            CheckDeath(unit);
            unit.OnUnitStateChanged();
        }
        
        

        public void ForceMoveUnit(Unit unit, Vector2Int newPos)
        {
            if (unit == null || !IsValidMapPosition(newPos)) return;

            RemoveUnitFromMap(unit);
            TileData newTile = mapData[newPos.x, newPos.y];
            if (unit is Battalion bat) newTile.Battalion = bat;
            else if (unit is Facility fac) newTile.Facility = fac;
            unit.ReceiveForcedMove(newPos);
            // PlaceUnitOnMap(unit, newPos);
            //
            // unit.transform.localPosition = MapToLocal(newPos);
            //          
            // unit.OnUnitStateChanged();
            //          
            // Debug.Log($"{unit.name} 被强制位移至 {newPos}");
        }
        
        public void WithdrawUnit(Unit unit)
        {
            if (factionActiveUnits.ContainsKey(unit.faction))
            {
                factionActiveUnits[unit.faction].Remove(unit);
            }
            RemoveUnitFromMap(unit);
            
            ClearAllSelection();
			
            unit.gameObject.SetActive(false);
          
            if (!retreatedUnits.Contains(unit))
            {
                retreatedUnits.Add(unit);
            }
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
                // 2. 处理掉落 (Loot)
                // 规则：击杀者存在 + 击杀者是玩家 + 死者不是玩家 + 击杀者是部队
                Debug.Log($"[CheckDeath] Unit {unit.name} is dead. Checking loot...");
                Unit killer = unit.LastAttacker;
				
                if (killer == null) Debug.Log("[CheckDeath] No killer (LastAttacker is null).");
                else Debug.Log($"[CheckDeath] Killer: {killer.name}, Faction: {killer.faction}, Type: {killer.GetType().Name}");

                if (killer != null && 
                    killer.faction == Faction.Player && 
                    unit.faction != Faction.Player &&
                    killer is Battalion killerBat)
                {
                    Debug.Log("[CheckDeath] Loot conditions met. Processing...");
                    ProcessLoot(unit, killerBat);
                }
                else
                {
                    Debug.Log("[CheckDeath] Loot conditions NOT met.");
                }

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
        
        private void ProcessLoot(Unit victim, Battalion killerBat)
        {
            if (victim.unitDefinition == null || victim.unitDefinition.lootRules == null) return;

            foreach (var rule in victim.unitDefinition.lootRules)
            {
                if (rule.lootTable == null) continue;

                // 判定概率
                if (UnityEngine.Random.Range(0, 100) < rule.dropChance)
                {
                    // Roll 物品
                    var item = rule.lootTable.Roll();
                    if (item != null)
                    {
                        killerBat.inventory.Add(item);
                
                        // [修改] 拼合字符串并显示
                        string msg = $"{killerBat.name} looted {item.amount}x {item.definition.itemName}";
                        Debug.Log($"[Loot] {msg}");
                
                        if (LootNotificationManager.Instance != null)
                        {
                            LootNotificationManager.Instance.ShowMessage(msg);
                        }
                    }
                }
            }
        }
        #endregion
        
        
    }
}
