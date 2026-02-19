using System.Collections.Generic;
using TMS.Domain.Entities;
using TMS.Domain.Enums;

namespace TMS.Domain.Abstractions;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TicketDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<SupportTicket?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid id, bool includeTags = false, bool includeComments = false, CancellationToken cancellationToken = default);
    Task<SupportTicket?> AttachTagAsync(Guid ticketId, string tagName, CancellationToken cancellationToken = default);
    Task<SupportTicket?> DetachTagAsync(Guid ticketId, Guid tagId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> UpdateAsync(Guid id, string title, string description, TicketStatus status, byte[]? attachmentBytes = null, string? attachmentFileName = null, string? attachmentContentType = null, CancellationToken cancellationToken = default);
    Task<TicketComment?> AddCommentAsync(Guid ticketId, string authorName, string message, DateTime createdAtUtc, CancellationToken cancellationToken = default);
}

public sealed record TicketDashboardStats(int Total, int InProgress, int Unresolved);
