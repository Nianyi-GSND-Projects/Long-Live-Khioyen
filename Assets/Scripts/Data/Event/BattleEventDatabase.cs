using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Battle Event Database")]
    public class BattleEventDatabase : ScriptableObject
    {
        public static BattleEventDatabase Instance => Resources.Load<BattleEventDatabase>("Data/BattleEventDatabase");

        public List<BattleEventDefinition> events = new List<BattleEventDefinition>();

        public BattleEventDefinition GetEvent(int id)
        {
            return events.Find(e => e.id == id);
        }
    }
}