## 2025-02-04 - Over-fetching in RecalculateHoldingsAsync
**Learning:** Found an `Include(t => t.FileUpload)` that was only used for filtering in the `Where` clause. EF Core translates navigation properties in `Where` to JOINs automatically, so `Include` was fetching unnecessary `FileUpload` data (columns + object tracking) for every transaction.
**Action:** When filtering by a related entity's property, verify if the related entity is actually accessed in the result processing. If not, remove `Include`. Use `AsNoTracking` for read-only calculation logic.
