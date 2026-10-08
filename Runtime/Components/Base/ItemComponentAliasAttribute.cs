using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 物品组件持久化别名特性 (Item Component Alias Attribute)。
    /// 用于组件类名重构、命名空间调整或架构迁移时，保证旧存档数据的向后兼容性与平滑迁移。
    /// 示例: [ItemComponentAlias("Cwc.InventoryEngine.OldEquipmentComponent", "EquipmentComponent")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ItemComponentAliasAttribute : Attribute
    {
        #region 4. 公共属性
        /// <summary>
        /// 声明的历史类型全名或兼容别名列表。
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }
        #endregion

        #region 5. 构造函数
        /// <summary>
        /// 构造函数，声明一个或多个历史兼容别名。
        /// </summary>
        /// <param name="aliases">历史别名字符串数组</param>
        public ItemComponentAliasAttribute(params string[] aliases)
        {
            Aliases = aliases ?? Array.Empty<string>();
        }
        #endregion
    }
}
