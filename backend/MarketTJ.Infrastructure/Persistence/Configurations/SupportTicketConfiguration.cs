using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketTJ.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.SupportTickets)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedToAdmin)
            .WithMany()
            .HasForeignKey(x => x.AssignedToAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
