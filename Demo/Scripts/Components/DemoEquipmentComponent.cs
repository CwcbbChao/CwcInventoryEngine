using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// 装备动态强化状态切片。
    /// 仅在装备实例被动态强化（修改攻防加成）时按需挂载。
    /// </summary>
    public class DemoEquipmentComponentState : IItemComponentState
    {
        public int? CustomAttackBonus;
        public int? CustomDefenseBonus;

        public void Reset()
        {
            CustomAttackBonus = null;
            CustomDefenseBonus = null;
        }

        public IItemComponentState Clone() => new DemoEquipmentComponentState
        {
            CustomAttackBonus = CustomAttackBonus,
            CustomDefenseBonus = CustomDefenseBonus
        };

        public bool CanStackWith(IItemComponentState other)
        {
            if (other is DemoEquipmentComponentState otherEquip)
            {
                return CustomAttackBonus == otherEquip.CustomAttackBonus
                    && CustomDefenseBonus == otherEquip.CustomDefenseBonus;
            }
            return false;
        }
    }

    /// <summary>
    /// Demo 装备享元组件。
    /// 配置在 ItemDefinition 的 Components 列表中。
    /// 实现 IItemCategorized、IEquippable 与 IItemPropertyProvider 接口。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/装备组件 Equipment")]
    public class DemoEquipmentComponent : ItemComponent, IItemCategorized, IEquippable, IItemPropertyProvider
    {
        #region 2. Inspector 字段
        [Header("装备属性")]
        [SerializeField]
        [Tooltip("装备部位/分类资产 (SO)")]
        private ItemCategorySO _equipmentCategory;

        [SerializeField]
        [Tooltip("需求等级")]
        private int _requiredLevel = 1;

        [SerializeField]
        [Tooltip("攻击力加成")]
        private int _attackBonus = 10;

        [SerializeField]
        [Tooltip("防御力加成")]
        private int _defenseBonus = 0;
        #endregion

        #region 4. 公共属性 (基准配置)
        public ItemCategorySO EquipmentCategory => _equipmentCategory;
        public int RequiredLevel => _requiredLevel;
        public int AttackBonus => _attackBonus;
        public int DefenseBonus => _defenseBonus;
        #endregion

        #region 6. 实例属性获取与动态强化
        public virtual int GetAttackBonus(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<DemoEquipmentComponentState>(out var state))
            {
                if (state.CustomAttackBonus.HasValue) return state.CustomAttackBonus.Value;
            }
            return _attackBonus;
        }

        public virtual int GetDefenseBonus(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<DemoEquipmentComponentState>(out var state))
            {
                if (state.CustomDefenseBonus.HasValue) return state.CustomDefenseBonus.Value;
            }
            return _defenseBonus;
        }

        public virtual void SetCustomAttackBonus(ItemInstance instance, int attackBonus)
        {
            if (instance == null) return;
            var state = instance.GetState<DemoEquipmentComponentState>();
            state.CustomAttackBonus = attackBonus;
        }

        public virtual void SetCustomDefenseBonus(ItemInstance instance, int defenseBonus)
        {
            if (instance == null) return;
            var state = instance.GetState<DemoEquipmentComponentState>();
            state.CustomDefenseBonus = defenseBonus;
        }
        #endregion

        #region 7. IItemCategorized 接口实现
        public virtual void GetCategories(ItemInstance instance, List<ItemCategorySO> results)
        {
            if (results != null && _equipmentCategory != null)
            {
                results.Add(_equipmentCategory);
            }
        }

        public virtual ItemCategorySO GetPrimaryCategory(ItemInstance instance)
        {
            return _equipmentCategory;
        }
        #endregion

        #region 8. IEquippable 接口实现
        public virtual bool CanEquip(ItemInstance instance, GameObject user)
        {
            return user != null && user.GetComponent<DemoCharacter>() != null;
        }

        public virtual void OnEquip(ItemInstance instance, GameObject user)
        {
            if (user != null && user.TryGetComponent<DemoCharacter>(out var character))
            {
                int atk = GetAttackBonus(instance);
                int def = GetDefenseBonus(instance);
                if (atk != 0) character.ModifyAttack(atk);
                if (def != 0) character.ModifyDefense(def);
                Debug.Log($"[DemoEquipmentComponent] 穿戴装备！读取组件配置攻击加成 +{atk}, 防御加成 +{def}");
            }
        }

        public virtual void OnUnequip(ItemInstance instance, GameObject user)
        {
            if (user != null && user.TryGetComponent<DemoCharacter>(out var character))
            {
                int atk = GetAttackBonus(instance);
                int def = GetDefenseBonus(instance);
                if (atk != 0) character.ModifyAttack(-atk);
                if (def != 0) character.ModifyDefense(-def);
                Debug.Log($"[DemoEquipmentComponent] 脱下装备！扣除装备配置攻击加成 {atk}, 扣除防御加成 {def}");
            }
        }
        #endregion

        #region 9. IItemPropertyProvider 接口实现
        public virtual bool TryGetProperty(ItemInstance instance, string key, out ItemPropertyValue value)
        {
            value = ItemPropertyValue.Empty;

            if (string.Equals(key, "SlotType", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "EquipmentSlot", StringComparison.OrdinalIgnoreCase))
            {
                if (_equipmentCategory != null)
                {
                    value = _equipmentCategory.DisplayName;
                    return true;
                }
            }

            if (string.Equals(key, "EquipmentCategory", StringComparison.OrdinalIgnoreCase))
            {
                if (_equipmentCategory != null)
                {
                    value = new ItemPropertyValue(_equipmentCategory);
                    return true;
                }
            }

            if (string.Equals(key, "RequiredLevel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Level", StringComparison.OrdinalIgnoreCase))
            {
                value = _requiredLevel;
                return true;
            }

            if (string.Equals(key, "AttackBonus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                value = GetAttackBonus(instance);
                return true;
            }

            if (string.Equals(key, "DefenseBonus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Defense", StringComparison.OrdinalIgnoreCase))
            {
                value = GetDefenseBonus(instance);
                return true;
            }

            return false;
        }
        #endregion
    }
}
