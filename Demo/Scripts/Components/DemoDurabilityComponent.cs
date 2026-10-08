using System;
using UnityEngine;

namespace Cwc.InventoryEngine.Demo
{
    /// <summary>
    /// 耐久度动态状态切片。
    /// 随 ItemInstance 挂载，保存当前耐久度数值。
    /// </summary>
    public class DemoDurabilityComponentState : IItemComponentState
    {
        public int CurrentDurability;
        public bool IsInitialized;

        public void Reset()
        {
            CurrentDurability = 0;
            IsInitialized = false;
        }

        public IItemComponentState Clone() => new DemoDurabilityComponentState
        {
            CurrentDurability = CurrentDurability,
            IsInitialized = IsInitialized
        };

        public bool CanStackWith(IItemComponentState other)
        {
            if (other is DemoDurabilityComponentState otherDur)
            {
                return CurrentDurability == otherDur.CurrentDurability && IsInitialized == otherDur.IsInitialized;
            }
            return false;
        }
    }

    /// <summary>
    /// Demo 专属 durability 持久化存盘数据结构 DTO。
    /// </summary>
    [Serializable]
    public class DemoDurabilitySaveData
    {
        public int CurrentDurability;
    }

    /// <summary>
    /// Demo 物品耐久度享元组件。
    /// 继承 ItemComponent，实现 IItemDisplay, IUsable, IEquippable 接口。
    /// 演示动态组件通过 ItemInstance.GetState&lt;DemoDurabilityComponentState&gt;() 干预堆叠兼容性与存盘自治。
    /// </summary>
    [Serializable]
    [ItemComponentPath("Demo 示例/耐久度 Durability")]
    public class DemoDurabilityComponent : ItemComponent, IItemDisplay, IUsable, IEquippable
    {
        #region 2. Inspector 字段
        [Header("耐久度基础配置")]
        [SerializeField]
        [Tooltip("物品的最大耐久度")]
        [Min(1)]
        private int _maxDurability = 100;
        #endregion

        #region 4. 公共属性
        public int MaxDurability => _maxDurability;
        #endregion

        #region 6. 状态查询与修改业务方法
        public virtual int GetCurrentDurability(ItemInstance instance)
        {
            if (instance == null) return _maxDurability;
            if (instance.TryGetState<DemoDurabilityComponentState>(out var state))
            {
                if (state.IsInitialized)
                {
                    return state.CurrentDurability;
                }
            }
            return _maxDurability;
        }

        public virtual float GetDurabilityPercent(ItemInstance instance)
        {
            return _maxDurability > 0 ? (float)GetCurrentDurability(instance) / _maxDurability : 0f;
        }

        public virtual bool IsBroken(ItemInstance instance)
        {
            return GetCurrentDurability(instance) <= 0;
        }

        public virtual bool ConsumeDurability(ItemInstance instance, int amount)
        {
            if (instance == null || amount <= 0) return false;

            var state = instance.GetState<DemoDurabilityComponentState>();
            if (!state.IsInitialized)
            {
                state.CurrentDurability = _maxDurability;
                state.IsInitialized = true;
            }

            state.CurrentDurability = Mathf.Max(0, state.CurrentDurability - amount);
            return state.CurrentDurability <= 0;
        }

        public virtual void Repair(ItemInstance instance, int amount)
        {
            if (instance == null || amount <= 0) return;

            var state = instance.GetState<DemoDurabilityComponentState>();
            if (!state.IsInitialized)
            {
                state.CurrentDurability = _maxDurability;
                state.IsInitialized = true;
            }

            state.CurrentDurability = Mathf.Min(_maxDurability, state.CurrentDurability + amount);
        }

        public virtual void SetCurrentDurability(ItemInstance instance, int currentDurability)
        {
            if (instance == null) return;
            var state = instance.GetState<DemoDurabilityComponentState>();
            state.CurrentDurability = Mathf.Clamp(currentDurability, 0, _maxDurability);
            state.IsInitialized = true;
        }
        #endregion

        #region 7. 生命周期重写
        public override void OnInstanceCreated(ItemInstance instance)
        {
            if (instance == null) return;
            var state = instance.GetState<DemoDurabilityComponentState>();
            state.CurrentDurability = _maxDurability;
            state.IsInitialized = true;
        }

        public override bool TryExportState(ItemInstance instance, out string jsonData)
        {
            if (instance != null && instance.TryGetState<DemoDurabilityComponentState>(out var state))
            {
                if (state.IsInitialized && state.CurrentDurability != _maxDurability)
                {
                    var saveData = new DemoDurabilitySaveData
                    {
                        CurrentDurability = state.CurrentDurability
                    };
                    jsonData = JsonUtility.ToJson(saveData);
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
                var saveData = JsonUtility.FromJson<DemoDurabilitySaveData>(jsonData);
                if (saveData != null)
                {
                    SetCurrentDurability(instance, saveData.CurrentDurability);
                }
            }
        }
        #endregion

        #region 8. IUsable 接口实现
        public bool CanUse(ItemInstance instance, GameObject user)
        {
            return !IsBroken(instance);
        }

        public bool OnUse(ItemInstance instance, GameObject user)
        {
            if (IsBroken(instance))
            {
                Debug.LogWarning("[DemoDurabilityComponent] 无法使用：道具已彻底损坏！");
                return false;
            }

            bool isBroken = ConsumeDurability(instance, 1);
            int cur = GetCurrentDurability(instance);
            Debug.Log($"[DemoDurabilityComponent] 挥舞消耗 1 点耐久，当前剩余: {cur}/{_maxDurability}" + (isBroken ? " (警告: 道具已损坏！)" : ""));
            return true;
        }
        #endregion

        #region 9. IEquippable 接口实现
        public bool CanEquip(ItemInstance instance, GameObject user)
        {
            return !IsBroken(instance);
        }

        public void OnEquip(ItemInstance instance, GameObject user) { }
        public void OnUnequip(ItemInstance instance, GameObject user) { }
        #endregion

        #region 10. IItemDisplay 接口实现
        public string GetDisplayName(ItemInstance instance) => null;
        public Sprite GetIcon(ItemInstance instance) => null;
        public string GetDescription(ItemInstance instance) => $"当前耐久度: {GetCurrentDurability(instance)} / {_maxDurability}";
        #endregion
    }
}
