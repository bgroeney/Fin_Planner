## 2026-01-26 - [PerformanceService N+1 Optimization]
**Learning:** `RebuildHistoryAsync` was performing N+1 queries by fetching prices inside a daily loop. EF Core `AsNoTracking` was also missing on read-only queries.
**Action:** Always check `for` loops in Service methods for database calls. Use bulk fetching (`Where(x => ids.Contains(x.Id))`) and in-memory dictionaries/cursors for time-series data processing.
