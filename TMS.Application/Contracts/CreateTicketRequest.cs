using System.Collections.Generic;

namespace TMS.Application.Contracts;

public sealed class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public string? AttachmentBase64 { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}
