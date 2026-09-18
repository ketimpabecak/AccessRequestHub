using AccessRequestHub.Domain;

namespace AccessRequestHub.Models;

public class CreateAccessRequestDto
{
    public string ClientRequestId { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public EnvironmentType Environment { get; set; }
    public AccessLevelType AccessLevel { get; set; }
    public string Justification { get; set; } = string.Empty;
}

public class DecisionDto
{
    public byte[] RowVersion { get; set; } = null!;

    public string? Reason { get; set; }
}


public class AccessRequestDetailDto
{
    public Guid Id { get; set; }
    public string ClientRequestId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = null!;

    public List<AuditEventDto> AuditEvents { get; set; } = new();
}

public class AuditEventDto
{
    public string Action { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}