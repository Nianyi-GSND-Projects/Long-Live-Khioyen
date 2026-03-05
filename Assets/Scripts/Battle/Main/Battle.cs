using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.Serialization;

namespace LongLiveKhioyen
{
	public partial class Battle : MonoBehaviour
	{
		static Battle _instance;
		public static Battle Instance => _instance;
		public System.Action onInitialized;
		public ActionDatabase actionDataBase;
		private CommanderRegistry commanderRegistry;
		
		AudioSource audioSource;

		#region Preset
		[Header("Level Preset Settings")]
		public bool useLevelPreset = false;
		public MapDataSO presetMapData; 
		public BattlePresetSO levelPreset;
		#endregion
		
		public bool IsUnitMoving { get; private set; } = false;
		public bool IsEventBlockingAI { get; set; }
		
		public IEnumerator WaitForEventBlocking()
		{
			while (IsEventBlockingAI)
			{
				yield return null;
			}
		}
		
		#region Battle data

		public BattleMetaData data;
		public ArmyStatus armyStatus;
		public Vector2Int Size => data.battleSize;
		
		public string BattleName => data.battleName;
		#endregion


		#region Life cycle
		void Awake()
		{
			_instance = this;
		}

		void OnDestroy()
		{
			_instance = null;
		}

		void Start()
		{
			#if BATTLE_TEST
				GenerateTestData();
			#else
				LoadBattleMetaData();
			#endif
			
			InitializeData();
			
			Debug.Log($"Current map Size: {Size.x}x{Size.y}");
			InitializeScene();
			
			InitializeComponent();
			
			InitializeGameStatus();
			
			#if BATTLE_TEST

			GenerateTestArmyData();
			
			#endif
			
			ImportArmyData();
			
			PlaceNonPlayerBattalionUnit();
			
			onInitialized?.Invoke();
			
			if (BattleEventManager.Instance != null)
			{
				BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnBattleStart);
			}
			
		}
		
		#endregion
		
		#region Initialization
		
		
		private void ImportArmyData()
		{
			Debug.Log("正在读取军队数据");
			if (useLevelPreset && levelPreset != null && levelPreset.usePresetPlayerArmy)
			{
				Debug.Log("Using Preset Player Army.");
              
				foreach (var data in levelPreset.playerReserveList)
				{
					BattalionDescriptor desc = new BattalionDescriptor
					{
						Definition = data.battalionDef,
						faction = Faction.Player,
						armyId = -1,
						placed = false,
						isVisible = data.isVisible,
						maxSolider = data.battalionDef.defaultMaxSolider,
						maxMorale = data.battalionDef.defaultMaxMorale
					};

					if (data.commanderTemplate != null)
					{
						desc.battalionCommander = data.commanderTemplate.CreateInstance(CommanderRegistry.Instance.GenerateID());
					}
					else if (data.useRandomCommander)
					{
						desc.battalionCommander = CommanderRegistry.Instance.GenerateCommander(data.randomCommanderProfile);
					}

					desc.maxSolider += desc.battalionCommander.GetMaxSoldiersBonus();
					desc.maxMorale += desc.battalionCommander.GetMaxMoraleBonus();
					desc.actionChance += desc.battalionCommander.GetActionChanceBonus();
					
					desc.currentSoliders = data.overrideSoldiers > 0
						? data.overrideSoldiers
						: desc.maxSolider;
					
					desc.currentMorale = data.overrideMorale > 0
						? data.overrideMorale
						: desc.maxMorale;
					
					desc.currentExp = 50;
					
					
					playerReserveTeam.Add(desc);
				}
				return;
			}
			
			for (int i = 0; i < armyStatus.battalionStatuses.Count; i++) 
				playerReserveTeam.Add(GenerateBattalionDescriptorFromBattalionStatus(armyStatus.battalionStatuses[i]));
		}


