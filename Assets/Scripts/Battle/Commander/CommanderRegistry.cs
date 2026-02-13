using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
	[CreateAssetMenu(menuName = "Long Live Khioyen/Commander Registry")]
	public class CommanderRegistry : ScriptableObject
	{
		#region 单例
		static CommanderRegistry instance;
		public static CommanderRegistry Instance
		{
			get
			{
				if(instance == null)
				{
					instance = Resources.Load<CommanderRegistry>("Data/Commander Registry");
					instance.Initialize();
				}
				return instance;
			}
		}

		const string RESOURCE_PATH = "Data/Commander Settings";

		void Initialize()
		{
			var settings = Resources.Load<CommanderSystemSettings>(RESOURCE_PATH);

			if(settings == null)
			{
				Debug.LogError($"【致命错误】在 Resources 文件夹下找不到名为 '{RESOURCE_PATH}' 的配置文件！" +
											 "请确保创建了 CommanderSystemSettings 并放置在 Resources 文件夹内。");
				return;
			}

			// 从配置中提取数据
			this.generatorConfig = settings.generatorConfig;
			this.presetCommanders = settings.presetCommanders;

			// 开始注册预设名将
			LoadPresetCommanders();

			Debug.Log("CommanderRegistry 初始化完成 (Auto-Spawned)");
		}
		#endregion

		#region 指挥官
		List<GameCommander> activeCommanders = new();
		int idCounter = 1000;

		CommanderGeneratorSO generatorConfig;
		List<CommanderTemplateSO> presetCommanders;

		void LoadPresetCommanders()
		{
			if(presetCommanders == null) return;
			foreach(var preset in presetCommanders)
			{
				RegisterCommander(preset.CreateInstance(GenerateID()));
			}
		}

		int GenerateID() => idCounter++;

		public GameCommander GenerateRandomCommander()
		{
			if(generatorConfig == null)
			{
				Debug.LogError("Generator Config 未加载，无法生成指挥官");
				return null;
			}

			GameCommander newCmd = new();
			newCmd.commanderId = GenerateID();
			newCmd.commanderName = generatorConfig.GetRandomName();
			newCmd.portrait = generatorConfig.GetRandomPortrait();

			newCmd.Zhi = Random.Range(generatorConfig.minSingleStat, generatorConfig.maxSingleStat);
			newCmd.Xin = Random.Range(generatorConfig.minSingleStat, generatorConfig.maxSingleStat);
			newCmd.Ren = Random.Range(generatorConfig.minSingleStat, generatorConfig.maxSingleStat);
			newCmd.Yong = Random.Range(generatorConfig.minSingleStat, generatorConfig.maxSingleStat);
			newCmd.Yan = Random.Range(generatorConfig.minSingleStat, generatorConfig.maxSingleStat);

			RegisterCommander(newCmd);
			return newCmd;
		}

		public void RegisterCommander(GameCommander commander)
		{
			activeCommanders.Add(commander);
		}

		public List<GameCommander> GetAllFreeCommanders()
		{
			return activeCommanders.Where(c => !c.isAssigned).ToList();
		}
		#endregion
	}
}