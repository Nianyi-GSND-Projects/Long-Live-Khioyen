using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLiveKhioyen
{
	public static class GameManager
	{
		#region Life cycle
#if DEBUG && UNITY_EDITOR
		/// <summary>是否处于刚从非正常路径启动的 debug session 中。</summary>
		static bool abruptDebug = false;
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		static void OnGameStart()
		{
			/* Local data */
			ReadSettings();
			RefreshSavegames();

			/* Built-in resources */
			InternalSettings = Resources.Load<InternalGameSettings>("Internal Game Settings");
			buildingDefinitionSheet = UnityEngine.Object.Instantiate(Resources.Load<BuildingDefinitionSheet>("Data/Building Definitions"));
			if(buildingDefinitionSheet == null)
			{
				Debug.LogError("Cannot construct buildings, cannot fetch the initial building definition sheet.");
				return;
			}

			/* Scene */
			SceneManager.activeSceneChanged += OnSceneChanged;
#if DEBUG && UNITY_EDITOR
			// Use initial game data if not starting from the menu scene.
			if(SceneManager.GetActiveScene().buildIndex != 0)
			{
				abruptDebug = true;
				StartNewGame();
			}
#endif
		}
		#endregion

		#region Built-in resources
		public static InternalGameSettings InternalSettings { get; private set; }
		static BuildingDefinitionSheet buildingDefinitionSheet;
		public static List<BuildingDefinition> BuildingDefinitions => buildingDefinitionSheet?.buildingDefinitions;

		public static bool FindBuildingDefinitionByType(string type, out BuildingDefinition definition)
		{
			definition = BuildingDefinitions.Find(d => d.id == type);
			return definition != null;
		}
		
		
		#endregion
		
		#region Local data
		#region Settings
		static readonly string settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
		static GameSettings settings = null;
		static readonly Action onSettingsChanged;

		static void EnsureSettingsPath()
		{
			if(Directory.Exists(settingsPath))
				Directory.Delete(settingsPath, true);
			if(!File.Exists(settingsPath))
				File.Create(settingsPath).Close();
		}

		static void ReadSettings()
		{
			if(File.Exists(settingsPath))
			{
				try
				{
					settings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(settingsPath));
				}
				catch(Exception e)
				{
					Debug.LogWarning($"Failed to read game settings at {settingsPath}.");
					Debug.LogError(e);
					settings = null;
				}
			}

			if(settings == null)
			{
				settings = new();
				WriteSettings();
			}

			onSettingsChanged?.Invoke();
		}

		public static void WriteSettings()
		{
			try
			{
				EnsureSettingsPath();
				File.WriteAllText(settingsPath, JsonUtility.ToJson(settings));
			}
			catch(Exception e)
			{
				Debug.LogWarning($"Failed to write game settings to {settingsPath}.");
				Debug.LogError(e);
			}
		}

		public static int ConvertToMonth(float time)
		{
			return Mathf.FloorToInt(time / InternalSettings.monthLength);
		}
		#endregion

		#region Savegames
		static readonly string savegamesDir = Path.Combine(Application.persistentDataPath, "savegames");
		static readonly List<string> savegameFilenames = new();
		public static IList<string> SavegameFilenames => savegameFilenames;
		public static Action onSavegamesChanged;

		static void EnsureSavegamesDir()
		{
			if(File.Exists(savegamesDir))
				File.Delete(savegamesDir);
			if(!Directory.Exists(savegamesDir))
				Directory.CreateDirectory(savegamesDir);
		}

		static string SavegameFileNameToPath(string filename)
		{
			return Path.Join(savegamesDir, filename);
		}

		public static void RefreshSavegames()
		{
			EnsureSavegamesDir();
			savegameFilenames.Clear();
			foreach(string path in Directory.EnumerateFiles(savegamesDir))
			{
				var filename = Path.GetRelativePath(savegamesDir, path);
				RecordSavegameFileName(filename, false);
			}
			onSavegamesChanged?.Invoke();
		}

		static void RecordSavegameFileName(string filename, bool invoke = true)
		{
			if(savegameFilenames.Contains(filename))
				return;
			savegameFilenames.Add(filename);
			if(invoke)
				onSavegamesChanged?.Invoke();
		}

		public static Savegame ReadSavegame(string filename)
		{
			var path = SavegameFileNameToPath(filename);
			try
			{
				var savegame = JsonUtility.FromJson<Savegame>(File.ReadAllText(path));
				return savegame;
			}
			catch(Exception e)
			{
				Debug.LogWarning($"Failed to read the savegame at {path}.");
				Debug.LogError(e);
				return null;
			}
		}

		public static void WriteSavegame(string filename, Savegame savegame)
		{
			var path = SavegameFileNameToPath(filename);
			try
			{
				if(!File.Exists(path))
					File.Create(path).Close();
				string json = JsonUtility.ToJson(savegame, true);
				File.WriteAllText(path, json);
				Debug.Log($"Wrote savegame to {path}.");
				RecordSavegameFileName(filename);
			}
			catch(Exception e)
			{
				Debug.LogWarning($"Failed to write savegame to {path}.");
				Debug.LogError(e);
			}
		}

		public static void DeleteSavegame(string filename)
		{
			var path = SavegameFileNameToPath(filename);
			try
			{
				if(!File.Exists(path))
					return;
				File.Delete(path);
				Debug.Log($"Delete savegame at {path}.");
				RefreshSavegames();
			}
			catch(Exception e)
			{
				Debug.LogWarning($"Failed to delete savegame at {path}.");
				Debug.LogError(e);
			}
		}
		#endregion
		#endregion

		#region Scene
		public static void SwitchScene(string sceneName)
		{
			SceneManager.LoadScene(sceneName);
		}

		static void OnSceneChanged(Scene previous, Scene after)
		{
			// Update environmental lighting.
			DynamicGI.UpdateEnvironment();
		}
		#endregion

		#region Game instance
		public static bool IsGameRunning => GameInstance.Instance != null;

		public static void StartNewGame()
		{
			if(IsGameRunning)
			{
				Debug.LogError("A game instance is already running, cannot start new game.");
				return;
			}

			GameData data = Utilities.DeepCopy(Resources.Load<GameDataSO>("Data/Initial Game Data").gameData);
			StartGameInstanceWithData(data);
		}

		public static void LoadGame(string filename)
		{
			if(IsGameRunning)
			{
				Debug.LogError("A game instance is already running, cannot load game.");
				return;
			}

			if(!savegameFilenames.Contains(filename))
			{
				Debug.LogError($"No savegame named {filename} exists, cannot load game.");
				return;
			}
			try
			{
				var savegame = ReadSavegame(filename);
				StartGameInstanceWithData(savegame.data);
			}
			catch(Exception e)
			{
				Debug.LogError($"Failed to load savegame from {filename}.");
				Debug.LogError(e);
				return;
			}
		}

		static void StartGameInstanceWithData(GameData data)
		{
			if(IsGameRunning)
			{
				Debug.LogError("A game instance is already running. Stopping it to start a new game.");
				StopCurrentGame();
			}

			GameObject go = new("Game Instance");
			go.AddComponent<GameInstance>();
			GameInstance.Instance.Data = data;
#if DEBUG && UNITY_EDITOR
			if(abruptDebug)
				abruptDebug = false;
			else
				SwitchScene("Polis");
#else
			SwitchScene("Polis");
#endif
		}

		public static void StopCurrentGame()
		{
			if(!IsGameRunning)
			{
				Debug.LogWarning("No game instance is currently running, cannot stop current game.");
				return;
			}

			// Destroy the current game instance.
			UnityEngine.Object.Destroy(GameInstance.Instance);
			SwitchScene("Start Menu");
		}

		public static void ExitGame()
		{
			Application.Quit();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}
		#endregion
	}
}
