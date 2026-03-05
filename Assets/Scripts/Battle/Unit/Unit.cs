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
        public Unit LastAttacker { get; private set; }
        public bool IsVisible { get; set; }
        
        public virtual int ZocPower => (int)GetStat(StatType.ZocPower);
        protected virtual void Start()
        {
        }
        
        public virtual void CalculateEntryStats(UnitDescriptor desc)
        {
            entryStats = new UnitEntryStats
            {
                maxHealth = desc.maxHealth,
                ZOCPower = desc.ZOCPower,
                repairPower = unitDefinition.defaultRepairPower,
                actionChance = desc.actionChance
            };
        }
        
        public virtual float GetStat(StatType stat)
        {
            if (entryStats == null) return 0;

            float baseValue = GetBaseStatValue(stat);

            float additiveBonus = 0f;
            float multiplicativeBonus = 1f;

            foreach (var buff in buffs)
            {
                if (buff.descriptor.definition is StatModifierBuffDefinition statBuff)
                {
                    foreach (var modifier in statBuff.Modifiers)
                    {
                        if (modifier.StatToModify == stat)
                        {
                            if (modifier.Type == ModifierType.Additive)
                            {
                                additiveBonus += modifier.Value;
                            }
                            else if (modifier.Type == ModifierType.Multiplicative)
                            {
                                multiplicativeBonus *= modifier.Value;
                            }
                        }
                    }
                }
            }

            return (baseValue + additiveBonus) * multiplicativeBonus;
        }
        
        public float GetBaseStatValue(StatType stat)
        {
            switch (stat)
            {
                case StatType.AttackPower:
                    return entryStats.attackPower;
                case StatType.DefensePower:
                    return entryStats.defensePower;
                case StatType.RepairPower:
                    return entryStats.repairPower;
                case StatType.Flexibility:
                    return entryStats.flexibility;
                case StatType.Discipline:
                    return entryStats.discipline;
                case StatType.Strategy:
                    return entryStats.strategy;
                case StatType.MaxHealth:
                    return entryStats.maxHealth;
                case StatType.MaxMorale:
                    return entryStats.maxMorale;
                case StatType.ZocPower:
                    return entryStats.ZOCPower;
                case StatType.ActionChance:
                    return entryStats.actionChance;
                case StatType.Movement:
                    float flexibility = GetStat(StatType.Flexibility);
                    int extra = 0;
                    if (this is Battalion bat) extra = bat.ExtraMovement;
                    return Mathf.FloorToInt(flexibility / BattleParam.Instance.mobilityPerMovement) + extra;
                case StatType.DamageResistance:
                    float defense = GetStat(StatType.DefensePower);
                    return defense * BattleParam.Instance.damageResistancePerDefense;
                default:
                    return 0;
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

        public void SetVisualController(UnitVisualController controller)
        {
            visualController = controller;
        }
        
        
       
        #endregion

        #region Effect
        
        public virtual void Heal(int amount)
        {
            if (entryStats == null) return; // 或者 Facility 用 Definition
    
            // 获取最大血量逻辑需要统一
            int max = 0;
            if (this is Battalion bat) max = bat.entryStats.maxHealth;
            else if (this is Facility fac) max = fac.Definition.defaultMaxDurability;

            int old = currentHealth;
            currentHealth = Mathf.Min(max, currentHealth + amount);
    
            if (currentHealth != old)
            {
                OnHealthChanged(); // [关键]
            }
        }

        public virtual void TakeDamage(int rawDamage, Unit attacker = null)
        {
            if (entryStats == null) return;
            if (attacker != null) LastAttacker = attacker;
            
            float defense = GetStat(StatType.DefensePower);
            float resistancePerDef = 0.05f;
            if (BattleParam.Instance != null)
            {
                resistancePerDef = BattleParam.Instance.damageResistancePerDefense;
            }
            
            float resistanceFactor = defense * resistancePerDef;
            resistanceFactor = Mathf.Clamp(resistanceFactor, 1f, 5f);
            int healthLoss = Mathf.FloorToInt(rawDamage / resistanceFactor);
            
            if (healthLoss > 0)
            {
                currentHealth -= healthLoss;
                currentHealth = Mathf.Max(0, currentHealth);
                Debug.Log($"{name} took {rawDamage} raw damage. Threshold: {resistanceFactor}. Lost {healthLoss} HP. Remaining: {currentHealth}");
                OnHealthChanged();
                
                if (Battle.Instance != null) 
                    Battle.Instance.MarkUnitDirty(this);
            }
        }
        
        protected virtual void OnHealthChanged() { }

        public abstract float GetPower();

        public abstract float GetDefense();

        public abstract float GetRepairPower();
        
        public void ReceiveForcedMove(Vector2Int newPosition)
        {
            Vector2Int oldPosition = this.position;
            
            this.position = newPosition;

            if (Battle.Instance != null)
            {
                transform.localPosition = Battle.Instance.MapToLocal(newPosition);
            }

            if (visualController != null)
            {
                visualController.RefreshVisuals();
            }

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
                
                if (buff.descriptor.definition != null)
                {
                    buff.descriptor.definition.OnTick(this, buff);
                }

                buff.TimePass();

                if (buff.currentDuration <= 0)
                {
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
