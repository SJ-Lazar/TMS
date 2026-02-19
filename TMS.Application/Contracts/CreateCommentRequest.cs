using System.ComponentModel.DataAnnotations;

namespace TMS.Application.Contracts;

public sealed class CreateCommentRequest
{
    [Required]
    [MaxLength(200)]
    public string AuthorName { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; init; } = string.Empty;
}
