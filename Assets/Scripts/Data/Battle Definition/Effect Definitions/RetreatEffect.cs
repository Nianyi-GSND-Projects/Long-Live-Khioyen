using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Effects/Retreat")]
    public class RetreatEffect : EffectDefinition
    {
        public override void Execute(ActionContext ctx)
        {
            Unit unit = ctx.User;
            if (unit == null) return;

            Debug.Log($"{unit.name} 已成功撤离战场！");

            // 1. 从地图移除
            Battle.Instance.RemoveUnitFromMap(unit);

            // 2. 从活跃列表移除 (这会影响胜利条件判断)
            // 注意：需要修改 Battle.cs 公开移除方法，或者在这里实现
            if (Battle.Instance != null)
            {
                Battle.Instance.WithdrawUnit(unit); // 需要在 Battle 中实现这个方法
            }

            // 3. 销毁对象 (或者隐藏并放入"已撤离"列表)
            unit.gameObject.SetActive(false);
            // Destroy(unit.gameObject); // 如果你想彻底销毁
        }
    }
}