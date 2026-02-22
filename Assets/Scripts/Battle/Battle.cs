using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

namespace LongLiveKhioyen
{
	public class Battle : MonoBehaviour
	{
		static Battle _instance;
		public static Battle Instance => _instance;
		public System.Action onInitialized;
		public ActionDatabase actionDataBase;
		private CommanderRegistry commanderRegistry;
		public string[,] mapTerrainData; 
		
		[Header("Level Preset Settings")]
		public bool useLevelPreset = false;
		public BattlePresetSO levelPreset;
		
		#region General Config

		public Color movementHighlightColor = Color.green; 
		public Color arrangementHighlightColor = Color.blue;
		public Color attackHighlightColor = Color.red;
		AudioSource audioSource;
		#endregion
		
		#region Visual
		
		[Header("Global Visual Config")]
		public GameObject globalFlagPrefab;
		public Material playerFactionMaterial;
		public Material enemyFactionMaterial;
		
		[Header("Map Settings")]
		public MapDataSO presetMapData; 
		
		public GameObject extractionPointPrefab;
		public Material GetFactionMaterial(Faction faction)
		{
			switch (faction)
			{
				case Faction.Player: return playerFactionMaterial;
				case Faction.Enemy: return enemyFactionMaterial;
				default: return null;
			}
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
#if BATTLE_TEST
				Debug_SetMapBorderAsExtraction();
#endif
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
							instanceId = -1, // 或者生成一个 ID
                          
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
							armyId = -1,
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
		
		public void CreateExtractionPoint(Vector2Int pos)
		{
			if (!IsValidMapPosition(pos)) return;
            
			TileData tile = mapData[pos.x, pos.y];
			tile.isExtractionPoint = true;

			// 生成永久视觉标记 (不同于那些临时的高亮格子)
			// 假设你有一个 extractionPointPrefab
			if (extractionPointPrefab != null) // 记得在 Battle 中加这个变量并拖拽 Prefab
			{
				Vector3 worldPos = MapToLocal(pos);
				// 稍微抬高一点防止穿模
				GameObject vfx = Instantiate(extractionPointPrefab, transform);
				vfx.transform.localPosition = worldPos;
                
				// 记录下来，方便以后可能的移除
				tile.TileVFX = vfx; 
			}
		}
		
		#endregion
		
		#region Test
		
		public int testPlayerReserveTeamCount;
		[Header("Test Config")]
		public FacilityDefinition testFacilityDefinition;
		private void GenerateTestData()
		{
			data = new BattleMetaData()
			{
				battleName = "Battle of Test",
				battleId = 0,
				battleTime = 0,
				battleType = BattleType.Encounter,
				battleSize = new Vector2Int(10, 10),
				battlePosition = new Vector2Int(0, 0),
				encounterOrientation = new Vector2Int(0, 0),
				battleGoal = BattleGoal.Annihilate,
				enemyCount = 4
			};
		}
		
		private void GenerateTestArmyData()
		{
			//PlayerReserveTeam
			armyStatus.armyCommander = CommanderRegistry.Instance.GetAllFreeCommanders()
				.Find(c => c.commanderName == "王 念一");
			
			armyStatus.battalionStatuses.Clear();
			
			for (int i = 0; i < testPlayerReserveTeamCount; i++)
			{
				BattalionStatus battalionStatus = new BattalionStatus()
				{
					battalionId = i,
					battalionName = "TestBattalion" + i,
					battalionCommander = CommanderRegistry.Instance.GenerateCommander(CommanderGenerationProfile.Default),
					battalionDefinition = defaultReserveTeamDefinition
				};
				
				battalionStatus.currentSolider = battalionStatus.MaxSolider;
				battalionStatus.currentMorale = battalionStatus.MaxMorale;
				battalionStatus.currentExp = battalionStatus.MaxExp;
				
				armyStatus.battalionStatuses.Add(battalionStatus);
				battalionStatus.battalionCommander.isAssigned = true;
				Debug.Log("Commander name: " + battalionStatus.battalionCommander.commanderName);
			}

		}

		
		#endregion
		
		#region Interface

		public event System.Action<Unit> OnUnitSelectionChanged;
		public event System.Action<List<Unit>> OnAmbiguousSelectionStarted;
		public event System.Action OnAmbiguousSelectionEnded;
		public event System.Action<BattalionDescriptor> OnReserveTeamSelectionChanged;
		public ArrangementModal arrangementModal;
		
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

			factionActiveUnits[Faction.Player].Add(SpawnBattalion(battalionDescriptor,mapPosition));
			ClearReserveTeamSelection();
		}
		public bool IsUnitMoving { get; private set; } = false;

		public void MovingBattalion(Vector2Int mapPosition)
		{
			if (!IsUnitSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			if (IsUnitMoving) return;
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					RemoveUnitFromMap(SelectedUnit);
					SelectedUnit.position = mapPosition;
					SelectedUnit.transform.localPosition = MapToLocal(SelectedUnit.position);
					PlaceUnitOnMap(SelectedUnit, SelectedUnit.position);
					break;
				
				case Stage.Battle:
					if (CurrentActionStage != PlayerActionStage.MovingBattalion) break;
					StartCoroutine(PerformPlayerMove(SelectedUnit, mapPosition));
					break;
				
				default:
					break;
			}
			
		}
		
		private IEnumerator PerformPlayerMove(Unit unit, Vector2Int targetPos)
		{
			IsUnitMoving = true;
			
			if (targetPos == unit.position)
			{
				// 直接进入行动选择阶段
				ChangeActionStage(PlayerActionStage.SelectingAction);
				IsUnitMoving = false;
				yield break;
			}
			// 1. 计算路径
			// 注意：FindPath 需要在 Battle.cs 中实现 (之前为 AI 加的那个)
			List<Vector2Int> path = FindPath(unit.position, targetPos, unit);
	
			if (path != null && path.Count > 0)
			{
				// 2. 执行移动动画
				yield return StartCoroutine(MoveUnit(unit, path));
		
				// 3. 移动后逻辑
				if (targetPos != initialUnitPosition) // 检查是否真的动了
				{
					unit.hasMovedThisTurn = true;
				}
		
				// TODO: 移动实际减少移动力 (目前简化为不减，或者在 MoveUnit 里减)
				// unit.currentMovement -= path.Count; 

				ChangeActionStage(PlayerActionStage.SelectingAction);
			}
			else
			{
				Debug.LogError($"无法找到通往 {targetPos} 的路径！");
			}

			IsUnitMoving = false;
		}
		#endregion

