namespace TMS.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
