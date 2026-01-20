using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Equipment Database")]
    public class EquipmentDatabase : ScriptableObject
    {
        private const string RESOURCE_PATH = "Data/EquipmentDB"; 
        
        #region Singleton
        private static EquipmentDatabase _instance;
        public static EquipmentDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EquipmentDatabase>(RESOURCE_PATH);
                    if (_instance == null)
                    {
                        Debug.LogError($"【致命错误】找不到装备数据库！请确保文件位于 'Assets/Resources/{RESOURCE_PATH}'");
                        return null;
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }
        #endregion

        public List<EquipmentDefinition> equipments = new List<EquipmentDefinition>();
        
        private Dictionary<int, EquipmentDefinition> _lookup;

        public void Initialize()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<int, EquipmentDefinition>();
            foreach (var eq in equipments)
            {
                if (eq == null) continue;
                if (!_lookup.ContainsKey(eq.equipmentId))
                {
                    _lookup.Add(eq.equipmentId, eq);
                }
            }
        }

        public EquipmentDefinition GetEquipment(int id)
        {
            if (_lookup == null) Initialize();
            
            if (_lookup.TryGetValue(id, out var eq))
                return eq;
            
            Debug.LogWarning($"未找到ID为 {id} 的装备");
            return null;
        }
    }
}