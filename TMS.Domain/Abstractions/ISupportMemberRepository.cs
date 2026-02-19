using TMS.Domain.Entities;

namespace TMS.Domain.Abstractions;

public interface ISupportMemberRepository
{
    Task<IReadOnlyList<SupportMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
    Task EnsureSeedDataAsync(IEnumerable<SupportMember> members, CancellationToken cancellationToken = default);
}
