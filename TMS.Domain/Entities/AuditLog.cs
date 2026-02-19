namespace TMS.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? TicketId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog()
    {
    }

    private AuditLog(Guid? ticketId, string action, string details, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        Action = action;
        Details = details;
        CreatedAtUtc = createdAtUtc;
    }

    public static AuditLog Create(Guid? ticketId, string action, string details, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(details);

        return new AuditLog(ticketId, action, details, createdAtUtc);
    }
}
