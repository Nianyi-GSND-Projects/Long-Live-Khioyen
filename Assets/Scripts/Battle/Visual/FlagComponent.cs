using UnityEngine;

namespace LongLiveKhioyen
{
    public interface IFactionColored
    {
        void SetFactionMaterial(Material mat);
    }
    public class FlagComponent : MonoBehaviour, IFactionColored
    {
        [Tooltip("需要变色的渲染器")]
        public Renderer targetRenderer;
        
        [Tooltip("需要变色的材质索引 (0, 1, 2...)")]
        public int materialIndex = 0;

        public void SetFactionMaterial(Material mat)
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) 
            {
                Debug.LogError("FlagComponent: 找不到 Renderer！");
                return;
            }

            // 1. 获取共享材质数组 (引用)
            // 如果你只改颜色不换材质球，可以用 PropertyBlock
            // 如果你是彻底换材质球，必须这样做：
            var mats = targetRenderer.sharedMaterials; 

            // 检查索引越界
            if (materialIndex >= mats.Length)
            {
                Debug.LogError($"FlagComponent: 材质索引 {materialIndex} 越界！当前只有 {mats.Length} 个材质。");
                return;
            }

            // 2. 替换
            mats[materialIndex] = mat;

            // 3. 赋值回去 (关键！)
            targetRenderer.sharedMaterials = mats;
            
            Debug.Log($"旗帜颜色已更新为: {mat.name}");
        }
    }
}