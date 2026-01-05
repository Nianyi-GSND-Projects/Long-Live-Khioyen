using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LongLiveKhioyen
{
    public class CommanderRegistry : MonoBehaviour
    {
        private const string RESOURCE_PATH = "Data/CommanderSettings";

        #region Auto-Generated Singleton
        
        private static CommanderRegistry _instance;
        public static CommanderRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CommanderRegistry>();

                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("_CommanderRegistry_Auto");
                        _instance = obj.AddComponent<CommanderRegistry>();
                        
                        DontDestroyOnLoad(obj); 
                        
                        _instance.Initialize(); 
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        // 运行时数据
        [SerializeField]
        private List<GameCommander> activeCommanders = new List<GameCommander>();
        private int idCounter = 1000;
        
        private CommanderGeneratorSO generatorConfig;
        private List<CommanderTemplateSO> presetCommanders;

        private void Initialize()
        {
            var settings = Resources.Load<CommanderSystemSettings>(RESOURCE_PATH);

            if (settings == null)
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

        private void LoadPresetCommanders()
        {
            if (presetCommanders == null) return;
            foreach (var preset in presetCommanders)
            {
                RegisterCommander(preset.CreateInstance(GenerateID()));
            }
        }

        private int GenerateID() => idCounter++;

        public GameCommander GenerateRandomCommander()
        {
            if (generatorConfig == null)
            {
                Debug.LogError("Generator Config 未加载，无法生成指挥官");
                return null;
            }

            GameCommander newCmd = new GameCommander();
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
    }
}