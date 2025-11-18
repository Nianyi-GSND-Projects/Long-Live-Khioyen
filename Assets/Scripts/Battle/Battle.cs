using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using System.Linq;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V20;

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
		private HashSet<Vector2Int> availableMovePositions;
		private HashSet<Vector2Int> availableArrangementPositions;
		private HashSet<Vector2Int> availableTargetPositions;
		
		public Color movementHighlightColor = Color.green; 
		public Color arrangementHighlightColor = Color.blue;
		public Color attackHighlightColor = Color.red;
		
		public GameObject HextilePrefab;
		private Dictionary<Vector2Int,HexTile> hexTiles = new();
		AudioSource audioSource;
		
		private Coroutine battleLoopCoroutine;
		
		static Battle instance;
		public static Battle Instance => instance;
		public System.Action onInitialized;
		
		public Stage currentStage;
		public TurnState currentTurnState;
		public PlayerActionStage currentActionStage;
		public int currentActiontype;
		
		public bool isPlayerTurnOver;
		public bool isPreparingAction;
		private HashSet<Battalion> playerBattalions;
		private HashSet<Battalion> enemyBattalions;
		public int TurnCount { get; private set; }
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
			GameInstance.Instance.ExecuteWhenInitialized(Initialize);
		}
		void Initialize()
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
			currentTurnState = TurnState.PlayerTurn;
			availableMovePositions = new HashSet<Vector2Int>();
			availableArrangementPositions = new HashSet<Vector2Int>();
			
			GenerateArrangementSlot();
			onInitialized?.Invoke();
			TurnCount = 0;
		}

		
		#endregion
		
		#region Test

		public void BattleTest()
		{
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
					currentSoliders = 20,
					currentMurale = defaultReserveTeamDefinition.defaultMaxMorale,
					currentTraining = 100
				};
				enemyBattalions.Add(SpawnBattalion(compilation));
				data.EnemyBattalions.Add(compilation);
			}
		}
		#endregion
		
		#region Stages
		
		public bool isInArrangementStage = false;
		public bool isInBattleStage = false;
		public bool isReserveTeamSelected = false;
		public bool isBattalionSelected = false;
		public void ChangeStage(Stage stage)
		{
			OnExitStage(currentStage);
			
			currentStage = stage;
			OnEnterStage(currentStage);
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
		
		#region Preparation
		
		
		
		#endregion
		
		#region Arrangement
		
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
			if (!isBattalionSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			BattalionCompilation compilation = SelectedBattalion.Compilation;
			switch (currentStage)
			{
				case Stage.Arrangement:
					arrangementOccupancy[compilation.position.x, compilation.position.y] = null;
					compilation.position = mapPosition;
					SelectedBattalion.transform.localPosition = MapToLocal(compilation.position);
					arrangementOccupancy[compilation.position.x, compilation.position.y] = SelectedBattalion;
					break;
				
				case Stage.Battle:
					if (currentActionStage != PlayerActionStage.MovingBattalion) break;
					arrangementOccupancy[compilation.position.x, compilation.position.y] = null;
					compilation.position = mapPosition;
					SelectedBattalion.transform.localPosition = MapToLocal(compilation.position);
					arrangementOccupancy[compilation.position.x, compilation.position.y] = SelectedBattalion;
					ChangeActionStage(PlayerActionStage.SelectingAction);
					break;
				
				default:
					break;
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
			isReserveTeamSelected = false;
		}
		
		public void ClearBattalionSelection()
		{
			SelectedBattalion = null;
			isBattalionSelected = false;
			if(currentStage == Stage.Battle) ClearAllHexHighlights();
			availableMovePositions.Clear();
		}
		public bool TestAvailableMovePositions(Vector2Int mapPosition)
		{
			return availableMovePositions.Contains(mapPosition);
		}
		public void SelectBattalion(Battalion battalion)
		{
			switch (currentStage)
			{
				case Stage.Arrangement:
					SelectedBattalion = battalion;
					isBattalionSelected = true;
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
					isBattalionSelected = true;
					if (currentActionStage == PlayerActionStage.None)
					{
						int moveRange = battalion.Compilation.currentMovement;
						availableMovePositions = GetAccessableTilesInRange(SelectedBattalion.Compilation.position, moveRange);
						ChangeActionStage(PlayerActionStage.MovingBattalion);
					}
					break;
				
				default:
					break;
			}
			
		}
		
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
		public event System.Action OnPlayerTurnStarted;
		public event System.Action OnPlayerTurnEnded;
		public event System.Action OnActionSelectionStarted;
		public event System.Action OnActionSelectionEnded;
		public void ChangeActionStage(PlayerActionStage stage)
		{
			if (currentActionStage == PlayerActionStage.SelectingAction)
			{
				OnActionSelectionEnded?.Invoke();
			}
			currentActionStage = stage;
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
		private IEnumerator BattleTurnLoop()
		{
			Debug.Log("Battle Start!");
			while (true)
			{
				currentTurnState = TurnState.PlayerTurn;
				yield return StartCoroutine(PlayerTurnCoroutine());
				//
				currentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
				
				currentTurnState = TurnState.EnemyTurn;
				yield return StartCoroutine(EnemyTurnCoroutine());
				
				currentTurnState = TurnState.Processing;
				yield return new WaitForSeconds(1);
				if (CheckGameOver()) yield break;
			}

		}

		private IEnumerator PlayerTurnCoroutine()
		{
			isPlayerTurnOver = false;
			Debug.Log("Player Turn!");

			foreach (var battalion in playerBattalions)
			{
				battalion.Compilation.currentMovement = battalion.Compilation.battalionDefinition.defaultFlexibility/10;
				Debug.Log("Battalion " + battalion.Compilation.battalionId + " movement: " + battalion.Compilation.currentMovement);
				battalion.Compilation.ActionEnd = false;
			}
			//
			OnPlayerTurnStarted?.Invoke();

			while (!isPlayerTurnOver)
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

		public bool CheckGameOver()
		{
			//TODO：加入游戏结束判断
			if (TurnCount > 3)
			{
				ChangeStage(Stage.Settlement);
				return true;
			}
			return false;
		}
		
		public void EndPlayerTurn()
		{
			if(currentTurnState == TurnState.PlayerTurn) isPlayerTurnOver = true;
			else Debug.LogError("It's not player's turn!");
		}
		
		#endregion
		
		
		#region Data
		#region Battle data

		// BattleData data => GameInstance.Instance.LastBattle;
		public BattleData data = new();
		public Vector2Int Size => data.battleSize;
		public string Id => data.id;
		#endregion
		
		#region Game data
		/// <summary>
		/// 会被 <c>GameInstance</c> 在退出战役时调用；
		/// 返回值会被应用到实际游戏数据上。
		/// </summary>
		public BattleResult YieldResult()
		{
			//结算战役。
			BattleResult result = new BattleResult();
			result.CollectLoot(data);
			return result;
		}
		#endregion
		#endregion
		
		#region Map Generation
		public GameObject Map;
		public Grid hexgrid;
		public float Xscale;
		public float Yscale;
		public float SizeScale;

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
		#endregion
		
		#region Control mode
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

		[SerializeField] CinemachineVirtualCamera camera;
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
		
		#region Grid
		
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

		public bool IsValidMapPosition(Vector2Int pos)
		{
			return pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y;
		}
		
		public bool ValidateArrangementPlacement(Vector2Int placement)
		{
				if(!IsValidMapPosition(placement))
					return false;
				if (!availableArrangementPositions.Contains(placement)&&currentStage == Stage.Arrangement) 
					return false;
			return true;
		}
		
		#endregion
		
		#region Battalions
		
		Battalion[,] arrangementOccupancy;
		readonly List<Battalion> battalions = new();
		public System.Action onArrangementOccupancyChanged;
		public ReserveTeam currentReserveTeam;
		Battalion currentBattalion;
		public BattalionDefinition defaultReserveTeamDefinition;
		public BattalionDefinition defaultEnemyDefinition;
		public ReserveTeam CreateDefaultReserveTeam()
		{
			ReserveTeam newTeam = new ReserveTeam();
			newTeam.battalionDefinition = defaultReserveTeamDefinition;
			newTeam.battalionCommander = new ();
			newTeam.currentSoliders = newTeam.battalionDefinition.defaultMaxSolider;
			newTeam.currentMurale = newTeam.battalionDefinition.defaultMaxMorale;
			newTeam.currentTraining = 100;
			return newTeam;
		}
		
		public Battalion SelectedBattalion
		{
			get => currentBattalion;
			set
			{
				if (currentBattalion != null)
					currentBattalion.Selected = false;
				
				currentBattalion = value;

				if (currentBattalion != null)
				{
					currentBattalion.Selected = true;
					//TODO: 打开行动面板
				}
			}
		}

		public ReserveTeam SelectedReserveTeam
		{
			get => currentReserveTeam;
			set
			{
				if (value == currentReserveTeam)
					return;


				currentReserveTeam = value;

				if (currentReserveTeam != null)
				{

				}
			}
		}

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
		
		public void PositionBattalion(Transform battalion, BattalionDefinition definition, BattalionCompilation compilation)
		{
			battalion.SetParent(transform, false);
			battalion.localPosition = MapToLocal(compilation.position);
		}
		#endregion
		
		#region Action

		public void ActionWait()
		{
			
			SelectedBattalion.Compilation.ActionEnd = true;
			ClearAllSelection();
			ChangeActionStage(PlayerActionStage.None);
			
		}
		
		public void ActionAttackPrepare()
		{
			isPreparingAction = true;
			currentActiontype = 1;
			ChangeActionStage(PlayerActionStage.SelectingTarget);
		}


		public bool ValidateActionTarget(Vector2Int placement)
		{
			if(!IsValidMapPosition(placement))
				return false;

			switch (currentActiontype)
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
			return true;
		}
		
		public void ApplyAction(Vector2Int mapPosition)
		{
			if (!isBattalionSelected)
			{
				Debug.Log("No battalion selected.");
				return;
			}
			
			BattalionCompilation compilation = SelectedBattalion.Compilation;

			switch (currentActiontype)
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
		public void CheckDeath(Battalion battalion)
		{
			if (battalion.Compilation.currentSoliders <= 0)
			{
				RemoveBattalionWhileBattle(battalion);
				Debug.Log($"Battalion {battalion.Compilation.battalionId} die off!");

			}
				
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
		#endregion
		
		#region Functions
		
		public void ProceedToNextStage()
		{
			switch(currentStage)
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
		
		public void ExitBattle()
		{
			GameInstance.Instance.ExitBattle();
		}
		
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
	}
}
