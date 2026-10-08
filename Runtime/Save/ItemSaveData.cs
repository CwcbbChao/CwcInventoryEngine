using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 单个物品实例的序列化持久化传输 DTO。
    /// 完全解耦具体加载框架，仅包含 Token 化的 AssetKey、Guid、堆叠数以及组件增量数据。
    /// </summary>
    [Serializable]
    public class ItemSaveData
    {
        #region 1. 私有静态字段
        private static readonly Dictionary<Type, HashSet<string>> _aliasCache = new();
        #endregion

        #region Serialized DTO Fields
        /// <summary>
        /// 物品实例的 Guid 字符串。
        /// </summary>
        public string InstanceGuid;

        /// <summary>
        /// 由 IItemAssetResolver 解析导出的静态资产标识 Token。
        /// </summary>
        public string AssetKey;

        /// <summary>
        /// 当前堆叠数量。
        /// </summary>
        public int StackCount;

        /// <summary>
        /// 组件自治序列化导出的动态增量状态列表。
        /// </summary>
        public List<ComponentSaveEntry> ComponentStates = new();
        #endregion

        #region Export Logic
        /// <summary>
        /// 将运行时 ItemInstance 导出为安全的标准 DTO 存盘数据。
        /// </summary>
        public static ItemSaveData Export(ItemInstance instance, IItemAssetResolver resolver)
        {
            if (instance == null || resolver == null) return null;

            string assetKey = resolver.GetAssetKey(instance.Definition);
            if (string.IsNullOrEmpty(assetKey))
            {
                Debug.LogWarning($"[CwcInventoryEngine] 无法为物品资产 '{instance.Definition.name}' 获取 AssetKey！");
                return null;
            }

            ItemSaveData saveData = new ItemSaveData
            {
                InstanceGuid = instance.InstanceID.ToString(),
                AssetKey = assetKey,
                StackCount = instance.StackCount,
                ComponentStates = new List<ComponentSaveEntry>()
            };

            // 存盘自治：遍历享元组件，由组件自治导出指定实例的动态状态
            var components = instance.Components;
            if (components != null)
            {
                int compCount = components.Count;
                for (int i = 0; i < compCount; i++)
                {
                    var comp = components[i];
                    if (comp == null) continue;

                    if (comp.TryExportState(instance, out string jsonData) && !string.IsNullOrEmpty(jsonData))
                    {
                        saveData.ComponentStates.Add(new ComponentSaveEntry
                        {
                            ComponentType = comp.GetType().FullName,
                            JsonData = jsonData
                        });
                    }
                }
            }

            return saveData;
        }
        #endregion

        #region Import Logic
        /// <summary>
        /// 从 DTO 存盘数据还原 ItemInstance，并具备完善的读盘容错机制。
        /// </summary>
        public ItemInstance Restore(IItemAssetResolver resolver)
        {
            if (resolver == null)
            {
                Debug.LogError("[CwcInventoryEngine] 还原物品失败：未提供 IItemAssetResolver！");
                return null;
            }

            ItemDefinition def = resolver.ResolveDefinition(AssetKey);
            if (def == null)
            {
                Debug.LogWarning($"[CwcInventoryEngine] 无法根据 AssetKey '{AssetKey}' 解析对应的 ItemDefinition 资产！");
                return null;
            }

            ItemId parsedId = ItemId.Parse(InstanceGuid);
            if (parsedId.IsEmpty)
            {
                parsedId = ItemId.NewId();
            }

            // 创建带指定 ID 的实例
            ItemInstance instance = def.CreateInstanceWithId(parsedId, StackCount);

            // 读盘自治还原组件状态
            if (ComponentStates != null && ComponentStates.Count > 0)
            {
                int stateCount = ComponentStates.Count;
                for (int i = 0; i < stateCount; i++)
                {
                    var entry = ComponentStates[i];
                    if (entry == null || string.IsNullOrEmpty(entry.ComponentType)) continue;

                    // 匹配享元组件
                    if (TryFindComponentByType(instance.Definition, entry.ComponentType, out var comp))
                    {
                        comp.ImportState(instance, entry.JsonData);
                    }
                    else
                    {
                        Debug.LogWarning($"[CwcInventoryEngine] 存档中含有类型为 '{entry.ComponentType}' 的组件，但在该物品定义 '{def.name}' 中未找到对应的匹配项，已自动跳过。");
                    }
                }
            }

            // 触发读盘完成生命周期
            instance.TriggerLoadedLifecycle();
            return instance;
        }

        private static HashSet<string> GetTypeAliases(Type type)
        {
            if (_aliasCache.TryGetValue(type, out var set))
            {
                return set;
            }

            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var attrs = (ItemComponentAliasAttribute[])type.GetCustomAttributes(typeof(ItemComponentAliasAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                var aliases = attrs[0].Aliases;
                if (aliases != null)
                {
                    int count = aliases.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!string.IsNullOrEmpty(aliases[i]))
                        {
                            set.Add(aliases[i]);
                        }
                    }
                }
            }

            _aliasCache[type] = set;
            return set;
        }

        private bool TryFindComponentByType(ItemDefinition def, string typeFullName, out ItemComponent comp)
        {
            if (def != null && def.Components != null && !string.IsNullOrEmpty(typeFullName))
            {
                var components = def.Components;
                int count = components.Count;

                // 1. 第一优先级：FullName 严格精确匹配 (最快路径)
                for (int i = 0; i < count; i++)
                {
                    var c = components[i];
                    if (c != null && string.Equals(c.GetType().FullName, typeFullName, StringComparison.Ordinal))
                    {
                        comp = c;
                        return true;
                    }
                }

                // 2. 第二优先级：纯类名忽略大小写匹配 (容忍命名空间重构)
                for (int i = 0; i < count; i++)
                {
                    var c = components[i];
                    if (c != null && string.Equals(c.GetType().Name, typeFullName, StringComparison.OrdinalIgnoreCase))
                    {
                        comp = c;
                        return true;
                    }
                }

                // 3. 第三优先级：检查 [ItemComponentAlias] 声明的历史兼容别名
                for (int i = 0; i < count; i++)
                {
                    var c = components[i];
                    if (c != null)
                    {
                        var aliases = GetTypeAliases(c.GetType());
                        if (aliases.Contains(typeFullName))
                        {
                            comp = c;
                            return true;
                        }
                    }
                }
            }

            comp = null;
            return false;
        }
        #endregion
    }
}
