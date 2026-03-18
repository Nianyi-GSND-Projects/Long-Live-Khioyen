using UnityEngine;
using Nianyi.UnityPack;

namespace LongLiveKhioyen
{
	public class WorldMap2D : MonoBehaviour
	{
		ArmyStatus Army => GameInstance.Instance.ActiveArmy;

		#region Life cycle
		void Awake()
		{
			Construct();
			GameInstance.Instance.TimeScale = 1;

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
			mapRenderer.sprite = GameInstance.Instance.Data.world.data2D.mapImage;
			mapRenderer.transform.localScale = Vector3.one * GameInstance.Instance.Data.world.data2D.scale;

			foreach(var polisData in GameInstance.Instance.Data.poleis)
				SpawnPolis(polisData);
		}

		WorldMapPolis SpawnPolis(PolisData polisData)
		{
			var wp = HierarchyUtility.InstantiatePrefabFromResource<WorldMapPolis>("Prefabs/World Map/Polis");
			wp.transform.SetParent(poleisContainer, false);
			wp.transform.localPosition = new Vector3(polisData.position.x, polisData.position.y, 0) * GameInstance.Instance.Data.world.data2D.scale;
			wp.Initialize(polisData);
			return wp;
		}
		#endregion

		#region Food
		public System.Action onStarved;

		void OnPlayerMove(Vector3 movement)
		{
			float distance = movement.magnitude;
			float foodCost = distance * Army.CarriedWeight * GameManager.InternalSettings.worldMapFoodCostRate;

			bool willStarve = foodCost > Army.carriedFood;
			Army.carriedFood = Mathf.Max(0, Army.carriedFood - foodCost);
			if(willStarve)
				Starve();
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