		private void InitializeScene()
		{
			Debug.Log("正在初始化场景");
			transform.rotation = Quaternion.Euler(0, 0, 0);
			gameObject.isStatic = true;
			GenerateHexGrid();
			AnchorPosition = MapToWorld(new Vector2Int(data.battleSize.x/2, data.battleSize.y/2));
			if (useLevelPreset && levelPreset != null)
			{
				foreach(var p in levelPreset.extractionPoints) CreateExtractionPoint(p);
                
				// 3. 读取布阵点
				availableArrangementPositions.Clear();
				foreach(var p in levelPreset.playerDeployPoints) availableArrangementPositions.Add(p);
			}
			else
			{
				GenerateArrangementSlot();
			}
			
			if (fogOfWarController != null)
			{
				fogOfWarController.Initialize(Size);
			}
			
		}
		
		private void InitializeComponent()
		{
			Debug.Log("正在初始化战场组件");
			audioSource = GetComponent<AudioSource>();
		}
		
		private void InitializeBuildableFacilities()
		{
			Debug.Log("正在初始化建设列表");
			// TODO: 从 GameInstance 或 TechTree 读取
			if (buildableFacilities == null) buildableFacilities = new List<FacilityDefinition>();
		}
		
		private void InitializeData()
		{
			Debug.Log("正在初始化数据");
			#if BATTLE_TEST
			armyStatus = new ArmyStatus();
			#else
			armyStatus = GameInstance.Instance.ActiveArmy;
			#endif
			InitializeBuildableFacilities();
			
			actionDataBase.Initialize();
			commanderRegistry = CommanderRegistry.Instance; 
			
			availableMovePositions = new HashSet<Vector2Int>();
			availableArrangementPositions = new HashSet<Vector2Int>();
			availableTargetPositions = new HashSet<Vector2Int>();

			if (useLevelPreset && levelPreset != null)
			{
				presetMapData = levelPreset.mapData;
				if (BattleEventManager.Instance != null && levelPreset.levelEvents != null)
				{
					BattleEventManager.Instance.levelEvents.Clear(); 
                  
					foreach (var evt in levelPreset.levelEvents)
					{
						if (evt != null)
						{
							BattleEventManager.Instance.levelEvents.Add(evt);
						}
					}
					Debug.Log($"Loaded {levelPreset.levelEvents.Count} events from preset.");
				}
			}
				
			
			if (presetMapData != null)
			{
				data.battleSize = new Vector2Int(presetMapData.width, presetMapData.height);
				Debug.Log($"使用预设地图尺寸: {Size}");
			}
			
			mapTerrainData = new string[Size.x, Size.y];
			mapData = new TileData[Size.x, Size.y];
			fogMap = new FogState[Size.x, Size.y];
			for (int x = 0; x < Size.x; x++)
			for (int y = 0; y < Size.y; y++)
				fogMap[x, y] = FogState.Concealed;
			
			for(int x=0; x<Size.x; x++)
				for(int y=0; y<Size.y; y++)
					mapData[x,y] = new TileData();
			
			playerReserveTeam = new List<BattalionDescriptor>();
			factionActiveUnits = new Dictionary<Faction, HashSet<Unit>>();
			factionVisibleUnits = new Dictionary<Faction, HashSet<Unit>>();
			
			foreach (Faction f in System.Enum.GetValues(typeof(Faction)))
			{
				factionActiveUnits.Add(f, new HashSet<Unit>());
				factionVisibleUnits.Add(f, new HashSet<Unit>());
			}
		}
		
		private void InitializeGameStatus()
		{
			Debug.Log("正在初始化游戏状态");
			TurnCount = 0;
			CurrentTurnState = TurnState.Processing;
			
			CurrentActionStage = PlayerActionStage.None;
			_previousActionStage = PlayerActionStage.None;
			ChangeStage(Stage.Preparation);
		}

		private void PlaceNonPlayerBattalionUnit()
		{
			List<BattalionDescriptor> enemyReserveTeam = new List<BattalionDescriptor>();

			if (useLevelPreset && levelPreset != null)
			{
				
					PlaceFixedNonPlayerUnits();
					PlaceRandomNonPlayerUnits();
				return;
			}

		}
		
		private void PlaceFixedNonPlayerUnits()
		{
			Debug.Log("Placing fixed preplaced units...");
			foreach (var spawnData in levelPreset.preplacedUnits)
				{
						RegisterUnitToBattle(GenerateDescriptorFromSpawnData(spawnData), spawnData.position,spawnData.isVisible);
				}
		}

