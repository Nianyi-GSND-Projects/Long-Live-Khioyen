using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Rules/Identity Rule")]
    public class CommanderIdentityRuleSO : ScriptableObject
    {
        public string ruleName; // e.g., "Han", "Xiong"
        public Race race;
        [Header("Name Pool")]
        public List<string> firstNames;
        public List<string> lastNames;

        [Header("Portrait Pool")]
        public List<Sprite> portraits;

        public string GenerateName()
        {
            string first = firstNames != null && firstNames.Count > 0 ? firstNames[Random.Range(0, firstNames.Count)] : "";
            string last = lastNames != null && lastNames.Count > 0 ? lastNames[Random.Range(0, lastNames.Count)] : "Commander";
            return first + last; // 简单拼接，可扩展
        }

        public Sprite GetRandomPortrait()
        {
            if (portraits == null || portraits.Count == 0) return null;
            return portraits[Random.Range(0, portraits.Count)];
        }
    }
}