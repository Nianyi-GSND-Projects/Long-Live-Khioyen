using UnityEngine;
using System.Collections.Generic;

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
			UiManager.onUiOpened += OnUiOpened;
			UiManager.onUiClosed += OnUiClosed;

			lastPolis = Data.poleis.Find(p => p.id == Data.lastPolis);
			Paused = false;
		}

		/// <summary>
		/// 退出游戏，回到主菜单时触发。
		/// </summary>
		void OnDestroy()
		{
			UiManager.onUiOpened -= OnUiOpened;
			UiManager.onUiClosed -= OnUiClosed;

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

		// 临时保存：在出城（出征）时从 `PolisData` 中抽取的驻军条目列表。
		// 这些条目在玩家处于大地图时由 `GameInstance` 持有，返回城池时会恢复回原城池。
		List<GarrisonEntry> extractedGarrisonEntries;
		// 表示这些驻军来源于哪个 polis（用于在回到相同 polis 时恢复）。
		string garrisonSourcePolisId;

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
					// 如果此前在出城时把本城的驻军抽出并由 GameInstance 持有，且现在回到同一城池，
					// 则把这些条目恢复回 polis 中（把驻军放回城内）。
					if(extractedGarrisonEntries != null && garrisonSourcePolisId == polis.id)
					{
						polis.RestoreGarrison(extractedGarrisonEntries);
						extractedGarrisonEntries = null;
						garrisonSourcePolisId = null;
					}
					Debug.Log($"Entering polis \"{LastPolis.id}\".");
					CurrentMode = Mode.Polis;
					break;
				case PolisType.Hostile:
					LastPolis = polis;
					battleMetaData = PrepareBattleMetadata();
					// 如果在离开城池时抽取了驻军（extractedGarrisonEntries 非空），
					// 在进入战斗（攻城）场景前用这些数据初始化 `ArmyStatus`。
					if(extractedGarrisonEntries != null)
					{
						var army = ArmyStatus.Instance;
						if(extractedGarrisonEntries.Count > 0)
							army.armyCommander = extractedGarrisonEntries[0].commander;
						army.battalionStatuses.Clear();
						// TODO: 将 extractedGarrisonEntries 中的 assignedBattalionIds 映射为
						//       ArmyStatus.BattalionStatus 并填充到 army.battalionStatuses 中。
					}
					Debug.Log($"Attacking polis \"{LastPolis.id}\".");
					CurrentMode = Mode.Battle;
					break;
				default:
					throw new System.NotSupportedException();
			}
		}

		/// <summary>从我方城池出征。</summary>
		public void DepartFromPolis()
		{
			Debug.Log($"Departing from polis \"{LastPolis.id}\".");
			if(LastPolis != null)
			{
				// 从城池抽出驻军并由 GameInstance 暂存（大地图期间持有），以便战斗场景使用或回城恢复
				extractedGarrisonEntries = LastPolis.ExtractGarrison();
				garrisonSourcePolisId = LastPolis.id;
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
		public float GameTime
		{
			get => Data.gameTime;
			private set => Data.gameTime = value;
		}
		public int CurrentMonth => GameManager.ConvertToMonth(GameTime);
		static float MonthLength => GameManager.InternalSettings.monthLength;

		public void AdvanceTime(float dt)
		{
			if(dt <= 0)
			{
				Debug.LogWarning("Time must be advanced positively.");
				return;
			}

			int targetMonth = GameManager.ConvertToMonth(GameTime + dt);
			float remaining = dt;
			while(CurrentMonth != targetMonth)
			{
				float nextMonthStart = (CurrentMonth + 1) * MonthLength;
				float advanced = nextMonthStart - GameTime;
				GameTime = nextMonthStart;
				remaining -= advanced;

				PushMonthPassToPoleis();
			}
			GameTime += remaining;

			onGameTimeAdvanced?.Invoke(dt);
		}

		void PushMonthPassToPoleis()
		{
			foreach(var polis in Data.poleis)
			{
				PolisTask task = new(
					PolisTaskType.monthPassed,
					GameTime - polis.LastTime,
					CurrentMonth.ToString()
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

		public System.Action<float> onGameTimeAdvanced;
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

		#region UI
		void OnUiOpened(GameObject ui)
		{
			if(IsPauseUi(ui))
				Paused = true;
		}

		void OnUiClosed(GameObject ui)
		{
			if(IsPauseUi(ui))
				Paused = false;
		}

		bool IsPauseUi(GameObject go)
		{
			return go.GetComponent<PauseMenu>() != null;
		}
		#endregion
	}
}