		private void PlaceRandomNonPlayerUnits()
		{
			Debug.Log("Placing random enemies based on rules...");
			if (levelPreset.nonPlayerUnitsSpawnZones == null || levelPreset.nonPlayerUnitsSpawnZones.Count == 0)
			{
				Debug.LogError(
					"Random enemy generation failed: Enemy Spawn Zones are not defined in the BattlePresetSO.");
				return;
			}

			List<Vector2Int> availableSpawnPoints = new List<Vector2Int>(levelPreset.nonPlayerUnitsSpawnZones);
			foreach (var rule in levelPreset.randomEnemyRules)
			{
				if (rule.unitDefinition == null) continue;

				int count = Random.Range(rule.minCount, rule.maxCount + 1);

				for (int i = 0; i < count; i++)
				{
					if (availableSpawnPoints.Count == 0)
					{
						Debug.LogWarning("Ran out of spawn points. Some enemies were not placed.");
						break;
					}

					int randomIndex = Random.Range(0, availableSpawnPoints.Count);
					Vector2Int spawnPos = availableSpawnPoints[randomIndex];
					availableSpawnPoints.RemoveAt(randomIndex);
					UnitDescriptor desc = GenerateDescriptorFromRule(rule);
					if (desc != null)
					{
						RegisterUnitToBattle(desc, spawnPos);
					}
				}
			}
		}

		private UnitDescriptor GenerateDescriptorFromSpawnData(PreplacedUnitData spawnData)
		{
			if (spawnData.isFacility)
					{
						FacilityDescriptor facDesc = new FacilityDescriptor
						{
							Definition = spawnData.facilityDef,
							faction = spawnData.faction,
							instanceId = spawnData.instanceId,
							isVisible = spawnData.isVisible,
							zocPower = spawnData.facilityDef.defaultZocPower,
							visionRange = spawnData.facilityDef.defaultVisionRange,
							maxDurability = spawnData.facilityDef.defaultMaxDurability,
							currentDurability = spawnData.overrideSoldiers > 0 ? spawnData.overrideSoldiers : spawnData.facilityDef.defaultMaxDurability, // 复用 overrideSoldiers 字段作为耐久度
						};
						return facDesc;
					}
					else
					{
						BattalionDescriptor desc = new BattalionDescriptor
						{
							Definition = spawnData.battalionDef,
							faction = spawnData.faction,
							instanceId = spawnData.instanceId,
							isVisible = spawnData.isVisible,
							zocPower = spawnData.battalionDef.defaultZocPower,
							visionRange = spawnData.battalionDef.defaultVisionRange,
							placed = false,
						};
						if (spawnData.commanderTemplate != null)
						{
							// 使用模板
							desc.battalionCommander = spawnData.commanderTemplate.CreateInstance(CommanderRegistry.Instance.GenerateID());
						}
						else if (spawnData.useRandomCommander)
						{
							// 使用随机生成
							desc.battalionCommander = CommanderRegistry.Instance.GenerateCommander(spawnData.randomCommanderProfile);
						}
						else
						{
							desc.battalionCommander = null;
						}

						desc.maxSolider = spawnData.battalionDef.defaultMaxSolider;
						desc.maxMorale = spawnData.battalionDef.defaultMaxMorale;
						
						if (desc.battalionCommander != null)
						{
							desc.maxSolider += desc.battalionCommander.GetMaxSoldiersBonus();

							desc.maxMorale += desc.battalionCommander.GetMaxMoraleBonus();
							
							desc.actionChance += desc.battalionCommander.GetActionChanceBonus();
						}
						
						desc.currentSoliders = spawnData.overrideSoldiers > 0
							? spawnData.overrideSoldiers
							: desc.maxSolider;
						
						desc.currentMorale =
							spawnData.overrideMorale > 0 ? spawnData.overrideMorale : desc.maxMorale;
						
						desc.currentExp = 50;

						return desc;
					}
		}
		
