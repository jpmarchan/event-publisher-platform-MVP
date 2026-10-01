using EventService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Venue).IsRequired().HasMaxLength(200);
        // Npgsql solo acepta UTC en timestamptz.
        builder.Property(e => e.Date)
            .IsRequired()
            .HasConversion(
                toProvider => toProvider.ToUniversalTime(),
                fromProvider => fromProvider);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // EF trabaja sobre el campo privado _zones.
        builder.Navigation(e => e.Zones).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Zones)
            .WithOne()
            .HasForeignKey("EventId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
