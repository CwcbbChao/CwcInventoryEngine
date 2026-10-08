using System;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 药水享元组件 (Flyweight Potion Component)。
    /// 继承 ItemComponent 并实现 IUsable 接口。
    /// 纯无状态设计，所有业务逻辑均通过传入的 ItemInstance 闭环执行生命恢复与堆叠扣减。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/药水 Potion")]
    public class DemoPotionComponent : ItemComponent, IUsable
    {
        #region 2. Inspector 字段
        [SerializeField]
        [Tooltip("使用后恢复的生命值数量")]
        private int _healAmount = 5;
        #endregion

        #region 4. 公共属性
        /// <summary>
        /// 恢复生命值数量。
        /// </summary>
        public int HealAmount => _healAmount;
        #endregion

        #region 6. IUsable 接口实现
        /// <summary>
        /// 判定当前药水是否可使用：需使用者拥有 DemoCharacter 且生命值未满，且物品堆叠大于 0。
        /// </summary>
        public bool CanUse(ItemInstance instance, GameObject user)
        {
            if (instance == null || user == null || instance.StackCount <= 0) return false;

            if (user.TryGetComponent<DemoCharacter>(out var character))
            {
                return character.CurrentHp < character.MaxHp;
            }

            return false;
        }

        /// <summary>
        /// 执行使用药水：恢复生命值，并内聚扣除当前物品实例的 1 个堆叠数量。
        /// </summary>
        public bool OnUse(ItemInstance instance, GameObject user)
        {
            if (!CanUse(instance, user))
            {
                Debug.LogWarning("[DemoPotionComponent] 无法使用药水：使用者无效、生命值已满或数量不足！");
                return false;
            }

            if (user.TryGetComponent<DemoCharacter>(out var character))
            {
                bool healed = character.Heal(_healAmount);
                if (healed)
                {
                    Debug.Log($"[DemoPotionComponent] 已成功使用 1 瓶药水，恢复 {_healAmount} 点生命值！");
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
