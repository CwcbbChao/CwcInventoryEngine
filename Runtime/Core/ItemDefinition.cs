using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 静态物品资产定义 (ScriptableObject)。
    /// 极简根数据，仅包含通用堆叠上限与享元组件列表。
    /// 严禁在此处硬编码任何业务扩展属性（如攻击力、防御力、图标等）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Cwc/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        #region 2. Inspector 字段
        [Tooltip("最大堆叠上限。<=1 表示不可堆叠物品。")]
        [SerializeField] private int maxStack = 99;

        [FormerlySerializedAs("Components")]
        [SerializeReference]
        [Tooltip("多态享元组件列表。借助 [SerializeReference] 保存在当前物品资产内部。")]
        [SerializeField] private List<ItemComponent> _components = new();
        #endregion

        #region 4. 公共属性
        /// <summary>
        /// 堆叠上限。<=1 表示不可堆叠。
        /// </summary>
        public int MaxStack => Mathf.Max(1, maxStack);

        /// <summary>
        /// 是否允许堆叠。
        /// </summary>
        public bool IsStackable => MaxStack > 1;

        /// <summary>
        /// 多态享元组件只读列表。
        /// </summary>
        public IReadOnlyList<ItemComponent> Components => _components;
        #endregion

        #region 6. 实例创建方法
        /// <summary>
        /// 创建一个新的运行时物品实例 (ID 自动生成)。
        /// </summary>
        /// <param name="count">初始堆叠数量</param>
        /// <returns>运行时物品实例</returns>
        public virtual ItemInstance CreateInstance(int count = 1)
        {
            return CreateInstanceWithId(ItemId.NewId(), count);
        }

        /// <summary>
        /// 使用指定的 ItemId 创建运行时物品实例（常用于读盘还原）。
        /// </summary>
        /// <param name="instanceId">特定的物品实例标识</param>
        /// <param name="count">初始堆叠数量</param>
        /// <returns>运行时物品实例</returns>
        public virtual ItemInstance CreateInstanceWithId(ItemId instanceId, int count = 1)
        {
            int clampedCount = Mathf.Clamp(count, 1, MaxStack);
            ItemInstance instance = new ItemInstance(instanceId, this, clampedCount);
            return instance;
        }
        #endregion

        #region 7. 组件查询方法 (零 GC)
        /// <summary>
        /// 检查物品上是否存在指定的组件或接口实现。
        /// </summary>
        public virtual bool HasComponent<T>() where T : class
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                if (_components[i] is T)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取首个匹配的指定组件或接口实现（零 GC）。
        /// </summary>
        public virtual bool TryGetComponent<T>(out T comp) where T : class
        {
            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                if (_components[i] is T target)
                {
                    comp = target;
                    return true;
                }
            }
            comp = null;
            return false;
        }

        /// <summary>
        /// 获取首个匹配的组件或接口，若不存在则返回 null。
        /// </summary>
        public virtual T GetComponent<T>() where T : class
        {
            TryGetComponent<T>(out T comp);
            return comp;
        }

        /// <summary>
        /// 零 GC 获取所有匹配的组件或接口实现列表。
        /// </summary>
        public virtual void GetComponents<T>(List<T> results) where T : class
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            int count = _components.Count;
            for (int i = 0; i < count; i++)
            {
                var comp = _components[i];
                if (comp is T target)
                {
                    results.Add(target);
                }
            }
        }

        /// <summary>
        /// 获取所有匹配的组件或接口实现数组。
        /// </summary>
        public virtual T[] GetComponents<T>() where T : class
        {
            var list = new List<T>();
            GetComponents(list);
            return list.ToArray();
        }

        /// <summary>
        /// 根据 Type 获取享元组件。
        /// </summary>
        public virtual bool TryGetComponent(Type componentType, out ItemComponent comp)
        {
            if (componentType != null && _components != null)
            {
                int count = _components.Count;
                for (int i = 0; i < count; i++)
                {
                    var candidate = _components[i];
                    if (candidate != null && candidate.GetType() == componentType)
                    {
                        comp = candidate;
                        return true;
                    }
                }
            }
            comp = null;
            return false;
        }

        /// <summary>
        /// 向物品定义追加享元组件（常用于运行期纯代码构建临时物品定义）。
        /// </summary>
        /// <param name="component">目标享元组件</param>
        public virtual void AddComponent(ItemComponent component)
        {
            if (component == null) return;
            _components ??= new List<ItemComponent>();
            if (!_components.Contains(component))
            {
                _components.Add(component);
            }
        }
        #endregion
    }
}
