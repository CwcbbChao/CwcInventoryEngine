# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-08

### Added
- **Flyweight Architecture & State Slicing (享元模式与动态状态切片架构)**:
  - `ItemDefinition`: ScriptableObject immutable asset template hosting polymorphic `ItemComponent` definitions via `[SerializeReference]`.
  - `ItemInstance`: Lightweight C# runtime instance referencing the shared definition with on-demand `IItemComponentState` slices, achieving 0-GC allocation for unmodified plain items.
  - `ItemId`: Readonly Guid value-type struct ensuring zero GC allocation and fast equality checks.
  - O(1) Fast-path stacking verification for unmodified plain items, with deep slice delegation for modified items.
- **Pure C# Domain Model & Container Transactions (纯 C# 领域实体与事务容器)**:
  - `InventoryContainer`: Pure POCO domain model implementing `IReadOnlyInventoryContainer`, fully decoupled from `MonoBehaviour` for frictionless unit testing.
  - Native atomic container operations: Add, Remove, Split, Swap, Defragment (eliminate air pockets & merge partial stacks), Sort, QuickStackTo, and TransferAll.
  - `BatchScope`: Transaction context suppressing redundant event dispatches until the scope completes.
  - Dynamic capacity expansion and shrink protection (`ResizeCapacity`).
- **Decoupled Dual-Bus Pipeline & Global Registry (双总线管线与全局解耦注册中心)**:
  - `InventoryEventPipeline`: Readonly struct event pipeline (`in InventoryEvent`) with dead-target validation protecting against destroyed Unity objects.
  - `InventoryRequestPipeline`: Active intent request bus allowing UI and gameplay logic to broadcast actions (`Drop`, `Use`, `Move`, `Split`, `Sort`, `Defragment`, `SetSlotLock`) without referencing concrete containers.
  - `InventoryBuiltinRequestHandler`: Auto-initialized builtin handler managing intra- and cross-container slot movement, swapping, and defragmentation.
  - `InventoryRegistry`: Global string-based ID mapping registry with sub-system registration reset supporting Disable Domain Reload.
- **Smart Pickup Router (智能拾取路由器)**:
  - `InventoryRouter`: Automatically routes pickups to restricted equipment/hotbar slots before falling back to main inventories.
  - Extensible `IInventoryRoutePolicy` interface for project-level custom dispatch algorithms.
  - `ItemPickup`: Generic world drop pickup component with double-pickup locking and global routing integration.
  - `InventoryDropHandler`: Composable drop handler resolving player position and spawning drop prefabs safely.
- **Zero-Boxing Query & Sorting System (0-GC 判别联合属性评估与查询系统)**:
  - `ItemPropertyValue`: Discriminated union readonly struct eliminating boxing allocations during sorting and filtering.
  - `ItemPropertyEvaluator`: Fast property evaluator querying `IItemPropertyProvider` with fallback adapters.
  - `InventoryQuerySpec`: Composable specification grouping filter rules (`IItemRule`) and sort keys for UI-driven queries.
- **UI Architecture & Data-Window Sliding (数据窗口平移 UI 系统)**:
  - `InventoryListUIController`: High-performance list and grid controller reusing fixed UI slots via Data Window Sliding, eliminating runtime Instantiate/Destroy allocations.
  - `InventoryListItemUI`: Slot UI component using `CanvasGroup` alpha toggling instead of `GameObject.SetActive`, avoiding canvas mesh rebuild overhead.
  - `InventoryViewBase`: Logic gatekeeper base class preventing background calculation when views are hidden.
  - `MainInventoryView`: Category tab switching, search filtering, and detail panel binding.
  - `EquipmentInventoryView`: Equipment slot navigation and sub-menu popup with automatic `CanAccept` filtering for instant equipment swapping.
  - `InventoryUIController`: Top-level mediator managing input provider routing and view transitions.
- **Autonomous Persistence & Refactoring Safeguards (存盘读盘自治与版本重构容错)**:
  - Component autonomy: `ItemComponent.TryExportState` and `ImportState` allow components to independently manage serialization.
  - `ItemSaveData` & `InventorySaveData`: Standard DTO persistence format decoupled from concrete loading frameworks.
  - Refactoring compatibility: Supports full name matching, short name matching, and `[ItemComponentAlias]` attributes to survive class renames and namespace migrations.
- **Editor Tooling & Inspector Polish (编辑器工具链)**:
  - `ItemDefinitionEditor`: Custom inspector eliminating native `[SerializeReference]` blue selection highlights, featuring flat headers, `AdvancedDropdown` component search tree, and single/all component clipboard copy-paste.
  - `InventoryComponentEditor`: Real-time readonly inspector debug slot viewer with empty slot filtering and runtime clear shortcut.
- **Unit Testing Suite (单元测试套件)**:
  - `ItemFlyweightTests`: Comprehensive NUnit editor tests verifying pure flyweight zero allocation, dynamic state slicing, stack splitting, autonomous save/load, defragmentation, and query sorting.