		#region Selection
		private List<Unit> currentAmbiguousCandidates;
		public BattalionDescriptor SelectedBattalionDescriptor
		{
			get => CurrentBattalionDescriptor;
			set
			{
				if (value == CurrentBattalionDescriptor)
					return;
				
				if (value != null) ClearUnitSelection();
				
				CurrentBattalionDescriptor = value;
				IsReserveTeamSelected = (value != null);
				OnReserveTeamSelectionChanged?.Invoke(CurrentBattalionDescriptor);
				
			}
		}
		
		public Unit SelectedUnit
		{
			get => CurrentUnit;
			set
			{
				if (value == CurrentUnit) return;
				
				if (CurrentUnit != null)
					CurrentUnit.Selected = false;
				
				CurrentUnit = value;

				if (CurrentUnit != null)
				{
					CurrentUnit.Selected = true;
					//TODO: 打开行动面板
				}
				
				OnUnitSelectionChanged?.Invoke(CurrentUnit);
			}
		}
		
		public void ClearAllSelection()
		{
			ClearReserveTeamSelection();
			ClearUnitSelection();
		}
		
		public void ClearReserveTeamSelection()
		{
			SelectedBattalionDescriptor = null;
			IsReserveTeamSelected = false;
		}
		
		public void ClearUnitSelection()
		{
			SelectedUnit = null;
			IsUnitSelected = false;
			if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
			availableMovePositions.Clear();
		}

		public void ClearAmbiguousSelection()
		{
			currentAmbiguousCandidates = null;
			OnAmbiguousSelectionEnded?.Invoke();
		}
		public void InteractWithTile(Vector2Int gridPos)
		{
			if (!IsValidMapPosition(gridPos)) return;
			if (IsUnitMoving) return;
			if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget) 
				return;
			
			if (CurrentStage == Stage.Battle && CurrentTurnState != TurnState.PlayerTurn)
			{
				Debug.Log("Not your turn!");
				return;
			}
			
			//若在移动
			if (CurrentActionStage == PlayerActionStage.MovingBattalion && availableMovePositions.Contains(gridPos))
			{
				MovingBattalion(gridPos);
				return;
			}
			
			if (CurrentActionStage == PlayerActionStage.SelectingTarget && availableTargetPositions.Contains(gridPos))
			{
				// 如果目标格有多个可攻击对象（比如部队+设施），也需要进入歧义选择
				// 但为了简化，这里暂时保持之前的 ApplyAction 逻辑，或者你也在这里加入歧义判断
				// 这里先演示基础的“点击选中”逻辑的歧义处理
				ApplyAction(gridPos);
				return;
			}
			
			TileData tile = mapData[gridPos.x,gridPos.y];
			List<Unit> candidates = new List<Unit>();
			if (tile.Battalion != null) candidates.Add(tile.Battalion);
			if (tile.Facility != null) candidates.Add(tile.Facility);
			
			if (candidates.Count == 0)
			{
				if (CurrentActionStage == PlayerActionStage.None)
				{
					ClearAllSelection();
				}
				return;
			}
			
			 if (candidates.Count == 1)
			 {
			 	SelectUnit(candidates[0]);
			 }
			 else
			 {
			 	EnterAmbiguousState(candidates);
			 }
		}
		
		private void EnterAmbiguousState(List<Unit> candidates)
		{
			currentAmbiguousCandidates = candidates;
			ChangeActionStage(PlayerActionStage.SelectingAmbiguousTarget);
		}
		public void ResolveAmbiguousSelection(Unit selectedUnit)
		{

			ChangeActionStage(PlayerActionStage.None);
			
			SelectUnit(selectedUnit);
		}
		
