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
		
		#region Battle data

		//TODO:加载战斗数据
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
			//加载MetaData
			#if BATTLE_TEST
				GenerateTestData();
			#else
				LoadBattleMetaData();
			#endif
			
			InitializeData();
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
			
			if (useLevelPreset && levelPreset != null && levelPreset.usePresetPlayerArmy)
			{
				Debug.Log("Using Preset Player Army.");
              
				foreach (var data in levelPreset.playerReserveList)
				{
					// 构造 Descriptor
					BattalionDescriptor desc = new BattalionDescriptor
					{
						Definition = data.battalionDef,
						faction = Faction.Player,
						armyId = -1, // 预设单位没有全局 ID
						placed = false,
                      
						// 应用 Override
						maxSolider = data.battalionDef.defaultMaxSolider,
						currentSoliders = data.overrideSoldiers > 0 ? data.overrideSoldiers : data.battalionDef.defaultMaxSolider,
                      
						maxMorale = data.battalionDef.defaultMaxMorale,
						currentMurale = data.overrideMorale > 0 ? data.overrideMorale : data.battalionDef.defaultMaxMorale,
                      
						maxTraining = 100,
						currentTraining = 50
					};

					// 生成指挥官
					if (data.commanderTemplate != null)
					{
						desc.battalionCommander = data.commanderTemplate.CreateInstance(CommanderRegistry.Instance.GenerateID());
					}
					else if (data.useRandomCommander)
					{
						desc.battalionCommander = CommanderRegistry.Instance.GenerateCommander(data.randomCommanderProfile);
					}
                  
					playerReserveTeam.Add(desc);
				}
				return; // 跳过后续逻辑
			}
			
			for (int i = 0; i < armyStatus.battalionStatuses.Count; i++) 
				playerReserveTeam.Add(GenerateBattalionDescriptorFromBattalionStatus(armyStatus.battalionStatuses[i]));
		}

		public BattalionDescriptor GenerateBattalionDescriptorFromBattalionStatus(BattalionStatus battalionStatus)
		{
			BattalionDescriptor battalionDescriptor = new BattalionDescriptor();
			//根据battalionStatus烘焙单位属性快照
			battalionDescriptor.Definition = battalionStatus.battalionDefinition;
			
			battalionDescriptor.armyId = battalionStatus.battalionId;
			//该预备队在整个Army中的id

			battalionDescriptor.faction = Faction.Player;
			battalionDescriptor.battalionCommander = battalionStatus.battalionCommander;
			
			//TODO 科技树与全局增益影响
			battalionDescriptor.maxSolider = battalionStatus.MaxSolider;
			battalionDescriptor.maxMorale = battalionStatus.MaxMorale;
			battalionDescriptor.maxTraining = battalionStatus.MaxExp;
			battalionDescriptor.currentSoliders = battalionStatus.currentSolider;
			battalionDescriptor.currentMurale = battalionStatus.currentMorale;
			battalionDescriptor.currentTraining = battalionStatus.currentExp;
			battalionDescriptor.placed = false;
			
			return battalionDescriptor;
			
		}

		private void InitializeScene()
		{
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
			
			
		}
		
		private void InitializeComponent()
		{
			audioSource = GetComponent<AudioSource>();
		}
		
		private void InitializeData()
		{
			#if BATTLE_TEST
			armyStatus = new ArmyStatus();
			#else
			armyStatus = GameInstance.Instance.ActiveArmy;
			#endif
			
			actionDataBase.Initialize();
			commanderRegistry = CommanderRegistry.Instance; 
			
			mapTerrainData = new string[Size.x, Size.y];
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
			
			mapData = new TileData[Size.x, Size.y];
			for(int x=0; x<Size.x; x++)
				for(int y=0; y<Size.y; y++)
					mapData[x,y] = new TileData();
			
			
			
			playerReserveTeam = new List<BattalionDescriptor>();
			
			factionActiveUnits = new Dictionary<Faction, HashSet<Unit>>();
			foreach (Faction f in System.Enum.GetValues(typeof(Faction)))
			{
				factionActiveUnits.Add(f, new HashSet<Unit>());
			}
			
			#if BATTLE_TEST
			testPlayerReserveTeamCount = 3;
			
			#endif
		}
		
		private void InitializeGameStatus()
		{
			TurnCount = 0;
			CurrentTurnState = TurnState.PlayerTurn;
			ChangeStage(Stage.Preparation);
		}

		private void PlaceNonPlayerBattalionUnit()
		{
			List<BattalionDescriptor> enemyReserveTeam = new List<BattalionDescriptor>();

			if (useLevelPreset && levelPreset != null)
			{
				foreach (var spawnData in levelPreset.preplacedUnits)
				{
					if (spawnData.isFacility)
					{
						FacilityDescriptor facDesc = new FacilityDescriptor
						{
							Definition = spawnData.facilityDef,
							faction = spawnData.faction,
							instanceId = spawnData.instanceId,
                          
							// 应用 Override (如果有)
							maxDurability = spawnData.facilityDef.defaultMaxDurability,
							currentDurability = spawnData.overrideSoldiers > 0 ? spawnData.overrideSoldiers : spawnData.facilityDef.defaultMaxDurability, // 复用 overrideSoldiers 字段作为耐久度
						};
						
						Facility facility = SpawnUnit<Facility, FacilityDefinition,FacilityDescriptor>(
							facDesc,
							spawnData.position
						);
			
						factionActiveUnits[Faction.Player].Add(facility);
					}
					else
					{
						// 构造临时的 Descriptor
						BattalionDescriptor desc = new BattalionDescriptor
						{
							Definition = spawnData.battalionDef,
							faction = spawnData.faction,
							instanceId = spawnData.instanceId,
							armyId = spawnData.instanceId,
							placed = false,
							maxSolider = spawnData.battalionDef.defaultMaxSolider,
							currentSoliders = spawnData.overrideSoldiers > 0 ? spawnData.overrideSoldiers : spawnData.battalionDef.defaultMaxSolider,
                  
							maxMorale = spawnData.battalionDef.defaultMaxMorale,
							currentMurale = spawnData.overrideMorale > 0 ? spawnData.overrideMorale : spawnData.battalionDef.defaultMaxMorale,
                  
							maxTraining = 100,
							currentTraining = 50
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
						
						SpawnBattalion(desc, spawnData.position);
					}
				}
				return; // [关键] 既然读了预设，就跳过后面的随机生成
			}

			//GenerateEnemyData();
			//TODO 
		}
		
		#endregion
		
		#region Interface

		public event System.Action<Unit> OnUnitSelectionChanged;
		public event System.Action<List<Unit>> OnAmbiguousSelectionStarted;
		public event System.Action OnAmbiguousSelectionEnded;
		public event System.Action<BattalionDescriptor> OnReserveTeamSelectionChanged;
		
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
					if (unit is Battalion bat && bat.gameObject.activeSelf && bat.currentSoliders > 0)
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
					// 关键修改：只检查 Battalion 类型
					if (unit is Battalion bat && bat.gameObject.activeSelf && bat.currentSoliders > 0)
					{
						playerBattalionWipedOut = false;
						break;
					}
				}
			}

			if (playerBattalionWipedOut) return (true, false); // 我方部队全灭 -> 失败


			return (false, false);
		}
		
		
		private void EndBattle(bool isWin)
		{
			Debug.Log($"战斗结束！结果: {(isWin ? "胜利" : "失败")}");
	
			// 1. 切换阶段
			ChangeStage(Stage.Settlement);
			
			BattleResult result = YieldResult();
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
					if (unit is Battalion bat) survivors[bat.InstanceId] = bat;
				}
			}
			
			foreach (var unit in retreatedUnits)
			{
				if (unit is Battalion bat && unit.faction == Faction.Player) 
					survivors[bat.InstanceId] = bat;
			}

			for (int i = armyStatus.battalionStatuses.Count - 1; i >= 0; i--)
			{
				var status = armyStatus.battalionStatuses[i];
              
				if (survivors.TryGetValue(status.battalionId, out Battalion bat))
				{
					// 存活 (在场或撤退)
					status.currentSolider = bat.currentSoliders;
					status.currentMorale = bat.currentMurale;
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
			BattleResult result = new BattleResult();
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
			GameInstance.Instance.ExitBattle();
		}
		
		#endregion
	}
	
	
	public class BattleResult
	{
		//BattleResult即各个部队的缴获情况
		public bool Victory = false;
		public List<inBattleItem> Loot;
	}
}
