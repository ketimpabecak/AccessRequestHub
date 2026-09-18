using System.ComponentModel.DataAnnotations;

namespace AccessRequestHub.Domain.Entities;

public class AccessRequest
{
    public Guid Id { get; set; }
    [MaxLength(100)]
    public string ClientRequestId { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public EnvironmentType Environment { get; set; }
    public AccessLevelType AccessLevel { get; set; }
    public string Justification { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }
    public string PolicyVersion { get; set; } = "v1";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ConcurrencyCheck]
    public byte[] RowVersion { get; set; } = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}