using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 物品掉落动态状态切片。
    /// 仅在物品实例被动态锁定丢弃状态或单独覆盖掉落 Prefab 时按需挂载。
    /// </summary>
    public class ItemDropComponentState : IItemComponentState
    {
        public bool? CustomCanDropOverride;
        public GameObject CustomDropPrefabOverride;

        public void Reset()
        {
            CustomCanDropOverride = null;
            CustomDropPrefabOverride = null;
        }

        public IItemComponentState Clone() => new ItemDropComponentState
        {
            CustomCanDropOverride = CustomCanDropOverride,
            CustomDropPrefabOverride = CustomDropPrefabOverride
        };

        public bool CanStackWith(IItemComponentState other)
        {
            if (other is ItemDropComponentState otherDrop)
            {
                return CustomCanDropOverride == otherDrop.CustomCanDropOverride
                    && CustomDropPrefabOverride == otherDrop.CustomDropPrefabOverride;
            }
            return false;
        }
    }

    /// <summary>
    /// 物品掉落控制享元组件。
    /// 配置在 ItemDefinition 的 Components 列表中，控制物品的丢弃限制、专属掉落物 Prefab 与音效。
    /// </summary>
    [Serializable]
    [ItemComponentPath("通用/物品掉落 Drop")]
    public class ItemDropComponent : ItemComponent
    {
        #region 2. Inspector 字段
        [SerializeField]
        [Tooltip("该物品是否允许丢弃。若设为 false，丢弃请求将被直接拒绝 (例如任务道具、绑定装备)。")]
        private bool _canDrop = true;

        [SerializeField]
        [Tooltip("该物品专用的场景掉落物 Prefab (需挂载 ItemPickup 组件)。若为空，将回退使用项目的默认掉落物 Prefab。")]
        private GameObject _customDropPrefab;

        [SerializeField]
        [Tooltip("丢弃该物品时播放的自定义音效 (可选)。")]
        private AudioClip _dropSound;
        #endregion

        #region 4. 公共属性 (基准配置)
        public bool CanDrop => _canDrop;
        public GameObject CustomDropPrefab => _customDropPrefab;
        public AudioClip DropSound => _dropSound;
        #endregion

        #region 6. 公共业务方法
        /// <summary>
        /// 获取指定物品实例当前是否允许丢弃（优先读取实例动态覆盖，无覆盖则返回静态配置）。
        /// </summary>
        public virtual bool GetCanDrop(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<ItemDropComponentState>(out var state))
            {
                if (state.CustomCanDropOverride.HasValue)
                {
                    return state.CustomCanDropOverride.Value;
                }
            }
            return _canDrop;
        }

        /// <summary>
        /// 获取指定物品实例的掉落预制件（优先读取实例动态覆盖，无覆盖则返回静态配置）。
        /// </summary>
        public virtual GameObject GetCustomDropPrefab(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<ItemDropComponentState>(out var state))
            {
                if (state.CustomDropPrefabOverride != null)
                {
                    return state.CustomDropPrefabOverride;
                }
            }
            return _customDropPrefab;
        }

        /// <summary>
        /// 获取丢弃音效。
        /// </summary>
        public virtual AudioClip GetDropSound(ItemInstance instance)
        {
            return _dropSound;
        }

        /// <summary>
        /// 运行时动态设置/锁定该物品实例的丢弃状态 (例如装备绑定后禁止丢弃)。
        /// </summary>
        public virtual void SetCanDrop(ItemInstance instance, bool canDrop)
        {
            if (instance == null) return;
            var state = instance.GetState<ItemDropComponentState>();
            state.CustomCanDropOverride = canDrop;
        }

        /// <summary>
        /// 运行时动态重置丢弃状态锁定。
        /// </summary>
        public virtual void ClearCanDropOverride(ItemInstance instance)
        {
            if (instance == null) return;
            if (instance.TryGetState<ItemDropComponentState>(out var state))
            {
                state.CustomCanDropOverride = null;
            }
        }

        /// <summary>
        /// 运行时动态覆盖掉落 Prefab。
        /// </summary>
        public virtual void SetCustomDropPrefab(ItemInstance instance, GameObject dropPrefab)
        {
            if (instance == null) return;
            var state = instance.GetState<ItemDropComponentState>();
            state.CustomDropPrefabOverride = dropPrefab;
        }
        #endregion
    }
}
