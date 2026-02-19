using System;
using System.Collections.Generic;
using System.Linq;
using TMS.Application.Abstractions;
using TMS.Application.Contracts;
using TMS.Domain.Abstractions;
using TMS.Domain.Entities;

namespace TMS.Application.Services;

public sealed class TicketService
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMemberRepository _memberRepository;
    private readonly IDateTimeProvider _clock;

    public TicketService(
        ISupportTicketRepository ticketRepository,
        ISupportMemberRepository memberRepository,
        IDateTimeProvider clock)
    {
        _ticketRepository = ticketRepository;
        _memberRepository = memberRepository;
        _clock = clock;
    }

    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var members = await _memberRepository.GetActiveMembersAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _ticketRepository.FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                var assignedName = GetAssignedSupportMemberName(existing.AssignedSupportMemberId, members);
                return MapToResponse(existing, assignedName);
            }
        }

        if (members.Count == 0)
        {
            throw new InvalidOperationException("No active support members available for assignment.");
        }

        // Unresolved tickets are used to balance load.
        var unresolvedTickets = await _ticketRepository.GetOpenTicketsAsync(cancellationToken);
        var assignee = SelectAssignee(members, unresolvedTickets);

        var ticket = SupportTicket.Create(request.Title, request.Description, request.RequesterName, _clock.UtcNow, request.IdempotencyKey);

        if (!string.IsNullOrWhiteSpace(request.AttachmentBase64))
        {
            var attachmentBytes = Convert.FromBase64String(request.AttachmentBase64);
            var fileName = string.IsNullOrWhiteSpace(request.AttachmentFileName) ? "attachment" : request.AttachmentFileName;
            var contentType = string.IsNullOrWhiteSpace(request.AttachmentContentType) ? "application/octet-stream" : request.AttachmentContentType;
            ticket.AttachFile(fileName, contentType, attachmentBytes);
        }

        ticket.Assign(assignee.Id);

        await _ticketRepository.AddAsync(ticket, request.Tags, cancellationToken);

        return MapToResponse(ticket, assignee.Name);
    }

    public async Task<TicketResponse?> AttachTagAsync(Guid ticketId, string tagName, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.AttachTagAsync(ticketId, tagName, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var members = await _memberRepository.GetActiveMembersAsync(cancellationToken);
        var assignedName = GetAssignedSupportMemberName(ticket.AssignedSupportMemberId, members);
        return MapToResponse(ticket, assignedName);
    }

    public async Task<TicketResponse?> DetachTagAsync(Guid ticketId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.DetachTagAsync(ticketId, tagId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var members = await _memberRepository.GetActiveMembersAsync(cancellationToken);
        var assignedName = GetAssignedSupportMemberName(ticket.AssignedSupportMemberId, members);
        return MapToResponse(ticket, assignedName);
    }

    private static SupportMember SelectAssignee(IReadOnlyList<SupportMember> members, IReadOnlyList<SupportTicket> openTickets)
    {
        var load = members.ToDictionary(m => m.Id, _ => 0);
        foreach (var ticket in openTickets)
        {
            if (ticket.AssignedSupportMemberId.HasValue && load.ContainsKey(ticket.AssignedSupportMemberId.Value))
            {
                load[ticket.AssignedSupportMemberId.Value]++;
            }
        }

        var ordered = members.OrderBy(m => load[m.Id]).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return ordered.First();
    }

    private static string GetAssignedSupportMemberName(Guid? memberId, IReadOnlyList<SupportMember> members)
    {
        if (memberId is null)
        {
            return string.Empty;
        }

        return members.FirstOrDefault(m => m.Id == memberId.Value)?.Name ?? string.Empty;
    }

    private static TicketResponse MapToResponse(SupportTicket ticket, string assignedSupportMember)
    {
        return new TicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            RequesterName = ticket.RequesterName,
            CreatedAtUtc = ticket.CreatedAtUtc,
            AssignedSupportMember = assignedSupportMember,
            Status = ticket.Status.ToString(),
            AttachmentFileName = ticket.AttachmentFileName,
            AttachmentContentType = ticket.AttachmentContentType,
            AttachmentBase64 = ticket.AttachmentBytes is null ? null : Convert.ToBase64String(ticket.AttachmentBytes),
            IdempotencyKey = ticket.IdempotencyKey,
            ConcurrencyToken = ticket.RowVersion is null ? null : Convert.ToBase64String(ticket.RowVersion),
            Tags = ticket.Tags.Select(t => t.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }
}
