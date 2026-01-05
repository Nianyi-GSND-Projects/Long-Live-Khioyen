using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Commander/Generator Settings")]
    public class CommanderGeneratorSO : ScriptableObject
    {
        [Header("Name Pool")]
        public List<string> firstNames; // 姓
        public List<string> lastNames;  // 名

        [Header("Portrait Pool")]
        public List<Sprite> randomPortraits;

        [Header("Stat Generation")]
        public int minStatTotal = 150; // 五维总和最小值
        public int maxStatTotal = 350; // 五维总和最大值
        public int minSingleStat = 10;
        public int maxSingleStat = 90;

        // 生成随机名字
        public string GetRandomName()
        {
            string first = firstNames.Count > 0 ? firstNames[Random.Range(0, firstNames.Count)] : "Unknown";
            string last = lastNames.Count > 0 ? lastNames[Random.Range(0, lastNames.Count)] : "Commander";
            return last + first; // 根据语言习惯调整中间是否加空格
        }

        // 生成随机头像
        public Sprite GetRandomPortrait()
        {
            if (randomPortraits.Count == 0) return null;
            return randomPortraits[Random.Range(0, randomPortraits.Count)];
        }
    }
}