using System;
using System.Collections.Generic;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 运行时动态物品实例 (Flyweight Item Instance)。
    /// 作为轻量级数据容器存在，直接引用只读的 ItemDefinition 享元模板。
    /// 仅在产生动态覆盖数据时按需挂载 IItemComponentState 切片，实现 0-GC 与极致轻量化。
    /// </summary>
    public class ItemInstance
    {
        #region 3. 受保护状态字段
        protected Dictionary<Type, IItemComponentState> _runtimeStates;
        #endregion

        #region 4. 公共属性
        /// <summary>
        /// 全局唯一物品实例标识 (零 GC 值类型)。
        /// </summary>
        public ItemId InstanceID { get; }

        /// <summary>
        /// 关联的静态 ScriptableObject 资产定义。
        /// </summary>
        public ItemDefinition Definition { get; }

        /// <summary>
        /// 当前堆叠数量。
        /// </summary>
        public int StackCount { get; internal set; }

        /// <summary>
        /// 享元组件只读列表（直接指向 Definition.Components，零堆分配）。
        /// </summary>
        public IReadOnlyList<ItemComponent> Components => Definition != null ? Definition.Components : Array.Empty<ItemComponent>();

        /// <summary>
        /// 当前实例是否包含任何运行时动态状态切片。
        /// 若为 false，说明该物品为纯白板/未修改物品，存盘与堆叠判定具备极致性能。
        /// </summary>
        public bool HasAnyRuntimeState => _runtimeStates != null && _runtimeStates.Count > 0;
        #endregion

        #region 5. 构造函数
        /// <summary>
        /// 构造函数，绑定静态定义并触发创建生命周期。
        /// </summary>
        public ItemInstance(ItemId instanceID, ItemDefinition definition, int stackCount)
        {
            InstanceID = instanceID;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            StackCount = stackCount;

            InitializeComponents();
        }
        #endregion

        #region 6. 统一动态状态管理 API (GetState / TryGetState / SetState)
        /// <summary>
        /// 获取或创建指定类型的运行时动态状态切片。
        /// 状态纯粹对齐道具实例自身，以状态类型 Type 为唯一标识。
        /// </summary>
        /// <typeparam name="TState">继承自 IItemComponentState 的状态容器类型</typeparam>
        /// <returns>状态实例</returns>
        public virtual TState GetState<TState>() where TState : class, IItemComponentState, new()
        {
            _runtimeStates ??= new Dictionary<Type, IItemComponentState>(4);
            var key = typeof(TState);
            if (!_runtimeStates.TryGetValue(key, out var state))
            {
                state = new TState();
                _runtimeStates[key] = state;
            }
            return (TState)state;
        }

        /// <summary>
        /// 获取或创建指定类型的运行时动态状态切片（GetState 的语义化别名）。
        /// </summary>
        public virtual TState GetOrCreateState<TState>() where TState : class, IItemComponentState, new()
        {
            return GetState<TState>();
        }

        /// <summary>
        /// 尝试获取已存在的动态状态切片。若不存在不会进行实例化。
        /// </summary>
        public virtual bool TryGetState<TState>(out TState state) where TState : class, IItemComponentState
        {
            if (_runtimeStates != null)
            {
                if (_runtimeStates.TryGetValue(typeof(TState), out var s))
                {
                    state = (TState)s;
                    return true;
                }
            }
            state = null;
            return false;
        }

        /// <summary>
        /// 显式注册或设置指定状态实例。
        /// </summary>
        public virtual void SetState<TState>(TState state) where TState : class, IItemComponentState
        {
            if (state == null) return;
            _runtimeStates ??= new Dictionary<Type, IItemComponentState>(4);
            _runtimeStates[typeof(TState)] = state;
        }

        /// <summary>
        /// 底层显式注册原生状态类型与切片实例（常用于状态克隆与底层复原）。
        /// </summary>
        public virtual void SetRawState(Type type, IItemComponentState state)
        {
            if (type == null || state == null) return;
            _runtimeStates ??= new Dictionary<Type, IItemComponentState>(4);
            _runtimeStates[type] = state;
        }

        /// <summary>
        /// 移除指定类型的动态状态切片。若移除后字典为空，则释放字典。
        /// </summary>
        public virtual bool RemoveState<TState>() where TState : class, IItemComponentState
        {
            return RemoveRawState(typeof(TState));
        }

        /// <summary>
        /// 底层移除指定类型的动态状态切片。
        /// </summary>
        public virtual bool RemoveRawState(Type type)
        {
            if (_runtimeStates == null || type == null) return false;
            if (_runtimeStates.TryGetValue(type, out var state))
            {
                state?.Reset();
                _runtimeStates.Remove(type);
                return true;
            }
            return false;
        }
        #endregion

        #region 7. 组件透明查询 API (穿透至 Definition 享元列表，零 GC)
        /// <summary>
        /// 检查物品上是否存在指定的组件或接口实现（且处于启用状态）。
        /// </summary>
        public virtual bool HasComponent<T>() where T : class
        {
            return Definition != null && Definition.HasComponent<T>();
        }

        /// <summary>
        /// 获取优先级最高且处于启用状态的指定组件或接口实现（零 GC）。
        /// </summary>
        public virtual bool TryGetComponent<T>(out T comp) where T : class
        {
            if (Definition != null)
            {
                return Definition.TryGetComponent(out comp);
            }
            comp = null;
            return false;
        }

        /// <summary>
        /// 获取优先级最高且处于启用状态的组件或接口，若不存在则返回 null。
        /// </summary>
        public virtual T GetComponent<T>() where T : class
        {
            return Definition != null ? Definition.GetComponent<T>() : null;
        }

        /// <summary>
        /// 零 GC 获取所有匹配且已启用的组件或接口实现列表。
        /// </summary>
        public virtual void GetComponents<T>(List<T> results) where T : class
        {
            if (Definition != null)
            {
                Definition.GetComponents(results);
            }
        }

        /// <summary>
        /// 获取所有匹配且已启用的组件或接口实现数组。
        /// </summary>
        public virtual T[] GetComponents<T>() where T : class
        {
            return Definition != null ? Definition.GetComponents<T>() : Array.Empty<T>();
        }

        /// <summary>
        /// 根据 Type 获取享元组件。
        /// </summary>
        public virtual bool TryGetComponent(Type type, out ItemComponent comp)
        {
            if (Definition != null)
            {
                return Definition.TryGetComponent(type, out comp);
            }
            comp = null;
            return false;
        }
        #endregion

        #region 8. 堆叠与拆分
        /// <summary>
        /// 拆分当前物品堆叠数量，扣减自身堆叠数并产生并返回一个新的物品实例。
        /// </summary>
        public virtual ItemInstance Split(int splitCount)
        {
            if (splitCount <= 0 || StackCount <= 0) return null;
            int actualCount = Math.Min(splitCount, StackCount);

            int oldStackCount = StackCount;
            StackCount -= actualCount;
            TriggerStackCountChanged(oldStackCount, StackCount);

            ItemInstance newInstance = Definition.CreateInstance(actualCount);

            // 防御性克隆已有运行时动态状态切片
            if (_runtimeStates != null && _runtimeStates.Count > 0)
            {
                foreach (var kvp in _runtimeStates)
                {
                    if (kvp.Value != null)
                    {
                        newInstance.SetRawState(kvp.Key, kvp.Value.Clone());
                    }
                }
            }

            TriggerStackSplit(newInstance, actualCount);

            return newInstance;
        }

        /// <summary>
        /// 校验当前物品是否可以与目标物品进行堆叠合并。
        /// 条件：
        /// 1. 基础校验：目标有效且不为自身，Definition 相同且属于可堆叠物品，目标槽位未满；
        /// 2. 状态极速路径：双方均无动态状态（纯白板物品），直接放行允许堆叠；
        /// 3. 状态数量校验：若状态切片数量不一致（如一方为白板，另一方有动态修改），直接拒绝；
        /// 4. 切片深度校验：双方均有状态时，逐一校验状态类型匹配并委托每个状态切片的 CanStackWith 进行业务兼容性判定。
        /// </summary>
        public virtual bool CanStackWith(ItemInstance target)
        {
            if (target == null) return false;
            if (ReferenceEquals(this, target)) return false;
            if (Definition != target.Definition) return false;
            if (!Definition.IsStackable) return false;
            if (target.StackCount >= target.Definition.MaxStack) return false;

            // 1. 双方均无动态状态（白板物品）：极速放行
            if (!HasAnyRuntimeState && !target.HasAnyRuntimeState) return true;

            // 2. 状态切片数量不一致（如一方为白板，另一方有动态状态）：立刻拒绝
            int myCount = _runtimeStates?.Count ?? 0;
            int targetCount = target._runtimeStates?.Count ?? 0;
            if (myCount != targetCount) return false;

            // 3. 双方均有状态：逐一校验类型匹配并委托状态切片判定兼容性
            if (_runtimeStates != null && target._runtimeStates != null)
            {
                foreach (var kvp in _runtimeStates)
                {
                    var stateType = kvp.Key;
                    var myState = kvp.Value;

                    if (!target._runtimeStates.TryGetValue(stateType, out var targetState))
                    {
                        return false;
                    }

                    if (myState != null && !myState.CanStackWith(targetState))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        #endregion

        #region 9. 内部生命周期触发派发 (向 Definition.Components 广播 (this, ...))
        protected virtual void InitializeComponents()
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnInstanceCreated(this);
                }
            }
        }

        internal virtual void TriggerLoadedLifecycle()
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnInstanceLoaded(this);
                }
            }
        }

        internal virtual void TriggerAddedToContainer(InventoryContainer container, int slotIndex)
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnAddedToContainer(this, container, slotIndex);
                }
            }
        }

        internal virtual void TriggerRemovedFromContainer(InventoryContainer container, int slotIndex)
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnRemovedFromContainer(this, container, slotIndex);
                }
            }
        }

        internal virtual void TriggerStackCountChanged(int oldCount, int newCount)
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnStackCountChanged(this, oldCount, newCount);
                }
            }
        }

        internal virtual void TriggerStackMerged(ItemInstance sourceInstance, int transferredCount)
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnStackMerged(this, sourceInstance, transferredCount);
                }
            }
        }

        internal virtual void TriggerStackSplit(ItemInstance newInstance, int splitCount)
        {
            var comps = Definition.Components;
            if (comps == null) return;

            int count = comps.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = comps[i];
                if (comp != null)
                {
                    comp.OnStackSplit(this, newInstance, splitCount);
                }
            }
        }

        internal virtual void TriggerInstanceDestroyed()
        {
            var comps = Definition.Components;
            if (comps != null)
            {
                int count = comps.Count;
                for (int i = 0; i < count; i++)
                {
                    var comp = comps[i];
                    if (comp != null)
                    {
                        comp.OnInstanceDestroyed(this);
                    }
                }
            }

            // 重置并清空所有动态状态切片
            if (_runtimeStates != null)
            {
                foreach (var state in _runtimeStates.Values)
                {
                    state?.Reset();
                }
                _runtimeStates.Clear();
            }
        }
        #endregion
    }
}
