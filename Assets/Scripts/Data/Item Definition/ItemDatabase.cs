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
        
        private Dictionary<string, ItemDefinition> _lookup;
        private Dictionary<string, ItemDefinition> _nameLookup;
        
        public void Initialize()
        {
            if (_lookup != null) return;

            _lookup = new();
            _nameLookup = new();

            foreach (var item in items)
            {
                if (item == null) continue;

                // ID 索引
                if (!_lookup.ContainsKey(item.itemId))
                {
                    _lookup.Add(item.itemId, item);
                }
                else
                {
                    Debug.LogWarning($"物品ID冲突: {item.itemId} 已存在 ({item.itemName})");
                }

                // 名字索引 (可选，方便调试)
                if (!_nameLookup.ContainsKey(item.itemName))
                {
                    _nameLookup.Add(item.itemName, item);
                }
            }
        }
        
        public ItemDefinition GetItem(string id)
        {
            if (_lookup == null) Initialize();
            
            if (_lookup.TryGetValue(id, out var item))
                return item;
            
            Debug.LogWarning($"未找到ID为 {id} 的物品");
            return null;
        }
        
        public ItemDefinition GetItemByName(string name)
        {
            if (_nameLookup == null) Initialize();
            
            if (_nameLookup.TryGetValue(name, out var item))
                return item;
            
            return null;
        }
    }
}
