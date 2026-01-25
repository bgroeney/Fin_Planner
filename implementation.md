# Financial Planner Application - Implementation Plan

A web-based financial planning application for portfolio tracking, performance analysis, AI-powered recommendations, and decision journaling.

**Stack:** C# ASP.NET Core (.NET 10) | Vue 3 + Vite | PostgreSQL | GCP Hosting | Gemini AI

---

## Architecture Overview

```mermaid
graph TB
    subgraph Frontend["Frontend (Vue 3 + Vite)"]
        UI[Vue Components]
        Store[Pinia Store]
        Charts[ApexCharts]
        API[Axios API Client]
    end
    
    subgraph Backend["Backend (ASP.NET Core .NET 10)"]
        Controllers[API Controllers]
        Scalar[Scalar API Docs]
        Services[Business Services]
        AI[Gemini AI Service]
        Jobs[Background Jobs]
    end
    
    subgraph GCP["GCP Infrastructure"]
        CloudRun[Cloud Run]
        CloudSQL[(Cloud SQL PostgreSQL)]
        Storage[Cloud Storage]
    end
    
    subgraph External["External Services"]
        Yahoo[Yahoo Finance API]
        Gemini[Gemini API]
    end
    
    UI --> Store --> API --> Controllers
    Controllers --> Services --> CloudSQL
    AI --> Gemini
    Jobs --> Yahoo
```

---

## Domain Model

Updated hierarchy: **Owner → Portfolio → Account → Asset**

```mermaid
erDiagram
    User ||--o{ Portfolio : owns
    Portfolio ||--o{ Account : contains
    Account ||--o{ Holding : contains
    Holding }|--|| Asset : references
    Asset }o--o| AssetCategory : "user-assigned"
    Account ||--o{ Transaction : has
    Portfolio ||--o{ Decision : tracks
    Decision }o--o| AIRecommendation : generated_by
    Portfolio ||--o{ PerformanceSnapshot : records
    
    User {
        uuid id PK
        string email
        string password_hash
        string role "Admin|Editor"
        datetime created_at
    }
    
    Portfolio {
        uuid id PK
        uuid owner_id FK
        string name
        json target_allocation
    }
    
    Account {
        uuid id PK
        uuid portfolio_id FK
        string account_number "e.g. WRAP075815"
        string account_name
        string provider "e.g. Netwealth"
        string product_type "e.g. Wrap Services"
    }
    
    AssetCategory {
        uuid id PK
        uuid portfolio_id FK
        string name
        string code
        decimal target_percentage
        int display_order
    }
    
    Asset {
        uuid id PK
        string symbol "e.g. VHY, SPC5039AU"
        string name
        string asset_type "ETF|ManagedFund|TD|Cash"
    }
    
    Holding {
        uuid id PK
        uuid account_id FK
        uuid asset_id FK
        uuid category_id FK "user-assigned"
        decimal units "cached - recalc on txn change"
        decimal avg_cost "cached - recalc on txn change"
        decimal current_value "calculated: units × price"
    }
    
    Transaction {
        uuid id PK
        uuid account_id FK
        uuid asset_id FK
        string type "Purchase|Sale|Distribution|Fee"
        decimal units
        decimal amount
        date effective_date
        string narration
    }
    
    Decision {
        uuid id PK
        uuid portfolio_id FK
        string type "AI|Manual"
        string status "Pending|Approved|Rejected"
        text rationale
        json snapshot_before
        json snapshot_after
    }
    
    AIRecommendation {
        uuid id PK
        uuid asset_id FK
        string action "Buy|Sell|Hold"
        text summary
        text analysis
        decimal confidence
    }
```

---

## Data Caching Strategy

Holding values use a **hybrid approach** for optimal performance:

| Field | Strategy | Trigger |
|-------|----------|---------|
| `units` | Cached | Recalculated on transaction insert/update/delete |
| `avg_cost` | Cached | Recalculated on transaction change (weighted average of purchases) |
| `current_value` | Calculated on-demand | `units × current_price` from market data |

This avoids repeatedly summing thousands of transactions on each read while keeping values accurate.

---

## Proposed Changes

### Backend - Controllers

| Controller | Endpoints |
|------------|-----------|
| `AuthController` | Login, Register, Me |
| `PortfoliosController` | CRUD, Performance, Allocation, Rebalance |
| `AccountsController` | CRUD per portfolio |
| `HoldingsController` | CRUD per account, Assign category |
| `DecisionsController` | History, Log manual, Approve/Reject AI |
| `AIController` | Analyze portfolio, Analyze holding, Get recommendations |
| `ImportController` | Upload CSV, Preview, Confirm import |
| `ReportsController` | EOFY, Performance, Tax summary |
| `CategoriesController` | CRUD user-defined categories |

---

### Backend - Services

