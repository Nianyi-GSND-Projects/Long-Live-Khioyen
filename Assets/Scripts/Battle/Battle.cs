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
			arrangementOccupancy = new Battalion[Size.x, Size.y];
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
			
			playerBattalions = new HashSet<Battalion>();
			enemyBattalions = new HashSet<Battalion>();
			
			//加入测试敌人
			{
				BattalionCompilation compilation = new()
				{
					//TODO:填入有意义数据
					battalionId = 1,
					position = new Vector2Int(3, 3),
					battalionDefinition = defaultReserveTeamDefinition,
					battalionCommander = new(),
					currentSoliders = TestEnemySoliders,
					currentMurale = defaultReserveTeamDefinition.defaultMaxMorale,
					currentTraining = 100
				};
				enemyBattalions.Add(SpawnBattalion(compilation));
				data.EnemyBattalions.Add(compilation);
			}
		}
		
		#endregion
		
		#region Interface
		public ArrangementModal arrangementModal;
		
		public void PlacingPlayerBattalion(ReserveTeam reserveTeam, Vector2Int mapPosition)
		{
			if (!data.playerReserveTeams.Contains(reserveTeam))
			{
				Debug.Log("Battalion name: " + reserveTeam.battalionDefinition.battalionId + "Don't exist in your reserve teams.");
				return;
			}

			if (reserveTeam.placed)
			{
				Debug.Log("Battalion name: " + reserveTeam.battalionDefinition.battalionId + "already placed.");
				return;
			}

			BattalionCompilation compilation = new()
			{
				//TODO:填入有意义数据
				battalionId = 0,
				position = mapPosition,
				battalionDefinition = reserveTeam.battalionDefinition,
				battalionCommander = reserveTeam.battalionCommander,
				currentSoliders = reserveTeam.currentSoliders,
				currentMurale = reserveTeam.currentMurale,
				currentTraining = reserveTeam.currentTraining,
			};

			playerBattalions.Add(SpawnBattalion(compilation));
			data.PlayerBattalions.Add(compilation);
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
			BattalionCompilation compilation = SelectedBattalion.Compilation;
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					arrangementOccupancy[compilation.position.x, compilation.position.y] = null;
					compilation.position = mapPosition;
					SelectedBattalion.transform.localPosition = MapToLocal(compilation.position);
					arrangementOccupancy[compilation.position.x, compilation.position.y] = SelectedBattalion;
					break;
				
				case Stage.Battle:
					if (CurrentActionStage != PlayerActionStage.MovingBattalion) break;
					arrangementOccupancy[compilation.position.x, compilation.position.y] = null;
					compilation.position = mapPosition;
					//TODO:移动实际减少移动力
					SelectedBattalion.transform.localPosition = MapToLocal(compilation.position);
					arrangementOccupancy[compilation.position.x, compilation.position.y] = SelectedBattalion;
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
		
		public Battalion SelectedBattalion
		{
			get => CurrentBattalion;
			set
			{
				if (CurrentBattalion != null)
					CurrentBattalion.Selected = false;
				
				CurrentBattalion = value;

				if (CurrentBattalion != null)
				{
					CurrentBattalion.Selected = true;
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
			SelectedBattalion = null;
			IsBattalionSelected = false;
			if(CurrentStage == Stage.Battle) ClearAllHexHighlights();
			availableMovePositions.Clear();
		}
		
		public void SelectBattalion(Battalion battalion)
		{
			if (!playerBattalions.Contains(battalion))
			{
				Debug.Log("Battalion " + battalion.Compilation.battalionId + " is not your battalion.");
				return;
			}
			
			switch (CurrentStage)
			{
				case Stage.Arrangement:
					SelectedBattalion = battalion;
					IsBattalionSelected = true;
					break;
				
				case Stage.Battle:
					
					if (battalion.Compilation.ActionEnd)
					{
						Debug.Log("Battalion " + battalion.Compilation.battalionId + " has already finished its action!");
						break;
					}
					
					if (battalion.Compilation.currentMovement == 0)
					{
						Debug.Log("Battalion " + battalion.Compilation.battalionId + " has no movement!");
						break;
					}
					
					SelectedBattalion = battalion;
					IsBattalionSelected = true;
					
					initialUnitPosition = SelectedBattalion.Compilation.position;
					initialUnitMovement = battalion.Compilation.currentMovement;
					if (CurrentActionStage == PlayerActionStage.None)
					{
						int moveRange = initialUnitMovement;
						availableMovePositions = GetAccessableTilesInRange(SelectedBattalion.Compilation.position, moveRange);
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
					if (enemyBattalions.Contains(arrangementOccupancy[placement.x, placement.y]))
						return true;
					return false;
				
				default:
					return false;
			}
		}
		
		public void CheckDeath(Battalion battalion)
		{
			if (battalion.Compilation.currentSoliders <= 0)
			{
				RemoveBattalionWhileBattle(battalion);
				Debug.Log($"Battalion {battalion.Compilation.battalionId} die off!");

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

			foreach (var battalion in playerBattalions)
			{
				battalion.Compilation.currentMovement = battalion.Compilation.battalionDefinition.defaultFlexibility/10;
				Debug.Log("Battalion " + battalion.Compilation.battalionId + " movement: " + battalion.Compilation.currentMovement);
				battalion.Compilation.ActionEnd = false;
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
			arrangementOccupancy[SelectedBattalion.Compilation.position.x, SelectedBattalion.Compilation.position.y] = null;
			SelectedBattalion.Compilation.position = initialUnitPosition;
			arrangementOccupancy[initialUnitPosition.x, initialUnitPosition.y] = SelectedBattalion;
			SelectedBattalion.Compilation.currentMovement = initialUnitMovement;
			SelectedBattalion.transform.localPosition = MapToLocal(initialUnitPosition);
			
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
					availableTargetPositions = GetAllTilesInRange(SelectedBattalion.Compilation.position, SelectedBattalion.Definition.attackRange);
					HighlightTiles(availableTargetPositions,attackHighlightColor);
					Debug.Log("Change action stage to SelectingTarget");
					break;
			}
		}
		
		public void ActionWait()
		{
			
			SelectedBattalion.Compilation.ActionEnd = true;
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
			
			BattalionCompilation compilation = SelectedBattalion.Compilation;

			switch (CurrentActionType)
			{
				case 1:
					Battalion TargetEnemy = arrangementOccupancy[mapPosition.x, mapPosition.y];
					Attack(SelectedBattalion,TargetEnemy);
					break;
				default:
					break;
			}
			SelectedBattalion.Compilation.ActionEnd = true;
			ChangeActionStage(PlayerActionStage.None);
		}

		public void Attack(Battalion source, Battalion target)
		{
			//TODO：完善战斗计算
			target.Compilation.currentSoliders -= source.Definition.defaultAttack * 2;
			source.Compilation.currentSoliders -= target.Definition.defaultAttack;
			Debug.Log($"Battalion {source.Compilation.battalionId} Attack Enemy Battalion + {target.Compilation.battalionId}");
			Debug.Log($"Battalion {source.Compilation.battalionId} remaining solider: {source.Compilation.currentSoliders}");
			Debug.Log($"Battalion {target.Compilation.battalionId} remaining solider: {target.Compilation.currentSoliders}");
			CheckDeath(source);
			CheckDeath(target);
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
		
		Battalion[,] arrangementOccupancy;
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
		
		#region Battalions
		
		readonly List<Battalion> battalions = new();
				
		private HashSet<Battalion> playerBattalions;
		private HashSet<Battalion> enemyBattalions;
		
		
		public ReserveTeam CurrentReserveTeam{ get; set; }
		public Battalion CurrentBattalion{ get; set; }
		
		public BattalionDefinition defaultReserveTeamDefinition;
		public BattalionDefinition defaultEnemyDefinition;
		
		Battalion SpawnBattalion(BattalionCompilation compilation)
		{
			
			var battalion = new GameObject().AddComponent<Battalion>();
			PositionBattalion(battalion.transform, compilation.battalionDefinition,compilation);
			//audioSource.PlayOneShot(compilation.battalionDefinition.SelectedSoundEffect);
			battalions.Add(battalion);
			battalion.Compilation = compilation;
			battalion.Definition = compilation.battalionDefinition;
			arrangementOccupancy[compilation.position.x, compilation.position.y] = battalion;
			return battalion;
		}

		public void RemoveBattalionWhileArrangement(Battalion battalion)
		{
			battalions.Remove(battalion);
			arrangementOccupancy[battalion.Compilation.position.x, battalion.Compilation.position.y] = null;
			if(playerBattalions.Contains(battalion))playerBattalions.Remove(battalion);
			else if(enemyBattalions.Contains(battalion)) enemyBattalions.Remove(battalion);
			
			if(data.EnemyBattalions.Contains(battalion.Compilation)) data.EnemyBattalions.Remove(battalion.Compilation);
			else if(data.PlayerBattalions.Contains(battalion.Compilation)) data.PlayerBattalions.Remove(battalion.Compilation);
			if(SelectedBattalion == battalion) ClearAllSelection();
			Destroy(battalion.gameObject);
		}
		
		public void RemoveBattalionWhileBattle(Battalion battalion)
		{
			battalions.Remove(battalion);
			arrangementOccupancy[battalion.Compilation.position.x, battalion.Compilation.position.y] = null;
			if(playerBattalions.Contains(battalion))playerBattalions.Remove(battalion);
			else if(enemyBattalions.Contains(battalion)) enemyBattalions.Remove(battalion);
			
			if(data.EnemyBattalions.Contains(battalion.Compilation)) data.EnemyBattalions.Remove(battalion.Compilation);
			else if(data.PlayerBattalions.Contains(battalion.Compilation)) data.PlayerBattalions.Remove(battalion.Compilation);
			if(SelectedBattalion == battalion) ClearAllSelection();
			Destroy(battalion.gameObject);
		}
		
		public void PositionBattalion(Transform battalion, BattalionDefinition definition, BattalionCompilation compilation)
		{
			battalion.SetParent(transform, false);
			battalion.localPosition = MapToLocal(compilation.position);
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
