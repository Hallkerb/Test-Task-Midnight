# Test Task: Idle Tycoon

Core game mechanics: A 3D Idle Tycoon supermarket simulator where players purchase and upgrade store slots, manage AI customer flow, collect profits, and expand the store territory.

## Setup & Launch

- **Unity Version:** `6000.3.17f1`
- **Main Scene:** `Assets/Scenes/MainMenu.unity`

## Controls & Interaction

- **LMB / Tap / Drag:** Interact with UI, click on build slots to buy or upgrade, and interact with panels.
- **WASD / Arrow Keys / Drag:** Camera navigation across the store territory.

## Gameplay

1. **Initial Setup:** The player begins with starting capital and unlocks initial slots (cash registers, shelves).
2. **Customer AI Loop:** Customers spawn automatically via `CustomerSpawner`, navigate to shelves to pick products, queue at active cash registers `CashRegister`, pay, and leave.
3. **Economy & Progression:** Sales generate soft currency (`Cash`). Funds are reinvested into upgrading existing objects (increasing capacity, speed, profit) or expanding new store zones via `StoreSlot`.
4. **Offline Earnings & Currency Exchange:** Players can receive passive offline income calculated from average income per second, as well as use the currency exchange panel.
5. **Persistence:** The game state (currency balances, unlocked slots, upgrade levels) automatically saves to a JSON file.

## Script Structure

Scripts/
├── Building/      BuildingData, BuildSlot, StoreSlot, RegisterBuildingData, ShelfBuildingData, Slot, Interfaces
├── Core/          GameSettings, StoreLevel, EconomyManager, StoreRegistry, Save System (SaveSystem, SaveData, etc.)
├── Customers/     Customer (AI), CustomerSpawner, IShopCustomer
├── Store/         CashRegister, Shelf, ProductData, Interfaces (ICheckout, IShelf, IUpgradable)
├── UI/            MainMenuController, PauseMenu, UpgradePanel, OfflineEarnedPanel, CurrencyExchangePanel, CurrencyView, SettingsPanel, LevelView, CustomersView, SalePopupView, TotalEarnedView
├── Utils/         SizeChecker
└── LoadingScreen / SceneTransition 

## Architectural Solutions

- **Component-Based Approach:** Each object holds a single responsibility. `BuildSlot` manages purchasing and upgrading states, `Shelf` and `CashRegister` control operational logic, and `Customer` handles AI behavior independently.
- **Interfaces over Direct Dependencies:** Uses `IBuildable`, `IUpgradable`, `IShelf`, `ICheckout`, and `IShopCustomer` to keep systems loosely coupled.
- **Object Pooling:** `CustomerSpawner` utilizes Unity's native `ObjectPool<T>` to eliminate allocations and garbage collection spikes during customer spawn/despawn cycles.
- **Event-Driven Architecture:** C# `Action` events (`OnBalanceChanged`, `OnCustomersChanged`, `OnBuilt`, `OnStateChanged`) decouple core game logic from UI views.
- **Extensibility:** Adding new store objects or products requires creating new `BuildingData` / `ProductData` ScriptableObjects and placing a `BuildSlot` prefab in the scene without modifying existing code.
- **Native JSON Save System:** Custom serialization via `JsonUtility` and `File.WriteAllText`.
