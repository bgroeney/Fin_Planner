# BOLT'S JOURNAL - CRITICAL LEARNINGS ONLY

## 2024-12-16 - PerformanceService N+1 Contradiction
**Learning:** The memory states that services like `PerformanceService` use pre-fetching to avoid N+1 queries, but the actual code in `PerformanceService.RecalculateHistoryAsync` performs a classic N+1 query inside a date loop (`_context.HistoricalPrices.FirstOrDefaultAsync` inside a `for` loop). The code even has a comment acknowledging this ("// NOTE: This could be slow if doing N+1 queries").
**Action:** Do not trust architectural claims in documentation/memory blindly; verify against the code. Future optimization should prioritize fixing `PerformanceService` by implementing the pre-fetching strategy described in the comments.
