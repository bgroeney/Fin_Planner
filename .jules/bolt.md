# Bolt's Journal

## 2025-02-18 - Missing Test Infrastructure
**Learning:** The project lacks a dedicated test project/infrastructure for the backend (`Mineplex.FinPlanner.Api`). This makes verification of performance optimizations risky and reliant on static analysis and compilation checks.
**Action:** When working on backend optimizations, prioritize changes that are verifiable via strict type checking and clear logical equivalence. Recommend adding a test project in future.
