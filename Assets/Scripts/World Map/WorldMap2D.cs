using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
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
		}

		void Update()
		{
			if(Army == null)
				return;

			float dt = Time.deltaTime;
			GameInstance.Instance.AdvanceTime_Scaled(dt * GameManager.InternalSettings.worldMapTimeScale);

			RefreshFoodSlider();
		}
		#endregion

		#region Construction
		[SerializeField] WorldMap2DPlayer player;
		[SerializeField] RectTransform mapAnchor;
		[SerializeField] Image mapImage;
		[SerializeField] RectTransform poleisContainer;

		void Construct()
		{
			player.onMove += OnPlayerMove;

			mapImage.sprite = GameInstance.Instance.Data.world.data2D.mapImage;
			mapImage.SetNativeSize();
			mapImage.transform.localScale *= GameInstance.Instance.Data.world.data2D.scale;

			foreach(var polisData in GameInstance.Instance.Data.poleis)
				SpawnPolis(polisData);
		}

		WorldMapPolis SpawnPolis(PolisData polisData)
		{
			var wp = HierarchyUtility.InstantiatePrefabFromResource<WorldMapPolis>("Prefabs/World Map/Polis");
			wp.transform.SetParent(poleisContainer, false);
			wp.transform.localPosition = new Vector3(polisData.position.x, polisData.position.y, 0) * GameInstance.Instance.Data.world.data2D.scale;
			return wp;
		}
		#endregion

		#region Food
		[SerializeField] Slider foodSlider;
		[SerializeField] TMP_Text foodText;

		void RefreshFoodSlider()
		{
			float foodValue = 0;
			if(Army.initialFood != 0)
				foodValue = Army.carriedFood / Army.initialFood;
			foodSlider.value = foodValue;
			foodText.text = $"{Mathf.FloorToInt(Army.carriedFood)}";
		}

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
