## 2026-05-22 - Missing Backend Tests & N+1 Patterns
**Learning:** The backend has no test project configured. Logic verification must be done via strict code review and manual reasoning. Also, heavily nested loops with DB calls were found in core services.
**Action:** Always verify compilation with `dotnet build`. Be extra careful with refactoring logic as there are no safety nets.
