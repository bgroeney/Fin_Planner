# Bolt's Journal

## 2025-02-18 - Missing Test Infrastructure
**Learning:** The project lacks a dedicated test project/infrastructure for the backend (`Mineplex.FinPlanner.Api`). This makes verification of performance optimizations risky and reliant on static analysis and compilation checks.
**Action:** When working on backend optimizations, prioritize changes that are verifiable via strict type checking and clear logical equivalence. Recommend adding a test project in future.
## 2026-01-26 - Critical N+1 in Performance Service
**Learning:** `PerformanceService.RebuildHistoryAsync` performs a query for every asset for every day in the history loop. This is a massive N+1 bottleneck.
**Action:** Refactor `RebuildHistoryAsync` to batch-fetch `HistoricalPrices` for the entire date range into a memory lookup before the loop.
