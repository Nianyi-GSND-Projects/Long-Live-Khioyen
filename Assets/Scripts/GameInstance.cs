using UnityEngine;

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
		}

		/// <summary>
		/// 退出游戏，回到主菜单时触发。
		/// </summary>
		void OnDestroy()
		{
			Paused = false;

			Destroy(gameObject);

			if(instance == this)
				instance = null;
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

			if(polis.isControlled)
			{
				LastPolis = polis;
				Debug.Log($"Entering polis \"{LastPolis.id}\".");
				CurrentMode = Mode.Polis;
			}
			else if(polis.isHostile)
			{
				LastPolis = polis;
				Debug.Log($"Attacking polis \"{LastPolis.id}\".");
				CurrentMode = Mode.Battle;
			}
			else throw new System.NotSupportedException();
		}

		/// <summary>从我方城池出征。</summary>
		public void DepartFromPolis()
		{
			Debug.Log($"Departing from polis \"{LastPolis.id}\".");
			CurrentMode = Mode.WorldMap;
		}

		/// <summary>停止进攻敌方城池，回到大地图。</summary>
		public void ExitBattle()
		{
			Debug.Log($"Exiting battle against polis \"{LastPolis.id}\".");
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
			foreach(var poleis in Data.poleis)
			{
				PolisTask task = new()
				{
					type = PolisTaskType.monthPassed,
					parameters = new string[] { CurrentMonth.ToString() },
					remainingTime = GameTime - poleis.lastTime,
				};
				poleis.AddTask(task);
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

		#region Pause
		PauseMenu pauseMenu;
		public void OpenPauseMenu()
		{
			if(Instance == null)
			{
				Debug.LogWarning("Pause menu can only be opened when a game instance is running.");
				return;
			}
			if(pauseMenu != null)
				return;

			pauseMenu = Instantiate(Resources.Load<GameObject>("Prefabs/UI/Pause/Pause Menu")).GetComponent<PauseMenu>();
			Paused = true;
		}

		public void ClosePauseMenu()
		{
			if(pauseMenu == null)
				return;

			Destroy(pauseMenu.gameObject);
			pauseMenu = null;
			Paused = false;
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
		#endregion
	}
}
