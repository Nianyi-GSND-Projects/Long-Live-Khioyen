using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace LongLiveKhioyen
{
    public class BattleEditorWindow : EditorWindow
    {
        // ----------------------------------------------------
        // 数据引用
        // ----------------------------------------------------
        private BattlePresetSO currentPreset;
        private TerrainDatabase _terrainDatabase;
        
        // ----------------------------------------------------
        // 编辑器状态
        // ----------------------------------------------------
        private enum ToolMode { None, DeployPoint, ExtractionPoint, PlaceUnit, PlaceFacility, Erase }
        private ToolMode currentTool = ToolMode.None;

        // 笔刷设置
        private Faction selectedFaction = Faction.Enemy;
        private BattalionDefinition selectedBattalionDef;
        private FacilityDefinition selectedFacilityDef;

        // 视觉设置 (保持与 MapEditor 一致)
        private const float CELL_SIZE = 30f;
        private const float OFFSET_X = 15f; 
        private Vector2 scrollPos;

        [MenuItem("Long Live Khioyen/Battle Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<BattleEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            // 自动加载地形数据库用于显示底图
            if (_terrainDatabase == null)
            {
                _terrainDatabase = Resources.Load<TerrainDatabase>("Data/TerrainDatabase");
                if (_terrainDatabase == null)
                {
                    // 尝试搜索
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
                HandleInput();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("请先指派一个 BattlePresetSO，并确保该 Preset 内已引用 MapDataSO。", MessageType.Info);
                GUILayout.FlexibleSpace();
            }
        }

        // ----------------------------------------------------
        // 工具栏与设置
        // ----------------------------------------------------
        private void DrawToolbar()
        {
            GUILayout.BeginVertical("box");
            
            // 1. 基础文件配置
            GUILayout.BeginHorizontal();
            GUILayout.Label("Level Preset:", GUILayout.Width(80));
            currentPreset = (BattlePresetSO)EditorGUILayout.ObjectField(currentPreset, typeof(BattlePresetSO), false);
            GUILayout.EndHorizontal();

            if (currentPreset != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Map Data:", GUILayout.Width(80));
                // 允许在这里更换底图
                EditorGUI.BeginChangeCheck();
                currentPreset.mapData = (MapDataSO)EditorGUILayout.ObjectField(currentPreset.mapData, typeof(MapDataSO), false);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(currentPreset);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.Label("Tools", EditorStyles.boldLabel);

            // 2. 工具模式选择 (Grid Layout)
            GUILayout.BeginHorizontal();
            if (DrawToggleBtn("View", ToolMode.None)) currentTool = ToolMode.None;
            if (DrawToggleBtn("Eraser", ToolMode.Erase)) currentTool = ToolMode.Erase;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // 浅蓝
            if (DrawToggleBtn("Deploy Zone", ToolMode.DeployPoint)) currentTool = ToolMode.DeployPoint;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // 浅绿
            if (DrawToggleBtn("Exit Zone", ToolMode.ExtractionPoint)) currentTool = ToolMode.ExtractionPoint;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (DrawToggleBtn("Unit (Bat)", ToolMode.PlaceUnit)) currentTool = ToolMode.PlaceUnit;
            if (DrawToggleBtn("Facility", ToolMode.PlaceFacility)) currentTool = ToolMode.PlaceFacility;
            GUILayout.EndHorizontal();

            // 3. 动态笔刷设置
            DrawToolSettings();

            GUILayout.EndVertical();
        }

        private bool DrawToggleBtn(string label, ToolMode mode)
        {
            bool isActive = currentTool == mode;
            if (GUILayout.Toggle(isActive, label, EditorStyles.toolbarButton))
            {
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

        // ----------------------------------------------------
        // 地图绘制 (核心可视化)
        // ----------------------------------------------------
        
        // 在类中定义一个成员变量用来存储当前的绘图区域
private Rect mapRenderRect; 

private void DrawEditorArea()
{
    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
    
    int width = currentPreset.mapData.width;
    int height = currentPreset.mapData.height;

    // 1. 获取准确的绘制区域 (这是核心修复)
    // 这里申请了布局空间，Unity会自动帮我们算出它在窗口中的准确位置(x, y)
    float totalWidth = width * CELL_SIZE + OFFSET_X + 20;
    float totalHeight = height * CELL_SIZE + 20;
    mapRenderRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

    // 绘制背景
    EditorGUI.DrawRect(mapRenderRect, new Color(0.15f, 0.15f, 0.15f));

    // --- 开始绘制格子 ---
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            // 注意：这里所有的坐标计算都要基于 mapRenderRect.x 和 mapRenderRect.y
            float xPos = mapRenderRect.x + x * CELL_SIZE + (y % 2 == 1 ? OFFSET_X : 0);
            
            // Y轴反转 (0在最下方)
            float yPos = mapRenderRect.y + (height - 1 - y) * CELL_SIZE; 

            Rect cellRect = new Rect(xPos, yPos, CELL_SIZE - 2, CELL_SIZE - 2);
            Vector2Int gridPos = new Vector2Int(x, y);

            // ... (原本的绘制代码：画地形、画单位、画框等，保持不变) ...
            
            // 绘制地形底色
            string tId = currentPreset.mapData.GetTerrainAt(x, y);
            Color baseColor = GetTerrainColor(tId);
            EditorGUI.DrawRect(cellRect, baseColor);
            
            // 绘制单位和覆盖层 (复用之前的逻辑)
            DrawOverlays(gridPos, cellRect);
            DrawUnits(gridPos, cellRect);
            
            // 鼠标悬停高亮
            if (cellRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(cellRect, new Color(1, 1, 1, 0.3f));
            }
        }
    }

    // --- 在 ScrollView 内部直接处理输入 ---
    // 这样不需要去猜 Toolbar 的高度，因为 mapRenderRect 包含了准确位置
    HandleInputInScrollArea();

    EditorGUILayout.EndScrollView();
}

private void HandleInputInScrollArea()
{
    Event e = Event.current;
    
    // 只有鼠标在地图区域内才响应
    if (mapRenderRect.Contains(e.mousePosition))
    {
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            // 1. 计算鼠标相对于地图区域左上角的偏移
            float relativeX = e.mousePosition.x - mapRenderRect.x;
            float relativeY = e.mousePosition.y - mapRenderRect.y;

            int mapH = currentPreset.mapData.height;

            // 2. 反算 Y 轴 (注意：绘制时用了 height - 1 - y，这里要反过来)
            // 公式：yPos = startY + (H - 1 - y) * size
            // => (yPos - startY) / size = H - 1 - y
            // => y = H - 1 - ((yPos - startY) / size)
            int y = mapH - 1 - Mathf.FloorToInt(relativeY / CELL_SIZE);

            if (y >= 0 && y < mapH)
            {
                // 3. 反算 X 轴 (考虑奇偶行偏移)
                float rowOffset = (y % 2 == 1 ? OFFSET_X : 0);
                int x = Mathf.FloorToInt((relativeX - rowOffset) / CELL_SIZE);

                if (x >= 0 && x < currentPreset.mapData.width)
                {
                    ApplyPaint(new Vector2Int(x, y));
                    e.Use(); // 消费事件
                    Repaint(); // 强制重绘以显示点击反馈
                }
            }
        }
    }
}
       

        private void DrawOverlays(Vector2Int pos, Rect rect)
        {
            // 部署点 - 蓝色边框或半透明填充
            if (currentPreset.playerDeployPoints != null && currentPreset.playerDeployPoints.Contains(pos))
            {
                EditorGUI.DrawRect(rect, new Color(0f, 0.5f, 1f, 0.4f)); 
                GUI.Label(rect, "D", EditorStyles.miniLabel);
            }

            // 撤离点 - 绿色边框或半透明填充
            if (currentPreset.extractionPoints != null && currentPreset.extractionPoints.Contains(pos))
            {
                EditorGUI.DrawRect(rect, new Color(0f, 1f, 0.2f, 0.4f));
                GUI.Label(rect, "E", EditorStyles.miniLabel);
            }
        }

        private void DrawUnits(Vector2Int pos, Rect rect)
        {
            if (currentPreset.preplacedUnits == null) return;

            // 查找该位置是否有单位
            var unitData = currentPreset.preplacedUnits.FirstOrDefault(u => u.position == pos);
            if (unitData != null)
            {
                // 绘制阵营颜色的小方块
                Rect unitRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
                Color factionColor = unitData.faction == Faction.Player ? Color.cyan : Color.red;
                if (unitData.faction == Faction.Neutral) factionColor = Color.yellow;
                
                EditorGUI.DrawRect(unitRect, factionColor);

                // 绘制标识文字 (Facility vs Battalion)
                string label = unitData.isFacility ? "F" : "B";
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = Color.black;
                style.alignment = TextAnchor.MiddleCenter;
                GUI.Label(unitRect, label, style);
            }
        }

        // ----------------------------------------------------
        // 输入处理
        // ----------------------------------------------------
        private void HandleInput()
        {
            Event e = Event.current;
            
            // 简单的点击检测
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                // 注意：这里需要根据 Toolbar 的高度进行调整，或者更稳健地使用 GUILayoutUtility 计算的区域
                // 为了简化，我们假设用户是在 DrawEditorArea 的 ScrollView 内部操作的
                // 最准确的方法是做射线检测，但在 EditorGUI 里我们倒推坐标

                // 获取鼠标相对于 ScrollView 的位置
                Vector2 mouseInScroll = e.mousePosition; 

                // 粗略计算：
                // Toolbar高度约 130px (随内容变化)，这部分比较 tricky。
                // 更好的方式是：在 DrawEditorArea 记录下 `drawArea` 的屏幕坐标，这里为了代码简洁
                // 我们通过简单的布局估算。在 ScrollView 内部，坐标是 (0,0) 开始的。
                // 但是 Event.mousePosition 是相对于 Window 的。
                // 所以我们需要减去 ScrollView 的起始 Y。

                // 简化方案：依靠 TryGetGridPositionFromMouse 逻辑
                if (TryGetGridPositionFromMouse(e.mousePosition, out Vector2Int gridPos))
                {
                    ApplyPaint(gridPos);
                    e.Use();
                    Repaint();
                }
            }
        }

        private bool TryGetGridPositionFromMouse(Vector2 mousePos, out Vector2Int gridPos)
        {
            gridPos = Vector2Int.zero;
            
            // 这里是一个简化的反算，假设 Toolbar 高度固定。
            // 实际项目中建议使用 GUILayoutUtility.GetLastRect() 在 DrawEditorArea 中记录区域
            float toolbarHeight = 140f; 
            float relativeY = mousePos.y - toolbarHeight + scrollPos.y;
            float relativeX = mousePos.x + scrollPos.x;

            if (relativeY < 0) return false;

            int mapH = currentPreset.mapData.height;
            
            // 反转 Y 轴逻辑
            int y = mapH - 1 - Mathf.FloorToInt(relativeY / CELL_SIZE);
            if (y < 0 || y >= mapH) return false;

            float rowOffsetX = (y % 2 == 1 ? OFFSET_X : 0);
            int x = Mathf.FloorToInt((relativeX - rowOffsetX - 5) / CELL_SIZE); // 5是左padding
            
            if (x < 0 || x >= currentPreset.mapData.width) return false;

            gridPos = new Vector2Int(x, y);
            return true;
        }

        // ----------------------------------------------------
        // 笔刷逻辑
        // ----------------------------------------------------
        private void ApplyPaint(Vector2Int pos)
        {
            // 记录撤销操作
            Undo.RecordObject(currentPreset, "Edit Level");

            // 初始化列表以防为空
            if (currentPreset.playerDeployPoints == null) currentPreset.playerDeployPoints = new List<Vector2Int>();
            if (currentPreset.extractionPoints == null) currentPreset.extractionPoints = new List<Vector2Int>();
            if (currentPreset.preplacedUnits == null) currentPreset.preplacedUnits = new List<UnitSpawnData>();

            switch (currentTool)
            {
                case ToolMode.DeployPoint:
                    ToggleList(currentPreset.playerDeployPoints, pos);
                    // 互斥：如果是部署点，就不能是撤离点
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
            }

            // 标记脏数据，确保保存
            EditorUtility.SetDirty(currentPreset);
        }

        private void ToggleList(List<Vector2Int> list, Vector2Int pos)
        {
            if (list.Contains(pos)) list.Remove(pos);
            else list.Add(pos);
        }

        private void PlaceUnit(Vector2Int pos, bool isFacility)
        {
            // 1. 移除该位置已有的单位
            currentPreset.preplacedUnits.RemoveAll(u => u.position == pos);

            // 2. 添加新单位
            UnitSpawnData newData = new UnitSpawnData
            {
                position = pos,
                faction = selectedFaction,
                isFacility = isFacility,
                battalionDef = isFacility ? null : selectedBattalionDef,
                facilityDef = isFacility ? selectedFacilityDef : null
            };
            currentPreset.preplacedUnits.Add(newData);
        }

        private void EraseAt(Vector2Int pos)
        {
            currentPreset.playerDeployPoints.Remove(pos);
            currentPreset.extractionPoints.Remove(pos);
            currentPreset.preplacedUnits.RemoveAll(u => u.position == pos);
        }

        // ----------------------------------------------------
        // 辅助
        // ----------------------------------------------------
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