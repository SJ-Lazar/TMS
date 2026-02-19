using System;
using System.Collections.Generic;

namespace TMS.Application.Contracts;

public sealed class TicketResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string AssignedSupportMember { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? AttachmentFileName { get; init; }
    public string? AttachmentContentType { get; init; }
    public string? AttachmentBase64 { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? ConcurrencyToken { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TicketCommentResponse> Comments { get; init; } = Array.Empty<TicketCommentResponse>();
}

public sealed class TicketCommentResponse
{
    public Guid Id { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
