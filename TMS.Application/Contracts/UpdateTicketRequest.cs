using System.ComponentModel.DataAnnotations;

namespace TMS.Application.Contracts;

public sealed class UpdateTicketRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public string Status { get; init; } = string.Empty;

    public string? AttachmentFileName { get; init; }

    public string? AttachmentContentType { get; init; }

    public string? AttachmentBase64 { get; init; }
}
