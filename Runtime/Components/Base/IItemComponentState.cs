namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 运行时物品组件状态接口。
    /// 必须由 class 实现，作为外挂切片动态挂载在 ItemInstance 上，实现享元模式下的读写隔离。
    /// </summary>
    public interface IItemComponentState
    {
        /// <summary>
        /// 当物品实例重置、回池或被销毁时调用，将内部可变变量归零复位。
        /// </summary>
        void Reset();

        /// <summary>
        /// 创建当前状态切片的副本（用于拆分或实例化时的状态防御性复制）。
        /// </summary>
        /// <returns>克隆后的状态实例</returns>
        IItemComponentState Clone();

        /// <summary>
        /// 判定目标状态切片是否与当前状态切片兼容（允许合并堆叠）。
        /// </summary>
        /// <param name="other">目标物品对应的状态切片</param>
        /// <returns>若兼容允许合并返回 true，否则返回 false</returns>
        bool CanStackWith(IItemComponentState other);
    }
}
