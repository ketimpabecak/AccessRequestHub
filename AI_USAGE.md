# AI Usage Report
AI-assisted tools were used extensively for planning, architecture, code generation, debugging, and testing. Below are the most impactful interactions.

## 1. Architectural Planning & Domain Modeling
- **Ask:** "How to model an access request system with idempotency and optimistic concurrency in .NET 8?"
- **Suggestion:** AI suggested using a `ClientRequestId` with a unique database constraint for idempotency, and a `byte[] RowVersion` with the `[Timestamp]` attribute for concurrency.
- **Action:** Accepted. This formed the core of `AccessRequest.cs`.

## 2. Debugging Test Isolation (xUnit + SQLite)
- **Ask:** "Why are my xUnit tests failing with 'UNIQUE constraint failed: Users.Id' when using SQLite :memory:?"
- **Suggestion:** AI initially suggested keeping a single `SqliteConnection` open. However, this still caused race conditions in xUnit's parallel execution.
- **Action:** **Rejected/Changed.** I implemented a more robust solution: generating a unique temporary file (`Path.GetTempFileName()`) for each test instance and deleting it in `Dispose()`, guaranteeing 100% test isolation.

## 3. Three Things AI Got Wrong (And How I Fixed Them)
1. **Cross-Database Constraint Handling:** AI initially wrote the idempotency catch block to only check for SQL Server error codes (2601/2627). When running xUnit tests with SQLite, it threw error code 19, causing the test to crash. I fixed this by adding a specific check for `Microsoft.Data.Sqlite.SqliteException` with error code 19.
2. **Test Logic for Concurrency:** AI's initial concurrency test approved the request *first*, then tried to approve it again with a stale token. This triggered the "terminal state" validation before the concurrency check could even run. I corrected the test logic to simulate a stale `RowVersion` update *while the request was still in a pending state*, accurately proving that the `DbUpdateConcurrencyException` is thrown as intended.
3. **SQLite & [Timestamp] Attribute:** AI initially used the `[Timestamp]` attribute for the `RowVersion` property. Whil