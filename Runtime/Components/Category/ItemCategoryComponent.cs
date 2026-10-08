using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 通用物品分类运行时动态状态切片。
    /// 仅在物品实例被动态追加额外分类标签时按需分配挂载。
    /// </summary>
    public class ItemCategoryComponentState : IItemComponentState
    {
        public readonly List<ItemCategorySO> DynamicCategories = new();

        public void Reset()
        {
            DynamicCategories.Clear();
        }

        public IItemComponentState Clone()
        {
            var clone = new ItemCategoryComponentState();
            clone.DynamicCategories.AddRange(DynamicCategories);
            return clone;
        }

        public bool CanStackWith(IItemComponentState other)
        {
            if (other is ItemCategoryComponentState otherCat)
            {
                if (DynamicCategories.Count != otherCat.DynamicCategories.Count) return false;
                int count = DynamicCategories.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!otherCat.DynamicCategories.Contains(DynamicCategories[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 通用物品分类享元组件。
    /// 可挂载在任意 ItemDefinition 上，赋予物品一个或多个分类/标签。
    /// 实现 IItemCategorized 接口，支持纯静态只读与动态追加扩展。
    /// </summary>
    [Serializable]
    [ItemComponentPath("常规/通用物品分类 Item Category")]
    public class ItemCategoryComponent : ItemComponent, IItemCategorized
    {
        #region 2. Inspector 字段
        [Header("分类与标签配置")]
        [SerializeField]
        [Tooltip("物品分类/类型资产列表 (SO)，可支持一个或多个分类标签")]
        private List<ItemCategorySO> _categories = new();
        #endregion

        #region 4. 公共属性
        /// <summary>
        /// 静态配置的分类列表 (只读)。
        /// </summary>
        public IReadOnlyList<ItemCategorySO> Categories => _categories;
        #endregion

        #region 6. 公共业务方法
        /// <summary>
        /// 获取指定物品实例的有效分类列表（静态配置 + 动态追加）。
        /// </summary>
        public virtual void GetCategories(ItemInstance instance, List<ItemCategorySO> results)
        {
            if (results == null) return;

            // 1. 添加静态配置中的分类
            if (_categories != null)
            {
                int staticCount = _categories.Count;
                for (int i = 0; i < staticCount; i++)
                {
                    var cat = _categories[i];
                    if (cat != null && !results.Contains(cat))
                    {
                        results.Add(cat);
                    }
                }
            }

            // 2. 检查并合并动态追加的分类
            if (instance != null && instance.TryGetState<ItemCategoryComponentState>(out var state))
            {
                var dynList = state.DynamicCategories;
                int dynCount = dynList.Count;
                for (int i = 0; i < dynCount; i++)
                {
                    var cat = dynList[i];
                    if (cat != null && !results.Contains(cat))
                    {
                        results.Add(cat);
                    }
                }
            }
        }

        /// <summary>
        /// 零 GC 获取首个有效分类资产（静态优先，无静态则取首个动态追加分类）。
        /// </summary>
        public virtual ItemCategorySO GetPrimaryCategory(ItemInstance instance)
        {
            if (_categories != null && _categories.Count > 0 && _categories[0] != null)
            {
                return _categories[0];
            }

            if (instance != null && instance.TryGetState<ItemCategoryComponentState>(out var state))
            {
                if (state.DynamicCategories.Count > 0)
                {
                    return state.DynamicCategories[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 动态向指定物品实例追加额外的分类标签。
        /// </summary>
        public virtual void AddDynamicCategory(ItemInstance instance, ItemCategorySO category)
        {
            if (instance == null || category == null) return;

            var state = instance.GetState<ItemCategoryComponentState>();
            if (!state.DynamicCategories.Contains(category))
            {
                state.DynamicCategories.Add(category);
            }
        }

        /// <summary>
        /// 从指定物品实例中移除动态追加的分类标签。
        /// </summary>
        public virtual bool RemoveDynamicCategory(ItemInstance instance, ItemCategorySO category)
        {
            if (instance == null || category == null) return false;

            if (instance.TryGetState<ItemCategoryComponentState>(out var state))
            {
                return state.DynamicCategories.Remove(category);
            }
            return false;
        }
        #endregion
    }
}
