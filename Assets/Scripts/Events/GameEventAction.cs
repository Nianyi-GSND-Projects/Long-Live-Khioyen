using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    public abstract class GameEventAction : ScriptableObject
    {
        [TextArea] public string description;
        public abstract void Execute();
        
        [Header("Flow Control")]
        public bool isBlocking = false;
        
        public virtual IEnumerator ExecuteCoroutine()
        {
            Execute(); // 默认调用同步版本
            yield break; // 默认不等待
        }
        
        public virtual void Execute(BattleEventContext ctx)
        {
            Execute(); // 如果子类没重写带参的 Execute，就回退到无参的 Execute
        }
        
        public virtual IEnumerator ExecuteCoroutine(BattleEventContext ctx)
        {
            Execute(ctx); 
            yield break; 
        }
    }
}