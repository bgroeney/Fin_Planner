## 2026-01-31 - PerformanceService N+1 Queries
**Learning:** The codebase contains explicit "Optimization" comments marking areas where performance was deferred (e.g., `PerformanceService.RebuildHistoryAsync`). These are low-hanging fruit for significant gains.
**Action:** Search for "Optimization" or "TODO" comments in Service classes to find pre-identified bottlenecks.
