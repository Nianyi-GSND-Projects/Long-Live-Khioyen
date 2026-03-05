using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class RandomEnemySpawnRule
    {
        public UnitDefinition unitDefinition;
        public Faction faction;
        [Min(0)] public int minCount = 1;
        [Min(0)] public int maxCount = 3;
        public bool useRandomCommander;
        public CommanderGenerationProfile commanderProfile;
    }
}