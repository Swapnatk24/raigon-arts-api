using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Data;

public class RaigonDbContext : DbContext
{
    public RaigonDbContext(DbContextOptions<RaigonDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderPhoto> OrderPhotos => Set<OrderPhoto>();
    public DbSet<FrameSize> FrameSizes => Set<FrameSize>();
    public DbSet<WorkshopSetting> WorkshopSettings => Set<WorkshopSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Phone);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderPhoto
        modelBuilder.Entity<OrderPhoto>(entity =>
        {
            entity.HasOne(p => p.Order)
                  .WithMany(o => o.Photos)
                  .HasForeignKey(p => p.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FrameSize
        modelBuilder.Entity<FrameSize>(entity =>
        {
            entity.HasIndex(f => f.Code).IsUnique();
        });
    }
}
