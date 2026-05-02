# Diagramme de classes — Gestion Portefeuille

Ce document décrit les classes principales du projet (couches **Core**, **Infrastructure**, **Services**).  
**Affichage** : prévisualisation Markdown dans Cursor / VS Code, ou copier-coller sur [https://mermaid.live](https://mermaid.live) pour exporter en **PNG** / **SVG** / **PDF**.

---

## 1. Domaine (entités EF Core + énumérations)

Modèle persistant et relations configurées dans `AppDbContext`.

```mermaid
classDiagram
    direction TB

    class AssetType {
        <<enumeration>>
        Stock
        Crypto
    }

    class TransactionType {
        <<enumeration>>
        Buy
        Sell
    }

    class TrendSignal {
        <<enumeration>>
        Bearish
        Neutral
        Bullish
    }

    class Asset {
        +int Id
        +string Symbol
        +string Name
        +AssetType AssetType
        +decimal CurrentPrice
    }

    class Transaction {
        +int Id
        +int AssetId
        +TransactionType Type
        +decimal Quantity
        +decimal UnitPrice
        +DateTime Date
        +decimal TotalAmount
    }

    class PriceHistory {
        +int Id
        +int AssetId
        +DateTime Date
        +decimal ClosePrice
    }

    class PortfolioBudget {
        +int Id
        +decimal InitialBudget
        +decimal AvailableCash
    }

    class PortfolioValuePoint {
        +int Id
        +DateTime Date
        +decimal TotalValue
    }

    class AppDbContext {
        <<DbContext>>
        +DbSet~Asset~ Assets
        +DbSet~Transaction~ Transactions
        +DbSet~PortfolioBudget~ PortfolioBudgets
        +DbSet~PriceHistory~ PriceHistories
        +DbSet~PortfolioValuePoint~ PortfolioValuePoints
    }

    Asset "1" --> "*" Transaction : contient
    Asset "1" --> "*" PriceHistory : historise
    Transaction --> Asset : Asset
    PriceHistory --> Asset : Asset

    Asset ..> AssetType : utilise
    Transaction ..> TransactionType : utilise

    AppDbContext ..> Asset : mappe
    AppDbContext ..> Transaction : mappe
    AppDbContext ..> PriceHistory : mappe
    AppDbContext ..> PortfolioBudget : mappe
    AppDbContext ..> PortfolioValuePoint : mappe
```

---

## 2. Modèles de vue / DTO (couche Core.Models)

Données calculées ou transportées entre services et UI (non toutes persistées).

```mermaid
classDiagram
    direction TB

    class HoldingSummary {
        +int AssetId
        +AssetType AssetType
        +string Symbol
        +string Name
        +decimal Quantity
        +decimal AverageBuyPrice
        +decimal CostBasis
        +decimal CurrentPrice
        +decimal MarketValue
        +decimal GainLoss
        +decimal AllocationPercent
    }

    class PortfolioSnapshot {
        +decimal InitialBudget
        +decimal AvailableCash
        +decimal InvestedAmount
        +decimal PortfolioValue
        +decimal TotalValue
        +decimal GainLoss
        +decimal RoiPercent
    }

    class AssetAnalytics {
        +int AssetId
        +string Symbol
        +decimal CurrentPrice
        +decimal SimpleMovingAverage
        +decimal VolatilityPercent
        +TrendSignal Signal
        +decimal DistanceToSmaPercent
        +decimal SharpeLikeAnnualized
        +decimal MaxDrawdownPercent
    }

    class PortfolioAlert {
        +AlertSeverity Severity
        +string Title
        +string Message
    }

    class AlertSeverity {
        <<enumeration>>
        Info
        Warning
        Danger
    }

    class BacktestResult {
        +int AssetId
        +string Symbol
        +int SmaPeriod
        +decimal InitialCash
        +decimal FinalStrategyEquity
        +decimal FinalBuyHoldEquity
        +int TradeCount
        +string RuleDescription
    }

    class PagedResultOfT {
        <<generic T>>
        +Items
        +int TotalCount
        +int Page
        +int PageSize
        +int TotalPages
    }

    PortfolioSnapshot "1" *-- "*" HoldingSummary : Holdings
    PortfolioSnapshot ..> AssetType : AllocationByType

    PortfolioAlert ..> AlertSeverity
    AssetAnalytics ..> TrendSignal : Signal
    HoldingSummary ..> AssetType : AssetType
```

---

## 3. Services, interfaces et dépendances (architecture)

Les pages Blazor consomment les **interfaces** ; les implémentations utilisent `AppDbContext` (et parfois d’autres services).

```mermaid
classDiagram
    direction TB

    class AppDbContext {
        <<DbContext>>
    }

    class IAssetService {
        <<interface>>
        +GetAllAsync()
        +GetPagedAsync()
        +GetByIdAsync()
        +CreateAsync()
        +UpdateAsync()
        +DeleteAsync()
    }

    class ITransactionService {
        <<interface>>
        +GetAllAsync()
        +GetPagedAsync()
        +CreateAsync()
        +DeleteAsync()
    }

    class IPortfolioService {
        <<interface>>
        +GetSnapshotAsync()
        +GetBudgetAsync()
        +UpdateBudgetAsync()
        +RecordDailyPortfolioValueAsync()
        +GetPortfolioValueHistoryAsync()
    }

    class IAnalyticsService {
        <<interface>>
        +GetAssetAnalyticsAsync()
        +EnsurePriceHistorySeedAsync()
        +RunSmaTrendBacktestAsync()
    }

    class IPortfolioAlertService {
        <<interface>>
        +GetAlertsAsync()
    }

    class IPriceDataService {
        <<interface>>
        +RefreshMarketPricesAsync()
        +ImportPricesFromCsvAsync()
    }

    class AssetService {
    }

    class TransactionService {
    }

    class PortfolioService {
    }

    class AnalyticsService {
    }

    class PortfolioAlertService {
    }

    class PriceDataService {
    }

    class PositionCostHelper {
        <<static>>
        +FromTransactions()
    }

    class DbInitializer {
        <<static>>
        +SeedAsync()
    }

    AssetService ..|> IAssetService
    TransactionService ..|> ITransactionService
    PortfolioService ..|> IPortfolioService
    AnalyticsService ..|> IAnalyticsService
    PortfolioAlertService ..|> IPortfolioAlertService
    PriceDataService ..|> IPriceDataService

    AssetService --> AppDbContext
    TransactionService --> AppDbContext
    TransactionService --> IPortfolioService
    PortfolioService --> AppDbContext
    AnalyticsService --> AppDbContext
    PriceDataService --> AppDbContext
    PortfolioAlertService --> IPortfolioService
    PortfolioAlertService --> IAnalyticsService

    PortfolioService ..> PositionCostHelper : utilise
```

> **Note** : `PriceDataService` utilise aussi `HttpClient`, `IMemoryCache` et `IOptions~PriceDataOptions~` (non représentés ici pour garder le diagramme lisible).  
> `PortfolioAlertService` utilise `IOptions~AlertOptions~`.

---

## 4. Vue synthétique des couches (paquetages)

```mermaid
flowchart LR
    subgraph UI["GestionPortefeuille.Web (Blazor)"]
        Pages[Pages / Composants]
    end

    subgraph SVC["GestionPortefeuille.Services"]
        Impl[Services]
    end

    subgraph CORE["GestionPortefeuille.Core"]
        Ent[Entities / Models / Interfaces / Enums]
    end

    subgraph INF["GestionPortefeuille.Infrastructure"]
        DB[(AppDbContext\nSQLite)]
    end

    Pages --> Ent
    Pages --> Impl
    Impl --> Ent
    Impl --> DB
    DB --> Ent
```

---

## Export pour le rapport PDF

1. Ouvrir [mermaid.live](https://mermaid.live).  
2. Coller l’un des blocs `mermaid` (sans les balises \`\`\`).  
3. **Actions → PNG / SVG** pour insérer dans Word / LaTeX / PowerPoint.

Pour un diagramme **UML officiel** (fichier `.uml` / PlantUML), indique-le et on pourra générer la variante PlantUML équivalente.
