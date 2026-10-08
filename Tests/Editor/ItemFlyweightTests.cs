using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Cwc.InventoryEngine.Query;

namespace Cwc.InventoryEngine.Tests
{
    public class ItemFlyweightTests
    {
        #region Mock State & Components
        public class TestMockState : IItemComponentState
        {
            public int CustomValue;
            public string CustomTag;

            public void Reset()
            {
                CustomValue = 0;
                CustomTag = null;
            }

            public IItemComponentState Clone() => new TestMockState
            {
                CustomValue = CustomValue,
                CustomTag = CustomTag
            };

            public bool CanStackWith(IItemComponentState other)
            {
                if (other is TestMockState otherMock)
                {
                    return CustomValue == otherMock.CustomValue &&
                           string.Equals(CustomTag, otherMock.CustomTag, StringComparison.Ordinal);
                }
                return false;
            }
        }

        [Serializable]
        public class TestPureFlyweightComponent : ItemComponent, IItemPropertyProvider
        {
            public int BasePower = 50;

            public virtual bool TryGetProperty(ItemInstance instance, string key, out ItemPropertyValue value)
            {
                value = ItemPropertyValue.Empty;
                if (string.Equals(key, "Power", StringComparison.OrdinalIgnoreCase))
                {
                    int total = BasePower;
                    if (instance != null && instance.TryGetState<TestMockState>(out var state))
                    {
                        total += state.CustomValue;
                    }
                    value = total;
                    return true;
                }
                return false;
            }

            public override bool TryExportState(ItemInstance instance, out string jsonData)
            {
                if (instance != null && instance.TryGetState<TestMockState>(out var state))
                {
                    jsonData = $"{state.CustomValue}:{state.CustomTag}";
                    return true;
                }
                jsonData = null;
                return false;
            }