		private UnitDescriptor GenerateDescriptorFromRule(RandomEnemySpawnRule enemySpawnRule)
			{
				if (enemySpawnRule.unitDefinition is  FacilityDefinition facDef)
					{
						FacilityDescriptor facDesc = new FacilityDescriptor
						{
							Definition = facDef,
							faction = enemySpawnRule.faction,
							isVisible = facDef.defaultVisibility,
							zocPower = facDef.defaultZocPower,
							visionRange = facDef.defaultVisionRange,
							maxDurability = facDef.defaultMaxDurability,
							currentDurability = facDef.defaultMaxDurability,
						};
						return facDesc;
					}
					else if(enemySpawnRule.unitDefinition is BattalionDefinition batDef)
					{
						BattalionDescriptor desc = new BattalionDescriptor
						{
							Definition =  batDef,
							faction = enemySpawnRule.faction,
							isVisible = batDef.defaultVisibility,
							zocPower = batDef.defaultZocPower,
							visionRange = batDef.defaultVisionRange,
							placed = false,
						};
						if (enemySpawnRule.useRandomCommander)
						{
							desc.battalionCommander = CommanderRegistry.Instance.GenerateCommander(enemySpawnRule.commanderProfile);
						}
						else
						{
							desc.battalionCommander = null;
						}

						desc.maxSolider = batDef.defaultMaxSolider;
						desc.maxMorale = batDef.defaultMaxMorale;
						
						if (desc.battalionCommander != null)
						{
							desc.maxSolider += desc.battalionCommander.GetMaxSoldiersBonus();

							desc.maxMorale += desc.battalionCommander.GetMaxMoraleBonus();
							
							desc.actionChance += desc.battalionCommander.GetActionChanceBonus();
						}
						
						desc.currentSoliders = desc.maxSolider;
						
						desc.currentMorale = desc.maxMorale;
						
						desc.currentExp = 50;

						return desc;
					}

				return null;
			}

		#endregion
		
		#region Interface

		public event System.Action<Unit> OnUnitSelectionChanged;
		public event System.Action<List<Unit>> OnAmbiguousSelectionStarted;
		public event System.Action OnAmbiguousSelectionEnded;
		public event System.Action<BattalionDescriptor> OnReserveTeamSelectionChanged;
		
		public event System.Action OnUnitPlaced;
		
		#endregion
		
		#region End Game
		
		public void CheckBattleEnd()
		{
			if (CurrentStage == Stage.Settlement) return; // 防止重复触发

			bool isBattleOver = false;
			bool isWin = false;

			switch (data.battleGoal)
			{
				case BattleGoal.Annihilate:
					(isBattleOver, isWin) = CheckAnnihilateCondition();
					break;
		
				// TODO: 其他模式 (Capture, Survive, etc.)
				// case BattleGoal.Capture: ...
		
				default:
					Debug.LogWarning($"未实现的战斗目标: {data.battleGoal}");
					break;
			}

			if (isBattleOver)
			{
				EndBattle(isWin);
			}
		}
		
		private (bool isOver, bool isWin) CheckAnnihilateCondition()
		{
			bool enemyBattalionWipedOut = true;
			if (factionActiveUnits.ContainsKey(Faction.Enemy))
			{
				foreach (var unit in factionActiveUnits[Faction.Enemy])
				{
					if (unit is Battalion bat && bat.currentSoliders > 0)
					{
						enemyBattalionWipedOut = false;
						break;
					}
				}
			}
	
			if (enemyBattalionWipedOut) return (true, true);

			bool playerBattalionWipedOut = true;
			if (factionActiveUnits.ContainsKey(Faction.Player))
			{
				foreach (var unit in factionActiveUnits[Faction.Player])
				{
					if (unit is Battalion bat && bat.gameObject.activeSelf && bat.currentSoliders > 0)
					{
						playerBattalionWipedOut = false;
						break;
					}
				}
			}

			if (playerBattalionWipedOut) return (true, false);


			return (false, false);
		}

		BattleResult result;
		private void EndBattle(bool isWin)
		{
			Debug.Log($"战斗结束！结果: {(isWin ? "胜利" : "失败")}");
	
			// 1. 切换阶段
			ChangeStage(Stage.Settlement);
			
			result = YieldResult();
			result.Victory = isWin;

			// 2. 显示结算 UI (这里假设有一个 BattleResultUI 单例)
			if (BattleResultUI.Instance != null)
			{
				BattleResultUI.Instance.Show(result);
			}
			else
			{
				Debug.LogError("BattleResultUI Instance not found!");
			}
		}

