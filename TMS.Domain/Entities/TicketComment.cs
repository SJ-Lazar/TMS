using System;

namespace TMS.Domain.Entities;

public sealed class TicketComment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private TicketComment()
    {
    }

    private TicketComment(Guid id, Guid ticketId, string authorName, string message, DateTime createdAtUtc)
    {
        Id = id;
        TicketId = ticketId;
        AuthorName = authorName;
        Message = message;
        CreatedAtUtc = createdAtUtc;
    }

    public static TicketComment Create(Guid ticketId, string authorName, string message, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new TicketComment(Guid.NewGuid(), ticketId, authorName, message, createdAtUtc);
    }
}
