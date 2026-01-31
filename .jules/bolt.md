## 2026-02-01 - N+1 Query in Date-based Simulation
**Learning:** When simulating history day-by-day (e.g., performance calculation), fetching data inside the loop (even with `FirstOrDefault` or index-based lookups) creates a massive N+1 bottleneck (Dates * Assets).
**Action:** Use an "Event Sourcing" in-memory approach:
1. Fetch all events (prices, transactions) for the entire date range in bulk.
2. Group them by Date.
3. Iterate through dates, applying state changes (price updates, unit changes) incrementally.
This reduces database load from O(D*A) queries to O(1) bulk queries and moves processing to O(D) in-memory operations.
