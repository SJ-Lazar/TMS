using System;
using System.Collections.Generic;
using System.Linq;
using TMS.Application.Abstractions;
using TMS.Application.Contracts;
using TMS.Application.Services;
using TMS.Domain.Abstractions;
using TMS.Domain.Entities;
using NUnit.Framework;

namespace TMS.Tests;

public class TicketServiceTests
{
    private InMemoryTicketRepository _ticketRepository = null!;
    private InMemoryMemberRepository _memberRepository = null!;
    private TicketService _service = null!;

    [SetUp]
    public void Setup()
    {
        _ticketRepository = new InMemoryTicketRepository();
        _memberRepository = new InMemoryMemberRepository();
        _memberRepository.SeedMembers(new List<SupportMember>
        {
            new(Guid.NewGuid(), "Alice"),
            new(Guid.NewGuid(), "Bob"),
            new(Guid.NewGuid(), "Carol"),
            new(Guid.NewGuid(), "Dave"),
            new(Guid.NewGuid(), "Eve")
        });

        _service = new TicketService(_ticketRepository, _memberRepository, new FixedClock());
    }

    [Test]
    public async Task Creates_ticket_and_balances_assignment()
    {
        var assignments = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var response = await _service.CreateTicketAsync(new CreateTicketRequest
            {
                Title = $"Issue {i}",
                Description = "Test",
                RequesterName = "User"
            });
            assignments.Add(response.AssignedSupportMember);
        }

        Assert.That(assignments.Distinct().Count(), Is.EqualTo(5));
    }

    [Test]
    public async Task Applies_tags_on_creation()
    {
        var response = await _service.CreateTicketAsync(new CreateTicketRequest
        {
            Title = "Tagged issue",
            Description = "Test",
            RequesterName = "User",
            Tags = new List<string> { "P1", "Onboarding" }
        });

        Assert.That(response.Tags, Does.Contain("P1"));
        Assert.That(response.Tags, Does.Contain("Onboarding"));
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class InMemoryTicketRepository : ISupportTicketRepository
    {
        private readonly List<SupportTicket> _tickets = new();
        private readonly List<Tag> _tags = new();

        public Task AddAsync(SupportTicket ticket, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            var normalizedTags = tags?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedTags is { Count: > 0 })
            {
                foreach (var name in normalizedTags)
                {
                    var tag = _tags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Tag.Create(name);
                    if (!_tags.Contains(tag))
                    {
                        _tags.Add(tag);
                    }
                    ticket.AddTag(tag);
                }
            }

            _tickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SupportTicket> open = _tickets
                .Where(t => t.Status == Domain.Enums.TicketStatus.Open || t.Status == Domain.Enums.TicketStatus.InProgress)
                .ToList();
            return Task.FromResult(open);
        }

        public Task<TicketDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
        {
            var total = _tickets.Count;
            var inProgress = _tickets.Count(t => t.Status == Domain.Enums.TicketStatus.InProgress);
            var unresolved = _tickets.Count(t => t.Status == Domain.Enums.TicketStatus.Open || t.Status == Domain.Enums.TicketStatus.InProgress);
            return Task.FromResult(new TicketDashboardStats(total, inProgress, unresolved));
        }

        public Task<SupportTicket?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
            var ticket = _tickets.FirstOrDefault(t => t.IdempotencyKey == idempotencyKey);
            return Task.FromResult(ticket);
        }

        public Task<SupportTicket?> GetByIdAsync(Guid id, bool includeTags = false, CancellationToken cancellationToken = default)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(ticket);
        }

        public Task<SupportTicket?> AttachTagAsync(Guid ticketId, string tagName, CancellationToken cancellationToken = default)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket is null)
            {
                return Task.FromResult<SupportTicket?>(null);
            }

            var tag = _tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)) ?? Tag.Create(tagName);
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }

            ticket.AddTag(tag);
            return Task.FromResult<SupportTicket?>(ticket);
        }

        public Task<SupportTicket?> DetachTagAsync(Guid ticketId, Guid tagId, CancellationToken cancellationToken = default)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            ticket?.RemoveTag(tagId);
            return Task.FromResult(ticket);
        }
    }

    private sealed class InMemoryMemberRepository : ISupportMemberRepository
    {
        private List<SupportMember> _members = new();

        public Task<IReadOnlyList<SupportMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SupportMember> active = _members.Where(m => m.IsActive).ToList();
            return Task.FromResult(active);
        }

        public Task EnsureSeedDataAsync(IEnumerable<SupportMember> members, CancellationToken cancellationToken = default)
        {
            SeedMembers(members);
            return Task.CompletedTask;
        }

        public void SeedMembers(IEnumerable<SupportMember> members)
        {
            _members = members.ToList();
        }
    }
}
