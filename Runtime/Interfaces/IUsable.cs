using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可使用/消耗物品标准接口 (Usable Interface)。
    /// 在享元模式下，方法必须传入当前目标物品实例 ItemInstance 以实现行为绑定。
    /// </summary>
    public interface IUsable
    {
        /// <summary>
        /// 判定指定物品实例是否允许被目标使用者使用。
        /// </summary>
        /// <param name="instance">当前物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若允许使用返回 true，否则返回 false</returns>
        bool CanUse(ItemInstance instance, GameObject user);

        /// <summary>
        /// 执行物品使用逻辑。
        /// </summary>
        /// <param name="instance">当前物品实例</param>
        /// <param name="user">使用者 GameObject 实体</param>
        /// <returns>若成功使用返回 true，使用失败或条件不满足返回 false</returns>
        bool OnUse(ItemInstance instance, GameObject user);
    }
}