		public void SelectUnit(Unit unit)
		{
			if (CurrentStage == Stage.Battle && CurrentTurnState != TurnState.PlayerTurn)
			{
				Debug.Log("Not your turn!");
				return;
			}

			SelectedUnit = unit;
			IsUnitSelected = true;
			
			if (IsReserveTeamSelected) 
				ClearReserveTeamSelection();
			
			
			if (!factionActiveUnits[Faction.Player].Contains(unit))
			{
				Debug.Log("Battalion " + unit.InstanceId + " is not your battalion.");
				if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
				return;
			}
			
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					break;
				
				case Stage.Battle:
					
					if (CurrentTurnState != TurnState.PlayerTurn)
					{
						Debug.Log("Not Your Turn!");
						return;
					}
					
					if (unit.actionDone)
					{
						Debug.Log("Battalion " + unit.InstanceId + " has already finished its action!");
						break;
					}
					
					if (unit is Battalion bat && bat.currentMovement == 0)
					{
						Debug.Log("Battalion " + bat.InstanceId + " has no movement!");
						break;
					}
					
					initialUnitPosition = SelectedUnit.position;
					if(unit is Battalion battalion)
					initialUnitMovement = battalion.currentMovement;
					//TODO 可移动的设施？
					if (CurrentActionStage == PlayerActionStage.None)
					{
						if (unit.unitDefinition.movable)
						{
							int moveRange = initialUnitMovement;
							availableMovePositions = GetAccessableTilesInRange(SelectedUnit, moveRange);
							ChangeActionStage(PlayerActionStage.MovingBattalion);
						}
						else if (unit.unitDefinition.actionable)
						{
							availableMovePositions.Clear();
							ChangeActionStage(PlayerActionStage.SelectingAction);
						}
					}
					
					break;
				
				default:
					break;
			}
			
		}
		#endregion
		
		
		#region Valid Check
		
		private HashSet<Vector2Int> availableMovePositions;
		private HashSet<Vector2Int> availableArrangementPositions;
		private HashSet<Vector2Int> availableTargetPositions;
		private HashSet<Unit> dirtyUnits = new HashSet<Unit>();
		
		public void MarkUnitDirty(Unit unit)
		{
			if (unit != null && !dirtyUnits.Contains(unit))
			{
				dirtyUnits.Add(unit);
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
		
		public bool HasAnyValidTarget(Unit user, ActionDefinition action)
		{
			if (user == null || action == null) return false;

			// 1. Self 类型：只检查自己脚下
			if (action.targetCountType == TargetCountType.Self)
			{
				return action.IsTileValidTarget(user, user.position);
			}

			// 2. 范围搜索：找到一个就返回 True
			Vector3Int centerCube = OffsetToCube(user.position);
			int N = action.range;
			int minN = action.minRange;

			for (int q = -N; q <= N; q++)
			{
				for (int r = -N; r <= N; r++)
				{
					for (int s = -N; s <= N; s++)
					{
						if (q + r + s == 0)
						{
							int dist = (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(s)) / 2;
							if (dist > N || dist < minN) continue;

							Vector3Int neighborCube = centerCube + new Vector3Int(q, r, s);
							Vector2Int neighborPos = CubeToOffset(neighborCube);

							if (IsValidMapPosition(neighborPos))
							{
								// [核心优化] 只要找到一个合法的，立刻返回 true
								if (action.IsTileValidTarget(user, neighborPos))
								{
									return true;
								}
							}
						}
					}
				}
			}

			return false; // 找遍了也没找到
		}
		
		public bool TestAvailableMovePositions(Vector2Int mapPosition)
		{
			return availableMovePositions.Contains(mapPosition);
		}
		
		public bool IsValidMapPosition(Vector2Int pos)
		{
			return pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y;
		}
		
		public bool IsTargetPositionValid(Vector2Int pos)
		{
			return availableTargetPositions != null && availableTargetPositions.Contains(pos);
		}
		public bool ValidateArrangementPlacement(Vector2Int placement)
		{
			if(!IsValidMapPosition(placement))
				return false;
			if (!availableArrangementPositions.Contains(placement)&&CurrentStage == Stage.Arrangement) 
				return false;
			
			UnitPassability terrainPass = hexTiles[placement].TerrainDefinition.unitPassability;
			if (terrainPass == UnitPassability.Impassable) 
				return false;
			
			if (mapData[placement.x, placement.y].Battalion != null)
				return false;
			
			return true;
		}
		
		
		public void CheckDeath(Unit unit)
		{
			if(unit is Battalion battalion && battalion.currentSoliders <= 0)
			{
				RemoveUnitFromBattle(battalion);
				Debug.Log($"Battalion {battalion.InstanceId} die off!");
				return;
			}
			else if (unit is Facility facility && facility.currentDurability<=0)
			{
				RemoveUnitFromBattle(facility);
				Debug.Log($"Facility {facility.InstanceId} destroyed!");
				return;
			}

			return;
		}
		
		
		
		#endregion
		
		#region Stages

		#region Stage Variables
		
		public Stage CurrentStage{ get; set; }
		
		public bool IsInArrangementStage { get; set; } = false;
		public bool IsInBattleStage { get; set; }= false;
		public bool IsReserveTeamSelected { get; set; }= false;
		public bool IsUnitSelected { get; set; }= false;

		#endregion
		
		public void ProceedToNextStage()
		{
			switch(CurrentStage)
			{
				case Stage.Preparation:
					ChangeStage(Stage.Arrangement);
					break;
				case Stage.Arrangement:
					ChangeStage(Stage.Battle);
					break;
				case Stage.Battle:
					ChangeStage(Stage.Settlement);
					break;
				default:
					break;
			}
		}
		public void ChangeStage(Stage stage)
		{
			OnExitStage(CurrentStage);
			
			CurrentStage = stage;
			OnEnterStage(CurrentStage);
		}
		
		void OnEnterStage(Stage stage)
		{
			switch (stage)
			{
				case Stage.Arrangement:
					Debug.Log("OnEnter: 布置阶段");
					HighlightTiles(availableArrangementPositions,arrangementHighlightColor);
					break;
				case Stage.Battle:
					BattleStageInitialize();
					battleLoopCoroutine = StartCoroutine(BattleTurnLoop());
					Debug.Log("OnEnter: 战斗阶段");
					break;
				case Stage.Settlement:
					Debug.Log("OnEnter: 结算阶段");
					break;
			}
		}
		
		void OnExitStage(Stage stage)
		{
			switch (stage)
			{
				case Stage.Arrangement:
					Debug.Log("OnExit: 布置阶段");
					ClearAllHexHighlights();
					ClearAllSelection();
					break;
				case Stage.Battle:
					if (battleLoopCoroutine != null)
					{
						StopCoroutine(battleLoopCoroutine);
						battleLoopCoroutine = null;
					}
					Debug.Log("OnExit: 战斗阶段");
					break;
			}
		}
		
		public bool CheckGameOver()
		{
			//TODO：加入游戏结束判断
			// if (TurnCount > 3)
			// {
			// 	ChangeStage(Stage.Settlement);
			// 	return true;
			// }
			return false;
		}
		
		#region Preparation
		
		
		
		#endregion
		
		#region Arrangement
		
		#endregion
		
		#region Battle

		public void BattleStageInitialize()
		{
			
		}

		
		#endregion
		
		#region Settlement

		
		#endregion
		
		#endregion
		
		#region Turn
		
		//战斗阶段回合管理
		
		public bool IsPlayerTurnOver { get; set; }
		
		public TurnState CurrentTurnState{ get; set; }
		
		private Coroutine battleLoopCoroutine;
		public int TurnCount { get; private set; }
		private IEnumerator BattleTurnLoop()
		{
			Debug.Log("Battle Start!");
			while (true)
			{
				
				CurrentTurnState = TurnState.PlayerTurn;
				if (BattleEventManager.Instance != null)
					BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnTurnStart);
				
				yield return StartCoroutine(PlayerTurnCoroutine());
				
				if (BattleEventManager.Instance != null)
					BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnTurnEnd);
				
				CheckBattleEnd();
				if (CurrentStage == Stage.Settlement) yield break;
				
				CurrentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
				
				CurrentTurnState = TurnState.EnemyTurn;
				yield return StartCoroutine(EnemyTurnCoroutine());
				
				CheckBattleEnd();
				if (CurrentStage == Stage.Settlement) yield break;
				
				CurrentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
				
				UpdateAllTileEffects(); 
				//UpdateAllUnitBuffs(); 
			}

		}
		private IEnumerator PlayerTurnCoroutine()
		{
			IsPlayerTurnOver = false;
			TurnCount++;
			Debug.Log("Player Turn!");

			foreach (var unit in factionActiveUnits[Faction.Player])
			{
				unit.OnTurnStart();
			}
			//
			OnPlayerTurnStarted?.Invoke();

			while (!IsPlayerTurnOver)
			{
				yield return null;
			}
			Debug.Log("Player Turn End!");
			ChangeActionStage(PlayerActionStage.None);
			foreach (var unit in factionActiveUnits[Faction.Player])
			{
				//改成实际数值
				unit.selected = false;
			}
			OnPlayerTurnEnded?.Invoke();
		}

		private IEnumerator EnemyTurnCoroutine()
		{
			Debug.Log("Enemy Turn!");
			yield return new WaitForSeconds(1.0f); 
			
			
			// 获取所有敌方单位
			List<Unit> enemyUnits = new List<Unit>(factionActiveUnits[Faction.Enemy]);
			
			foreach (var unit in enemyUnits)
			{
				if (unit != null && unit.gameObject.activeSelf)
				{
					unit.OnTurnStart();
				}
			}
				
			// 确保 AIController 存在
			if (AIController.Instance == null)
			{
				// 如果场景里没挂，临时挂一个
				gameObject.AddComponent<AIController>();
			}

			// 交给 AIController 处理
			yield return StartCoroutine(AIController.Instance.ProcessTurn(enemyUnits));
			
			Debug.Log("Enemy Turn End!");
			
			// 重置状态
			foreach (var unit in factionActiveUnits[Faction.Enemy])
			{
				unit.actionDone = false;
			}
		}
		
		public void EndPlayerTurn()
		{
			if (CurrentTurnState == TurnState.PlayerTurn)
			{
				
				
				if (CurrentActionStage == PlayerActionStage.SelectingTarget)
				{
					CancelAction();
					ChangeActionStage(PlayerActionStage.SelectingAction);
				}
				if (CurrentActionStage == PlayerActionStage.SelectingAction)
				{
					CancelMovement();
					ChangeActionStage(PlayerActionStage.MovingBattalion);
				}
				if (CurrentActionStage == PlayerActionStage.MovingBattalion)
				{
					ClearAllSelection();
					ChangeActionStage(PlayerActionStage.None);
				}
				ClearAllHexHighlights();
				IsPlayerTurnOver = true;
			}
			else Debug.LogError("It's not player's turn!");
		}
		
		#region Turnevent Registeration
		
		public event System.Action OnPlayerTurnStarted;
		public event System.Action OnPlayerTurnEnded;
		public event System.Action OnActionSelectionStarted;
		public event System.Action OnActionSelectionEnded;
		
		#endregion
		
		#endregion
		
		#region PlayerAction
		
		public bool IsPreparingAction{ get; set; }
		
		private Vector2Int initialUnitPosition;

		private int initialUnitMovement;
		public PlayerActionStage CurrentActionStage{ get; set; }
		
		public ActionDefinition CurrentAction { get; private set; }

		public void CancelMovement()
		{
			RemoveUnitFromMap(SelectedUnit);
			SelectedUnit.position = initialUnitPosition;
			PlaceUnitOnMap( SelectedUnit,initialUnitPosition);
			if(SelectedUnit is Battalion bat) bat.currentMovement = initialUnitMovement;
			SelectedUnit.transform.localPosition = MapToLocal(initialUnitPosition);
			SelectedUnit.hasMovedThisTurn = false;
			availableMovePositions = GetAccessableTilesInRange(SelectedUnit, initialUnitMovement);
		}

		public void CancelAction()
		{
			availableTargetPositions.Clear();
			CurrentAction = null;
			IsPreparingAction = false;
			ClearAllHexHighlights();
		}
		public void ChangeActionStage(PlayerActionStage stage)
		{
			if (CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget)
			{
				OnAmbiguousSelectionEnded?.Invoke();
				currentAmbiguousCandidates = null;
			}
			
			if (CurrentActionStage == PlayerActionStage.SelectingAction)
			{
				OnActionSelectionEnded?.Invoke();
			}
			
			CurrentActionStage = stage;
			switch (stage)
			{
				case PlayerActionStage.None:
					Debug.Log("Change action stage to None");
					ClearAllSelection();
					ClearAllHexHighlights();
					break;
				
				case PlayerActionStage.MovingBattalion:
					Debug.Log("Change action stage to MovingBattalion");
					ClearAllHexHighlights();
					HighlightTiles(availableMovePositions,movementHighlightColor);
					break;
				
				case PlayerActionStage.SelectingAction:
					Debug.Log("Change action stage to SelectingAction");
					ClearAllHexHighlights();
					OnActionSelectionStarted?.Invoke();
					//TODO:单位处悬浮菜单，锁定滚动
					break;
				
				case PlayerActionStage.SelectingTarget:
					if (CurrentAction != null)
					{
						availableTargetPositions = GetValidActionTargetTiles(SelectedUnit, CurrentAction);
						HighlightTiles(availableTargetPositions, attackHighlightColor); // 建议改个名，比如 targetHighlightColor
						Debug.Log($"进入目标选择阶段: {CurrentAction.actionName}, 可选目标数: {availableTargetPositions.Count}");
					}
					else
					{
						Debug.LogError("进入选择目标阶段，但 CurrentAction 为空！");
						ChangeActionStage(PlayerActionStage.SelectingAction);
					}
					break;
				
				case PlayerActionStage.SelectingAmbiguousTarget:
					Debug.Log("Change action stage to SelectingAmbiguousTarget");
					ClearAllHexHighlights();
					// 触发事件，把刚才存下来的列表发给 UI
					OnAmbiguousSelectionStarted?.Invoke(currentAmbiguousCandidates);
					break;
			}
		}
		
		public void ActionWait()
		{
			
			SelectedUnit.actionDone = true;
			ClearAllSelection();
			ChangeActionStage(PlayerActionStage.None);
			
		}
		
		public void PrepareAction(ActionDefinition action)
		{
			if (action == null) return;

			IsPreparingAction = true;
			CurrentAction = action;
			
			ChangeActionStage(PlayerActionStage.SelectingTarget);
		}
		
		public void ApplyAction(Vector2Int mapPosition)
		{
			if (!IsUnitSelected || CurrentAction == null)
			{
				Debug.LogWarning("No unit selected or no action prepared.");
				return;
			}

			if (!CurrentAction.IsTileValidTarget(SelectedUnit, mapPosition))
			{
				Debug.Log($"位置 {mapPosition} 无效。");
				return;
			}
			
			ExecuteActionLogic(SelectedUnit, mapPosition);
		}

		private void ExecuteActionLogic(Unit source, Vector2Int targetPos)
		{

			bool success = CurrentAction.Perform(source, targetPos);

			if (success)
			{
				ResolveDirtyUnits();

				if (SelectedUnit) SelectedUnit.actionDone = true;
				ClearAllSelection();
				ChangeActionStage(PlayerActionStage.None);
				
				if (BattleEventManager.Instance != null)
					BattleEventManager.Instance.OnEventTrigger(BattleEventTriggerType.OnPlayerActionEnd);

			}
		}
		
		public IEnumerator MoveUnit(Unit unit, List<Vector2Int> path)
		{
			if (unit == null || path == null || path.Count == 0) yield break;

			// 1. 标记移动开始
			unit.hasMovedThisTurn = true;
			
			// 2. 逐步移动 (简单的协程动画)
			foreach (var pos in path)
			{
				// 移除旧位置占位
				RemoveUnitFromMap(unit);
				
				// 更新数据坐标
				unit.position = pos;
				
				// 放置新位置占位
				PlaceUnitOnMap(unit, pos);
				
				// 更新视觉位置 (简单的插值动画)
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
			}
			
			// 3. 移动结束处理
			unit.OnUnitStateChanged();
		}
		#endregion
		
		#region View Control
		public Transform anchor;
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

		[SerializeField] new CinemachineVirtualCamera camera;
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
		#endregion
		
		#region Grid Map
		
		public Grid hexgrid;
		public float Xscale;
		public float Yscale;
		
		public TileData[,] mapData; 
		public GameObject HextilePrefab;
		private Dictionary<Vector2Int,HexTile> hexTiles = new();
		
		void GenerateHexGrid()
		{
			Quaternion hexRotation = Quaternion.Euler(0, 30, 0);
			if(HextilePrefab == null)
			{
				Debug.LogError("Hextile prefab is not assigned!");
				return;
			}
			Transform mapContainer = new GameObject("HexMapContainer").transform;
			mapContainer.SetParent(transform, false);
			
			for (int y = 0; y < Size.y; y++)
			{
				for (int x = 0; x < Size.x; x++)
				{
					Vector2Int mapPos = new Vector2Int(x, y);

					Vector3 worldPos = MapToLocal(mapPos); 

					GameObject tileObject = Instantiate(HextilePrefab, worldPos, hexRotation, mapContainer);
					tileObject.name = $"Hex Tile ({x}, {y})";
            
					HexTile hexTile = tileObject.GetComponent<HexTile>();
					hexTile.mapPosition = mapPos;
					hexTiles.Add(mapPos, hexTile);
					//TODO
					if (presetMapData != null)
					{
						// 从预设数据中读取地形 ID
						string terrainId = presetMapData.GetTerrainAt(x, y);
						AssignTerrainToTile(hexTile, terrainId);
					}
					else
					{
						// 如果没拖地图，默认全是平原
						AssignTerrainToTile(hexTile, "Plain");
					}
				}
			}
		}
		public void Debug_SetMapBorderAsExtraction()
		{
			if (mapData == null) return;

			for (int x = 0; x < Size.x; x++)
			{
				for (int y = 0; y < Size.y; y++)
				{
					// 边缘判定：x=0, x=max, y=0, y=max
					if (x == 0 || x == Size.x - 1 || y == 0 || y == Size.y - 1)
					{
						CreateExtractionPoint(new Vector2Int(x, y));
					}
				}
			}
			Debug.Log("Debug: Map borders set as extraction points.");
		}

		public void AssignTerrainToTile(HexTile tile, string terrainType)
		{
			TerrainDefinition def = TerrainDatabase.Instance.GetTerrain(terrainType);
			if (def != null)
			{
				tile.SetTerrain(def);
				mapTerrainData[tile.mapPosition.x, tile.mapPosition.y] = terrainType;
			}
		}
		public int GetHexDistance(Vector2Int a, Vector2Int b)
		{
			Vector3Int ac = OffsetToCube(a);
			Vector3Int bc = OffsetToCube(b);
			return (Mathf.Abs(ac.x - bc.x) + Mathf.Abs(ac.y - bc.y) + Mathf.Abs(ac.z - bc.z)) / 2;
		}
		
		private Vector3Int OffsetToCube(Vector2Int hex)
		{
			var q = hex.x - (hex.y - (hex.y & 1)) / 2;
			var r = hex.y;
			return new Vector3Int(q, r, -q - r);
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
		
		public bool CanUnitStopOnTile(Unit unit, Vector2Int pos)
		{
			if (!IsValidMapPosition(pos)) return false;
			TileData tile = mapData[pos.x, pos.y];
			//假如目标地点上有单位，则不可停驻
			if (tile.Battalion&& tile.Battalion != unit) return false;
			
			//假如目标地点有设施，则设施的可通行性覆盖地形本身的可通行性
			//否则，考虑地形本身的可通行性
			UnitPassability p;
			if (tile.Facility) p = tile.Facility.Definition.passability;
			else p = hexTiles[pos].TerrainDefinition.unitPassability;

			return p switch
			{
				UnitPassability.Impassable => false,
				UnitPassability.Passable => false,
				UnitPassability.AlliesPassable => false,
				UnitPassability.Stoppable => true,
				UnitPassability.AlliesStoppable => tile.Facility.faction == unit.faction,
				_ => true,
			};
		}
		
		public bool CanUnitPassThroughTile(Unit unit, Vector2Int pos)
		{
			if (!IsValidMapPosition(pos)) return false;
			TileData tile = mapData[pos.x, pos.y];
			if (tile.Battalion)
			{
				if (tile.Battalion.faction == unit.faction)
				{
					if (tile.Battalion.Definition.passability == UnitPassability.Impassable) return false;
					return true;
				}
				else return false;
			}
			UnitPassability p;
			if (tile.Facility) p = tile.Facility.Definition.passability;
			else p = hexTiles[pos].TerrainDefinition.unitPassability;

			return p switch
			{
				UnitPassability.Impassable => false,
				UnitPassability.Passable => true,
				UnitPassability.Stoppable => true,
				UnitPassability.AlliesPassable or UnitPassability.AlliesStoppable => tile.Facility.faction == unit.faction,
				_ => true,
			};
		}
		
		private Vector2Int GetRandomValidPosition(UnitPassability passability)
		{
			int x = Random.Range(0, Size.x);
			int y = Random.Range(0, Size.y);
			
			int attempts = 0;
			int maxAttempts = 1000;
			
			while (attempts < maxAttempts)
			{
				Vector2Int pos = new Vector2Int(x, y);
                
				// 1. 检查格子是否有单位 (现有逻辑)
				bool isOccupied = mapData[x, y].Battalion != null || mapData[x, y].Facility != null;

				UnitPassability terrainPass = hexTiles[pos].TerrainDefinition.unitPassability;
				bool isTerrainWalkable = (terrainPass == UnitPassability.Stoppable || terrainPass == UnitPassability.Passable); 

				if (!isOccupied && isTerrainWalkable)
				{
					return pos;
				}

				// 重试
				x = Random.Range(0, Size.x);
				y = Random.Range(0, Size.y);
				attempts++;
			}
			
			return new Vector2Int(x,y);
		}

		void GenerateArrangementSlot()
		{
			//TODO:根据玩家进入战斗的角度，在合适的位置创建部署区
			for(int i= 0;i < 3;i++)
			for (int j = 0; j < 3; j++)
			{
				availableArrangementPositions.Add(new Vector2Int(i, j));
			}
		}
		public void AddTileEffect(Vector2Int pos, TileEffectDefinition def, int duration, Unit source)
		{
			if (!IsValidMapPosition(pos)) return;
			TileData tile = mapData[pos.x, pos.y];

			// 1. 创建数据实例
			TileEffect effect = new TileEffect(def, duration, source);

			// 2. 生成视觉特效
			if (def.vfxPrefab != null)
			{
				Vector3 worldPos = MapToLocal(pos);
				// 稍微抬高一点防止穿模，或者依靠Prefab自带偏移
				GameObject vfx = Instantiate(def.vfxPrefab, transform); 
				vfx.transform.localPosition = worldPos;
				effect.vfxInstance = vfx;
			}

			// 3. 加入数据
			tile.Effects.Add(effect);
			Debug.Log($"Tile {pos} added effect: {def.effectName}");
		}
		
		public void UpdateAllTileEffects()
		{
			for (int x = 0; x < Size.x; x++)
			{
				for (int y = 0; y < Size.y; y++)
				{
					UpdateTileEffectsAt(new Vector2Int(x, y));
				}
			}
		}
		
		private void UpdateTileEffectsAt(Vector2Int pos)
		{
			TileData tile = mapData[pos.x, pos.y];
			if (tile.Effects.Count == 0) return;

			for (int i = tile.Effects.Count - 1; i >= 0; i--)
			{
				TileEffect effect = tile.Effects[i];

				if (effect.definition != null)
				{
					effect.definition.OnTick(tile, pos);
				}

				effect.currentDuration--;
				if (effect.currentDuration <= 0)
				{
					// 销毁特效物体
					if (effect.vfxInstance != null) Destroy(effect.vfxInstance);
					tile.Effects.RemoveAt(i);
				}
			}
		}
		
		private readonly Vector2Int[][] neighborOffsets = new Vector2Int[][]
		{
			// 偶数行 (y % 2 == 0) 的邻居偏移
			new Vector2Int[] 
			{ 
				new Vector2Int(0, 1),  // 右上
				new Vector2Int(1, 0),  // 右
				new Vector2Int(-1, -1), // 右下
				new Vector2Int(0, -1), // 左下
				new Vector2Int(-1, 0), // 左
				new Vector2Int(-1, 1)   // 左上
			},
			// 奇数行 (y % 2 != 0) 的邻居偏移
			new Vector2Int[] 
			{ 
				new Vector2Int(1, 1),   // 右上
				new Vector2Int(1, 0),   // 右
				new Vector2Int(1, -1),  // 右下
				new Vector2Int(0, -1), // 左下
				new Vector2Int(-1, 0),  // 左
				new Vector2Int(0, 1)  // 左上
			}
		};
		
		public Vector2 WorldToMap(Vector3 world)
		{
			Vector3Int gridPos = hexgrid.WorldToCell(world);
			return new(
				gridPos.x ,
				gridPos.y 
			);
		}
		public Vector2Int WorldToMapInt(Vector3 world)
		{
			//return Vector2Int.FloorToInt(WorldToMap(world));
			Vector3Int gridPos = hexgrid.WorldToCell(world);
			return new Vector2Int(gridPos.x, gridPos.y);
		}
		public Vector3 MapToWorld(Vector2Int map)
		{
			//return transform.localToWorldMatrix.MultiplyPoint(MapToLocal(map));
			Vector3Int gridPos = new Vector3Int(map.x, map.y, 0);
			return hexgrid.GetCellCenterWorld(gridPos);
		}
		public Vector3 MapToLocal(Vector2 map)
		{
			return hexgrid.CellToLocalInterpolated(new(
				map.x,
				map.y,
				0
			));
		}

		#endregion
		
		#region Units
		
		#region Unit Container
		
		public List<BattalionDescriptor> playerReserveTeam;
		
		private Dictionary<Faction,HashSet<Unit>> factionActiveUnits;
		public List<Unit> retreatedUnits = new List<Unit>();
		public List<Unit> deadUnits = new List<Unit>();
		
		#endregion
		public BattalionDescriptor CurrentBattalionDescriptor{ get; set; }
		public Unit CurrentUnit{ get; set; }
		
		public BattalionDefinition defaultReserveTeamDefinition;
		public BattalionDefinition defaultEnemyDefinition;
		
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
		
		public TUnit SpawnUnit<TUnit, TDef, TDesc>(TDesc descriptor, Vector2Int pos) 
			where TUnit : Unit<TDef>
			where TDef : UnitDefinition
			where TDesc : UnitDescriptor
		{
			// 1. 创建 GameObject
			var go = new GameObject($"{typeof(TUnit).Name}_{descriptor.Definition.unitName}");

			// 2. 挂载组件
			var unit = go.AddComponent<TUnit>();
            
			// 3. 通用数据初始化
			unit.Definition = (TDef)descriptor.Definition; 
			unit.InstanceId = descriptor.instanceId;
			unit.faction = descriptor.faction;
			unit.position = pos;
            
			// 4. 特殊初始化 (通过虚方法或类型判断)
			if (unit is Facility fac)
			{
				// 假设 FacilityDefinition 有 maxDurability 字段，或者是复用 defaultMaxSolider
				fac.currentDurability = fac.Definition.defaultMaxDurability; 
				// 你也可以在 Facility 类里写一个 Initialize() 方法来处理这些
			}
			else if (unit is Battalion bat)
			{
				// 部队属性初始化
				bat.currentSoliders = bat.Definition.defaultMaxSolider; // 默认满员，如果是预备队生成，后续会覆盖
				bat.currentMurale = bat.Definition.defaultMaxMorale;
				// 注意：BattalionDescriptor 里的数据会在 SpawnBattalion 的包装层里覆盖这里
			}
			
			unit.CalculateEntryStats(descriptor);
			
			// 5. 视觉初始化
			SetupUnitVisuals(unit);
            
			// 6. 放置到地图数据层
			PlaceUnitOnMap(unit, pos);
            
			// 7. 设置 Transform 父物体和位置
			unit.transform.SetParent(transform, false);
			unit.transform.localPosition = MapToLocal(pos);
            
			// 8. 技能初始化
			InitializeUnitActions(unit);

			return unit;
		}
		
		Battalion SpawnBattalion(BattalionDescriptor descriptor, Vector2Int position)
		{
			descriptor.instanceId = descriptor.armyId;
			Battalion battalion = SpawnUnit<Battalion, BattalionDefinition,BattalionDescriptor>(
				descriptor,
				position
			);

			// [覆盖特定数据] (因为 Descriptor 里存了存档数据，比如兵力不一定是满的)
			battalion.battalionCommander = descriptor.battalionCommander;
			battalion.currentSoliders = descriptor.currentSoliders;
			battalion.currentMurale = descriptor.currentMurale;
			battalion.currentTraining = descriptor.currentTraining;
            
			// 加入活跃列表
			factionActiveUnits[descriptor.faction].Add(battalion);
            
			// 标记已放置
			descriptor.placed = true;
            
			// 再次初始化技能 (因为 Commander 赋值了，可能有了新技能)
			// 虽然 SpawnUnit 里调了一次，但那时还没 Commander，所以得刷新一下
			InitializeUnitActions(battalion); 

			return battalion;
		}
		
		public void InitializeUnitActions(Unit unit)
		{
			//默认攻击（对于设施来说，默认攻击不显示，因此等于没有）
			unit.DefaultAttack = unit.unitDefinition.defaultAttack;
			unit.DefaultRetreat = unit.unitDefinition.defaultRetreat;
			unit.DefaultInteract = unit.unitDefinition.defaultInteract;
			
			//单位原生行动
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
				
				Debug.Log($"Unit {unit.name} has {unit.runtimeUnitActions.Count} actions.");
			}
			//对于部队，还有来自指挥官的行动
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
			else
			{
				// 如果是特殊的阵营（比如 Neutral），且没有初始化进字典，这里会漏掉
				// 建议确保 InitializeData 里所有 Faction 都初始化了
			}
			unit.gameObject.SetActive(false);
			unit.transform.SetParent(null);
			
			if (!deadUnits.Contains(unit))
			{
				deadUnits.Add(unit);
			}
			
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

		public Vector3Int GetHexDirection(Vector2Int start, Vector2Int target)
		{
			Vector3Int startCube = OffsetToCube(start);
			Vector3Int targetCube = OffsetToCube(target);
			Vector3Int diff = targetCube - startCube;
			
			int len = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));
			if (len == 0) return Vector3Int.zero;

			return new Vector3Int(diff.x / len, diff.y / len, diff.z / len);
		}

		public Vector2Int GetTileInDirection(Vector2Int start, Vector3Int directionCube, int distance)
		{
			Vector3Int startCube = OffsetToCube(start);
			Vector3Int destCube = startCube + (directionCube * distance);
			return CubeToOffset(destCube);
		}
		
		public HashSet<Unit> GetUnitsByFaction(Faction faction)
		{
			if (factionActiveUnits.TryGetValue(faction, out var units))
			{
				return units;
			}
			return new HashSet<Unit>();
		}
		
		public Unit FindNearestUnit(Unit source, Faction targetFaction, UnitTypeFilter typeFilter = UnitTypeFilter.All)
		{
			Unit nearest = null;
			int minDist = int.MaxValue;

			var targets = GetUnitsByFaction(targetFaction);

			foreach (var targetUnit in targets)
			{
				if (targetUnit == null || !targetUnit.gameObject.activeSelf) continue;

				bool isTypeMatch = false;
				switch (typeFilter)
				{
					case UnitTypeFilter.All:
						isTypeMatch = true;
						break;
					case UnitTypeFilter.BattalionOnly:
						isTypeMatch = (targetUnit is Battalion);
						break;
					case UnitTypeFilter.FacilityOnly:
						isTypeMatch = (targetUnit is Facility);
						break;
				}
				
				if (!isTypeMatch) continue;
				
				int d = GetHexDistance(source.position, targetUnit.position);
        
				if (d < minDist)
				{
					minDist = d;
					nearest = targetUnit;
				}
			}
			return nearest;
		}
		
		#endregion
		
		#region Range
		
		public HashSet<Vector2Int> GetAccessableTilesInRange(Unit movingUnit, int range)
		{
			
			HashSet<Vector2Int> validDestinations = new HashSet<Vector2Int>();
			if (!movingUnit) return validDestinations;
			
			Vector2Int startPos = movingUnit.position;
			
			validDestinations.Add(startPos); 
			if (!IsValidMapPosition(startPos)) return validDestinations;
			
			
			if (!hexTiles.ContainsKey(startPos))
			{
				Debug.LogWarning($"尝试从一个不存在的格子 {startPos} 开始寻路。");
				return validDestinations;
			}
			
			HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();

			Queue<Vector2Int> frontier = new Queue<Vector2Int>();
			frontier.Enqueue(startPos);
			
			Dictionary<Vector2Int, int> CostSofar = new Dictionary<Vector2Int, int>();
			CostSofar[startPos] = 0;
			
			while (frontier.Count > 0)
			{
				Vector2Int currentPos = frontier.Dequeue();

				if (currentPos != startPos)
				{
					if(CanUnitStopOnTile(movingUnit,currentPos))
						validDestinations.Add(currentPos);
				}
				if(CanUnitPassThroughTile(movingUnit, currentPos)) reachableTiles.Add(currentPos);
				
				if (CostSofar[currentPos] >= range) continue;
				
				int parity = currentPos.y & 1;
				foreach (var offset in neighborOffsets[parity])
				{
					Vector2Int neighborPos = currentPos + offset;
					
					if (!CanUnitPassThroughTile(movingUnit, neighborPos)) continue;
 
					// int moveCost = TerrainDatabase.Instance.GetTerrain(mapTerrainData[neighborPos.x, neighborPos.y]).movementCost;
					int moveCost = 1; 
					int newCost = CostSofar[currentPos] + moveCost;

					if (newCost <= range && !CostSofar.ContainsKey(neighborPos))
					{
						CostSofar[neighborPos] = newCost;
						frontier.Enqueue(neighborPos);
					}
				}
			}

			return validDestinations;
		}
		
		// 简单的 BFS 寻路，返回路径列表
		public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, Unit unit)
		{
			List<Vector2Int> path = new List<Vector2Int>();
			if (start == end) return path;

			Queue<Vector2Int> frontier = new Queue<Vector2Int>();
			frontier.Enqueue(start);
			
			Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
			cameFrom[start] = start; // 标记起点

			while (frontier.Count > 0)
			{
				Vector2Int current = frontier.Dequeue();

				if (current == end) break;

				int parity = current.y & 1;
				foreach (var offset in neighborOffsets[parity])
				{
					Vector2Int next = current + offset;
					
					if (!IsValidMapPosition(next)) continue;
					if (cameFrom.ContainsKey(next)) continue;
					
					// 检查通行性 (如果是终点，允许停留检查；如果是中间点，允许穿过检查)
					bool isEnd = (next == end);
					if (isEnd)
					{
						if (!CanUnitStopOnTile(unit, next)) continue;
					}
					else
					{
						if (!CanUnitPassThroughTile(unit, next)) continue;
					}

					frontier.Enqueue(next);
					cameFrom[next] = current;
				}
			}

			if (!cameFrom.ContainsKey(end)) return null; // 无法到达

			// 重建路径
			Vector2Int curr = end;
			while (curr != start)
			{
				path.Add(curr);
				curr = cameFrom[curr];
			}
			path.Reverse();
			return path;
		}
		
		public HashSet<Vector2Int> GetValidActionTargetTiles(Unit user, ActionDefinition action)
        {
            HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
            
            // 如果是 Self 类型，只返回自己脚下
            if (action.targetCountType == TargetCountType.Self)
            {
                validTiles.Add(user.position);
                return validTiles;
            }
            
            Vector3Int centerCube = OffsetToCube(user.position);
            int N = action.range;
            int minN = action.minRange; // 假设你有最小射程

            for (int q = -N; q <= N; q++)
            {
                for (int r = -N; r <= N; r++)
                {
                    for (int s = -N; s <= N; s++)
                    {
                        if (q + r + s == 0)
                        {
                            // 计算距离
                            int dist = (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(s)) / 2;
                            if (dist > N || dist < minN) continue;

                            // 转换回 Offset 坐标
                            Vector3Int neighborCube = centerCube + new Vector3Int(q, r, s);
                            Vector2Int neighborPos = CubeToOffset(neighborCube); // 需要添加 CubeToOffset 辅助方法

                            if (IsValidMapPosition(neighborPos))
                            {
	                            if (action.IsTileValidTarget(user, neighborPos))
	                            {
		                            validTiles.Add(neighborPos);
	                            }
                            }
                        }
                    }
                }
            }
            return validTiles;
        }
		public Vector2Int CubeToOffset(Vector3Int cube)
		{
			var col = cube.x + (cube.y - (cube.y & 1)) / 2;
			var row = cube.y;
			return new Vector2Int(col, row);
		}
		
		public HashSet<Vector2Int> GetAllTilesInRange(Vector2Int startPos, int range)
		{
			HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();
			
			if (!hexTiles.ContainsKey(startPos))
			{
				Debug.LogWarning($"Function GetAllTilesInRange: 基准位置 {startPos} 不存在。");
				return reachableTiles;
			}
			
			Queue<Vector2Int> frontier = new Queue<Vector2Int>();
			frontier.Enqueue(startPos);
			
			Dictionary<Vector2Int, int> distanceTravelled = new Dictionary<Vector2Int, int>();
			distanceTravelled[startPos] = 0;
			
			while (frontier.Count > 0)
			{
				Vector2Int currentPos = frontier.Dequeue();
				
				reachableTiles.Add(currentPos);
				
				if (distanceTravelled[currentPos] >= range) continue;
				
				int parity = currentPos.y & 1;
				foreach (var offset in neighborOffsets[parity])
				{
					Vector2Int neighborPos = currentPos + offset;

					if (hexTiles.ContainsKey(neighborPos) && !distanceTravelled.ContainsKey(neighborPos))
					{
						distanceTravelled[neighborPos] = distanceTravelled[currentPos] + 1;
						frontier.Enqueue(neighborPos);
					}
				}
			}
    
			return reachableTiles;
		}
		
		# endregion
		
		#region Visual
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
