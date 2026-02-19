using Microsoft.EntityFrameworkCore;
using System.Linq;
using TMS.Domain.Entities;
using TMS.Domain.Enums;

namespace TMS.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task EnsureRosterAsync(TmsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.SupportMembers.AnyAsync(cancellationToken))
        {
            return;
        }

        var members = new List<SupportMember>
        {
            new(Guid.NewGuid(), "Alice"),
            new(Guid.NewGuid(), "Bob"),
            new(Guid.NewGuid(), "Carol"),
            new(Guid.NewGuid(), "Dave"),
            new(Guid.NewGuid(), "Eve")
        };

        await dbContext.SupportMembers.AddRangeAsync(members, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureTicketsAsync(TmsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Tickets.AnyAsync(cancellationToken))
        {
            return;
        }

        var assignee = await dbContext.SupportMembers.FirstOrDefaultAsync(cancellationToken);
        if (assignee is null)
        {
            return;
        }

        var ticket1 = SupportTicket.Create(
            "Onboarding help",
            "Need assistance with VPN configuration",
            "Jordan",
            DateTime.UtcNow.AddDays(-2));
        ticket1.AttachFile("vpn-instructions.txt", "text/plain", System.Text.Encoding.UTF8.GetBytes("VPN steps..."));
        ticket1.Assign(assignee.Id);

        var ticket2 = SupportTicket.Create(
            "Broken laptop",
            "Laptop screen flickers intermittently",
            "Riley",
            DateTime.UtcNow.AddDays(-1));
        ticket2.AttachFile("screen-photo.jpg", "image/jpeg", GeneratePlaceholderBytes());
        ticket2.Assign(assignee.Id);
        ticket2.Resolve();

        var networking = Tag.Create("Networking");
        var hardware = Tag.Create("Hardware");

        ticket1.AddTag(networking);
        ticket2.AddTag(hardware);

        await dbContext.Tags.AddRangeAsync(new[] { networking, hardware }, cancellationToken);
        await dbContext.Tickets.AddRangeAsync(new[] { ticket1, ticket2 }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static byte[] GeneratePlaceholderBytes()
    {
        // Small deterministic blob to act as seeded binary content
        return Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
    }
}