| Service | Responsibility |
|---------|----------------|
| `GeminiAIService` | Integration with Google Gemini API for recommendations |
| `MarketDataService` | Yahoo Finance API for prices (non-realtime) |
| `NetwealthImportService` | Parse Netwealth PortfolioValuation & TransactionListing CSVs |
| `DecisionTrackingService` | Log decisions, track outcomes |
| `ReportService` | Australian FY calculations (July-June), CGT, franking credits |
| `StressTestingService` | [Phase 2] Monte Carlo, scenario analysis skeleton |

---

### Netwealth CSV Import

Supports two file types from Sample_Data:

**PortfolioValuation** columns:
- Asset, Code, Current units, Avg cost, Price, Value $, Value %, Asset class

**TransactionListing** columns:
- Effective Date, Description, Asset, Code, Units, Debits, Credits, Purchase/Sale price

Import workflow:
1. Upload file → detect file type from headers
2. Parse account metadata (account number, name, provider)
3. Preview holdings/transactions with category assignment UI
4. User assigns categories to assets (persisted for future imports)
5. Confirm → create/update Account, Holdings, Transactions

---

### User-Defined Asset Categories

Users can create custom categories per portfolio. Default templates provided:

| Default Category | Code |
|------------------|------|
| Australian Equities - Large Cap | AU_LARGE |
| Australian Equities - Mid Cap | AU_MID |
| Australian Equities - Small Cap | AU_SMALL |
| Australian Property | AU_PROP |
| International - Large Cap | INT_LARGE |
| International - Mid Cap | INT_MID |
| International - Small Cap | INT_SMALL |
| International - Emerging | INT_EMRG |
| International - Property | INT_PROP |
| International - Commodities | INT_COMM |
| Fixed Interest | FIXED |
| Cash | CASH |

---

### Frontend - Views

| View | Features |
|------|----------|
| `DashboardView` | Portfolio cards, allocation chart (ApexCharts), pending recommendations |
| `PortfolioView` | Accounts list, combined holdings, performance chart |
| `AccountView` | Holdings table, transactions, import history |
| `HoldingDetailView` | Transaction history, AI analysis button, category assignment |
| `DecisionsView` | Timeline, filter by type/status, outcome tracking |
| `ImportView` | Upload wizard, column preview, category mapping |
| `ReportsView` | FY selector, report types, export options |
| `CategoriesView` | Manage custom categories, set targets |
| `SettingsView` | User profile, permissions (Admin only) |

---

### AI Integration - Gemini

```python
# Prompt structure for holding analysis
System: You are a professional investment analyst providing recommendations 
for an Australian investor. Consider CGT implications, franking credits, 
and local market conditions.

User: Analyze this holding:
- Asset: {symbol} ({name})
- Category: {user_category}
- Current allocation: {percentage}%
- Target: {target}%
- Cost base: ${cost_base}
- Current value: ${current_value}
- Holding period: {days} days
- Recent performance: {performance}%

Provide:
1. Action: BUY, SELL, or HOLD
2. Summary (2-3 sentences)
3. Detailed analysis (valuation, market, risk, tax)
4. Confidence (0-100)
```

---

### Phase 2 Skeleton - Stress Testing

Included in Phase 1 codebase as extension points:

| Component | Description |
|-----------|-------------|
| `IStressTestEngine` | Interface for simulation engines |
| `MonteCarloService` | Stub for Monte Carlo simulation |
| `ScenarioService` | Historical scenario modeling (GFC, COVID) |
| `StressTestController` | API endpoints (disabled/preview) |
| `StressTestView.vue` | UI placeholder with "Coming Soon" |

---

## Verification Plan

### Automated Tests
```bash
# Backend
cd Mineplex.FinPlanner.Api && dotnet test

# Frontend
cd fin-planner-ui && npm run test:unit
```

### Manual Verification
1. **Import Flow**: Upload sample Netwealth CSVs, verify parsing
2. **Category Assignment**: Assign custom categories, verify persistence
3. **AI Analysis**: Generate recommendation, approve/reject
4. **EOFY Report**: Generate FY 2024-25, verify July-June range
5. **Multi-account Portfolio**: Create portfolio with 2 accounts

---

## Implementation Timeline

```mermaid
gantt
    title Phase 1 Implementation
    dateFormat YYYY-MM-DD
    section Backend
    Project Setup & Auth      :a1, 2026-01-13, 4d
    Portfolio & Accounts      :a2, after a1, 3d
    Holdings & Transactions   :a3, after a2, 3d
    Netwealth Import          :a4, after a3, 3d
    Categories (User-defined) :a5, after a4, 2d
    Gemini AI Integration     :a6, after a5, 4d
    Reporting                 :a7, after a6, 3d
    Phase 2 Skeleton          :a8, after a7, 2d
    
    section Frontend
    Project Setup             :b1, 2026-01-13, 3d
    Dashboard & Portfolio     :b2, after a2, 3d
    Account & Holdings        :b3, after a3, 3d
    Import Wizard             :b4, after a4, 3d
    AI Integration UI         :b5, after a6, 2d
    Reports & Categories      :b6, after a7, 3d
```
