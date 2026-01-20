using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor.Build.Pipeline.Tasks;

namespace LongLiveKhioyen
{
	public class Battle : MonoBehaviour
	{
		static Battle _instance;
		public static Battle Instance => _instance;
		public System.Action onInitialized;
		public ActionDefinitionSheet actionDataBase;
		private CommanderRegistry commanderRegistry;
		public string[,] mapTerrainData; 
		BattleResult battleResult;
		#region General Config

		public Color movementHighlightColor = Color.green; 
		public Color arrangementHighlightColor = Color.blue;
		public Color attackHighlightColor = Color.red;
		AudioSource audioSource;
		#endregion
		
		#region Battle data

		//TODO:加载战斗数据
		public BattleMetaData data;
		public ArmyStatus armyStatus;
		public Vector2Int Size => data.battleSize;
		public string BattleName => data.battleName;

		private void LoadBattleMetaData()
		{
			//TODO
		}
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
		}
		
		#endregion
		
		#region Initialization
		
		
		private void ImportArmyData()
		{
			ArmyStatus armyStatus = ArmyStatus.Instance;

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
			GenerateDetailedMap();
			GenerateArrangementSlot();
		}
		
		private void InitializeComponent()
		{
			audioSource = GetComponent<AudioSource>();
		}
		
		private void InitializeData()
		{
			armyStatus = ArmyStatus.Instance;
			actionDataBase.Initialize();
			commanderRegistry = CommanderRegistry.Instance; 
			
			battleResult = new BattleResult();
			
			mapData = new TileData[Size.x, Size.y];
			for(int x=0; x<Size.x; x++)
				for(int y=0; y<Size.y; y++)
					mapData[x,y] = new TileData();
			
			mapTerrainData = new string[Size.x, Size.y];
			
			availableMovePositions = new HashSet<Vector2Int>();
			availableArrangementPositions = new HashSet<Vector2Int>();
			availableTargetPositions = new HashSet<Vector2Int>();
			
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
			
			#if BATTLE_TEST
			for (int i = 0; i < data.enemyCount; i++)
			{
				BattalionDescriptor battalionDescriptor = new()
				{
					armyId = -1,
					Definition = defaultEnemyDefinition,
					faction = Faction.Enemy,
					battalionCommander = null,
					currentSoliders = defaultEnemyDefinition.defaultMaxSolider,
					currentMurale = defaultEnemyDefinition.defaultMaxMorale,
					currentTraining = 0,
					maxSolider = defaultEnemyDefinition.defaultMaxSolider,
					maxMorale = defaultEnemyDefinition.defaultMaxMorale,
					maxTraining = 0,
					placed = false
				};
				enemyReserveTeam.Add(battalionDescriptor);
			}
			
			
			
			foreach (BattalionDescriptor battalionDescriptor in enemyReserveTeam)
			{
				Vector2Int pos = GetRandomValidPosition(UnitPassability.Stoppable);
				while (availableArrangementPositions.Contains(pos))
				{
					pos = GetRandomValidPosition(UnitPassability.Stoppable);
				}
				factionActiveUnits[Faction.Enemy].Add(SpawnBattalion(battalionDescriptor,pos));
			}
			#else 
			GenerateEnemyData();
			#endif
			
			
			//TODO 
		}

		
		#endregion
		
		#region Test
		
		public int testPlayerReserveTeamCount;
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
			ArmyStatus armyStatus = ArmyStatus.Instance;
			if(armyStatus == null) Debug.LogError("ArmyStatus is null!");
			armyStatus.armyCommander = CommanderRegistry.Instance.GetAllFreeCommanders()
				.Find(c => c.commanderName == "王 念一");
			
			Debug.Log("Army Commander name: " + armyStatus.armyCommander.commanderName);
			armyStatus.battalionStatuses.Clear();
			
