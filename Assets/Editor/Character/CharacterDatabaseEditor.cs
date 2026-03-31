using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(CharacterDatabase))]
public class CharacterDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        CharacterDatabase db = (CharacterDatabase)target;
        
        GUILayout.Space(10);
        if (GUILayout.Button("Collect All Characters", GUILayout.Height(30))) 
        {
            CollectCharacters(db);
        }
    }

    private void CollectCharacters(CharacterDatabase db)
    {
        // 使用反射访问私有字段 characters
        FieldInfo field = typeof(CharacterDatabase).GetField("characters", BindingFlags.NonPublic | BindingFlags.Instance);
        List<GameCharacter> list = (List<GameCharacter>)field.GetValue(db);
        if (list == null) list = new List<GameCharacter>();

        string[] guids = AssetDatabase.FindAssets("t:GameCharacter");
        List<GameCharacter> all = new List<GameCharacter>();
        
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameCharacter>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) all.Add(asset);
        }

        AssignIds(all, (c) => c.characterId, (c, id) => c.characterId = id);
        
        list.Clear();
        list.AddRange(all);
        list.Sort((a, b) => a.characterId.CompareTo(b.characterId));
        
        field.SetValue(db, list);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Collected {list.Count} characters into Character Database.");
    }

    private void AssignIds<T>(List<T> items, System.Func<T, int> getId, System.Action<T, int> setId) where T : Object
    {
        HashSet<int> used = new HashSet<int>();
        List<T> toAssign = new List<T>();

        foreach (var item in items)
        {
            int id = getId(item);
            if (id > 0 && !used.Contains(id)) used.Add(id);
            else toAssign.Add(item);
        }

        int next = 1;
        foreach (var item in toAssign)
        {
            while (used.Contains(next)) next++;
            setId(item, next);
            used.Add(next);
            EditorUtility.SetDirty(item);
        }
    }
}