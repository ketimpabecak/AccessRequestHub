# Access Request Hub (Phase 1)

MVP Access Request Hub sebagai single source of truth untuk permintaan akses aplikasi internal, fokus pada idempotency, optimistic concurrency, dan audit trail yang lengkap.

## Setup & Run

1. Pastikan **.NET 8 SDK** dan **SQL Server LocalDB** (atau SQL Server instance) sudah terinstall.
2. Clone repository ini.
3. Buka terminal di root folder, jalankan:
   ```bash
   dotnet restore
   dotnet ef database update --project AccessRequestHub.Api
   dotnet run --project AccessRequestHub.Api
4. Aplikasi akan berjalan di localhost

## Automated Test
1. Buka folder AccessRequestHub.Tests
2. Buka terminal di folder ini, jalankan:
   dotnet test
3. Automated test akan secara otomatis berjalan


# Demo Users (Simulated Auth)
Gunakan header HTTP X-Current-User-Id di Swagger (klik tombol "Authorize" di kanan atas) atau Postman:
Alice (Requester): 11111111-1111-1111-1111-111111111111
Bob (Manager): 22222222-2222-2222-2222-222222222222
Carol (System Owner - CRM): 33333333-3333-3333-3333-333333333333
Dana (System Owner - Finance): 44444444-4444-4444-4444-444444444444
Erin (Admin/Auditor): 55555555-5555-5555-5555-555555555555

# Demo Flow Scenarios
Standard Request: Login sebagai Alice -> POST request (NonProduction, Read) -> Login sebagai Bob -> POST approve -> Status menjadi Approved.
High-Risk Request: Login sebagai Alice -> POST request (Production) -> Login sebagai Bob -> POST approve (Status jadi PendingSystemOwner) -> Login sebagai Carol -> POST approve -> Status menjadi Approved.
Idempotency: POST request yang sama persis (ClientRequestId sama) dua kali -> Hanya 1 record yang terbentuk.
Concurrency: Ambil rowVersion dari GET request -> Ubah data di DB secara manual -> POST approve dengan rowVersion lama -> Akan mendapat error 409 Conflict.