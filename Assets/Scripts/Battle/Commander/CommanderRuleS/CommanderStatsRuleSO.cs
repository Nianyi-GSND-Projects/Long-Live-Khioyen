using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Rules/Stats Rule")]
    public class CommanderStatsRuleSO : ScriptableObject
    {
        public string ruleName; // e.g., "Scholar", "Warrior", "Balanced"

        [Header("Stat Ranges")]
        public Vector2Int zhiRange = new Vector2Int(10, 90);
        public Vector2Int xinRange = new Vector2Int(10, 90);
        public Vector2Int renRange = new Vector2Int(10, 90);
        public Vector2Int yongRange = new Vector2Int(10, 90);
        public Vector2Int yanRange = new Vector2Int(10, 90);

        public void ApplyStats(GameCommander commander)
        {
            commander.Zhi = Random.Range(zhiRange.x, zhiRange.y);
            commander.Xin = Random.Range(xinRange.x, xinRange.y);
            commander.Ren = Random.Range(renRange.x, renRange.y);
            commander.Yong = Random.Range(yongRange.x, yongRange.y);
            commander.Yan = Random.Range(yanRange.x, yanRange.y);
        }
    }
}