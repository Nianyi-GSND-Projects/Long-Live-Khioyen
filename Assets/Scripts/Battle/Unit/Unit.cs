using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public enum Faction
    {
        Player,
        Friend,
        Enemy,
        Neutral
    }
    
    public abstract class Unit : MonoBehaviour
    {
        public int InstanceId { get; set; } 
        public Faction faction;
        
        public abstract UnitDefinition unitDefinition { get; }
        public UnitEntryStats entryStats;
        
        [Header("InBattle State")]
        public int currentHealth;
        public List<Buff> buffs = new();
        public bool selected;
        public bool actionDone;
        public bool hasMovedThisTurn = false;
        public Vector2Int position { get; set; }
        
        protected virtual void Start()
        {
            visualController = GetComponent<UnitVisualController>();
            
            if (visualController != null)
            {
                visualController.Initialize(this);
            }
        }
        
        public virtual void CalculateEntryStats(UnitDescriptor desc)
        {
            entryStats = new UnitEntryStats();
        }
        
        public void OnUnitStateChanged()
        {
            if (visualController != null)
            {
                visualController.RefreshVisuals();
            }
        }
        
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                UpdateVisualState();
            }
        }

        public bool ActionDone
        {
            get => actionDone;
            set
            {
                actionDone = value;
                UpdateVisualState();
            }
        }
        
        public virtual void OnTurnStart()
        {
            actionDone = false;
            hasMovedThisTurn = false;
            
        }
        
        #region Action
        
        public ActionDefinition DefaultAttack;
        public ActionDefinition DefaultRetreat;
        public ActionDefinition DefaultInteract;
        
        public List<ActionDefinition> runtimeUnitActions = new List<ActionDefinition>();

        public List<ActionDefinition> runtimeCommanderActions = new List<ActionDefinition>();
        
        #endregion
        
        #region Visual state
        
        protected UnitVisualController visualController;
        
        protected GameObject model;

        public void UpdateVisualState()
        {
            if (visualController != null)
            {
                visualController.SetVisualState(selected, actionDone);
            }
        }

       
        #endregion

        #region Effect

        public virtual void TakeDamage(int rawDamage)
        {
            if (entryStats == null) return;
            
            float threshold = Mathf.Max(1.0f, entryStats.defensePower);
            int healthLoss = Mathf.FloorToInt(rawDamage / threshold);
            
            if (healthLoss > 0)
            {
                currentHealth -= healthLoss;
                currentHealth = Mathf.Max(0, currentHealth);
                
                Debug.Log($"{name} took {rawDamage} raw damage. Threshold: {threshold}. Lost {healthLoss} HP. Remaining: {currentHealth}");
                
                OnHealthChanged();
                
                if (Battle.Instance != null) 
                    Battle.Instance.MarkUnitDirty(this);
            }
            else
            {
                Debug.Log($"{name} took {rawDamage} raw damage but resisted (Threshold: {threshold}).");
            }
        }
        
        protected virtual void OnHealthChanged() { }

        public virtual float GetPower()
        {
            return entryStats != null ? entryStats.attackPower : 0;
        }
        
        public virtual float GetDefense() 
        {
            return entryStats != null ? entryStats.defensePower : 0;
        }
        public void ReceiveForcedMove(Vector2Int newPosition)
        {
            Vector2Int oldPosition = this.position;
            
            // 1. 更新数据坐标
            this.position = newPosition;

            // 2. 更新视觉位置 (直接瞬移，或者你可以改成 Tween 动画)
            if (Battle.Instance != null)
            {
                transform.localPosition = Battle.Instance.MapToLocal(newPosition);
            }

            // 3. 通知视觉控制器 (例如刷新 UI跟随，或者播放尘土特效)
            if (visualController != null)
            {
                visualController.RefreshVisuals(); // 或者专门写一个 OnMove 接口
            }

            // 4. [核心] 触发移动钩子
            OnPostForcedMove(oldPosition, newPosition);
        }
        
        protected virtual void OnPostForcedMove(Vector2Int oldPos, Vector2Int newPos)
        {
            Debug.Log($"{name} 从 {oldPos} 被强制推到了 {newPos}");
            
            /*
            if (Battle.Instance != null)
            {
                var tile = Battle.Instance.mapData[newPos.x, newPos.y];
                foreach(var effect in tile.Effects)
                {
                    effect.definition.OnEnter(this);
                }
            }
            */
        }
        public abstract void ApplyBuff(BuffDescriptor buffDescriptor);

        public void ClearAllBuff()
        {
            buffs.Clear();
        }
        
        public void RemoveBuffViaDefinition(BuffDefinition buffDefinition)
        {
            buffs.RemoveAll(buff => buff.descriptor.definition == buffDefinition);
        }

        public void UpdateBuffs()
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];
                
                // 执行逻辑钩子 (比如中毒扣血)
                if (buff.descriptor.definition != null)
                {
                    buff.descriptor.definition.OnTick(this, buff);
                }

                // 计时
                buff.TimePass();

                // 移除判定
                if (buff.currentDuration <= 0)
                {
                    // 执行移除逻辑 (比如恢复属性)
                    if (buff.descriptor.definition != null)
                        buff.descriptor.definition.OnRemove(this, buff);

                    buffs.RemoveAt(i);
                }
            }
        }
        #endregion
    }
    
    public abstract class Unit<T>: Unit where T: UnitDefinition
    {
        
        public T Definition { get; set; }
        public override UnitDefinition unitDefinition => Definition;
        
        
        #region Life cycle
        protected new void Start()
        {
            base.Start();
            name = Definition.unitName;
            
            Vector3 size = new(1, 1, 1);
            Vector3 center = new(0,0,0);
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = center;
        }
        #endregion

        
    }
}
