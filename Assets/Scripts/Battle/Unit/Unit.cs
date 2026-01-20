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


        #region Visual state
        protected GameObject model;
        protected readonly Dictionary<Renderer, Material[]> legacyMaterials = new();
        
        protected Material selectingMaterial;
        
        protected Material actionDoneMaterial;

        public void UpdateVisualState()
        {
            if(selected)
            {
                foreach(var renderer in legacyMaterials.Keys)
                    renderer.sharedMaterial = selectingMaterial;
            }
            else if (actionDone)
            {
                foreach(var renderer in legacyMaterials.Keys)
                    renderer.sharedMaterial = actionDoneMaterial;
            }
            else
            {
                foreach(var (renderer, mats) in legacyMaterials)
                    renderer.sharedMaterials = mats;
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
            foreach (Buff buff in buffs)
            {
                buff.TimePass();
                if (buff.currentDuration <= 0)
                {
                    buffs.Remove(buff);
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
        protected void Start()
        {
            name = Definition.unitName;
            model = Instantiate(Definition.unitModelTemplate);
            model.name = "Model";
            model.transform.SetParent(transform, false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                legacyMaterials[renderer] = renderer.sharedMaterials;
            selectingMaterial = Resources.Load<Material>("Materials/Polis/Construction_site");
            actionDoneMaterial= Resources.Load<Material>("Materials/Polis/Construction_site");
            // TODO:改成实际材质
            Vector3 size = new(1, 1, 1);
            Vector3 center = new(0,0,0);
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = center;
            UpdateVisualState();
        }
        #endregion

        
    }
}
