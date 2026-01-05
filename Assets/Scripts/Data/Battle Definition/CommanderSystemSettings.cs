using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    // 这个文件用来在Project窗口里配置所有引用
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Commander/System Settings")]
    public class CommanderSystemSettings : ScriptableObject
    {
        [Header("Core Config")]
        public CommanderGeneratorSO generatorConfig;
        public List<CommanderTemplateSO> presetCommanders;
    }
}