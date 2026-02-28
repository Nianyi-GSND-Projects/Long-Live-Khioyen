using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
	public class WorldMap : MonoBehaviour
	{
		InternalGameSettings Settings => GameManager.InternalSettings;
		ArmyStatus Army => GameInstance.Instance.ActiveArmy;

		#region Fields
		readonly List<PolisMiniature> polisMiniatures = new();
		public WorldMapPlayerArmy playerArmy;
		/** <summary>The distance the player army will spawn from last polis.</summary> */
		[Min(0)] public float departureDistance = 10;
		#endregion

		#region Life cycle
		void Awake()
		{
			Construct();
			Time.timeScale = 1.0f;
		}

		void Start()
		{
			GameInstance.Instance.TimeScale = 1;

			// Positions the player army to the front door of last polis.
			var lastPolis = polisMiniatures.Find(pm => pm.data.id == GameInstance.Instance.LastPolis.id);
			playerArmy.Controller.Teleport(lastPolis.transform.position + lastPolis.transform.forward * departureDistance);
			playerArmy.Controller.FaceTowards(lastPolis.transform.forward);

			playerArmy.onMove += OnPlayerMove;
		}

		void Update()
		{
			float dt = Time.deltaTime;
			GameInstance.Instance.AdvanceTime_Scaled(dt * Settings.worldMapTimeScale);

			RefreshFoodSlider();
		}
		#endregion

		#region Construction
		void Construct()
		{
			/* Poleis */
			foreach(var polisData in GameInstance.Instance.Data.poleis)
			{
				var polisMiniature = SpawnPolisMiniature(polisData);
				polisMiniatures.Add(polisMiniature);
			}
		}

		static GameObject controlledPolisMiniatureTemplate, hostilePolisMiniatureTemplate, friendlyPolisMiniatureTemplate;

		PolisMiniature SpawnPolisMiniature(PolisData polisData)
		{
			GameObject go;
			switch(polisData.type)
			{
				case PolisType.Controlled:
					if(!controlledPolisMiniatureTemplate)
						controlledPolisMiniatureTemplate = Resources.Load<GameObject>("Prefabs/World Map/Polis_miniature-controlled");
					go = Instantiate(controlledPolisMiniatureTemplate);
					break;
				case PolisType.Hostile:
					if(!hostilePolisMiniatureTemplate)
						hostilePolisMiniatureTemplate = Resources.Load<GameObject>("Prefabs/World Map/Polis_miniature-hostile");
					go = Instantiate(hostilePolisMiniatureTemplate);
					break;
				case PolisType.Friendly:
					if(!friendlyPolisMiniatureTemplate)
						friendlyPolisMiniatureTemplate = Resources.Load<GameObject>("Prefabs/World Map/Polis_miniature-friendly");
					go = Instantiate(friendlyPolisMiniatureTemplate);
					break;
				default:
					throw new System.NotSupportedException();
			}

			go.transform.SetParent(transform, false);
			go.transform.localPosition = new Vector3(polisData.position.x, 0, polisData.position.y) * GameInstance.Instance.Data.world.data3D.scale;
			go.transform.localEulerAngles = Vector3.up * polisData.orientation;

			var pm = go.GetComponent<PolisMiniature>();
			pm.data = polisData;
			return pm;
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

		void OnPlayerMove(float distance)
		{
			float foodCost = distance * Army.CarriedWeight * Settings.worldMapFoodCostRate;
			bool willStarve = foodCost > Army.carriedFood;
			Army.carriedFood = Mathf.Max(0, Army.carriedFood - foodCost);
			if(willStarve)
				Starve();
		}

		void Starve()
		{
			Debug.LogWarning("卧槽，你马饿死了。");
			playerArmy.Controller.enabled = false;
			// TODO: 饿死了的逻辑
			onStarved?.Invoke();
		}
		#endregion
	}
}
