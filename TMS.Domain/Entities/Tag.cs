using System.Collections.Generic;

namespace TMS.Domain.Entities;

public sealed class Tag
{
    private readonly List<SupportTicket> _tickets = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public IReadOnlyCollection<SupportTicket> Tickets => _tickets.AsReadOnly();

    private Tag()
    {
    }

    private Tag(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Tag Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tag(Guid.NewGuid(), name.Trim());
    }
}
