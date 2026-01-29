# Bolt's Journal

## 2024-05-23 - N+1 Query Pattern in Daily Processing Loops
**Learning:** The codebase performs date-based simulations (like rebuilding history) by iterating through days and querying the database inside the loop (e.g., for prices). This causes severe N+1 performance degradation.
**Action:** Always pre-fetch historical data for the entire date range into in-memory dictionaries (e.g., `Dictionary<Guid, List<HistoricalPrice>>`) before entering date-based loops.
