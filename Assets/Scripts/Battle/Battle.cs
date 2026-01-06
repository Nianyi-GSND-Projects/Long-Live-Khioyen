using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor.Build.Pipeline.Tasks;

namespace LongLiveKhioyen
{
	public enum Stage
	{
		Preparation,
		Arrangement,
		Battle,
		Settlement
	}
	
	public enum UnitTypeFilter
	{
		All,
		BattalionOnly,
		FacilityOnly
	}

	public enum TurnState
	{
		PlayerTurn,
		EnemyTurn,
		FriendTurn,
		Processing
	}

	public enum PlayerActionStage
	{
		None,
		MovingBattalion,
		SelectingAction,
		SelectingTarget
	}

	public struct TileData
	{
		public Unit Battalion;
		public Unit Facility;
	}
	
	public class Battle : MonoBehaviour
	{
		static Battle _instance;
		public static Battle Instance => _instance;
		public System.Action onInitialized;
		public ActionDefinitionSheet actionDataBase;
		public CommanderRegistry commanderRegistry;
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
			InitializeGameStatue();
			
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
			{
				BattalionDescriptor battalionDescriptor = new BattalionDescriptor()
				{
					armyId = i,
					Definition = armyStatus.battalionStatuses[i].battalionDefinition,
					faction = Faction.Player,
					battalionCommander = armyStatus.battalionStatuses[i].battalionCommander,
					currentSoliders = armyStatus.battalionStatuses[i].currentSolider,
					currentMurale = armyStatus.battalionStatuses[i].currentMorale,
					currentTraining = armyStatus.battalionStatuses[i].currentExp,
					maxSolider = armyStatus.battalionStatuses[i].MaxSolider,
					maxMorale = armyStatus.battalionStatuses[i].MaxMorale,
					maxTraining = armyStatus.battalionStatuses[i].MaxExp,
					placed = false
				};
				playerReserveTeam.Add(battalionDescriptor);
			}
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
			arrangementOccupancy = new Unit[Size.x, Size.y];
			
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
		
		private void InitializeGameStatue()
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
			if (!IsBattalionSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					arrangementOccupancy[SelectedUnit.position.x, SelectedUnit.position.y] = null;
					SelectedUnit.position = mapPosition;
					SelectedUnit.transform.localPosition = MapToLocal(SelectedUnit.position);
					arrangementOccupancy[SelectedUnit.position.x, SelectedUnit.position.y] = SelectedUnit;
					break;
				
				case Stage.Battle:
					if (CurrentActionStage != PlayerActionStage.MovingBattalion) break;
					arrangementOccupancy[SelectedUnit.position.x, SelectedUnit.position.y] = null;
					SelectedUnit.position = mapPosition;
					//TODO:移动实际减少移动力
					SelectedUnit.transform.localPosition = MapToLocal(SelectedUnit.position);
					arrangementOccupancy[SelectedUnit.position.x, SelectedUnit.position.y] = SelectedUnit;
					ChangeActionStage(PlayerActionStage.SelectingAction);
					break;
				
				default:
					break;
			}
			
		}
		#endregion

