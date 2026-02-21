using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	public class GameInstance : MonoBehaviour
	{
		#region Singleton
		static GameInstance instance;
		public static GameInstance Instance => instance;
		#endregion

		#region Life cycle
		void Awake()
		{
			if(instance != null && instance != this)
			{
				Destroy(this);
				return;
			}
			instance = this;
			DontDestroyOnLoad(this);
		}

		void Start()
		{
			lastPolis = Data.poleis.Find(p => p.id == Data.lastPolis);
			Paused = false;
			Data.time.onMonthPassed += PushMonthPassToPoleis;
		}

		/// <summary>
		/// 退出游戏，回到主菜单时触发。
		/// </summary>
		void OnDestroy()
		{
			Paused = false;
			Destroy(gameObject);
		}
		#endregion

		#region Serialization/deserialization
		public GameData Data { get; set; }

		Savegame MakeSavegame()
		{
			return new()
			{
				lastUpdatedTime = System.DateTime.Now,
				data = Data,
			};
		}

		public void SaveTo(string filename)
		{
			GameManager.WriteSavegame(filename, MakeSavegame());
		}
		#endregion

		#region Scene transition
		PolisData lastPolis;
		/// <summary>最后进入过的城池。</summary>
		/// <remarks>城池内/战斗场景的调度类可读取此值来知道初始化哪个城池。</remarks>
		public PolisData LastPolis
		{
			get => lastPolis;
			private set
			{
				lastPolis = value;
				Data.lastPolis = lastPolis.id;
			}
		}

		public ArmyStatus ActiveArmy { get; private set; }

		public enum Mode { Polis, WorldMap, Battle }
		Mode currentMode = Mode.Polis;
		public Mode CurrentMode
		{
			get => currentMode;
			private set
			{
				switch(currentMode = value)
				{
					case Mode.WorldMap:
						GameManager.SwitchScene("World Map");
						break;
					case Mode.Polis:
						GameManager.SwitchScene("Polis");
						break;
					case Mode.Battle:
						GameManager.SwitchScene("Battle");
						break;
					default:
						throw new System.NotSupportedException();
				}
			}
		}

		public void EnterPolis(string polisId)
		{
			var polis = Data.poleis.Find(p => p.id == polisId);
			if(polis == null)
			{
				Debug.LogWarning($"Cannot enter polis \"{polisId}\", failed to find.");
				return;
			}

			switch(polis.type)
			{
				case PolisType.Controlled:
					LastPolis = polis;

					// 军队入城
					polis.GarrisonArmy(ActiveArmy);
					ActiveArmy = null;

					Debug.Log($"Entering polis \"{LastPolis.id}\".");
					CurrentMode = Mode.Polis;
					break;

				case PolisType.Hostile:
					LastPolis = polis;
					battleMetaData = PrepareBattleMetadata();

					Debug.Log($"Attacking polis \"{LastPolis.id}\".");
					CurrentMode = Mode.Battle;
					break;

				default:
					throw new System.NotSupportedException();
			}
		}

		/// <summary>从我方城池出征。</summary>
		public void DepartFromPolis(IReadOnlyList<GameCommander> commanders, float foodAmount)
		{
			var garrisonedCommanders = LastPolis.GetGarrisonedCommanders();
			var validatedCommanders = commanders.Where(garrisonedCommanders.Contains).ToArray();
			if(!(validatedCommanders.Length > 0))
			{
				Debug.LogWarning("出城军队必须至少有一个指挥官！");
				return;
			}

			Debug.Log($"Departing from polis \"{LastPolis.id}\".");

			// 军队出城
			if(LastPolis != null)
			{
				// TODO: 根据出征 UI 决定军队编制与携带军粮量。
				ActiveArmy = LastPolis.LetOutGarrison(null, foodAmount, validatedCommanders);
			}

			CurrentMode = Mode.WorldMap;
		}

		BattleMetaData battleMetaData;
		public BattleMetaData BattleMetaData => battleMetaData;

		/// <summary>生成 Battle 生成战斗场景时需要的元信息。</summary>
		BattleMetaData PrepareBattleMetadata()
		{
			BattleMetaData data = new();
			// TODO
			return data;
		}

		/// <summary>停止进攻敌方城池，回到大地图。</summary>
		public void ExitBattle()
		{
			Debug.Log($"Exiting battle against polis \"{LastPolis.id}\".");
			battleMetaData = null;
			ApplyBattleResult(Battle.Instance.YieldResult());
			CurrentMode = Mode.WorldMap;
		}

		/// <summary>应用战役结算成果。</summary>
		void ApplyBattleResult(BattleResult result)
		{
			// TODO
		}
		#endregion

		#region Time
		public void AdvanceTime_Scaled(float dt)
		{
			Data.time.AdvanceByInGameTime(dt * ActualTimeScale);
		}

		void PushMonthPassToPoleis()
		{
			Debug.Log($"游戏整体度月。当前时间：{Data.time}。");
			foreach(var polis in Data.poleis)
			{
				PolisTask task = new(
					PolisTaskType.monthPassed,
					Data.time - polis.LastTime,
					Data.time.CurrentMonth.ToString()
				);
				polis.AddTask(task);
			}
		}

		float timeScale = 1.0f;
		public float TimeScale
		{
			get => timeScale;
			set
			{
				timeScale = value;
				UpdateActualTimeScale();
			}
		}

		public float ActualTimeScale
		{
			get => Time.timeScale;
			private set
			{
				Time.timeScale = value;
				onActualTimeScaleChanged?.Invoke();
			}
		}

		public System.Action<float> onGameTimeAdvanced
		{
			get => Data.time.onAdvancedByGameTime;
			set => Data.time.onAdvancedByGameTime = value;
		}
		public System.Action onActualTimeScaleChanged;

		void UpdateActualTimeScale()
		{
			ActualTimeScale = Paused ? 0 : TimeScale;
		}
		#endregion

		#region Pause
		public void OpenPauseMenu()
		{
			UiManager.Instance.OpenUiModalFromPrefabPath("Prefabs/UI/Pause/Pause Menu");
		}

		public System.Action onPauseStateChanged;

		bool paused = false;
		public bool Paused
		{
			get => paused;
			set
			{
				paused = value;
				UpdateActualTimeScale();
				onPauseStateChanged?.Invoke();
			}
		}
		#endregion
	}
}
