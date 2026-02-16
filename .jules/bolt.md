## 2026-01-26 - Critical N+1 in Performance Service
**Learning:** `PerformanceService.RebuildHistoryAsync` performs a query for every asset for every day in the history loop. This is a massive N+1 bottleneck.
**Action:** Refactor `RebuildHistoryAsync` to batch-fetch `HistoricalPrices` for the entire date range into a memory lookup before the loop.
