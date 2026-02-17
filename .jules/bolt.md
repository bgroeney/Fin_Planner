# Bolt's Journal

## 2025-02-18 - Missing Test Infrastructure
**Learning:** The project lacks a dedicated test project/infrastructure for the backend (`Mineplex.FinPlanner.Api`). This makes verification of performance optimizations risky and reliant on static analysis and compilation checks.
**Action:** When working on backend optimizations, prioritize changes that are verifiable via strict type checking and clear logical equivalence. Recommend adding a test project in future.
## 2025-05-22 - [Read-Only Queries Tracking]
**Learning:** The codebase defaults to tracking queries (`AsNoTracking` is missing) even in read-only API controllers like `HoldingsController` and services like `PortfolioService`. This causes unnecessary overhead for purely display-oriented endpoints.
**Action:** Default to `.AsNoTracking()` for all GET requests that project to DTOs. Check `PortfolioService` for future optimizations as it loads heavy object graphs with tracking.
## 2026-01-26 - Critical N+1 in Performance Service
**Learning:** `PerformanceService.RebuildHistoryAsync` performs a query for every asset for every day in the history loop. This is a massive N+1 bottleneck.
**Action:** Refactor `RebuildHistoryAsync` to batch-fetch `HistoricalPrices` for the entire date range into a memory lookup before the loop.
