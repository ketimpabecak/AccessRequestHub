namespace AccessRequestHub.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public AccessRequest Request { get; set; } = null!;

    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}