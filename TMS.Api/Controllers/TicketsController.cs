using Microsoft.AspNetCore.Mvc;
using TMS.Application.Contracts;
using TMS.Application.Services;
using TMS.Domain.Abstractions;

namespace TMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly ISupportTicketRepository _ticketRepository;

    public TicketsController(TicketService ticketService, ISupportTicketRepository ticketRepository)
    {
        _ticketService = ticketService;
        _ticketRepository = ticketRepository;
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.CreateTicketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
    }

    [HttpPost("{id:guid}/tags")]
    public async Task<ActionResult<TicketResponse>> AttachTag(Guid id, [FromBody] TagRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tag name is required.");
        }

        var ticket = await _ticketService.AttachTagAsync(id, request.Name, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

    [HttpDelete("{id:guid}/tags/{tagId:guid}")]
    public async Task<ActionResult<TicketResponse>> DetachTag(Guid id, Guid tagId, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.DetachTagAsync(id, tagId, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var stats = await _ticketRepository.GetDashboardStatsAsync(cancellationToken);
        return Ok(new DashboardResponse(stats.Total, stats.InProgress, stats.Unresolved));
    }

    [HttpGet("{id:guid}")]
    public ActionResult Get(Guid id)
    {
        // Placeholder for retrieving ticket by id; not required for auto-assignment scenario yet.
        return NotFound();
    }

    public sealed record DashboardResponse(int TotalTickets, int InProgressTickets, int UnresolvedTickets);
}
