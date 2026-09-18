using AccessRequestHub.Models;
using AccessRequestHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessRequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestsController : ControllerBase
{
    private readonly IAccessRequestService _service;
    private readonly ICurrentUserService _currentUser;

    public AccessRequestsController(IAccessRequestService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // POST: /api/accessrequests
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccessRequestDto dto)
    {
        var requesterId = _currentUser.GetCurrentUserId();
        var result = await _service.CreateRequestAsync(requesterId, dto);

        // Return 201 Created with the location header
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    // GET: /api/accessrequests/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _service.GetRequestAsync(id);
        return Ok(result);
    }

    // POST: /api/accessrequests/{id}/approve
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] DecisionDto dto)
    {
        var approverId = _currentUser.GetCurrentUserId();
        await _service.ApproveAsync(approverId, id, dto);
        return Ok(new { message = "Request approved successfully." });
    }

    // POST: /api/accessrequests/{id}/reject
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] DecisionDto dto)
    {
        var approverId = _currentUser.GetCurrentUserId();
        await _service.RejectAsync(approverId, id, dto);
        return Ok(new { message = "Request rejected successfully." });
    }
}