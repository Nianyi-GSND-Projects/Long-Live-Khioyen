using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

namespace LongLiveKhioyen
{
	public enum Stage
	{
		Preparation,
		Arrangement,
		Battle,
		Settlement
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
	
	public class Battle : MonoBehaviour
	{
		static Battle instance;
		public static Battle Instance => instance;
		public System.Action onInitialized;

		#region General Config

		public Color movementHighlightColor = Color.green; 
		public Color arrangementHighlightColor = Color.blue;
		public Color attackHighlightColor = Color.red;
		AudioSource audioSource;
		#endregion
		
		#region Battle data

		//TODO:加载战斗数据
		public BattleData data = new();
		public Vector2Int Size => data.battleSize;
		public string Id => data.id;
		#endregion

		
		#region Life cycle
		void Awake()
		{
			instance = this;
			audioSource = GetComponent<AudioSource>();
		}

		void OnDestroy()
		{
			instance = null;
		}

		void Start()
		{
			ChangeStage(Stage.Preparation);
			transform.rotation = Quaternion.Euler(0, 0, 0);
			gameObject.isStatic = true;
			GenerateHexGrid();
			arrangementOccupancy = new Unit[Size.x, Size.y];
			AnchorPosition = MapToWorld(new Vector2Int(data.battleSize.x/2, data.battleSize.y/2));
			BattleTest();
			
			//TODO:从出征队伍列表中读取部队
			//TODO:从地图数据中读取敌人部队与位置
			CurrentTurnState = TurnState.PlayerTurn;
			availableMovePositions = new HashSet<Vector2Int>();
			availableArrangementPositions = new HashSet<Vector2Int>();
			
			GenerateArrangementSlot();
			onInitialized?.Invoke();
			TurnCount = 0;
		}

		
		#endregion
		
		#region Test
		//用于进行功能测试部分的代码

		public int TestEnemySoliders;
		public ReserveTeam CreateDefaultReserveTeam()
		{
			//创建并返回一个默认类型的预备队
			ReserveTeam newTeam = new ReserveTeam();
			newTeam.battalionDefinition = defaultReserveTeamDefinition;
			newTeam.battalionCommander = new ();
			newTeam.currentSoliders = newTeam.battalionDefinition.defaultMaxSolider;
			newTeam.currentMurale = newTeam.battalionDefinition.defaultMaxMorale;
			newTeam.currentTraining = 100;
			return newTeam;
		}
		public void BattleTest()
		{
			//初始化测试场景
			ReserveTeam testTeam = CreateDefaultReserveTeam();
			if(testTeam == null) 
				Debug.LogError("Create default reserve team failed.");
			else
			{
				data.playerReserveTeams.Add(testTeam);
			}
			//向战斗预备队中加入默认部队
			
			playerActiveBattalions = new HashSet<Battalion>();
			enemyActiveBattalions = new HashSet<Battalion>();
			
			//加入测试敌人
			{
				BattalionDescriptor newBattalion = new()
				{
					InstanceId = 1,
					position = new Vector2Int(3, 3),
					Definition = defaultReserveTeamDefinition,
					battalionCommander = new(),
					currentSoliders = TestEnemySoliders,
					currentMurale = defaultReserveTeamDefinition.defaultMaxMorale,
					currentTraining = 100
				};
				
				enemyActiveBattalions.Add(SpawnBattalion(newBattalion));
			}
		}
		
		#endregion
		
		#region Interface
		public ArrangementModal arrangementModal;
		
