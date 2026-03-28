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

			gameObject.AddComponent<TooltipManager>();
		}

		void Start()
		{
			lastPolis = Data.GetPolis(Data.lastPolis);
			Paused = false;
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

		ArmyStatus activeArmy;
		public ArmyStatus ActiveArmy
		{
			get
			{
				if(activeArmy == null)
					activeArmy = new();
				return activeArmy;
			}
			set => activeArmy = value;
		}

		public enum Mode { Undefined, Polis, WorldMap, Battle }
		public System.Action onModeChanged;
		public Mode CurrentMode
		{
			get
			{
				return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex switch
				{
					1 => Mode.Polis,
					2 => Mode.WorldMap,
					3 => Mode.Battle,
					_ => Mode.Undefined,
				};
			}
			private set
			{
				switch(value)
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
				onModeChanged?.Invoke();
			}
		}

		public void EnterPolis(string polisId)
		{
			var polis = Data.GetPolis(polisId);
			if(polis == null)
			{
				Debug.LogWarning($"找不到城池 \"{polisId}\"，无法进入。");
				return;
			}

			switch(polis.type)
			{
				case PolisType.Controlled:
					LastPolis = polis;

					// 军队入城
					polis.GarrisonArmy(ActiveArmy);
					ActiveArmy = null;

					Debug.Log($"进入已控制城池 \"{LastPolis.id}\"。");
					CurrentMode = Mode.Polis;
					break;

				case PolisType.Hostile:
					LastPolis = polis;
					battleMetaData = PrepareBattleMetadata(LastPolis.position);

					Debug.Log($"进攻敌对城池 \"{LastPolis.id}\"。");
					CurrentMode = Mode.Battle;
					break;

				case PolisType.Friendly:
					Debug.LogWarning($"无法进入友好城池 \"{polisId}\"。");
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
		BattleMetaData PrepareBattleMetadata(Vector2 worldPosition)
		{
			var envParams = Data.world.GetEnviromentParams(worldPosition);
			BattleMetaData data = new()
			{
				battlePosition = worldPosition,
				envParams = envParams,
				//difficulty = Mathf.RoundToInt(envParams.difficulty),
				encounterOrientation = default,  // WONTFIX: 此量不打算提供
			};
			return data;
		}

		public bool IsWild { get; private set; } = false;  // 是否在大地图上临时游荡
		public Vector2 WildPos { get; private set; }
		public void EnterWildEncounterBattle(Vector2 worldPosition)
		{
			battleMetaData = PrepareBattleMetadata(worldPosition);

			Debug.Log($"进入野战。");
			IsWild = true;
			WildPos = worldPosition;
			CurrentMode = Mode.Battle;
			IsWild = false;
		}

		/// <summary>停止进攻敌方城池，回到大地图。</summary>
		public void ExitBattle(BattleResult battleResult)
		{
			Debug.Log($"Exiting battle against polis \"{LastPolis.id}\".");
			battleMetaData = null;
			ApplyBattleResult(battleResult);
			CurrentMode = Mode.WorldMap;
			IsWild = false;
		}

		/// <summary>应用战役结果。</summary>
		void ApplyBattleResult(BattleResult result)
		{
			// 度过时间
			Data.time.AdvanceByMonth(result.passedTime);

			var polis = Data.GetPolis(result.polisId);
			if(polis == null)
			{
				Debug.LogWarning($"找不到城池 \"{result.polisId}\"，无法应用战役结果。");
				return;
			}

			// TODO: 战利品入库

			// 若战斗成功，使城池变为友好
			if(result.Victory)
			{
				polis.type = polis.canControl ? PolisType.Controlled : PolisType.Friendly;
				polis.conquered = true;
				Debug.Log($"成功攻克城池 \"{result.polisId}\"。");
			}
			else
			{
				Debug.Log($"未能攻克城池 \"{result.polisId}\"。");
			}
		}
		#endregion

		#region Time scale
		public void AdvanceTime_Scaled(float dGt)
		{
			Data.time.AdvanceByInGameTime(dGt * ActualTimeScale);
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
