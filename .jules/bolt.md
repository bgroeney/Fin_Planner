## 2026-01-28 - [N+1 in Date-based Loops]
**Learning:** `PerformanceService.RebuildHistoryAsync` was iterating day-by-day (365+ days) and querying `HistoricalPrices` inside the loop. This caused massive N+1 issues (18k+ queries). Even with EF Core, simple `Where` in loops is deadly.
**Action:** Always pre-fetch time-series data into memory (`Dictionary<Guid, List<T>>`) when processing history. Watch out for other "Rebuild" or "Simulation" services (like `MonteCarloService` or `StressTestController`) which might share this pattern.
