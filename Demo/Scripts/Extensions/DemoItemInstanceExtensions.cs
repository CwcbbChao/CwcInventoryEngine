using System;
using System.Collections.Generic;
using UnityEngine;
using Cwc.InventoryEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// Demo 物品实例动作静态扩展类 (Demo Item Instance Extensions)。
    /// 提供针对 ItemInstance 的静态扩充 API，无需修改核心库即可直接实现使用、装备、取消装备等通用业务操作。
    /// 内部使用缓存列表遍历模式，做到零堆内存垃圾分配 (Zero GC Alloc)。
    /// </summary>
    public static class DemoItemInstanceExtensions
    {
        #region Public Static Methods - Usable Extensions
        /// <summary>
        /// 判断物品是否包含可使用组件且当前允许被目标使用者使用。
        /// 直接遍历享元组件列表，零堆分配且具备完全重入安全性。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若可以被使用返回 true，否则返回 false</returns>
        public static bool CanUse(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            var comps = item.Components;
            if (comps == null) return false;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                if (comps[i] is IUsable usable && usable.CanUse(item, user))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试使用物品。遍历并执行所有实现 IUsable 接口的组件逻辑。
        /// 直接遍历享元组件列表，零堆分配且具备完全重入安全性。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若成功触发至少一个组件的使用逻辑返回 true，否则返回 false</returns>
        public static bool TryUse(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            var comps = item.Components;
            if (comps == null) return false;

            bool anyUsed = false;
            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                if (comps[i] is IUsable usable && usable.CanUse(item, user))
                {
                    if (usable.OnUse(item, user))
                    {
                        anyUsed = true;
                    }
                }
            }

            return anyUsed;
        }
        #endregion

        #region Public Static Methods - Equippable Extensions
        /// <summary>
        /// 判断物品是否包含可装备组件且当前允许被目标角色穿戴。
        /// 直接遍历享元组件列表，零堆分配且具备完全重入安全性。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若允许穿戴返回 true，否则返回 false</returns>
        public static bool CanEquip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            var comps = item.Components;
            if (comps == null) return false;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                if (comps[i] is IEquippable equippable && equippable.CanEquip(item, user))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试穿戴装备。遍历并执行所有实现 IEquippable 接口的组件 OnEquip 穿戴逻辑。
        /// 直接遍历享元组件列表，零堆分配且具备完全重入安全性。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若成功穿戴返回 true，否则返回 false</returns>
        public static bool TryEquip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            var comps = item.Components;
            if (comps == null) return false;

            bool anyEquipped = false;
            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var equippable = comps[i] as IEquippable;
                if (equippable != null && equippable.CanEquip(item, user))
                {
                    equippable.OnEquip(item, user);
                    anyEquipped = true;
                }
            }

            return anyEquipped;
        }

        /// <summary>
        /// 尝试脱下/取消装备。遍历并执行所有实现 IEquippable 接口的组件 OnUnequip 脱下逻辑。
        /// 直接遍历享元组件列表，零堆分配且具备完全重入安全性。
        /// </summary>
        /// <param name="item">目标物品实例</param>
        /// <param name="user">角色/使用者 GameObject 实体</param>
        /// <returns>若成功执行脱下逻辑返回 true，否则返回 false</returns>
        public static bool TryUnequip(this ItemInstance item, GameObject user)
        {
            if (item == null || user == null) return false;

            var comps = item.Components;
            if (comps == null) return false;

            bool anyUnequipped = false;
            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                if (comps[i] is IEquippable equippable)
                {
                    equippable.OnUnequip(item, user);
                    anyUnequipped = true;
                }
            }

            return anyUnequipped;
        }
        #endregion
    }
}
