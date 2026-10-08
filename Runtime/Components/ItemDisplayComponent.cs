using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 物品显示动态状态切片。
    /// 仅在物品实例被动态改名、自定义图标或修改描述时按需挂载。
    /// </summary>
    public class ItemDisplayComponentState : IItemComponentState
    {
        public string CustomDisplayName;
        public Sprite CustomIcon;
        public string CustomDescription;

        public void Reset()
        {
            CustomDisplayName = null;
            CustomIcon = null;
            CustomDescription = null;
        }

        public IItemComponentState Clone() => new ItemDisplayComponentState
        {
            CustomDisplayName = CustomDisplayName,
            CustomIcon = CustomIcon,
            CustomDescription = CustomDescription
        };

        public bool CanStackWith(IItemComponentState other)
        {
            if (other is ItemDisplayComponentState otherDisplay)
            {
                return string.Equals(CustomDisplayName, otherDisplay.CustomDisplayName, StringComparison.Ordinal)
                    && CustomIcon == otherDisplay.CustomIcon
                    && string.Equals(CustomDescription, otherDisplay.CustomDescription, StringComparison.Ordinal);
            }
            return false;
        }
    }

    /// <summary>
    /// 物品 UI 显示享元组件。
    /// 配置在 ItemDefinition 的 Components 列表中，实现 IItemDisplay 接口。
    /// 默认读取静态配置中的 DisplayName、Icon、Description，同时支持通过 ItemDisplayComponentState 进行动态覆盖。
    /// </summary>
    [Serializable]
    [ItemComponentPath("UI 显示/基础 Display")]
    public class ItemDisplayComponent : ItemComponent, IItemDisplay
    {
        #region 1. 内部 DTO
        [Serializable]
        private class DisplaySaveData
        {
            public string CustomDisplayName;
            public string CustomDescription;
        }
        #endregion

        #region 2. Inspector 字段
        [SerializeField]
        [Tooltip("物品显示名称（留空则默认使用 Definition 资产名称）")]
        private string _displayName;

        [SerializeField]
        [Tooltip("物品 UI 图标")]
        private Sprite _icon;

        [SerializeField]
        [TextArea(3, 6)]
        [Tooltip("物品详细描述信息")]
        private string _description;
        #endregion

        #region 4. 公共属性 (静态基准配置)
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        #endregion

        #region 6. IItemDisplay 接口与业务方法
        public virtual string GetDisplayName(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<ItemDisplayComponentState>(out var state))
            {
                if (!string.IsNullOrEmpty(state.CustomDisplayName))
                {
                    return state.CustomDisplayName;
                }
            }
            return _displayName;
        }

        public virtual Sprite GetIcon(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<ItemDisplayComponentState>(out var state))
            {
                if (state.CustomIcon != null)
                {
                    return state.CustomIcon;
                }
            }
            return _icon;
        }

        public virtual string GetDescription(ItemInstance instance)
        {
            if (instance != null && instance.TryGetState<ItemDisplayComponentState>(out var state))
            {
                if (!string.IsNullOrEmpty(state.CustomDescription))
                {
                    return state.CustomDescription;
                }
            }
            return _description;
        }

        public virtual void SetCustomDisplayName(ItemInstance instance, string customDisplayName)
        {
            if (instance == null) return;
            var state = instance.GetState<ItemDisplayComponentState>();
            state.CustomDisplayName = customDisplayName;
        }

        public virtual void SetCustomIcon(ItemInstance instance, Sprite customIcon)
        {
            if (instance == null) return;
            var state = instance.GetState<ItemDisplayComponentState>();
            state.CustomIcon = customIcon;
        }

        public virtual void SetCustomDescription(ItemInstance instance, string customDescription)
        {
            if (instance == null) return;
            var state = instance.GetState<ItemDisplayComponentState>();
            state.CustomDescription = customDescription;
        }

        public override bool TryExportState(ItemInstance instance, out string jsonData)
        {
            if (instance != null && instance.TryGetState<ItemDisplayComponentState>(out var state))
            {
                if (!string.IsNullOrEmpty(state.CustomDisplayName) || !string.IsNullOrEmpty(state.CustomDescription))
                {
                    var data = new DisplaySaveData
                    {
                        CustomDisplayName = state.CustomDisplayName,
                        CustomDescription = state.CustomDescription
                    };
                    jsonData = JsonUtility.ToJson(data);
                    return true;
                }
            }

            jsonData = null;
            return false;
        }

        public override void ImportState(ItemInstance instance, string jsonData)
        {
            if (instance != null && !string.IsNullOrEmpty(jsonData))
            {
                var data = JsonUtility.FromJson<DisplaySaveData>(jsonData);
                if (data != null)
                {
                    var state = instance.GetState<ItemDisplayComponentState>();
                    state.CustomDisplayName = data.CustomDisplayName;
                    state.CustomDescription = data.CustomDescription;
                }
            }
        }
        #endregion
    }
}
