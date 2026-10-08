using System;

namespace Cwc.InventoryEngine.Query
{
    /// <summary>
    /// 物品/组件动态属性提取提供者接口。
    /// 在享元模式下，方法传入目标物品实例 ItemInstance，以支持动态强化值或静态配置值的提取。
    /// </summary>
    public interface IItemPropertyProvider
    {
        #region Public Methods
        /// <summary>
        /// 尝试提取指定物品实例在指定 Key 下的关键属性或元数据。
        /// </summary>
        /// <param name="instance">当前物品实例</param>
        /// <param name="key">属性名称标识符</param>
        /// <param name="value">提取到的轻量属性值 (ItemPropertyValue)</param>
        /// <returns>若成功找到该属性则返回 true，否则返回 false</returns>
        bool TryGetProperty(ItemInstance instance, string key, out ItemPropertyValue value);
        #endregion
    }
}
