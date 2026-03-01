using UnityEngine;

namespace LongLiveKhioyen
{
    // 运行时实例
    public class TileEffect
    {
        public TileEffectDefinition definition;
        public int currentDuration;
        public Unit sourceUnit;
        public GameObject vfxInstance;
        public TileEffect(TileEffectDefinition def, int duration, Unit source)
        {
            definition = def;
            currentDuration = duration;
            sourceUnit = source;
        }
    }
    
}