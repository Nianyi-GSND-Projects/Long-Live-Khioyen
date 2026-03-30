using UnityEngine;
using System.Collections.Generic;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class WorldMap2D : MonoBehaviour
	{
		ArmyStatus Army => GameInstance.Instance.ActiveArmy;
		GameData GameData => GameInstance.Instance.Data;
		public float MapScale => GameData.world.data2D.scale;

		public WorldMap2D Instance { get; private set; }

		#region Life cycle
		void Awake()
		{
			GameInstance.Instance.TimeScale = 1;
			Instance = this;
		}

		void Start()
		{
			Construct();
			var game = GameInstance.Instance;
			player.Teleport(game.IsWild ? game.WildPos : GetPolisLocalPosition(game.LastPolis));
			player.onMove += OnPlayerMove;
		}

		void Update()
		{
			if(Army == null)
				return;

			float dt = Time.deltaTime;
			GameInstance.Instance.AdvanceTime_Scaled(dt * GameManager.InternalSettings.worldMapTimeScale);
		}
		#endregion

		#region Construction
		[SerializeField] WorldMap2DPlayer player;
		public WorldMap2DPlayer Player => player;
		[SerializeField] SpriteRenderer mapRenderer;
		[SerializeField] Transform poleisContainer;

		void Construct()
		{
			mapRenderer.sprite = GameData.world.data2D.mapImage;
			mapRenderer.transform.localScale = Vector3.one * MapScale;

			// 生成碰撞
			var pc = mapRenderer.gameObject.AddComponent<PolygonCollider2D>();
			Sprite mapMask = GameData.world.data2D.mapMask;
			int shapeCount = mapMask.GetPhysicsShapeCount();
			pc.pathCount = shapeCount;
			for(int i = 0; i < shapeCount; ++i)
			{
				List<Vector2> shape = new();
				mapMask.GetPhysicsShape(i, shape);
				pc.SetPath(i, shape);
			}

			// 生成城池
			foreach(var polisData in GameData.poleis)
				SpawnPolis(polisData);
		}

		WorldMapPolis SpawnPolis(PolisData polisData)
		{
			var wp = HierarchyUtility.InstantiatePrefabFromResource<WorldMapPolis>("Prefabs/World Map/Polis");
			wp.transform.SetParent(poleisContainer, false);
			wp.transform.localPosition = GetPolisLocalPosition(polisData);
			wp.PolisData = polisData;
			return wp;
		}

		Vector3 GetPolisLocalPosition(PolisData polisData) => new Vector3(polisData.position.x, polisData.position.y, 0) * MapScale;
		#endregion

		#region Food
		public System.Action onStarved;

		void OnPlayerMove(Vector3 movement)
		{
			float distance = movement.magnitude;
			if(distance == 0)
				return;

			float foodCost = distance * Army.CarriedWeight * GameManager.InternalSettings.worldMapFoodCostRate;
			bool willStarve = foodCost > Army.carriedFood;
			Army.carriedFood = Mathf.Max(0, Army.carriedFood - foodCost);
			if(willStarve)
				Starve();

			Vector2 worldPos = player.WorldPos;
			WorldData.EnvironmentParams env = GameData.world.GetEnviromentParams(worldPos);
			//Debug.Log($"pos={worldPos}, env={env.ToString()}");  // DEBUG: 测试获取到的环境参数是否正确
			float difficulty = env.difficulty;
			float chance = 1 - Mathf.Pow(1 - difficulty * GameManager.InternalSettings.encounterRate, distance);
			float value = Random.value;
			if(value < chance)
			{
				Debug.Log("遇到暗雷");
				GameInstance.Instance.EnterWildEncounterBattle(player.WorldPos);
			}
		}

		void Starve()
		{
			Debug.Log("行军粮草耗尽。");
			// playerArmy.Controller.enabled = false;  // 牢宋说不用饿死了。
			// TODO: 饿死了的逻辑
			onStarved?.Invoke();
		}
		#endregion
	}
}