		#region Selection
		
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
			IsBattalionSelected = false;
			if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
			availableMovePositions.Clear();
		}
		
		public void SelectBattalion(Battalion battalion)
		{
			
			
			if (CurrentStage == Stage.Battle && CurrentTurnState != TurnState.PlayerTurn)
			{
				Debug.Log("Not your turn!");
				return;
			}

			SelectedUnit = battalion;
			IsBattalionSelected = true;
			
			if (IsReserveTeamSelected) 
				ClearReserveTeamSelection();
			
			
			if (!factionActiveUnits[Faction.Player].Contains(battalion))
			{
				Debug.Log("Battalion " + battalion.InstanceId + " is not your battalion.");
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
					
					if (battalion.actionDone)
					{
						Debug.Log("Battalion " + battalion.InstanceId + " has already finished its action!");
						break;
					}
					
					if (battalion.currentMovement == 0)
					{
						Debug.Log("Battalion " + battalion.InstanceId + " has no movement!");
						break;
					}
					
					initialUnitPosition = SelectedUnit.position;
					initialUnitMovement = battalion.currentMovement;
					if (CurrentActionStage == PlayerActionStage.None)
					{
						int moveRange = initialUnitMovement;
						availableMovePositions = GetAccessableTilesInRange(SelectedUnit.position, moveRange);
						ChangeActionStage(PlayerActionStage.MovingBattalion);
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
		
		public bool ValidateArrangementPlacement(Vector2Int placement)
		{
			if(!IsValidMapPosition(placement))
				return false;
			if (!availableArrangementPositions.Contains(placement)&&CurrentStage == Stage.Arrangement) 
				return false;
			return true;
		}
		
		public bool ValidateActionTarget(Vector2Int placement)
		{
			if(!IsValidMapPosition(placement))
				return false;

			switch (CurrentActionType)
			{
				//TODO:良好定义各种行动
				//1:Attack
				case 1:
					if (arrangementOccupancy[placement.x, placement.y] == null) return false;
					if (arrangementOccupancy[placement.x, placement.y] is Battalion bat)
					{
						if (factionActiveUnits[Faction.Enemy].Contains(bat) && bat.Definition.beAttacked == true)
							return true;
					}
					if (arrangementOccupancy[placement.x, placement.y] is Facility fac)
					{
						//TODO
					}
					return false;
				
				default:
					return false;
			}
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
		public bool IsBattalionSelected { get; set; }= false;

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
		public int CurrentActionType{ get; set; }

		public void CancelMovement()
		{
			arrangementOccupancy[SelectedUnit.position.x, SelectedUnit.position.y] = null;
			SelectedUnit.position = initialUnitPosition;
			arrangementOccupancy[initialUnitPosition.x, initialUnitPosition.y] = SelectedUnit;
			if(SelectedUnit is Battalion bat) bat.currentMovement = initialUnitMovement;
			SelectedUnit.transform.localPosition = MapToLocal(initialUnitPosition);
			
			availableMovePositions = GetAccessableTilesInRange(initialUnitPosition, initialUnitMovement);
		}

		public void CancelAction()
		{
			availableTargetPositions.Clear();
			CurrentActionType = -1;
			IsPreparingAction = false;
			ClearAllHexHighlights();
		}
		public void ChangeActionStage(PlayerActionStage stage)
		{
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
					availableTargetPositions = GetAttackableTiles();
					HighlightTiles(availableTargetPositions,attackHighlightColor);
					Debug.Log("Change action stage to SelectingTarget");
					break;
			}
		}
		
		public void ActionWait()
		{
			
			SelectedUnit.actionDone = true;
			ClearAllSelection();
			ChangeActionStage(PlayerActionStage.None);
			
		}
		
		public void ActionAttackPrepare()
		{
			IsPreparingAction = true;
			CurrentActionType = 1;
			ChangeActionStage(PlayerActionStage.SelectingTarget);
		}
		
		public void ApplyAction(Vector2Int mapPosition)
		{
			if (!IsBattalionSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			bool actionFinished = false;
			switch (CurrentActionType)
			{
				case 1:
					Unit TargetEnemyUnit = arrangementOccupancy[mapPosition.x, mapPosition.y];
					if((SelectedUnit is Battalion bat) && TargetEnemyUnit.unitDefinition.beAttacked == true) 
						actionFinished = Attack(bat,TargetEnemyUnit);
					break;
				default:
					break;
			}

			if (actionFinished == false)
			{
				Debug.Log("Action not Valid!");
				return;
			}
			if(SelectedUnit) SelectedUnit.actionDone = true;
			ChangeActionStage(PlayerActionStage.None);
		}

		public bool Attack(Battalion source, Unit target)
		{
			ActionDefinition attack = actionDataBase.GetAction("RegularAttack");
			if(attack.Perform(source,target))
			{
				CheckDeath(source);
				CheckDeath(target);
				return true;
			}
			    
			return false;
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
		
		Unit[,] arrangementOccupancy;
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
				}
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
		//To be checked
		
		
		private Vector2Int GetRandomValidPosition(UnitPassability passability)
		{
			int x = Random.Range(0, Size.x);
			int y = Random.Range(0, Size.y);

			if (passability == UnitPassability.Stoppable)
			{
				while (arrangementOccupancy[x, y] != null)
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
			arrangementOccupancy[battalion.position.x, battalion.position.y] = battalion;
			battalioninfo.placed = true;
			return battalion;
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
			arrangementOccupancy[battalion.position.x, battalion.position.y] = null;
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
				arrangementOccupancy[bat.position.x, bat.position.y] = null;
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
				
				HashSet<Vector2Int> moveableTiles = GetAccessableTilesInRange(aiUnit.position, aiUnit.currentMovement);
				
				Vector2Int bestPos = aiUnit.position;
				int minDistanceToTarget = int.MaxValue;
				bool canAttackFromBestPos = false;

				foreach (var pos in moveableTiles)
				{
					if (pos != aiUnit.position && arrangementOccupancy[pos.x, pos.y] != null) continue;

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
			arrangementOccupancy[unit.position.x, unit.position.y] = null;
			
			unit.position = targetPos;
			
			arrangementOccupancy[unit.position.x, unit.position.y] = unit;
			
			unit.transform.localPosition = MapToLocal(unit.position);
			
			Debug.Log($"Enemy moved to {targetPos}");
		}
		
		private void DoAIAttack(Battalion source, Unit target)
		{
			bool success = Attack(source, target);
			
			if(success)
			{
				Debug.Log($"Enemy attacked {target.name}");
			}
		}
		
		#endregion
		
		#region Range
		
		public HashSet<Vector2Int> GetAccessableTilesInRange(Vector2Int startPos, int range)
		{
			HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>();
			
			if (!hexTiles.ContainsKey(startPos))
			{
				Debug.LogWarning($"尝试从一个不存在的格子 {startPos} 开始寻路。");
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
					if (!IsValidMapPosition(neighborPos) ||
					    (arrangementOccupancy[neighborPos.x, neighborPos.y] != null)) continue;
					if (hexTiles.ContainsKey(neighborPos) && !distanceTravelled.ContainsKey(neighborPos))
					{
						distanceTravelled[neighborPos] = distanceTravelled[currentPos] + 1;
						frontier.Enqueue(neighborPos);
					}
				}
			}

			return reachableTiles;
		}

		public HashSet<Vector2Int> GetAttackableTiles()
		{
			if(SelectedUnit is Battalion bat)
			return GetAllTilesInRange(bat.position, bat.Definition.attackRange);
			return new HashSet<Vector2Int>();
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
		
		#region Functions
		
		public BattleResult YieldResult()
		{
			//结算战役
			BattleResult result = new BattleResult();
			result.CollectLoot();
			return result;
		}

		public void ExitBattle()
		{
			GameInstance.Instance.ExitBattle();
		}
		
		#endregion
	}

	public class BattleResult
	{
		public void CollectLoot()
		{
			
		}
	}
}
