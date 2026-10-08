using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 享元物品组件基类 (Flyweight Item Component)。
    /// 配置在 ItemDefinition 的 Components 列表中 (通过 [SerializeReference] 序列化)。
    /// 身兼静态配置与行为逻辑载体，在运行时作为只读单例存在，自身严禁持有任何私有可变运行态字段。
    /// 所有运行时状态均需通过 ItemInstance.GetState&lt;TComponentState&gt;() 按需外挂访问。
    /// </summary>
    [Serializable]
    public abstract class ItemComponent
    {
        #region 6. Public Methods
        #region Lifecycle Hooks (面向实例派发)
        /// <summary>
        /// 当物品实例首次在内存中被新建时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        public virtual void OnInstanceCreated(ItemInstance instance) { }

        /// <summary>
        /// 当物品实例从存档恢复反序列化完成后调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        public virtual void OnInstanceLoaded(ItemInstance instance) { }

        /// <summary>
        /// 当物品放入容器槽位时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        /// <param name="container">目标容器</param>
        /// <param name="slotIndex">目标槽位</param>
        public virtual void OnAddedToContainer(ItemInstance instance, InventoryContainer container, int slotIndex) { }

        /// <summary>
        /// 当物品从容器槽位移出时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        /// <param name="container">目标容器</param>
        /// <param name="slotIndex">源槽位</param>
        public virtual void OnRemovedFromContainer(ItemInstance instance, InventoryContainer container, int slotIndex) { }

        /// <summary>
        /// 当物品堆叠数量发生变化时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        /// <param name="oldCount">变更前的堆叠数量</param>
        /// <param name="newCount">变更后的堆叠数量</param>
        public virtual void OnStackCountChanged(ItemInstance instance, int oldCount, int newCount) { }

        /// <summary>
        /// 当源物品堆叠合并入当前物品时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        /// <param name="sourceInstance">源物品实例</param>
        /// <param name="transferredCount">转移的堆叠数量</param>
        public virtual void OnStackMerged(ItemInstance instance, ItemInstance sourceInstance, int transferredCount) { }

        /// <summary>
        /// 当从当前物品拆分出新物品实例时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        /// <param name="newInstance">拆分出的新物品实例</param>
        /// <param name="splitCount">拆分出的数量</param>
        public virtual void OnStackSplit(ItemInstance instance, ItemInstance newInstance, int splitCount) { }

        /// <summary>
        /// 当物品实例被彻底销毁或数量降为零时调用。
        /// </summary>
        /// <param name="instance">所属物品实例</param>
        public virtual void OnInstanceDestroyed(ItemInstance instance) { }
        #endregion

        #region Save / Load Autonomy (存盘自治)
        /// <summary>
        /// 存盘自治：从指定的物品实例中提取当前组件需要持久化的状态数据。
        /// </summary>
        /// <param name="instance">目标物品实例</param>
        /// <param name="jsonData">导出的 JSON 状态文本</param>
        /// <returns>若该组件有需要持久化的动态数据返回 true，否则返回 false</returns>
        public virtual bool TryExportState(ItemInstance instance, out string jsonData)
        {
            jsonData = null;
            return false;
        }

        /// <summary>
        /// 读盘自治：将持久化的 JSON 状态还原到指定物品实例中。
        /// </summary>
        /// <param name="instance">目标物品实例</param>
        /// <param name="jsonData">持久化的 JSON 状态文本</param>
        public virtual void ImportState(ItemInstance instance, string jsonData) { }
        #endregion
        #endregion
    }
}
