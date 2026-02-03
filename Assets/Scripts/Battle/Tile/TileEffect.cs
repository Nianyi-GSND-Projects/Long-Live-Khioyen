using UnityEngine;

namespace LongLiveKhioyen
{
    // 运行时实例
    public class TileEffect
    {
        public TileEffectDefinition definition;
        public int currentDuration;
        public Unit sourceUnit; // 施法者（用于计算伤害来源）
        public GameObject vfxInstance; // 场景中的特效物体引用

        public TileEffect(TileEffectDefinition def, int duration, Unit source)
        {
            definition = def;
            currentDuration = duration;
            sourceUnit = source;
        }
    }
    
}