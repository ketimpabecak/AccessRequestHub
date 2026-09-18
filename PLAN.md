# PLAN.md

## 1. Problem Understanding
Membangun MVP "Access Request Hub" sebagai single source of truth untuk permintaan akses aplikasi internal. Fokus utama adalah mencegah duplicate requests (idempotency), mencegah race conditions saat approval (optimistic concurrency), dan memastikan audit trail yang lengkap.

## 2. Architecture & Data Model
- **Backend**: .NET 8 Web API dengan Controller-based architecture.
- **Database**: SQL Server (LocalDB) menggunakan Entity Framework Core Code-First.
- **Frontend**: (Akan diimplementasikan di Phase berikutnya, kemungkinan React/TypeScript atau Blazor).
- **Data Model**: 
  - `User`: Menyimpan role dan self-referencing ManagerId.
  - `Application`: Terkait dengan SystemOwner.
  - `AccessRequest`: Inti bisnis, memiliki `ClientRequestId` (unique index untuk idempotency) dan `RowVersion` (timestamp untuk optimistic concurrency).
  - `AuditEvent`: Append-only log untuk setiap transisi state.

## 3. Implementation Order
1. ?Setup Project, Git, dan EF Core Schema (Phase 1).
2. ? Backend Business Logic: Services untuk Idempotency, State Machine, dan Concurrency (Phase 2).
3. ? API Endpoints & Server-side Authorization (Phase 2).
4. ? Frontend Implementation (User switcher, Forms, Inbox).
5. ? Automated Testing (xUnit).
6. ? Documentation (AI_USAGE.md, REVIEW.md, INTEGRITY.md, README.md).

## 4. Test Strategy
- Unit tests untuk business logic (state transitions, high-risk detection).
- Integration tests untuk idempotency (mengirim payload sama dua kali).
- Concurrency tests untuk memastikan optimistic concurrency (`RowVersion`) menolak update stale.

## 5. Trade-offs & Changes
- **Trade-off 1**: Menggunakan `DeleteBehavior.Restrict` pada foreign keys alih-alih `Cascade` untuk mencegah "multiple cascade paths" di SQL Server dan menjaga integritas data audit historis.
- **Trade-off 2**: Autentikasi disimulasikan via header/switcher sesuai instruksi assessment, namun authorization tetap ditegakkan ketat di backend.
- **Perubahan Plan**: Awalnya mencoba relasi bidirectional antara User dan Application, tetapi diubah menjadi unidirectional untuk menghindari kebingungan mapping di EF Core.