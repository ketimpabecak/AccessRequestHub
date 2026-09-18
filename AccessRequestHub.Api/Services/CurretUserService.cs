using AccessRequestHub.Exceptions;

namespace AccessRequestHub.Services;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Current-User-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerValue) || !Guid.TryParse(headerValue, out var userId))
        {
            throw new ForbiddenException("Missing or invalid X-Current-User-Id header. Please simulate login by providing a valid user GUID.");
        }

        return userId;
    }
}