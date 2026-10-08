using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可分类物品组件接口。
    /// 在享元模式下，方法传入目标物品实例 ItemInstance，收集包含静态配置与动态追加的全部分类资产。
    /// </summary>
    public interface IItemCategorized
    {
        /// <summary>
        /// 零 GC 收集该组件为指定物品实例贡献的所有分类资产。
        /// </summary>
        /// <param name="instance">当前物品实例</param>
        /// <param name="results">输出结果列表</param>
        void GetCategories(ItemInstance instance, List<ItemCategorySO> results);

        /// <summary>
        /// 零 GC 获取该组件为指定物品实例贡献的首个/主分类资产。
        /// 专用于 UI 标签、排序对比等快速查询场景，避免分配临时集合。
        /// </summary>
        /// <param name="instance">当前物品实例</param>
        /// <returns>首个有效分类资产，若无分类则返回 null</returns>
        ItemCategorySO GetPrimaryCategory(ItemInstance instance);
    }
}
