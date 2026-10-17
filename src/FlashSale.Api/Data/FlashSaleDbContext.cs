using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api;

public class FlashSaleDbContext(DbContextOptions<FlashSaleDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.TimeZoneId).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne<Event>().WithMany().HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => new { s.EventId, s.Label }).IsUnique();
            entity.Ignore(s => s.Status);
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasOne<Seat>().WithMany().HasForeignKey(h => h.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(h => h.SeatId).IsUnique();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasOne<Seat>().WithMany().HasForeignKey(r => r.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => r.SeatId).IsUnique();
        });
    }
}
