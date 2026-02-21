using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Data/Trait")]
    public class CommanderTraitSO : ScriptableObject
    {
        [TextArea] public string description;
        // TODO: 特性的具体影响逻辑 (被动技能)
    }
}