## 2025-05-22 - [Read-Only Queries Tracking]
**Learning:** The codebase defaults to tracking queries (`AsNoTracking` is missing) even in read-only API controllers like `HoldingsController` and services like `PortfolioService`. This causes unnecessary overhead for purely display-oriented endpoints.
**Action:** Default to `.AsNoTracking()` for all GET requests that project to DTOs. Check `PortfolioService` for future optimizations as it loads heavy object graphs with tracking.