            public override void ImportState(ItemInstance instance, string jsonData)
            {
                if (instance == null || string.IsNullOrEmpty(jsonData)) return;
                var parts = jsonData.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int val))
                {
                    var state = instance.GetState<TestMockState>();
                    state.CustomValue = val;
                    state.CustomTag = parts[1];
                }
            }
        }

        private class TestMockAssetResolver : IItemAssetResolver
        {
            private readonly Dictionary<string, ItemDefinition> _dict = new();

            public void Register(ItemDefinition def)
            {
                if (def != null) _dict[def.name] = def;
            }

            public string GetAssetKey(ItemDefinition definition)
            {
                return definition != null ? definition.name : string.Empty;
            }

            public ItemDefinition ResolveDefinition(string assetKey)
            {
                return !string.IsNullOrEmpty(assetKey) && _dict.TryGetValue(assetKey, out var def) ? def : null;
            }
        }
        #endregion

        #region Helper Methods
        private ItemDefinition CreateTestDefinition(string name, bool isStackable, int maxStack, params ItemComponent[] components)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.name = name;
            if (components != null)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    def.AddComponent(components[i]);
                }
            }
            return def;
        }
        #endregion

        #region Unit Tests
        [Test]
        public void Flyweight_PureStaticComponent_ZeroRuntimeAlloc()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("IronSword", false, 1, comp);

            var item = def.CreateInstance(1);

            Assert.IsNotNull(item);
            Assert.AreEqual("IronSword", item.Definition.name);
            Assert.IsFalse(item.HasAnyRuntimeState, "纯静态享元组件的白板物品实例不应分配任何动态状态切片");

            // 组件查询应穿透并返回 Definition 上的享元单例
            Assert.IsTrue(item.TryGetComponent<TestPureFlyweightComponent>(out var foundComp));
            Assert.AreSame(comp, foundComp);
        }

        [Test]
        public void Flyweight_DynamicState_SliceAllocationAndIsolation()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("MagicWand", false, 1, comp);

            var itemA = def.CreateInstance(1);
            var itemB = def.CreateInstance(1);

            // 修改 itemA 的动态状态
            var stateA = itemA.GetState<TestMockState>();
            stateA.CustomValue = 25;
            stateA.CustomTag = "Enchanted";

            Assert.IsTrue(itemA.HasAnyRuntimeState, "已分配状态切片的实例 HasAnyRuntimeState 应为 true");
            Assert.IsFalse(itemB.HasAnyRuntimeState, "未经修改的实例 itemB 不应受 itemA 状态影响 (隔离性)");

            Assert.IsTrue(itemA.TryGetState<TestMockState>(out var fetchedA));
            Assert.AreEqual(25, fetchedA.CustomValue);
            Assert.AreEqual("Enchanted", fetchedA.CustomTag);

            Assert.IsFalse(itemB.TryGetState<TestMockState>(out _));
        }

        [Test]
        public void Flyweight_Stacking_FastPath_And_StateCompatibility()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("Potion", true, 99, comp);

            var item1 = def.CreateInstance(5);
            var item2 = def.CreateInstance(10);

            // 1. 白板物品极速路径判定堆叠
            Assert.IsTrue(item1.CanStackWith(item2), "双方均无动态状态的白板物品应命中极速路径并允许堆叠");

            // 2. 赋予 item1 动态状态 (CustomValue = 10)
            item1.GetState<TestMockState>().CustomValue = 10;
            Assert.IsFalse(item1.CanStackWith(item2), "状态不一致的物品应拒绝堆叠");

            // 3. 将 item2 修改为不同状态 (CustomValue = 20)
            item2.GetState<TestMockState>().CustomValue = 20;
            Assert.IsFalse(item1.CanStackWith(item2), "状态切片不匹配的物品应拒绝堆叠");

            // 4. 将 item2 状态调整为完全相同 (CustomValue = 10)
            item2.GetState<TestMockState>().CustomValue = 10;
            Assert.IsTrue(item1.CanStackWith(item2), "状态完全兼容的物品应允许堆叠");

            // 5. 拆分测试：拆分出的新物品应保留克隆状态，且能够重新与原物品堆叠合并
            var splitItem = item1.Split(2);
            Assert.IsNotNull(splitItem);
            Assert.AreEqual(3, item1.StackCount);
            Assert.AreEqual(2, splitItem.StackCount);
            Assert.IsTrue(splitItem.TryGetState<TestMockState>(out var splitState));
            Assert.AreEqual(10, splitState.CustomValue);

            // 核心断言：拆分出来的物品能够重新堆叠合并回去！
            Assert.IsTrue(item1.CanStackWith(splitItem), "拆分后拥有相同状态切片的物品必须允许重新堆叠合并");
        }

        [Test]
        public void Flyweight_RemoveState_Restores_PristineState()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("Gem", true, 99, comp);

            var item = def.CreateInstance(1);
            item.GetState<TestMockState>().CustomValue = 100;
            Assert.IsTrue(item.HasAnyRuntimeState);

            bool removed = item.RemoveState<TestMockState>();
            Assert.IsTrue(removed);
            Assert.IsFalse(item.HasAnyRuntimeState, "移除状态切片后应恢复纯白板状态");
        }

        [Test]
        public void Flyweight_SaveLoad_Autonomy_RoundTrip()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("AncientRelic", false, 1, comp);
            var resolver = new TestMockAssetResolver();
            resolver.Register(def);

            var originalItem = def.CreateInstance(1);
            var state = originalItem.GetState<TestMockState>();
            state.CustomValue = 999;
            state.CustomTag = "Legendary";

            // 存盘导出
            var saveData = ItemSaveData.Export(originalItem, resolver);
            Assert.IsNotNull(saveData);
            Assert.IsNotNull(saveData.ComponentStates);
            Assert.Greater(saveData.ComponentStates.Count, 0, "具有自定义状态的组件应成功导出存盘数据");

            // 反序列化还原到新实例
            var restoredItem = saveData.Restore(resolver);
            Assert.IsNotNull(restoredItem);

            Assert.IsTrue(restoredItem.TryGetState<TestMockState>(out var restoredState));
            Assert.AreEqual(999, restoredState.CustomValue);
            Assert.AreEqual("Legendary", restoredState.CustomTag);
        }

        [Test]
        public void Flyweight_PolymorphicDispatch_Via_Interfaces()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("BattleAxe", false, 1, comp);

            var item = def.CreateInstance(3);

            // 1. 属性查询
            Assert.IsTrue(ItemPropertyEvaluator.TryGetProperty(item, "Power", out var propVal));
            Assert.AreEqual(50, propVal.IntValue);

            // 动态增加 20 点 Power
            item.GetState<TestMockState>().CustomValue = 20;
            Assert.IsTrue(ItemPropertyEvaluator.TryGetProperty(item, "Power", out propVal));
            Assert.AreEqual(70, propVal.IntValue);
        }

        [Test]
        public void Container_TryTakeSlotItem_PreservesState_WithoutDestroying()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("EnchantedSword", false, 1, comp);

            var container = new InventoryContainer(10);
            var item = def.CreateInstance(1);
            var state = item.GetState<TestMockState>();
            state.CustomValue = 888;
            state.CustomTag = "Sharp";

            // 存入槽位 0
            container.TryAddItemToSlot(0, item, out _);
            Assert.IsFalse(container.Slots[0].IsEmpty);

            // 1. 全量安全出库
            bool success = container.TryTakeSlotItem(0, 1, out var takenItem);
            Assert.IsTrue(success);
            Assert.IsNotNull(takenItem);
            Assert.IsTrue(container.Slots[0].IsEmpty, "槽位应已被清空");

            // 核心断言：动态状态切片未被清空重置！
            Assert.IsTrue(takenItem.TryGetState<TestMockState>(out var takenState));
            Assert.AreEqual(888, takenState.CustomValue);
            Assert.AreEqual("Sharp", takenState.CustomTag);
        }

        [Test]
        public void Container_TryTakeSlotItem_PartialSplit_ClonesStateCorrectly()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("StackableRune", true, 99, comp);

            var container = new InventoryContainer(10);
            var item = def.CreateInstance(10);
            item.GetState<TestMockState>().CustomValue = 42;

            container.TryAddItemToSlot(0, item, out _);

            // 2. 部分安全出库 (取出 4 个)
            bool success = container.TryTakeSlotItem(0, 4, out var takenItem);
            Assert.IsTrue(success);
            Assert.IsNotNull(takenItem);
            Assert.AreEqual(4, takenItem.StackCount);
            Assert.AreEqual(6, container.Slots[0].Item.StackCount);

            // 核心断言：取出的新实例与原槽位实例均完整持有状态切片
            Assert.IsTrue(takenItem.TryGetState<TestMockState>(out var takenState));
            Assert.AreEqual(42, takenState.CustomValue);

            Assert.IsTrue(container.Slots[0].Item.TryGetState<TestMockState>(out var slotState));
            Assert.AreEqual(42, slotState.CustomValue);
        }

        [ItemComponentAlias("OldPureComponent", "LegacyNamespace.PureComponent")]
        [Serializable]
        private class TestAliasedComponent : TestPureFlyweightComponent
        {
        }

        [Test]
        public void SaveLoad_TypeAliasAttribute_SmoothMigrationAndAutoUpgrade()
        {
            var comp = new TestAliasedComponent();
            var def = CreateTestDefinition("RelicWithAlias", false, 1, comp);
            var resolver = new TestMockAssetResolver();
            resolver.Register(def);

            // 模拟旧版本存档，使用历史别名 "OldPureComponent"
            var oldSaveData = new ItemSaveData
            {
                AssetKey = "RelicWithAlias",
                InstanceGuid = ItemId.NewId().ToString(),
                StackCount = 1,
                ComponentStates = new List<ComponentSaveEntry>
                {
                    new ComponentSaveEntry
                    {
                        ComponentType = "OldPureComponent",
                        JsonData = "777:Ancient"
                    }
                }
            };

            // 1. 读盘还原：通过别名成功匹配 TestAliasedComponent
            var restoredItem = oldSaveData.Restore(resolver);
            Assert.IsNotNull(restoredItem);
            Assert.IsTrue(restoredItem.TryGetState<TestMockState>(out var restoredState));
            Assert.AreEqual(777, restoredState.CustomValue);
            Assert.AreEqual("Ancient", restoredState.CustomTag);

            // 2. 自动升级：下次导出时自动使用当前最新的 FullName
            var newSaveData = ItemSaveData.Export(restoredItem, resolver);
            Assert.IsNotNull(newSaveData);
            Assert.AreEqual(1, newSaveData.ComponentStates.Count);
            Assert.AreEqual(typeof(TestAliasedComponent).FullName, newSaveData.ComponentStates[0].ComponentType);
        }

        [Test]
        public void Container_TryAddItem_OverflowSplit_ClonesStateCorrectly()
        {
            var comp = new TestPureFlyweightComponent();
            var def = CreateTestDefinition("StackRune", true, 5, comp);

            var container = new InventoryContainer(3);

            // 创建一个超出 MaxStack (5) 的物品实例 (堆叠 8)，并赋予动态状态
            var item = new ItemInstance(ItemId.NewId(), def, 8);
            item.GetState<TestMockState>().CustomValue = 123;
            item.GetState<TestMockState>().CustomTag = "Blessed";

            bool success = container.TryAddItem(item, out var remainder);
            Assert.IsTrue(success);
            Assert.IsNull(remainder, "3 个空槽足够容纳 8 个数量 (5 + 3)");

            // 槽位 0 应存放 5 个，槽位 1 应存放 3 个
            Assert.IsFalse(container.Slots[0].IsEmpty);
            Assert.AreEqual(5, container.Slots[0].Item.StackCount);
            Assert.IsFalse(container.Slots[1].IsEmpty);
            Assert.AreEqual(3, container.Slots[1].Item.StackCount);

            // 核心断言：被拆分放入槽位 0 的新实例也必须完整克隆持有动态状态切片
            Assert.IsTrue(container.Slots[0].Item.TryGetState<TestMockState>(out var state0));
            Assert.AreEqual(123, state0.CustomValue);
            Assert.AreEqual("Blessed", state0.CustomTag);

            Assert.IsTrue(container.Slots[1].Item.TryGetState<TestMockState>(out var state1));
            Assert.AreEqual(123, state1.CustomValue);
            Assert.AreEqual("Blessed", state1.CustomTag);
        }

        private class TestSlotOrderComparer : IComparer<ItemSlot>
        {
            public int Compare(ItemSlot x, ItemSlot y)
            {
                if (x == null && y == null) return 0;
                if (x == null || x.IsEmpty) return 1;
                if (y == null || y.IsEmpty) return -1;
                return string.Compare(x.Item.Definition.name, y.Item.Definition.name, StringComparison.Ordinal);
            }
        }

        [Test]
        public void Container_Sort_And_AutoSort_Lifecycle_Consistency()
        {
            var defA = CreateTestDefinition("Apple", true, 99);
            var defB = CreateTestDefinition("Banana", true, 99);

            var container = new InventoryContainer(5);
            container.TryAddItemToSlot(1, defB.CreateInstance(1), out _);
            container.TryAddItemToSlot(3, defA.CreateInstance(1), out _);

            // AutoSort 委托至 Sort，应消除空穴并将 Apple (槽位0) 与 Banana (槽位1) 重新排列
            container.AutoSort(new TestSlotOrderComparer());

            Assert.IsFalse(container.Slots[0].IsEmpty);
            Assert.AreEqual("Apple", container.Slots[0].Item.Definition.name);

            Assert.IsFalse(container.Slots[1].IsEmpty);
            Assert.AreEqual("Banana", container.Slots[1].Item.Definition.name);

            Assert.IsTrue(container.Slots[2].IsEmpty);
            Assert.IsTrue(container.Slots[3].IsEmpty);
            Assert.IsTrue(container.Slots[4].IsEmpty);
        }
        #endregion
    }
}
