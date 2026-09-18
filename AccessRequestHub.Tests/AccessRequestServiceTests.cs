using System.IO;
using AccessRequestHub.Data;
using AccessRequestHub.Domain;
using AccessRequestHub.Domain.Entities;
using AccessRequestHub.Exceptions;
using AccessRequestHub.Models;
using AccessRequestHub.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccessRequestHub.Tests;

public class AccessRequestServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AccessRequestService _service;
    private readonly string _dbPath;

    private readonly Guid _aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _carolId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _crmId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

    public AccessRequestServiceTests()
    {
        _dbPath = Path.GetTempFileName() + ".db";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new AppDbContext(options);

        _db.Database.EnsureCreated();

        _service = new AccessRequestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();

        if (File.Exists(_dbPath))
        {
            try
            {
                System.Threading.Thread.Sleep(50);
                File.Delete(_dbPath);
            }
            catch (IOException)
            {

            }
        }
    }

    // 1. Standard Flow Test
    [Fact]
    public async Task StandardRequest_ShouldBeApprovedByManager()
    {
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = "std-001",
            ApplicationId = _crmId,
            Environment = EnvironmentType.NonProduction,
            AccessLevel = AccessLevelType.Read,
            Justification = "Standard test"
        };

        var created = await _service.CreateRequestAsync(_aliceId, dto);
        Assert.Equal(RequestStatus.PendingManager.ToString(), created.Status);

        await _service.ApproveAsync(_bobId, created.Id, new DecisionDto { RowVersion = created.RowVersion });

        var final = await _service.GetRequestAsync(created.Id);
        Assert.Equal(RequestStatus.Approved.ToString(), final.Status);
    }

    // 2. High-Risk Flow Test
    [Fact]
    public async Task HighRiskRequest_ShouldRequireSystemOwnerApproval()
    {
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = "high-001",
            ApplicationId = _crmId,
            Environment = EnvironmentType.Production,
            AccessLevel = AccessLevelType.Read,
            Justification = "High risk test"
        };

        var created = await _service.CreateRequestAsync(_aliceId, dto);

        await _service.ApproveAsync(_bobId, created.Id, new DecisionDto { RowVersion = created.RowVersion });
        var afterBob = await _service.GetRequestAsync(created.Id);
        Assert.Equal(RequestStatus.PendingSystemOwner.ToString(), afterBob.Status);

        await _service.ApproveAsync(_carolId, created.Id, new DecisionDto { RowVersion = afterBob.RowVersion });
        var final = await _service.GetRequestAsync(created.Id);
        Assert.Equal(RequestStatus.Approved.ToString(), final.Status);
    }

    // 3. Idempotency Test
    [Fact]
    public async Task DuplicateClientRequestId_ShouldReturnExistingRequest()
    {
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = "idempotent-001",
            ApplicationId = _crmId,
            Environment = EnvironmentType.NonProduction,
            AccessLevel = AccessLevelType.Read,
            Justification = "First try"
        };

        var first = await _service.CreateRequestAsync(_aliceId, dto);
        var second = await _service.CreateRequestAsync(_aliceId, dto);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_db.AccessRequests.Where(r => r.ClientRequestId == "idempotent-001"));
    }

    // 4. Concurrency Test
    [Fact]
    public async Task StaleRowVersion_ShouldThrowConflictException()
    {
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = "concurrency-001",
            ApplicationId = _crmId,
            Environment = EnvironmentType.NonProduction,
            AccessLevel = AccessLevelType.Read,
            Justification = "Concurrency test"
        };

        var created = await _service.CreateRequestAsync(_aliceId, dto);
        var staleRowVersion = created.RowVersion; // Simpan versi lama

        var requestInDb = await _db.AccessRequests.FindAsync(created.Id)
            ?? throw new InvalidOperationException("Request not found for concurrency test");

        requestInDb.RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 };
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ApproveAsync(_bobId, created.Id, new DecisionDto { RowVersion = staleRowVersion }));
    }
}