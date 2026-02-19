using System;
using System.Collections.Generic;
using System.Linq;
using TMS.Domain.Enums;

namespace TMS.Domain.Entities;

public sealed class SupportTicket
{
    private readonly List<Tag> _tags = new();
    private readonly List<TicketComment> _comments = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string RequesterName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? AssignedSupportMemberId { get; private set; }
    public TicketStatus Status { get; private set; }
    public string? AttachmentFileName { get; private set; }
    public string? AttachmentContentType { get; private set; }
    public byte[]? AttachmentBytes { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyCollection<Tag> Tags => _tags;
    public IReadOnlyCollection<TicketComment> Comments => _comments;

    private SupportTicket()
    {
    }

    private SupportTicket(Guid id, string title, string description, string requesterName, DateTime createdAtUtc, string? idempotencyKey)
    {
        Id = id;
        Title = title;
        Description = description;
        RequesterName = requesterName;
        CreatedAtUtc = createdAtUtc;
        Status = TicketStatus.Open;
        IdempotencyKey = idempotencyKey;
    }

    public static SupportTicket Create(string title, string description, string requesterName, DateTime createdAtUtc, string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(requesterName);

        if (!string.IsNullOrWhiteSpace(idempotencyKey) && idempotencyKey.Length > 100)
        {
            throw new ArgumentException("Idempotency key is too long", nameof(idempotencyKey));
        }

        return new SupportTicket(Guid.NewGuid(), title, description, requesterName, createdAtUtc, idempotencyKey);
    }

    public void AttachFile(string fileName, string contentType, byte[] data)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException("Closed tickets cannot be edited.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(data);

        AttachmentFileName = fileName;
        AttachmentContentType = contentType;
        AttachmentBytes = data;
    }

    public void Assign(Guid supportMemberId)
    {
        AssignedSupportMemberId = supportMemberId;

        // In a typical support workflow, assignment moves the ticket into active work.
        // ("Unresolved" is represented by Open/InProgress.)
        if (Status == TicketStatus.Open)
        {
            Status = TicketStatus.InProgress;
        }
    }

    public void Resolve()
    {
        if (Status is TicketStatus.Closed)
        {
            throw new InvalidOperationException("Closed tickets cannot be resolved.");
        }

        Status = TicketStatus.Resolved;
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
    }

    public void AddTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (_tags.Any(t => string.Equals(t.Name, tag.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _tags.Add(tag);
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
        {
            return;
        }

        _tags.Remove(tag);
    }

    public void AddComment(string authorName, string message, DateTime createdAtUtc)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException("Closed tickets cannot be edited.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(authorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _comments.Add(TicketComment.Create(Id, authorName, message, createdAtUtc));
    }

    public void UpdateDetails(string title, string description)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException("Closed tickets cannot be edited.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Title = title;
        Description = description;
    }

    public void ChangeStatus(TicketStatus status)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException("Closed tickets cannot be edited.");
        }

        Status = status;
    }
}
