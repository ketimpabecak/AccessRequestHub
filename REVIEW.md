# Production Readiness Review

## Findings & Limitations
Finding 1
Finding: Authentication is simulated via HTTP Header (X-Current-User-Id).
Severity: Medium
Action: Documented as Phase 1 scope limitation. Real OIDC/OAuth to be added in Phase 2.
Status: Deferred
Evidence: README.md, CurrentUserService.cs

Finding 2
Finding: No frontend UI built yet (API only).
Severity: Low
Action: Phase 1 scope focused on backend correctness, API contracts, and automated tests. Frontend is deferred.
Status: Deferred
Evidence: PLAN.md

Finding 3
Finding: SQLite used for testing, SQL Server for production.
Severity: Low
Action: Intentional architectural choice for fast, isolated CI/CD testing. Handled via EF Core providers.
Status: Resolved

Evidence: AccessRequestServiceTests.cs

## Known Limitations
1. **Frontend Execution:** The critical flow (Create -> Approve/Reject -> Audit) is currently best demonstrated via **Swagger UI** or Postman, as the Blazor frontend HTTP client integration is incomplete due to timebox constraints. The UI components (User Switcher, Forms) are built but not fully wired to the backend.
2. **No Real Authentication:** Phase 1 scope allows simulated auth. Real OIDC/OAuth is deferred.

## Deferred Work
1. **Frontend Completion:** Finalize Blazor `HttpClient` configuration, wire up the "Create Request" form, and build the "My Requests" and "Approval Inbox" views with proper loading/error/conflict state handling.
2. **Real Authentication:** Replace the `X-Current-User-Id` header simulation with a proper JWT/OIDC provider.