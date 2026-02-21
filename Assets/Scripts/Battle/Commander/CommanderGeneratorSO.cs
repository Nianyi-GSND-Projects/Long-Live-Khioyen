using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Commander/Generator Settings")]
    public class CommanderGeneratorSO : ScriptableObject
    {
        [Header("Rule Libraries")]
        public List<CommanderIdentityRuleSO> identityRules = new List<CommanderIdentityRuleSO>();
        public List<CommanderStatsRuleSO> statsRules = new List<CommanderStatsRuleSO>();
        public List<CommanderTraitsRuleSO> traitsRules = new List<CommanderTraitsRuleSO>();

        public GameCommander Generate(CommanderGenerationProfile profile, int newId)
        {
            GameCommander cmd = new GameCommander();
            cmd.commanderId = newId;

            // 1. Identity
            var identityRule = identityRules.Find(r => r.ruleName == profile.identityRule);
            if (identityRule == null && identityRules.Count > 0) identityRule = identityRules[0]; // Fallback
            
            if (identityRule != null)
            {
                cmd.commanderName = identityRule.GenerateName();
                cmd.portrait = identityRule.GetRandomPortrait();
            }
            else
            {
                cmd.commanderName = "Unknown";
            }

            // 2. Stats
            var statsRule = statsRules.Find(r => r.ruleName == profile.statsRule);
            if (statsRule == null && statsRules.Count > 0) statsRule = statsRules[0]; // Fallback

            if (statsRule != null)
            {
                statsRule.ApplyStats(cmd);
            }
            else
            {
                // 默认属性
                cmd.Zhi = cmd.Xin = cmd.Ren = cmd.Yong = cmd.Yan = 50;
            }

            // 3. Traits
            var traitsRule = traitsRules.Find(r => r.ruleName == profile.traitsRule);
            if (traitsRule == null && traitsRules.Count > 0) traitsRule = traitsRules[0]; // Fallback

            if (traitsRule != null)
            {
                traitsRule.ApplyTraits(cmd);
            }

            return cmd;
        }
    }
}