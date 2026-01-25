# Commercial Property Module Design

## Overview
This module expands the FinPlanner into a specialized "Property Investment ERP." It provides a dedicated workspace for managing physical real estate assets, separate from the liquid portfolio, with a focus on yield, occupancy, and acquisition analysis.

## 1. Data Architecture (Specialized Module)

### Core Entities
- **`CommercialProperty`**: Independent entity from the main `Assets` table.
    - Fields: `Id`, `OwnerId`, `Address`, `TitleReference`, `TotalGFA`, `PurchasePrice`, `BuildingType` (Retail, Office, Industrial).
- **`PropertyValuation`**: History of property appraisals.
    - Fields: `PropertyId`, `Date`, `Value`, `Source` (Agent, External, Internal).
- **`PropertyLedger`**: Isolated transaction ledger for granular cashflow.
    - Types: `Rent_Gross`, `Outgoing_Recoverable`, `Council_Rates`, `Water_Rates`, `Insurance`, `Management_Fee`, `Repairs_CapEx`.
- **`LeaseProfile`**: Supports forecasting and vacancy tracking.
    - Fields: `PropertyId`, `TenantName`, `LeaseStart`, `LeaseEnd`, `OptionPeriod` (e.g., 5+5), `CurrentRent`, `ReviewType` (Fixed, CPI, Market).

### Portfolio Integration
- **Virtual Asset Link**: A record in the main `Assets` table that acts as a "Proxy" for the property.
- **Auto-Sync**: Background job updates the Proxy Asset's `CurrentPrice` periodically based on the latest `PropertyValuation` and records distribution income from the `PropertyLedger`.

## 2. Management UI & Dashboard

### The "Property Hub"
A high-density dashboard for active holdings using a Bento Grid layout:
- **Net Yield Gauge**: Visualizes (Rent - Expenses) / Current Value.
- **Vacancy Heatmap**: A 24-month rolling bar chart showing lease expiry "cliffs."
- **Cashflow Forecast**: 12-month projected ledger based on `LeaseProfile` vs `BudgetedExpenses`.

### Document Center (OCR Pipeline)
- **Inbox Interface**: Drag-and-drop zone for property manager statements and tax invoices.
- **OCR Engine**: AI-assisted extraction of Date, Amount, and Category (e.g., identifies "Water Rates" and pre-populates the ledger).
- **Audit Link**: Every transaction in the `PropertyLedger` links back to its source PDF.

## 3. Acquisition Scenario Planner

A dedicated "Sandbox" for evaluating new potential purchases.

### Spreadsheet Workbench
- **Input Grid**: Fully editable rows for `PurchasePrice`, `StampDuty`, `CapEx_Reserve`, `MarketRent`.
- **Valuation Logic**: Live calculation of Cap Rate, Intrinsic Value, and Weighted Average Lease Expiry (WALE).

### Monte Carlo Uncertainty Visuals
Quantifies risk through visual probability curves:
- **Dynamic Variable Cell**: Users input distributions (e.g., Interest Rate: 6.5% ± 1%).
- **Probability Curve**: Shows 5,000-run simulation of Net Wealth impact.
- **"Buy/Pass" Decision Matrix**:
    - **BUY**: Links scenario data to a new `CommercialProperty`.
    - **PASS**: Records as a historic look-back.
    - **UNECONOMIC**: Auto-flagged if intrinsic value < asking price by >10%.

## 4. Debt & Tax Logic

### Leverage Tracker
- **Mortgage Linking**: Track loan balances, interest rates (Fixed/Variable), and expiry dates.
- **LVR & ICR Monitoring**: Real-time calculation of Loan-to-Value and Interest Coverage Ratios.

### Depreciation & After-Tax View
- **Div 43 / Div 40 Schedules**: Manual input table for annual depreciation figures.
- **Post-Tax Performance**: Dashboard toggle to view performance after including non-cash tax shields and personal marginal rates.

## Success Criteria
- [ ] Property valuation updates are reflected in total portfolio net worth.
- [ ] Users can run a Monte Carlo simulation on a new prospect in < 30 seconds.
- [ ] The "Property Hub" sidebar creates a clear workspace context switch.
