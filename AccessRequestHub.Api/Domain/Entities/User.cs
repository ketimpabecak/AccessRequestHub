using static System.Net.Mime.MediaTypeNames;

namespace AccessRequestHub.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
}