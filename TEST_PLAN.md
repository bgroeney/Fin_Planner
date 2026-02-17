# FinPlanner Comprehensive Test Plan

## 1. Overview
This document outlines the testing strategy for the FinPlanner application, focusing on high-value, high-risk areas critical for financial decision-making.

## 2. Testing Strategy
We will adopt a risk-based testing approach, prioritizing core financial calculation engines and complex business logic.

### 2.1 Backend (C# / .NET)
Framework: **xUnit**
Mocking: **Moq**
Assertions: **FluentAssertions**

**High-Priority Test targets:**
1.  **Tax Optimization Service (`TaxOptimizationService`)**
    *   **Risk**: Incorrect tax calculations lead to direct financial loss/misinformation.
    *   **Tests**:
        *   FIFO parcel allocation logic.
        *   MinTax strategy (minimizing immediate tax liability).
        *   MaxGain strategy (maximizing realized gains).
        *   CGT Discount application (12-month rule).
2.  **Tax Distribution Service (`TaxDistributionService`)**
    *   **Risk**: Suboptimal distribution recommendations for Family Trusts.
    *   **Tests**:
        *   Marginal tax rate calculations.
        *   Streaming of franking credits.
        *   Distribution optimization across beneficiaries.
3.  **Monte Carlo Service (`MonteCarloService`)**
    *   **Risk**: Misleading long-term wealth projections.
    *   **Tests**:
        *   Statistical validity (mean/median convergence).
        *   Drawdown logic (Retirement phase).
        *   Inflation adjustment application.
4.  **Rebalancing Service (`RebalancingService`)**
    *   **Risk**: Incorrect buy/sell recommendations drifting portfolio from targets.
    *   **Tests**:
        *   Drift calculation.
        *   Trade generation to restore target allocation.

### 2.2 Frontend (Vue 3)
Framework: **Vitest**
Utils: **Vue Test Utils**

**High-Priority Test targets:**
1.  **Property Deal Workbench (`DealWorkbench.vue`)**
    *   Complex local state and calculations.
    *   Validation of input parameters.
2.  **Distribution Logic**
    *   Visual feedback on distribution changes.

## 3. Implementation Plan

### Phase 1: Backend Infrastructure & Tax Logic (Immediate)
- [ ] Create `Mineplex.FinPlanner.Tests` project.
- [ ] Configure Test Dependencies.
- [ ] Implement `TaxOptimizationServiceTests`.
- [ ] Implement `MonteCarloServiceTests`.

### Phase 2: Distribution & Rebalancing
- [ ] Implement `TaxDistributionServiceTests`.
- [ ] Implement `RebalancingServiceTests`.

### Phase 3: Frontend Critical Paths
- [ ] Configure `vitest` command.
- [ ] Add unit tests for utility functions (financial calcs).