		private void ApplyArmyChangesToArmyStatus() 
		{
			if (armyStatus == null || armyStatus.battalionStatuses == null) return;
			
			Dictionary<int, Battalion> survivors = new Dictionary<int, Battalion>();
			
			if (factionActiveUnits.ContainsKey(Faction.Player))
			{
				foreach (var unit in factionActiveUnits[Faction.Player])
				{
					if (unit is Battalion bat) survivors[bat.ArmyId] = bat;
				}
			}
			
			foreach (var unit in retreatedUnits)
			{
				if (unit is Battalion bat && unit.faction == Faction.Player) 
					survivors[bat.ArmyId] = bat;
			}

			for (int i = armyStatus.battalionStatuses.Count - 1; i >= 0; i--)
			{
				var status = armyStatus.battalionStatuses[i];
              
				if (survivors.TryGetValue(status.battalionId, out Battalion bat))
				{
					// 存活 (在场或撤退)
					status.currentSolider = bat.currentSoliders;
					status.currentMorale = bat.currentMorale;
					status.currentExp += bat.exp;//TODO：经验值转化
                  
					Debug.Log($"[Sync] Battalion {status.battalionName} survived. HP: {status.currentSolider}, EXP: +{bat.exp}");
				}
				else
				{
					// 死亡 (既不在场也不在撤退列表)
					// 也可以双重检查 deadUnits 以确保万无一失
					Debug.Log($"[Sync] Battalion {status.battalionName} KIA. Removing from army.");
					armyStatus.battalionStatuses.RemoveAt(i);
				}
			}
		}
		
		#endregion
		
		#region Functions
		
		public BattleResult YieldResult()
		{
			BattleResult result = new()
			{
				polisId = GameInstance.Instance.LastPolis.id,
				passedTime = GameManager.InternalSettings.timeCostPerBattle,  // TODO: 牢宋需给具体公式
			};
			ApplyArmyChangesToArmyStatus();
			CollectLoot(result);
			CollectResult(result);
			return result;
		}

		private void CollectResult(BattleResult result)
		{
			
		}
		private void CollectLoot(BattleResult result)
		{
			Dictionary<ItemDefinition, int> consolidatedLoot = new Dictionary<ItemDefinition, int>();
          
			// 辅助方法：处理单个单位的战利品
			void ProcessUnitLoot(Unit unit)
			{
				if (unit is Battalion bat && unit.faction == Faction.Player)
				{
					foreach (inBattleItem item in bat.inventory)
					{
						if (item == null || item.definition == null) continue;

						if (consolidatedLoot.ContainsKey(item.definition))
						{
							consolidatedLoot[item.definition] += item.amount;
						}
						else
						{
							consolidatedLoot.Add(item.definition, item.amount);
						}
					}
				}
			}

			// 1. 遍历在场单位
			if (factionActiveUnits.ContainsKey(Faction.Player))
			{
				foreach (var unit in factionActiveUnits[Faction.Player])
				{
					ProcessUnitLoot(unit);
				}
			}
          
			// 2. [新增] 遍历撤退单位
			foreach (var unit in retreatedUnits)
			{
				ProcessUnitLoot(unit);
			}
	
			if (result.Loot == null) result.Loot = new List<inBattleItem>();
	
			foreach (var kvp in consolidatedLoot)
			{
				ItemDefinition def = kvp.Key;
				int totalAmount = kvp.Value;

				inBattleItem newItem = new inBattleItem
				{
					definition = def,
					amount = totalAmount
				};

				result.Loot.Add(newItem);
			}
		}
		
		public void ExitBattle()
		{
			GameInstance.Instance.ExitBattle(result);
		}
		
		#endregion
	}
	
	
	public class BattleResult
	{
		public string polisId;
		//BattleResult即各个部队的缴获情况
		public bool Victory = false;
		/// <summary>战斗用时，按月计。</summary>
		public float passedTime;
		public List<inBattleItem> Loot;
	}
}
