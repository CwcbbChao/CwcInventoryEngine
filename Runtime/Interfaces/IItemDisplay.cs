using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 抽象物品 UI 显示结构接口。
    /// 在享元模式下，方法传入目标物品实例 ItemInstance，优先读取实例的动态覆盖，无覆盖则回退静态配置。
    /// </summary>
    public interface IItemDisplay
    {
        /// <summary>
        /// 获取指定物品实例的显示名称。
        /// </summary>
        string GetDisplayName(ItemInstance instance);

        /// <summary>
        /// 获取指定物品实例的显示图标。
        /// </summary>
        Sprite GetIcon(ItemInstance instance);

        /// <summary>
        /// 获取指定物品实例的详细描述文本。
        /// </summary>
        string GetDescription(ItemInstance instance);
    }
}
