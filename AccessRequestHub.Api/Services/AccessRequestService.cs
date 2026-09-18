using AccessRequestHub.Data;
using AccessRequestHub.Domain;
using AccessRequestHub.Domain.Entities;
using AccessRequestHub.Exceptions;
using AccessRequestHub.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestHub.Services;

public interface IAccessRequestService
{
    Task<AccessRequestDetailDto> CreateRequestAsync(Guid requesterId, CreateAccessRequestDto dto);
    Task<AccessRequestDetailDto> GetRequestAsync(Guid requestId);
    Task ApproveAsync(Guid approverId, Guid requestId, DecisionDto dto);
    Task RejectAsync(Guid approverId, Guid requestId, DecisionDto dto);
}

public class AccessRequestService : IAccessRequestService
{
    private readonly AppDbContext _db;

    public AccessRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AccessRequestDetailDto> CreateRequestAsync(Guid requesterId, CreateAccessRequestDto dto)
    {
        var requester = await _db.Users.FindAsync(requesterId)
            ?? throw new NotFoundException("Requester not found");

        var application = await _db.Applications.FindAsync(dto.ApplicationId)
            ?? throw new NotFoundException("Application not found");

        var request = new AccessRequest
        {
            ClientRequestId = dto.ClientRequestId,
            RequesterId = requesterId,
            ApplicationId = dto.ApplicationId,
            Environment = dto.Environment,
            AccessLevel = dto.AccessLevel,
            Justification = dto.Justification,
            Status = RequestStatus.PendingManager,
            PolicyVersion = "v1"
        };

        var audit = new AuditEvent
        {
            Request = request,
            ActorId = requesterId,
            Action = "Created",
            Timestamp = DateTime.UtcNow
        };

        _db.AccessRequests.Add(request);
        _db.AuditEvents.Add(audit);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {

            _db.Entry(request).State = EntityState.Detached;

            var existing = await _db.AccessRequests
                .Include(r => r.Requester)
                .Include(r => r.Application)
                .Include(r => r.AuditEvents).ThenInclude(a => a.Actor)
                .FirstAsync(r => r.ClientRequestId == dto.ClientRequestId);

            return MapToDetailDto(existing);
        }

        return MapToDetailDto(request);
    }

    public async Task<AccessRequestDetailDto> GetRequestAsync(Guid requestId)
    {
        var request = await _db.AccessRequests
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Include(r => r.AuditEvents).ThenInclude(a => a.Actor)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        return MapToDetailDto(request);
    }

    public async Task ApproveAsync(Guid approverId, Guid requestId, DecisionDto dto)
    {
        var request = await _db.AccessRequests
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .FirstAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        ValidateTerminalState(request);

        var approver = await _db.Users.FindAsync(approverId)
            ?? throw new NotFoundException("Approver not found");

        ValidateSelfApproval(request, approver);

        if (request.Status == RequestStatus.PendingManager)
        {
            ValidateManagerApproval(request, approver);

            bool isHighRisk = request.Environment == EnvironmentType.Production ||
                              request.AccessLevel == AccessLevelType.Admin;

            request.Status = isHighRisk ? RequestStatus.PendingSystemOwner : RequestStatus.Approved;
        }
        else if (request.Status == RequestStatus.PendingSystemOwner)
        {
            ValidateSystemOwnerApproval(request, approver);
            request.Status = RequestStatus.Approved;
        }
        else
        {
            throw new BusinessException("Invalid state for approval.");
        }

        await SaveWithConcurrencyCheckAsync(request, approverId, "Approved", dto.RowVersion);
    }

    public async Task RejectAsync(Guid approverId, Guid requestId, DecisionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BusinessException("Rejection reason is required.");

        var request = await _db.AccessRequests
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .FirstAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        ValidateTerminalState(request);

        var approver = await _db.Users.FindAsync(approverId)
            ?? throw new NotFoundException("Approver not found");

        ValidateSelfApproval(request, approver);

        if (request.Status == RequestStatus.PendingManager)
            ValidateManagerApproval(request, approver);
        else if (request.Status == RequestStatus.PendingSystemOwner)
            ValidateSystemOwnerApproval(request, approver);
        else
            throw new BusinessException("Invalid state for rejection.");

        request.Status = RequestStatus.Rejected;

        await SaveWithConcurrencyCheckAsync(request, approverId, "Rejected", dto.RowVersion, dto.Reason);
    }

    // --- PRIVATE HELPER METHODS ---

    private async Task SaveWithConcurrencyCheckAsync(AccessRequest request, Guid actorId, string action, byte[] clientRowVersion, string? reason = null)
    {
        _db.Entry(request).Property(r => r.RowVersion).OriginalValue = clientRowVersion;

        var audit = new AuditEvent
        {
            RequestId = request.Id,
            ActorId = actorId,
            Action = action,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
        _db.AuditEvents.Add(audit);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The request was modified by another user. Please refresh and try again.");
        }
    }

    private void ValidateTerminalState(AccessRequest request)
    {
        if (request.Status == RequestStatus.Approved || request.Status == RequestStatus.Rejected)
            throw new BusinessException("Request is already in a terminal state and cannot be modified.");
    }

    private void ValidateSelfApproval(AccessRequest request, User approver)
    {
        if (approver.Id == request.RequesterId)
            throw new ForbiddenException("You cannot approve or reject your own request.");
    }

    private void ValidateManagerApproval(AccessRequest request, User approver)
    {
        if (approver.Role != UserRole.Manager)
            throw new ForbiddenException("Only Managers can approve at this stage.");

        if (approver.Id != request.Requester.ManagerId)
            throw new ForbiddenException("You are not the manager of this requester.");
    }

    private void ValidateSystemOwnerApproval(AccessRequest request, User approver)
    {
        if (approver.Role != UserRole.SystemOwner)
            throw new ForbiddenException("Only System Owners can approve at this stage.");

        if (approver.Id != request.Application.SystemOwnerId)
            throw new ForbiddenException("You are not the System Owner of this application.");
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            return sqlEx.Number == 2601 || sqlEx.Number == 2627;
        }

        if (ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx)
        {
            return sqliteEx.SqliteErrorCode == 19 || sqliteEx.SqliteErrorCode == 2067;
        }

        return false;
    }

    private AccessRequestDetailDto MapToDetailDto(AccessRequest request)
    {
        return new AccessRequestDetailDto
        {
            Id = request.Id,
            ClientRequestId = request.ClientRequestId,
            RequesterName = request.Requester?.Name ?? "Unknown",
            ApplicationName = request.Application?.Name ?? "Unknown",
            Environment = request.Environment.ToString(),
            AccessLevel = request.AccessLevel.ToString(),
            Justification = request.Justification,
            Status = request.Status.ToString(),
            PolicyVersion = request.PolicyVersion,
            RowVersion = request.RowVersion,
            AuditEvents = request.AuditEvents?
                .OrderBy(a => a.Timestamp)
                .Select(a => new AuditEventDto
                {
                    Action = a.Action,
                    ActorName = a.Actor?.Name ?? "System",
                    Reason = a.Reason,
                    Timestamp = a.Timestamp
                }).ToList() ?? new()
        };
    }
}