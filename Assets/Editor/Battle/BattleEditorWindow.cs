using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
    public class BattleEditorWindow : EditorWindow
    {
        private BattlePresetSO currentPreset;
        private TerrainDatabase _terrainDatabase;
        
        private enum ToolMode { None, Select, DeployPoint, ExtractionPoint,EnemySpawnZone, PlaceUnit, PlaceFacility, Erase }
        private ToolMode currentTool = ToolMode.None;

        private Faction selectedFaction = Faction.Enemy;
        private BattalionDefinition selectedBattalionDef;
        private FacilityDefinition selectedFacilityDef;

        private PreplacedUnitData _selectedUnitData;

        private const float CELL_SIZE = 30f;
        private const float OFFSET_X = 15f; 
        private Vector2 scrollPos;
        private Rect mapRenderRect; 
        
        private bool showEvents = false;
        private bool showReserve = false;

        [MenuItem("Long Live Khioyen/Battle Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<BattleEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            if (_terrainDatabase == null)
            {
                _terrainDatabase = Resources.Load<TerrainDatabase>("Data/TerrainDatabase");
                if (_terrainDatabase == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:TerrainDatabase");
                    if (guids.Length > 0)
                        _terrainDatabase = AssetDatabase.LoadAssetAtPath<TerrainDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (currentPreset != null && currentPreset.mapData != null)
            {
                DrawEditorArea();
                
                // [新增] 绘制底部单位配置面板
                DrawUnitInspector();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("请先指派一个 BattlePresetSO，并确保该 Preset 内已引用 MapDataSO。", MessageType.Info);
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Level Preset:", GUILayout.Width(80));
            currentPreset = (BattlePresetSO)EditorGUILayout.ObjectField(currentPreset, typeof(BattlePresetSO), false);
            GUILayout.EndHorizontal();

            if (currentPreset != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Map Data:", GUILayout.Width(80));
                EditorGUI.BeginChangeCheck();
                currentPreset.mapData = (MapDataSO)EditorGUILayout.ObjectField(currentPreset.mapData, typeof(MapDataSO), false);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(currentPreset);
                GUILayout.EndHorizontal();
            }
            
            showEvents = EditorGUILayout.Foldout(showEvents, "Level Events", true);
            if (showEvents && currentPreset != null)
            {
                GUILayout.BeginVertical("helpbox");
        
                // 简单的列表编辑
                int count = currentPreset.levelEvents.Count;
                int newCount = EditorGUILayout.IntField("Size", count);
        
                if (newCount != count)
                {
                    // 调整列表大小
                    while (currentPreset.levelEvents.Count < newCount) currentPreset.levelEvents.Add(null);
                    while (currentPreset.levelEvents.Count > newCount) currentPreset.levelEvents.RemoveAt(currentPreset.levelEvents.Count - 1);
                }

                for (int i = 0; i < currentPreset.levelEvents.Count; i++)
                {
                    currentPreset.levelEvents[i] = (BattleEventDefinition)EditorGUILayout.ObjectField(
                        $"Event {i}", 
                        currentPreset.levelEvents[i], 
                        typeof(BattleEventDefinition), 
                        false
                    );
                }
                
                
                
                if (GUI.changed) EditorUtility.SetDirty(currentPreset);
        
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);
            GUILayout.Label("Tools", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            // [修改] None 改为 Select
            if (DrawToggleBtn("Select", ToolMode.Select)) currentTool = ToolMode.Select;
            if (DrawToggleBtn("Eraser", ToolMode.Erase)) currentTool = ToolMode.Erase;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); 
            if (DrawToggleBtn("Deploy Zone", ToolMode.DeployPoint)) currentTool = ToolMode.DeployPoint;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); 
            if (DrawToggleBtn("Exit Zone", ToolMode.ExtractionPoint)) currentTool = ToolMode.ExtractionPoint;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (DrawToggleBtn("Unit (Bat)", ToolMode.PlaceUnit)) currentTool = ToolMode.PlaceUnit;
            if (DrawToggleBtn("Facility", ToolMode.PlaceFacility)) currentTool = ToolMode.PlaceFacility;
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.7f, 0.4f); // 橙色
            if (DrawToggleBtn("Enemy Spawn", ToolMode.EnemySpawnZone)) currentTool = ToolMode.EnemySpawnZone;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            DrawToolSettings();
            
            showReserve = EditorGUILayout.Foldout(showReserve, "Player Reserve (Preset)", true);
            
            if (showReserve && currentPreset != null)
            {
                GUILayout.BeginVertical("helpbox");
        
                currentPreset.usePresetPlayerArmy = EditorGUILayout.Toggle("Use Preset Army", currentPreset.usePresetPlayerArmy);

                if (currentPreset.usePresetPlayerArmy)
                {
                    // 简单的列表管理
                    int count = currentPreset.playerReserveList.Count;
                    int newCount = EditorGUILayout.IntField("Size", count);
            
                    if (newCount != count)
                    {
                        while (currentPreset.playerReserveList.Count < newCount) currentPreset.playerReserveList.Add(new PreplacedUnitData());
                        while (currentPreset.playerReserveList.Count > newCount) currentPreset.playerReserveList.RemoveAt(currentPreset.playerReserveList.Count - 1);
                    }

                    for (int i = 0; i < currentPreset.playerReserveList.Count; i++)
                    {
                        var data = currentPreset.playerReserveList[i];
                
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Unit {i}:");
                        data.battalionDef = (BattalionDefinition)EditorGUILayout.ObjectField(data.battalionDef, typeof(BattalionDefinition), false);
                
                        // 这里可以展开更多配置 (Commander, Overrides)，或者点击后在下方 Inspector 显示
                        // 为了简单，这里只显示 Def，如果需要详细配置，建议写个 CustomPropertyDrawer 或者复用 DrawUnitInspector
                        if (GUILayout.Button("Edit"))
                        {
                            _selectedUnitData = data; // 复用底部面板！
                        }
                        GUILayout.EndHorizontal();
                    }
                }
        
                if (GUI.changed) EditorUtility.SetDirty(currentPreset);
                GUILayout.EndVertical();
            }
            DrawRandomEnemySettings();
            GUILayout.EndVertical();
        }
        private void DrawRandomEnemySettings()
        {
            if (currentPreset == null) return;

            EditorGUILayout.Space();
            var so = new SerializedObject(currentPreset);
                GUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField("Random Unit Generation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(so.FindProperty("nonPlayerUnitsSpawnZones"), true);
                EditorGUILayout.PropertyField(so.FindProperty("randomEnemyRules"), true);
                GUILayout.EndVertical();

            so.ApplyModifiedProperties();
        }

        private bool DrawToggleBtn(string label, ToolMode mode)
        {
            bool isActive = currentTool == mode;
            if (GUILayout.Toggle(isActive, label, EditorStyles.toolbarButton)!= isActive)
            {
                if(!isActive) 
                    return true; 
            }
            return false;
        }

        private void DrawToolSettings()
        {
            if (currentTool == ToolMode.PlaceUnit || currentTool == ToolMode.PlaceFacility)
            {
                GUILayout.Space(5);
                GUILayout.Label("Spawn Settings", EditorStyles.boldLabel);
                GUILayout.BeginVertical("helpbox");
                
                selectedFaction = (Faction)EditorGUILayout.EnumPopup("Faction", selectedFaction);

                if (currentTool == ToolMode.PlaceUnit)
                {
                    selectedBattalionDef = (BattalionDefinition)EditorGUILayout.ObjectField("Battalion", selectedBattalionDef, typeof(BattalionDefinition), false);
                }
                else
                {
                    selectedFacilityDef = (FacilityDefinition)EditorGUILayout.ObjectField("Facility", selectedFacilityDef, typeof(FacilityDefinition), false);
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawEditorArea()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400)); // 限制高度，留出空间给底部面板
            
            int width = currentPreset.mapData.width;
            int height = currentPreset.mapData.height;

            float totalWidth = width * CELL_SIZE + OFFSET_X + 20;
            float totalHeight = height * CELL_SIZE + 20;
            mapRenderRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            EditorGUI.DrawRect(mapRenderRect, new Color(0.15f, 0.15f, 0.15f));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xPos = mapRenderRect.x + x * CELL_SIZE + (y % 2 == 1 ? OFFSET_X : 0);
                    float yPos = mapRenderRect.y + (height - 1 - y) * CELL_SIZE; 

                    Rect cellRect = new Rect(xPos, yPos, CELL_SIZE - 2, CELL_SIZE - 2);
                    Vector2Int gridPos = new Vector2Int(x, y);

                    string tId = currentPreset.mapData.GetTerrainAt(x, y);
                    Color baseColor = GetTerrainColor(tId);
                    EditorGUI.DrawRect(cellRect, baseColor);
                    
                    DrawOverlays(gridPos, cellRect);
                    DrawUnits(gridPos, cellRect);
                    
                    // 高亮选中单位
                    if (_selectedUnitData != null && _selectedUnitData.position == gridPos)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(1, 1, 0, 0.5f)); // 黄色高亮
                    }

                    if (cellRect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(cellRect, new Color(1, 1, 1, 0.3f));
                    }
                }
            }

            HandleInputInScrollArea();

            EditorGUILayout.EndScrollView();
        }

        // [新增] 底部单位配置面板
        private void DrawUnitInspector()
        {
            if (_selectedUnitData == null) return;

            GUILayout.BeginVertical("box");
            GUILayout.Label($"Unit Configuration (ID: {_selectedUnitData.instanceId})", EditorStyles.boldLabel); // 显示 ID
            
            EditorGUI.BeginChangeCheck();

            // 基础信息 (只读)
            EditorGUILayout.LabelField("Type", _selectedUnitData.isFacility ? "Facility" : "Battalion");
            EditorGUILayout.LabelField("Definition", _selectedUnitData.isFacility ? 
                (_selectedUnitData.facilityDef ? _selectedUnitData.facilityDef.name : "None") : 
                (_selectedUnitData.battalionDef ? _selectedUnitData.battalionDef.name : "None"));
            
            _selectedUnitData.isVisible = EditorGUILayout.Toggle("Is Visible", _selectedUnitData.isVisible);
            GUILayout.Space(5);
            
            // 覆盖属性
            _selectedUnitData.overrideSoldiers = EditorGUILayout.IntField("Override Soldiers (-1 Default)", _selectedUnitData.overrideSoldiers);
            _selectedUnitData.overrideMorale = EditorGUILayout.IntField("Override Morale (-1 Default)", _selectedUnitData.overrideMorale);

            GUILayout.Space(5);
            
            // 指挥官配置 (仅针对 Battalion)
            if (!_selectedUnitData.isFacility)
            {
                _selectedUnitData.commanderTemplate = (CommanderTemplateSO)EditorGUILayout.ObjectField("Commander Template", _selectedUnitData.commanderTemplate, typeof(CommanderTemplateSO), false);

                _selectedUnitData.useRandomCommander = EditorGUILayout.Toggle("Use Random Commander", _selectedUnitData.useRandomCommander);
                
                if (_selectedUnitData.useRandomCommander)
                {
                    GUILayout.BeginVertical("helpbox");
                    GUILayout.Label("Random Generation Profile", EditorStyles.miniLabel);
                    
                    // 由于 CommanderGenerationProfile 是 struct，需要重新赋值
                    var profile = _selectedUnitData.randomCommanderProfile;
                    profile.identityRule = EditorGUILayout.TextField("Identity Rule", profile.identityRule);
                    profile.statsRule = EditorGUILayout.TextField("Stats Rule", profile.statsRule);
                    profile.traitsRule = EditorGUILayout.TextField("Traits Rule", profile.traitsRule);
                    profile.level = EditorGUILayout.IntField("Level", profile.level);
                    _selectedUnitData.randomCommanderProfile = profile;
                    
                    GUILayout.EndVertical();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentPreset);
            }
            
            GUILayout.EndVertical();
        }

        private void HandleInputInScrollArea()
        {
            Event e = Event.current;
            
            if (mapRenderRect.Contains(e.mousePosition))
            {
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    float relativeX = e.mousePosition.x - mapRenderRect.x;
                    float relativeY = e.mousePosition.y - mapRenderRect.y;

                    int mapH = currentPreset.mapData.height;
                    int y = mapH - 1 - Mathf.FloorToInt(relativeY / CELL_SIZE);

                    if (y >= 0 && y < mapH)
                    {
                        float rowOffset = (y % 2 == 1 ? OFFSET_X : 0);
                        int x = Mathf.FloorToInt((relativeX - rowOffset) / CELL_SIZE);

                        if (x >= 0 && x < currentPreset.mapData.width)
                        {
                            ApplyPaint(new Vector2Int(x, y));
                            e.Use(); 
                            Repaint(); 
                        }
                    }
                }
            }
        }

        private void ApplyPaint(Vector2Int pos)
        {
            // [修改] Select 模式逻辑
            if (currentTool == ToolMode.Select)
            {
                if (currentPreset.preplacedUnits != null)
                {
                    var unit = currentPreset.preplacedUnits.FirstOrDefault(u => u.position == pos);
                    if (unit != null)
                    {
                        _selectedUnitData = unit;
                        // 可以在这里打印日志确认选中
                        // Debug.Log($"Selected unit at {pos}");
                    }
                    else
                    {
                        _selectedUnitData = null; // 点击空地取消选择
                    }
                }
                return; // 选择模式不修改数据，直接返回
            }

            Undo.RecordObject(currentPreset, "Edit Level");
            if (currentPreset.nonPlayerUnitsSpawnZones == null) currentPreset.nonPlayerUnitsSpawnZones = new List<Vector2Int>();
            if (currentPreset.playerDeployPoints == null) currentPreset.playerDeployPoints = new List<Vector2Int>();
            if (currentPreset.extractionPoints == null) currentPreset.extractionPoints = new List<Vector2Int>();
            if (currentPreset.preplacedUnits == null) currentPreset.preplacedUnits = new List<PreplacedUnitData>();

            switch (currentTool)
            {
                case ToolMode.DeployPoint:
                    ToggleList(currentPreset.playerDeployPoints, pos);
                    if(currentPreset.playerDeployPoints.Contains(pos)) currentPreset.extractionPoints.Remove(pos);
                    break;

                case ToolMode.ExtractionPoint:
                    ToggleList(currentPreset.extractionPoints, pos);
                    if(currentPreset.extractionPoints.Contains(pos)) currentPreset.playerDeployPoints.Remove(pos);
                    break;

                case ToolMode.PlaceUnit:
                    if (selectedBattalionDef == null) return;
                    PlaceUnit(pos, false);
                    break;

                case ToolMode.PlaceFacility:
                    if (selectedFacilityDef == null) return;
                    PlaceUnit(pos, true);
                    break;
                
                case ToolMode.Erase:
                    EraseAt(pos);
                    break;
                
                case ToolMode.EnemySpawnZone:
                    ToggleList(currentPreset.nonPlayerUnitsSpawnZones,pos);
                    break;
            }

            EditorUtility.SetDirty(currentPreset);
        }

        private void ToggleList(List<Vector2Int> list, Vector2Int pos)
        {
            if (list.Contains(pos)) list.Remove(pos);
            else list.Add(pos);
        }

        private void PlaceUnit(Vector2Int pos, bool isFacility)
        {
            currentPreset.preplacedUnits.RemoveAll(u => u.position == pos);
            int newId = GetNextAvailableId();
            
            PreplacedUnitData newData = new PreplacedUnitData
            {
                instanceId = newId,
                position = pos,
                faction = selectedFaction,
                isFacility = isFacility,
                battalionDef = isFacility ? null : selectedBattalionDef,
                facilityDef = isFacility ? selectedFacilityDef : null
            };
            currentPreset.preplacedUnits.Add(newData);
            
            // [新增] 放置后自动选中，方便立即配置
            _selectedUnitData = newData;
        }

        private void EraseAt(Vector2Int pos)
        {
            currentPreset.playerDeployPoints.Remove(pos);
            currentPreset.extractionPoints.Remove(pos);
            currentPreset.preplacedUnits.RemoveAll(u => u.position == pos);
            
            if (_selectedUnitData != null && _selectedUnitData.position == pos)
            {
                _selectedUnitData = null;
            }
        }
        
        private int GetNextAvailableId()
        {
            if (currentPreset.preplacedUnits == null || currentPreset.preplacedUnits.Count == 0) return 1;

            // 获取所有已占用的 ID 并排序
            var usedIds = currentPreset.preplacedUnits
                .Select(u => u.instanceId)
                .Where(id => id > 0) // 忽略无效 ID
                .OrderBy(id => id)
                .ToList();

            int next = 1;
            foreach (var id in usedIds)
            {
                if (id == next) next++;
                else if (id > next) return next; // 发现空缺 (例如有 1, 3, 返回 2)
            }
            return next; // 没有空缺，返回最大值 + 1
        }

        private void DrawOverlays(Vector2Int pos, Rect rect)
        {
            if (currentPreset.playerDeployPoints != null && currentPreset.playerDeployPoints.Contains(pos))
            {
                EditorGUI.DrawRect(rect, new Color(0f, 0.5f, 1f, 0.4f)); 
                GUI.Label(rect, "D", EditorStyles.miniLabel);
            }

            if (currentPreset.extractionPoints != null && currentPreset.extractionPoints.Contains(pos))
            {
                EditorGUI.DrawRect(rect, new Color(0f, 1f, 0.2f, 0.4f));
                GUI.Label(rect, "E", EditorStyles.miniLabel);
            }

            if (currentPreset.nonPlayerUnitsSpawnZones != null && currentPreset.nonPlayerUnitsSpawnZones.Contains(pos))
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0f, 0.2f, 0.4f));
                GUI.Label(rect, "N", EditorStyles.miniLabel);
            }
        }

        private void DrawUnits(Vector2Int pos, Rect rect)
        {
            if (currentPreset.preplacedUnits == null) return;

            var unitData = currentPreset.preplacedUnits.FirstOrDefault(u => u.position == pos);
            if (unitData != null)
            {
                Rect unitRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
                Color factionColor = unitData.faction == Faction.Player ? Color.cyan : Color.red;
                if (unitData.faction == Faction.Neutral) factionColor = Color.yellow;
                
                EditorGUI.DrawRect(unitRect, factionColor);

                string label = unitData.isFacility ? "F" : "B";
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = Color.black;
                style.alignment = TextAnchor.MiddleCenter;
                GUI.Label(unitRect, label, style);
            }
        }

        private Color GetTerrainColor(string id)
        {
            if (_terrainDatabase == null) return Color.gray;
            var def = _terrainDatabase.GetTerrain(id);
            if (def != null && def.material != null && def.material.HasProperty("_Color"))
                return def.material.color;
            return Color.white;
        }
    }
}