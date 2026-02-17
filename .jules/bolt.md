# BOLT'S JOURNAL - PERFORMANCE OPTIMIZATIONS

## 2026-02-07 - PerformanceService Batching Strategy
**Learning:** `PerformanceService.RebuildHistoryAsync` was performing N+1 queries by fetching `HistoricalPrice` inside a daily loop for each asset. Optimizing this required fetching all history for the relevant assets in bulk and using in-memory cursors to efficiently find the correct price for each day while respecting the "carry forward" logic for missing data (e.g., weekends).
**Action:** Always batch fetch historical data when iterating through time series. Use sorted lists and cursors instead of repeated lookups. Ensure fallback logic (checking `CurrentPrice` if recent history is missing) is preserved correctly when moving to batch processing.
## 2026-01-26 - Critical N+1 in Performance Service
**Learning:** `PerformanceService.RebuildHistoryAsync` performs a query for every asset for every day in the history loop. This is a massive N+1 bottleneck.
**Action:** Refactor `RebuildHistoryAsync` to batch-fetch `HistoricalPrices` for the entire date range into a memory lookup before the loop.
