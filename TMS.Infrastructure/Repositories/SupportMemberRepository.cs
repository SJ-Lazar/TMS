using Microsoft.EntityFrameworkCore;
using TMS.Domain.Abstractions;
using TMS.Domain.Entities;
using TMS.Infrastructure.Persistence;

namespace TMS.Infrastructure.Repositories;

public sealed class SupportMemberRepository : ISupportMemberRepository
{
    private readonly TmsDbContext _dbContext;

    public SupportMemberRepository(TmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SupportMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupportMembers
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task EnsureSeedDataAsync(IEnumerable<SupportMember> members, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SupportMembers.CountAsync(cancellationToken);
        if (existing > 0)
        {
            return;
        }

        await _dbContext.SupportMembers.AddRangeAsync(members, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
