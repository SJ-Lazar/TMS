using System.Collections.Generic;
using TMS.Domain.Entities;

namespace TMS.Domain.Abstractions;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TicketDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<SupportTicket?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid id, bool includeTags = false, CancellationToken cancellationToken = default);
    Task<SupportTicket?> AttachTagAsync(Guid ticketId, string tagName, CancellationToken cancellationToken = default);
    Task<SupportTicket?> DetachTagAsync(Guid ticketId, Guid tagId, CancellationToken cancellationToken = default);
}

public sealed record TicketDashboardStats(int Total, int InProgress, int Unresolved);
