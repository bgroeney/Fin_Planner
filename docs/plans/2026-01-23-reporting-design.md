# Design: Reporting & Tax Optimization Requirements

Based on our brainstorming, here is the proposed design for the comprehensive reporting suite.

## Core Philosophy
"Empower decisions with data." 
Reports aren't just static PDFs; they are interactive tools that help the user minimize tax and maximize returns *before* they act, while providing audit-grade records for the past.

## 1. Reporting Dashboard (New View)

The `/reports` page will be transformed from a placeholder into a command center with three distinct sections:

### A. Performance Report Generator
**Goal:** Visualize wealth creation over time.
- **Controls:** Date Range Picker (Presets: Last Quarter, YTD, 1 Year, 3 Years, 5 Years, Since Inception), Portfolio Selector.
- **Visuals:**
    - **Growth Chart:** Line chart showing Portfolio Value vs Net Invested Capital (shows true profit).
    - **Benchmark Comparison:** Overlay the Portfolio's benchmark index performance.
    - **Returns Table:** Standardized annualized return columns (1yr, 3yr, 5yr) vs Benchmark.

### B. Tax Impact Center
**Goal:** Real-time visibility into tax liabilities.
- **Fiscal Year Selector:** Defaults to current FY.
- **Summary Cards:**
    - **Realized Gains:** Short-term vs Long-term (discounted).
    - **Estimated Tax:** Based on a configurable marginal tax rate setting.
    - **Income:** Dividends + Franking Credits.
- **Actionable Insight:** "Tax Loss Harvesting Opportunities" – highlights current holdings in a loss position that could offset gains.

### C. Transaction Ledger
**Goal:** Audit trail.
- **Table:** Searchable, filterable list of all events (Buy, Sell, Dividend, Return of Capital).
- **Export:** CSV/PDF export buttons for accountant delivery.

---

## 2. Tax Optimization Engine (New Logic)

This is the "intelligence" layer that powers both the reports and the decision suggestions.

### A. Parcel Tracking System
We need to track every "buy" as a distinct tax parcel.
- **Model Update:** Add `ParcelId` to `Holdings` or a separate `TaxParcels` table.
- **Attributes:** Acquisition Date, Cost Base, Units, Current Market Value.

### B. Allocation Methods (Per Decision)
When an *AI Recommendation* or *Manual Decision* suggests selling, the user sees a "Tax Optimizer" toggle:
- **Methods:**
    - **Minimize Tax (Default):** AI selects parcels with highest cost base or >12mo holding period (50% discount).
    - **Maximize Gain:** AI selects parcels with lowest cost base (rare, but useful for low-income years).
    - **FIFO:** Strict First-In-First-Out.
    - **Specific ID:** User manually checks boxes next to specific parcels to sell.

### C. Decision Integration
- The `CreateDecision` flow will be updated to include a "Projected Tax Impact" calculation based on the selected method.
- The AI Rationale will explicitly state: *"Selling this parcel saves $X in tax compared to a FIFO approach."*

---

## 3. Historical Data & Snapshots

### A. Backfilling History
- Since valid historical data "extends back to inception", we will treat the Import process as the source of truth.
- **Action:** Create a `RebuildPerformanceHistory` job that runs after every import. It walks through all transactions chronologically to generate/update daily `PerformanceSnapshot` records.

### B. Snapshotting
- **Trigger:** Daily midnight job + On-Demand (post-import).
- **Storage:** Normalized `PerformanceSnapshot` table (already exists) will be the primary source for fast chart rendering.

---

## Open Question
Does this design cover the "Tax Allocation Method" requirement sufficiently? Specifically, allowing the choice *at the moment of decision* (with a default preference) rather than a global setting?