			for (int i = 0; i < testPlayerReserveTeamCount; i++)
			{
				BattalionStatus battalionStatus = new BattalionStatus()
				{
					battalionId = i,
					battalionName = "TestBattalion" + i,
					// battalionCommander = new GameCommander()
					// {
					// 	commanderId = 0,
					// 	commanderName = "TestBattalionCommander" + i,
					// 	Zhi = 50,
					// 	Xin = 50,
					// 	Ren = 50,
					// 	Yong = 50,
					// 	Yan = 50
					// },
					battalionCommander = CommanderRegistry.Instance.GenerateRandomCommander(),
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
		
		public void MovingBattalion(Vector2Int mapPosition)
		{
			if (!IsUnitSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			
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
					RemoveUnitFromMap(SelectedUnit);
					SelectedUnit.position = mapPosition;
					//TODO:移动实际减少移动力
					SelectedUnit.transform.localPosition = MapToLocal(SelectedUnit.position);
					PlaceUnitOnMap(SelectedUnit, SelectedUnit.position);
					ChangeActionStage(PlayerActionStage.SelectingAction);
					break;
				
				default:
					break;
			}
			
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
				yield return StartCoroutine(PlayerTurnCoroutine());
				//
				CurrentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
				
				CurrentTurnState = TurnState.EnemyTurn;
				yield return StartCoroutine(EnemyTurnCoroutine());
				
				CurrentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
			}

		}
		private IEnumerator PlayerTurnCoroutine()
		{
			IsPlayerTurnOver = false;
			TurnCount++;
			Debug.Log("Player Turn!");

			foreach (var unit in factionActiveUnits[Faction.Player])
			{
				if(unit is Battalion bat)
				bat.currentMovement = bat.Definition.defaultFlexibility/10;
				//改成实际数值
				unit.actionDone = false;
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
			yield return new WaitForSeconds(2.0f); 
			List<Unit> enemyUnits = new List<Unit>(factionActiveUnits[Faction.Enemy]);
			foreach (var unit in enemyUnits)
			{
				
				if (unit == null || !unit.gameObject.activeSelf) continue;
				if (unit is not Battalion aiBattalion) continue;
				
				aiBattalion.currentMovement = aiBattalion.Definition.defaultFlexibility/10;
				
				yield return new WaitForSeconds(0.5f);
				
				yield return StartCoroutine(ProcessAIUnitTurn(aiBattalion));
				
				yield return new WaitForSeconds(0.3f);
			}
			
			//TODO：加入敌人逻辑
			Debug.Log("Enemy Turn End!");
			
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
		
		public Unit GetBestTargetOnTile(Vector2Int targetPosition)
		{
			TileData tile = mapData[targetPosition.x, targetPosition.y];
			if (tile.IsEmpty) return null;

			// 如果当前没有行动，默认逻辑 (比如返回部队)
			if (CurrentAction == null) return tile.Battalion != null ? (Unit)tile.Battalion : tile.Facility;

			// 1. 尝试获取部队
			if (tile.Battalion != null)
			{
				// 检查部队是否符合当前技能的条件
				if (CurrentAction.CheckTargetConditions(SelectedUnit, tile.Battalion))
					return tile.Battalion;
			}

			// 2. 尝试获取设施
			if (tile.Facility != null)
			{
				if (CurrentAction.CheckTargetConditions(SelectedUnit, tile.Facility))
					return tile.Facility;
			}

			// 3. 如果都不符合条件但格子上有人，返回任意一个让后续逻辑处理（可能会报错提示无效目标）
			return tile.Battalion != null ? (Unit)tile.Battalion : tile.Facility;
		}
		
		public void ApplyAction(Vector2Int mapPosition)
		{
			if (!IsUnitSelected || CurrentAction == null)
			{
				Debug.LogWarning("No unit selected or no action prepared.");
				return;
			}

			// 1. 获取目标
			Unit targetUnit = GetBestTargetOnTile(mapPosition);

			// 2. 验证目标是否存在
			if (targetUnit == null)
			{
				Debug.Log("该位置没有有效目标。");
				return;
			}

			// 3. 验证目标是否合法 (再次检查条件，防止 UI 漏洞)
			if (!CurrentAction.CheckTargetConditions(SelectedUnit, targetUnit))
			{
				Debug.Log($"目标 {targetUnit.name} 不满足行动 {CurrentAction.actionName} 的条件。");
				return;
			}

			// 4. 执行逻辑
			ExecuteActionLogic(SelectedUnit, targetUnit);
		}

		private void ExecuteActionLogic(Unit source, Unit target)
		{
			// 执行 ActionDefinition 定义的 Perform
			bool success = CurrentAction.Perform(source, target);

			if (success)
			{
				// 检查可能的死亡
				CheckDeath(source);
				CheckDeath(target);

				// 标记行动结束
				if (SelectedUnit) SelectedUnit.actionDone = true;
                
				// 清理状态
				ClearAllSelection(); // 或者 CancelAction()
				ChangeActionStage(PlayerActionStage.None);
			}
			else
			{
				Debug.Log("Action Perform returned false.");
			}
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
					AssignTerrainToTile(hexTile, "Plain");
				}
			}
		}

		public void AssignTerrainToTile(HexTile tile, string terrainType)
		{
			TerrainDefinition def = TerrainDB.Instance.GetTerrain(terrainType);
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
			unit.position = pos;
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

			if (passability == UnitPassability.Stoppable)
			{
				while (mapData[x, y].Battalion != null)
				{
					x = Random.Range(0, Size.x);
					y = Random.Range(0, Size.y);
				}
			}
			
			return new Vector2Int(x,y);
		}
		void GenerateDetailedMap()
		{
			//TODO 读取数据或生成地图细节
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
		
		#endregion
		public BattalionDescriptor CurrentBattalionDescriptor{ get; set; }
		public Unit CurrentUnit{ get; set; }
		
		public BattalionDefinition defaultReserveTeamDefinition;
		public BattalionDefinition defaultEnemyDefinition;
		
		Battalion SpawnBattalion(BattalionDescriptor battalioninfo, Vector2Int position)
		{
			Battalion battalion = GenerateBattalionFromDescriptor(battalioninfo);
			battalion.position = position;
			PositionBattalion(battalion);
			//audioSource.PlayOneShot(compilation.battalionDefinition.SelectedSoundEffect);
			factionActiveUnits[battalioninfo.faction].Add(battalion);
			PlaceUnitOnMap(battalion, position);
			battalioninfo.placed = true;
			InitializeUnitActions(battalion);
			return battalion;
		}
		
		public void InitializeUnitActions(Unit unit)
		{
			//默认攻击（对于设施来说，默认攻击不显示，因此等于没有）
			unit.DefaultAttack = unit.unitDefinition.defaultAttack;
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
		public Battalion GenerateBattalionFromDescriptor(BattalionDescriptor battalioninfo)
		{
			var battalion = new GameObject().AddComponent<Battalion>();
			battalion.InstanceId = battalioninfo.armyId;
			battalion.faction = battalioninfo.faction;
			battalion.Definition = battalioninfo.Definition;
			battalion.battalionCommander = battalioninfo.battalionCommander;
			battalion.currentSoliders = battalioninfo.currentSoliders;
			return battalion;
		}
		
		public void RemoveBattalionWhileArrangement(Battalion battalion)
		{
			factionActiveUnits[battalion.faction].Remove(battalion);
			RemoveUnitFromMap(battalion);
			if(SelectedUnit == battalion) ClearAllSelection();
			playerReserveTeam[battalion.InstanceId].placed = false;
			Destroy(battalion.gameObject);
		}
		
		public void RemoveUnitFromBattle(Unit unit)
		{
			if(unit == null) return;
			
			if (unit is Battalion bat)
			{
				factionActiveUnits[bat.faction].Remove(bat);
				RemoveUnitFromMap(bat);
				if(SelectedUnit == bat) ClearAllSelection();
				bat.gameObject.SetActive(false);
				bat.transform.SetParent(null);
				return;
			}

			if (unit is Facility fac)
			{
				return;
			}
			
			
		}
		
		public void PositionBattalion(Battalion battalion)
		{
			battalion.transform.SetParent(transform, false);
			battalion.transform.localPosition = MapToLocal(battalion.position);
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
		
		#region AI
		
		private IEnumerator ProcessAIUnitTurn(Battalion aiUnit)
		{
			//Testing 只有攻击指令
			Debug.Log("Enemy Action Start!");
			
			Faction targetFaction = Faction.Player;
			Unit target = FindNearestUnit(aiUnit, targetFaction,UnitTypeFilter.BattalionOnly);

			if (target == null)
			{
				Debug.Log($"Enemy {aiUnit.InstanceId} has no target.");
				yield break;
			}
			
			int dist = GetHexDistance(aiUnit.position, target.position);
			
			if (dist <= aiUnit.Definition.attackRange)
			{
				Debug.Log($"Enemy {aiUnit.InstanceId} attacks directly.");
				DoAIAttack(aiUnit, target);
			}
			else
			{
				
				HashSet<Vector2Int> moveableTiles = GetAccessableTilesInRange(aiUnit, aiUnit.currentMovement);
				
				Vector2Int bestPos = aiUnit.position;
				int minDistanceToTarget = int.MaxValue;
				bool canAttackFromBestPos = false;

				foreach (var pos in moveableTiles)
				{
					if (pos != aiUnit.position && !CanUnitStopOnTile(aiUnit,pos)) continue;

					int d = GetHexDistance(pos, target.position);
					
					bool inRange = d <= aiUnit.Definition.attackRange;
					
					if (inRange && !canAttackFromBestPos)
					{
						bestPos = pos;
						minDistanceToTarget = d;
						canAttackFromBestPos = true;
					}
					else if (inRange == canAttackFromBestPos)
					{
						if (d < minDistanceToTarget)
						{
							bestPos = pos;
							minDistanceToTarget = d;
						}
					}
				}
				
				if (bestPos != aiUnit.position)
				{

					DoAIMove(aiUnit, bestPos);
					yield return new WaitForSeconds(0.5f);
					
					if (GetHexDistance(aiUnit.position, target.position) <= aiUnit.Definition.attackRange)
					{
						DoAIAttack(aiUnit, target);
					}
				}
			}

			aiUnit.actionDone = true;
		}
		
		private void DoAIMove(Battalion unit, Vector2Int targetPos)
		{
			RemoveUnitFromMap(unit);
			
			unit.position = targetPos;
			
			PlaceUnitOnMap(unit, targetPos);
			
			unit.transform.localPosition = MapToLocal(unit.position);
			
			Debug.Log($"Enemy moved to {targetPos}");
		}
		
		private void DoAIAttack(Battalion source, Unit target)
		{
			ActionDefinition attackAction = source.DefaultAttack;

			if (attackAction == null)
			{
				Debug.LogError($"Unit {source.name} has no DefaultAttack defined!");
				return;
			}

			// 2. 直接调用 ActionDefinition 的 Perform
			bool success = attackAction.Perform(source, target);
    
			if(success)
			{
				Debug.Log($"Enemy attacked {target.name}");
				
				CheckDeath(source);
				CheckDeath(target);
        
				source.actionDone = true; 
			}
		}
		
		#endregion
		
		#region Range
		
		public HashSet<Vector2Int> GetAccessableTilesInRange(Unit movingUnit, int range)
		{
			
			HashSet<Vector2Int> validDestinations = new HashSet<Vector2Int>();
			
			if (!movingUnit) return validDestinations;
			
			Vector2Int startPos = movingUnit.position;
			
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
 
					// int moveCost = TerrainDB.Instance.GetTerrain(mapTerrainData[neighborPos.x, neighborPos.y]).movementCost;
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
		
		
		public HashSet<Vector2Int> GetValidActionTargetTiles(Unit user, ActionDefinition action)
        {
            HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
            
            // 如果是 Self 类型，只返回自己脚下
            if (action.targetCountType == TargetCountType.Self)
            {
                validTiles.Add(user.position);
                return validTiles;
            }

            // 使用 BFS 搜索范围 (不考虑地形阻挡，通常技能范围是无视地形的“射程”)
            // 如果需要考虑视线阻挡(Line of Sight)，这里需要改为 Raycast 逻辑
            
            // 简单的曼哈顿距离/六边形距离遍历
            // 为了性能，我们可以直接遍历 range 范围内的所有坐标，然后计算距离
            
            // 获取当前坐标的 Cube 坐标
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
                                TileData tile = mapData[neighborPos.x, neighborPos.y];
                                
                                bool hasValidTarget = false;

                                if (tile.Battalion != null && action.CheckTargetConditions(user, tile.Battalion))
                                    hasValidTarget = true;
                                
                                // 检查设施
                                if (!hasValidTarget && tile.Facility != null && action.CheckTargetConditions(user, tile.Facility))
                                    hasValidTarget = true;

                               
                                if (action.CanTargetEmptyTile && tile.IsEmpty) hasValidTarget = true;

                                if (hasValidTarget)
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
		
		private void EndGame(bool isWin)
		{
			battleResult.Victory = isWin;
			
			//TODO：跳转结算阶段
		}

		private void ApplyArmyChangesToArmyStatus()
		{
			//TODO:将战斗损耗应用回军队状态
			//TODO：更新指挥官经验值、等级或死亡与负伤
		}
		
		#endregion
		
		#region Functions
		
		public BattleResult YieldResult()
		{
			CollectLoot(battleResult);
			return battleResult;
		}

		private void CollectLoot(BattleResult result)
		{
			Dictionary<ItemDefinition, int> consolidatedLoot = new Dictionary<ItemDefinition, int>();
            
			foreach (var unit in factionActiveUnits[Faction.Player])
			{
				if (unit is Battalion bat)
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