		public void PlacingPlayerBattalion(ReserveTeam reserveTeam, Vector2Int mapPosition)
		{
			if (!data.playerReserveTeams.Contains(reserveTeam))
			{
				Debug.Log("Battalion name: " + reserveTeam.battalionDefinition.unitName + "Don't exist in your reserve teams.");
				return;
			}

			if (reserveTeam.placed)
			{
				Debug.Log("Battalion name: " + reserveTeam.battalionDefinition.unitName + "already placed.");
				return;
			}

			BattalionDescriptor newBattalion = new()
			{
				//TODO:填入有意义数据
				InstanceId = 0,
				position = mapPosition,
				Definition = reserveTeam.battalionDefinition,
				battalionCommander = reserveTeam.battalionCommander,
				currentSoliders = reserveTeam.currentSoliders,
				currentMurale = reserveTeam.currentMurale,
				currentTraining = reserveTeam.currentTraining,
			};

			playerActiveBattalions.Add(SpawnBattalion(newBattalion));
			reserveTeam.placed = true;
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
		
		public ReserveTeam SelectedReserveTeam
		{
			get => CurrentReserveTeam;
			set
			{
				if (value == CurrentReserveTeam)
					return;


				CurrentReserveTeam = value;

				if (CurrentReserveTeam != null)
				{

				}
			}
		}
		
		public Unit SelectedUnit
		{
			get => CurrentUnit;
			set
			{
				if (CurrentUnit != null)
					CurrentUnit.Selected = false;
				
				CurrentUnit = value;

				if (CurrentUnit != null)
				{
					CurrentUnit.Selected = true;
					//TODO: 打开行动面板
				}
			}
		}
		
		public void ClearAllSelection()
		{
			ClearReserveTeamSelection();
			ClearBattalionSelection();
		}
		
		public void ClearReserveTeamSelection()
		{
			SelectedReserveTeam = null;
			IsReserveTeamSelected = false;
		}
		
		public void ClearBattalionSelection()
		{
			SelectedUnit = null;
			IsBattalionSelected = false;
			if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
			availableMovePositions.Clear();
		}
		
		public void SelectBattalion(Battalion battalion)
		{
			if (!playerActiveBattalions.Contains(battalion))
			{
				Debug.Log("Battalion " + battalion.InstanceId + " is not your battalion.");
				return;
			}
			
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					SelectedUnit = battalion;
					IsBattalionSelected = true;
					break;
				
				case Stage.Battle:
					
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
					
					SelectedUnit = battalion;
					IsBattalionSelected = true;
					
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
						if (enemyActiveBattalions.Contains(bat) && bat.Definition.beAttacked == true)
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
		
		public void CheckDeathBattalion(Battalion battalion)
		{
			if (battalion.currentSoliders <= 0)
			{
				RemoveUnitFromBattle(battalion);
				Debug.Log($"Battalion {battalion.InstanceId} die off!");

			}
		}
		public void CheckDeathFacility(Facility facility)
		{
			if (facility.currentDurability <= 0)
			{
			//	RemoveFacilityWhileBattle(facility);
				Debug.Log($"Facility {facility.InstanceId} destroyed!");

			}
				
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
			Debug.Log("Player Turn!");

			foreach (var battalion in playerActiveBattalions)
			{
				battalion.currentMovement = battalion.Definition.defaultFlexibility/10;
				Debug.Log("Battalion " + battalion.InstanceId + " movement: " + battalion.currentMovement);
				battalion.actionDone = false;
			}
			//
			OnPlayerTurnStarted?.Invoke();

			while (!IsPlayerTurnOver)
			{
				yield return null;
			}
			Debug.Log("Player Turn End!");
			ChangeActionStage(PlayerActionStage.None);
			OnPlayerTurnEnded?.Invoke();
		}
		
		private IEnumerator EnemyTurnCoroutine()
		{
			
			Debug.Log("Enemy Turn!");
			yield return new WaitForSeconds(2.0f); 
			//TODO：加入敌人逻辑
			Debug.Log("Enemy Turn End!");
		}
		
		public void EndPlayerTurn()
		{
			if(CurrentTurnState == TurnState.PlayerTurn) IsPlayerTurnOver = true;
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
			//TODO：完善战斗计算
			if (target is Battalion bat)
			{
				bat.currentSoliders -= source.Definition.defaultAttack * 2;
				source.currentSoliders -= bat.Definition.defaultAttack;
				Debug.Log($"Battalion {source.InstanceId} Attack Enemy Battalion + {target.InstanceId}");
				Debug.Log($"Battalion {source.InstanceId} remaining solider: {source.InstanceId}");
				Debug.Log($"Battalion {target.InstanceId} remaining solider: {target.InstanceId}");
				CheckDeathBattalion(source);
				CheckDeathBattalion(bat);
				return true;
			}
			else if (target is Facility fac)
			{
				fac.currentDurability -= source.Definition.defaultAttack * 2;
				Debug.Log($"Battalion {source.InstanceId} Attack Enemy Facility + {target.InstanceId}");
				Debug.Log($"Battalion {source.InstanceId} remaining solider: {source.InstanceId}");
				CheckDeathBattalion(source);
				CheckDeathFacility(fac);
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
			
			for (int y = -1; y < Size.y+1; y++)
			{
				for (int x = -1; x < Size.x+1; x++)
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

		void GenerateArrangementSlot()
		{
			//TODO:根据玩家进入战斗的角度，在合适的位置创建部署区
			for(int i=0;i<3;i++)
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
		
		readonly List<Battalion> activeBattalions = new();
				
		private HashSet<Battalion> playerActiveBattalions;
		private HashSet<Battalion> enemyActiveBattalions;
		
		
		public ReserveTeam CurrentReserveTeam{ get; set; }
		public Unit CurrentUnit{ get; set; }
		
		public BattalionDefinition defaultReserveTeamDefinition;
		public BattalionDefinition defaultEnemyDefinition;
		
		Battalion SpawnBattalion(BattalionDescriptor battalioninfo)
		{
			
			var battalion = new GameObject().AddComponent<Battalion>();
			GenerateBattalionFromDescriptor(battalion, battalioninfo);
			PositionBattalion(battalion);
			//audioSource.PlayOneShot(compilation.battalionDefinition.SelectedSoundEffect);
			activeBattalions.Add(battalion);
			arrangementOccupancy[battalion.position.x, battalion.position.y] = battalion;
			return battalion;
		}

		public void GenerateBattalionFromDescriptor(Battalion battalion, BattalionDescriptor battalioninfo)
		{
			battalion.InstanceId = battalioninfo.InstanceId;
			battalion.position = battalioninfo.position;
			battalion.Definition = battalioninfo.Definition;
			battalion.battalionCommander = battalioninfo.battalionCommander;
			battalion.currentSoliders = battalioninfo.currentSoliders;
			
		}
		public void RemoveBattalionWhileArrangement(Battalion battalion)
		{
			activeBattalions.Remove(battalion);
			arrangementOccupancy[battalion.position.x, battalion.position.y] = null;
			if(playerActiveBattalions.Contains(battalion))playerActiveBattalions.Remove(battalion);
			else if(enemyActiveBattalions.Contains(battalion)) enemyActiveBattalions.Remove(battalion);
			
			if(data.EnemyBattalions.Contains(battalion)) data.EnemyBattalions.Remove(battalion);
			else if(data.PlayerBattalions.Contains(battalion)) data.PlayerBattalions.Remove(battalion);
			if(SelectedUnit == battalion) ClearAllSelection();
			Destroy(battalion.gameObject);
		}
		
		public void RemoveUnitFromBattle(Unit unit)
		{
			if(unit == null) return;
			
			if (unit is Battalion bat)
			{
				activeBattalions.Remove(bat);
				arrangementOccupancy[bat.position.x, bat.position.y] = null;
				if(playerActiveBattalions.Contains(bat))playerActiveBattalions.Remove(bat);
				else if(enemyActiveBattalions.Contains(bat)) enemyActiveBattalions.Remove(bat);
			
				if(data.EnemyBattalions.Contains(bat)) data.EnemyBattalions.Remove(bat);
				else if(data.PlayerBattalions.Contains(bat)) data.PlayerBattalions.Remove(bat);
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
			result.CollectLoot(data);
			return result;
		}

		public void ExitBattle()
		{
			GameInstance.Instance.ExitBattle();
		}
		
		#endregion
	}
}
