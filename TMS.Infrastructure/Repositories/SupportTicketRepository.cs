using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TMS.Domain.Abstractions;
using TMS.Domain.Entities;
using TMS.Domain.Enums;
using TMS.Infrastructure.Persistence;

namespace TMS.Infrastructure.Repositories;

public sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly TmsDbContext _dbContext;

    public SupportTicketRepository(TmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SupportTicket ticket, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var normalizedTags = tags?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags is { Count: > 0 })
        {
            var existing = await _dbContext.Tags
                .Where(t => normalizedTags.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var missingNames = normalizedTags
                .Except(existing.Select(t => t.Name), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in missingNames)
            {
                var tag = Tag.Create(name);
                _dbContext.Tags.Add(tag);
                existing.Add(tag);
            }

            foreach (var tag in existing)
            {
                ticket.AddTag(tag);
            }
        }

        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
        _dbContext.AuditLogs.Add(AuditLog.Create(ticket.Id, "Created", $"Ticket '{ticket.Title}' created.", DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default)
    {
        // "Open tickets" for load balancing = unresolved work (Open + InProgress)
        return await _dbContext.Tickets
            .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        // Single query: count total + per-status counts, then compute the requested dashboard view.
        var counts = await _dbContext.Tickets
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = counts.Sum(x => x.Count);
        var inProgress = counts.FirstOrDefault(x => x.Status == TicketStatus.InProgress)?.Count ?? 0;
        var unresolved = (counts.FirstOrDefault(x => x.Status == TicketStatus.Open)?.Count ?? 0)
                         + inProgress;

        return new TicketDashboardStats(total, inProgress, unresolved);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .Include(t => t.Tags)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportTicket?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return await _dbContext.Tickets
            .Include(t => t.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, bool includeTags = false, CancellationToken cancellationToken = default)
    {
        IQueryable<SupportTicket> query = _dbContext.Tickets;

        if (includeTags)
        {
            query = query.Include(t => t.Tags);
        }

        return await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<SupportTicket?> AttachTagAsync(Guid ticketId, string tagName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);

        var ticket = await _dbContext.Tickets
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var normalized = tagName.Trim();
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == normalized, cancellationToken);
        if (tag is null)
        {
            tag = Tag.Create(normalized);
            _dbContext.Tags.Add(tag);
        }

        ticket.AddTag(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<SupportTicket?> DetachTagAsync(Guid ticketId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        ticket.RemoveTag(tagId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}
