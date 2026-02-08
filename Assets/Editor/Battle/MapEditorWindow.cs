using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class MapEditorWindow : EditorWindow
    {
        // ----------------------------------------------------
        // 数据部分
        // ----------------------------------------------------
        private MapDataSO currentMapAsset; // 当前正在编辑的资源文件
        private TerrainDatabase _terrainDatabase; // 用于获取地形列表和颜色

        private int mapWidth = 10;
        private int mapHeight = 10;

        // 临时编辑数据 (y, x) -> string ID
        // 使用一维数组模拟二维以简化逻辑: index = y * width + x
        private string[] tempMapData;

        // 笔刷设置
        private int selectedTerrainIndex = 0; // 当前选中的地形在 DB 列表中的索引

        private string CurrentBrushId => _terrainDatabase != null && _terrainDatabase.terrainDefinitions.Count > 0
            ? _terrainDatabase.terrainDefinitions[selectedTerrainIndex].terrainName
            : "Plain";

        // 视觉设置
        private const float CELL_SIZE = 30f;
        private const float OFFSET_X = 15f; // 奇数行偏移量
        private Vector2 scrollPos;

        // ----------------------------------------------------
        // 菜单入口
        // ----------------------------------------------------
        [MenuItem("Long Live Khioyen/Map Editor")]
        public static void ShowWindow()
        {
            GetWindow<MapEditorWindow>("Map Editor");
        }

        private void OnEnable()
        {
            // 自动寻找 TerrainDatabase
            // 注意：这里假设你之前已经把 TerrainDatabase 改为了 Resources 单例模式
            // 如果没有，这里尝试用 AssetDatabase 查找
            if (_terrainDatabase == null)
            {
                _terrainDatabase = Resources.Load<TerrainDatabase>("Data/TerrainDatabase");
                if (_terrainDatabase == null)
                {
                    // 备用方案：全项目搜索
                    string[] guids = AssetDatabase.FindAssets("t:TerrainDatabase");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        _terrainDatabase = AssetDatabase.LoadAssetAtPath<TerrainDatabase>(path);
                    }
                }
            }
        }

        // ----------------------------------------------------
        // GUI 绘制
        // ----------------------------------------------------
        private void OnGUI()
        {
            if (_terrainDatabase == null)
            {
                // 尝试自动加载
                _terrainDatabase = Resources.Load<TerrainDatabase>("Data/TerrainDatabase");
                if (_terrainDatabase == null)
                {
                    EditorGUILayout.HelpBox("未找到 TerrainDatabase！", MessageType.Error);
                    return;
                }
            }

            DrawToolbar(); // 现在 Toolbar 包含了所有按钮和笔刷
            DrawMapArea();
            HandleInput();
        }

       private void DrawToolbar()
        {
            // --- 第一行：文件管理 ---
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 1. 资源槽位 (占用尽可能多的空间)
            GUILayout.Label("Map File:", GUILayout.Width(60));
            currentMapAsset = (MapDataSO)EditorGUILayout.ObjectField(currentMapAsset, typeof(MapDataSO), false);

            // 2. Load 按钮
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                LoadMap();
            }

            // 3. New 按钮
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                CreateNewMap();
            }

            // 4. Save 按钮 (加点颜色让它显眼)
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // 浅绿色
            if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SaveMap();
            }
            GUI.backgroundColor = Color.white; // 还原颜色

            GUILayout.EndHorizontal();

            // --- 第二行：画笔与设置 ---
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 5. 宽高设置
            GUILayout.Label("Size:", GUILayout.Width(35));
            GUILayout.Label("W", GUILayout.Width(15));
            mapWidth = EditorGUILayout.IntField(mapWidth, GUILayout.Width(40));
            GUILayout.Label("H", GUILayout.Width(15));
            mapHeight = EditorGUILayout.IntField(mapHeight, GUILayout.Width(40));

            GUILayout.Space(10); // 分隔符

            // 6. 笔刷选择器 (整合在这里)
            DrawBrushSelectorContent(); 

            GUILayout.FlexibleSpace(); // 填满剩余空间
            GUILayout.EndHorizontal();
        }

        // 把原本 DrawBrushSelector 里的内容提取出来，方便嵌入 Toolbar
        private void DrawBrushSelectorContent()
        {
            GUILayout.Label("Brush:", GUILayout.Width(45));

            // 安全检查与重新加载逻辑
            if (_terrainDatabase == null || _terrainDatabase.terrainDefinitions == null)
            {
                _terrainDatabase = Resources.Load<TerrainDatabase>("Data/TerrainDatabase");
                if (_terrainDatabase == null) return;
            }

            // 实时构建列表
            int count = _terrainDatabase.terrainDefinitions.Count;
            string[] options = new string[count];
            for (int i = 0; i < count; i++)
            {
                options[i] = _terrainDatabase.terrainDefinitions[i] != null ? _terrainDatabase.terrainDefinitions[i].terrainName : "Null";
            }

            if (selectedTerrainIndex >= count) selectedTerrainIndex = 0;

            // 下拉菜单
            selectedTerrainIndex = EditorGUILayout.Popup(selectedTerrainIndex, options, GUILayout.Width(120));

            // 颜色预览小方块
            if (options.Length > 0 && selectedTerrainIndex < _terrainDatabase.terrainDefinitions.Count)
            {
                var def = _terrainDatabase.terrainDefinitions[selectedTerrainIndex];
                Color previewColor = Color.white;
                if (def != null && def.material != null && def.material.HasProperty("_Color"))
                    previewColor = def.material.color;

                Rect colorRect = GUILayoutUtility.GetRect(15, 15, GUILayout.Width(15));
                // 微调一下位置让它垂直居中
                colorRect.y += 2; 
                EditorGUI.DrawRect(colorRect, previewColor);
            }
        }
        private void DrawBrushSelector()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Brush: ", GUILayout.Width(50));

            string[] options = new string[_terrainDatabase.terrainDefinitions.Count];
            for (int i = 0; i < _terrainDatabase.terrainDefinitions.Count; i++)
            {
                options[i] = _terrainDatabase.terrainDefinitions[i].terrainName;
            }

            selectedTerrainIndex = EditorGUILayout.Popup(selectedTerrainIndex, options, GUILayout.Width(150));

            // 显示当前选中的颜色预览
            if (options.Length > 0)
            {
                var def = _terrainDatabase.terrainDefinitions[selectedTerrainIndex];
                // 尝试从材质获取颜色，如果材质没颜色属性则用白色
                Color previewColor = Color.white;
                if (def.material != null && def.material.HasProperty("_Color"))
                    previewColor = def.material.color;

                Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                EditorGUI.DrawRect(colorRect, previewColor);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawMapArea()
        {
            if (tempMapData == null) return;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 预留绘制区域
            float totalWidth = mapWidth * CELL_SIZE + OFFSET_X + 20;
            float totalHeight = mapHeight * CELL_SIZE + 20;
            Rect drawArea = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            // 绘制背景
            EditorGUI.DrawRect(drawArea, new Color(0.2f, 0.2f, 0.2f));

            // 绘制格子
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // 计算每个格子的 Rect
                    // 奇数行右移 (Odd-R)
                    float xPos = drawArea.x + x * CELL_SIZE + (y % 2 == 1 ? OFFSET_X : 0);
                    float yPos = drawArea.y + (mapHeight - 1 - y) * CELL_SIZE; // Y轴反转，让(0,0)在左下角符合直觉，或者保持左上角

                    Rect cellRect = new Rect(xPos, yPos, CELL_SIZE - 2, CELL_SIZE - 2); // -2 为了留缝隙

                    // 获取格子颜色
                    int index = y * mapWidth + x;
                    string tId = tempMapData[index];
                    Color cellColor = GetColorByTerrainId(tId);

                    EditorGUI.DrawRect(cellRect, cellColor);

                    // (可选) 绘制坐标文字
                    // EditorGUI.LabelField(cellRect, $"{x},{y}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // ----------------------------------------------------
        // 交互逻辑 (鼠标点击绘制)
        // ----------------------------------------------------
        private void HandleInput()
        {
            Event e = Event.current;

            // 必须在 Repaint 或 Layout 阶段之外处理输入，或者在特定区域内
            // 简单的做法：检测鼠标是否在绘制区域内

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                // 计算鼠标在 ScrollView 内容区域的坐标
                // 这部分稍微有点复杂，因为 GUILayoutUtility.GetRect 获取的是相对坐标
                // 简单的做法是反向推算：利用 cell size

                // 这里我们简化：假设 map area 紧接着 toolbar 下面
                // 工具栏高度大概 40px
                float startY = 65f; // 粗略估算，或者记录 DrawMapArea 里的 drawArea.y

                Vector2 mousePos = e.mousePosition + scrollPos; // 加上滚动偏移

                // 计算点击了哪个格子
                // 注意：这里需要更严谨的碰撞检测，但对于矩形近似足够了

                float relativeY = mousePos.y - startY;
                int y = mapHeight - 1 - Mathf.FloorToInt(relativeY / CELL_SIZE); // 反转 Y

                if (y >= 0 && y < mapHeight)
                {
                    float rowOffsetX = (y % 2 == 1 ? OFFSET_X : 0);
                    float relativeX = mousePos.x - rowOffsetX - 5; // 5是左边距
                    int x = Mathf.FloorToInt(relativeX / CELL_SIZE);

                    if (x >= 0 && x < mapWidth)
                    {
                        PaintTile(x, y);
                        e.Use(); // 消费事件，防止穿透
                    }
                }
            }
        }

        private void PaintTile(int x, int y)
        {
            if (tempMapData == null) return;

            int index = y * mapWidth + x;
            if (index >= 0 && index < tempMapData.Length)
            {
                string newId = CurrentBrushId;
                if (tempMapData[index] != newId)
                {
                    tempMapData[index] = newId;
                    Repaint(); // 强制重绘窗口
                }
            }
        }

        // ----------------------------------------------------
        // 辅助方法
        // ----------------------------------------------------

        private Color GetColorByTerrainId(string id)
        {
            if (string.IsNullOrEmpty(id)) return Color.gray;

            // 查找定义
            var def = _terrainDatabase.terrainDefinitions.Find(t => t.terrainName == id);
            if (def != null && def.material != null && def.material.HasProperty("_Color"))
            {
                return def.material.color;
            }

            return Color.white; // 默认颜色
        }

        private void CreateNewMap()
        {
            tempMapData = new string[mapWidth * mapHeight];
            // 填充默认值
            for (int i = 0; i < tempMapData.Length; i++) tempMapData[i] = "Plain";
            currentMapAsset = null; // 这是一个新地图，还没保存为文件
        }

        private void LoadMap()
        {
            if (currentMapAsset == null)
            {
                Debug.LogWarning("请先将 MapDataSO 文件拖入插槽！");
                return;
            }

            mapWidth = currentMapAsset.width;
            mapHeight = currentMapAsset.height;
            tempMapData = (string[])currentMapAsset.terrainIds.Clone(); // 深拷贝

            Repaint();
            Debug.Log("地图加载成功！");
        }

        private void SaveMap()
        {
            if (tempMapData == null) return;

            // 1. 如果没有关联 Asset，先创建文件
            if (currentMapAsset == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Map", "NewMapData", "asset", "Save Map Data");
                if (string.IsNullOrEmpty(path)) return;

                currentMapAsset = CreateInstance<MapDataSO>();
                AssetDatabase.CreateAsset(currentMapAsset, path);
            }

            // 2. 写入数据
            currentMapAsset.width = mapWidth;
            currentMapAsset.height = mapHeight;
            currentMapAsset.terrainIds = (string[])tempMapData.Clone();

            // 3. 标记脏数据并保存
            EditorUtility.SetDirty(currentMapAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"地图已保存到: {currentMapAsset.name}");
        }
    }
}