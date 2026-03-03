using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Item DataBase")]
    public class ItemDatabase : ScriptableObject
    {
        private const string RESOURCE_PATH = "Data/ItemDB";
        
        #region Singleton
        private static ItemDatabase _instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ItemDatabase>(RESOURCE_PATH);
                    if (_instance == null)
                    {
                        Debug.LogError($"【致命错误】找不到物品数据库！请确保文件位于 'Assets/Resources/{RESOURCE_PATH}'");
                        return null;
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }
        #endregion
        
        public List<ItemDefinition> items = new List<ItemDefinition>();
        public List<EquipmentDefinition> equipments = new List<EquipmentDefinition>();
        
        // 原有的基于 String ID 的查找表
        private Dictionary<string, ItemDefinition> _lookup;
        private Dictionary<string, ItemDefinition> _nameLookup;
        
        // [新增] 基于 Int ID 的查找表
        private Dictionary<int, ItemDefinition> _intIdLookup;
        
        public void Initialize()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, ItemDefinition>();
            _nameLookup = new Dictionary<string, ItemDefinition>();
            _intIdLookup = new Dictionary<int, ItemDefinition>(); // 初始化

            foreach (var item in items)
            {
                if (item == null) continue;

                // String ID 索引
                if (!string.IsNullOrEmpty(item.itemId) && !_lookup.ContainsKey(item.itemId))
                {
                    _lookup.Add(item.itemId, item);
                }

                // Name 索引
                if (!string.IsNullOrEmpty(item.itemName) && !_nameLookup.ContainsKey(item.itemName))
                {
                    _nameLookup.Add(item.itemName, item);
                }

                // [新增] Int ID 索引
                if (!_intIdLookup.ContainsKey(item.id))
                {
                    _intIdLookup.Add(item.id, item);
                }
                else
                {
                    Debug.LogWarning($"ItemDatabase: Duplicate Int ID {item.id} for {item.itemName}");
                }
            }
        }
        
        public ItemDefinition GetItem(string id)
        {
            if (_lookup == null) Initialize();
            
            if (_lookup.TryGetValue(id, out var item))
                return item;
            
            return null;
        }
        
        public ItemDefinition GetItemByName(string name)
        {
            if (_nameLookup == null) Initialize();
            
            if (_nameLookup.TryGetValue(name, out var item))
                return item;
            
            return null;
        }

        // [新增] 基于 Int ID 获取物品
        public ItemDefinition GetItem(int id)
        {
            if (_intIdLookup == null) Initialize();

            if (_intIdLookup.TryGetValue(id, out var item))
            {
                return item;
            }
            
            Debug.LogWarning($"ItemDatabase: Item Int ID {id} not found.");
            return null;
        }
        
        // Helper to get Equipment specifically if needed, though GetItem(int) works for both
        public EquipmentDefinition GetEquipment(int id)
        {
            var item = GetItem(id);
            return item as EquipmentDefinition;
        }
    }
}
