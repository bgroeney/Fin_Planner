## 2026-01-30 - N+1 in Date-Based Loops
**Learning:** Services iterating over date ranges (like `PerformanceService`) tend to query database per day, causing massive N+1 issues.
**Action:** Always check date loops in services and refactor to bulk-fetch data into dictionaries before the loop.
