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
        public Vector2Int position { get; set; }
        
        public bool selected;
        public bool actionDone;
        public Faction faction;
        public abstract UnitDefinition unitDefinition { get; }
        
        public List<Buff> buffs = new();
        
        protected virtual void Start()
        {
            visualController = GetComponent<UnitVisualController>();
            
            if (visualController != null)
            {
                visualController.Initialize(this);
            }
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
        
        #region Action
        
        public ActionDefinition DefaultAttack;

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

        public abstract void TakeDamage(int damage);//该单位受到伤害

        public abstract float GetPower(); //获取该单位的攻击力

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
