using Microsoft.EntityFrameworkCore;
using OrderPayment.Models;

public class OrderPaymentDbContext : DbContext
{
    public DbSet<Product> products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; } // Yeni model ekledik

    public OrderPaymentDbContext(DbContextOptions<OrderPaymentDbContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User - Order ilişkisi (Bir kullanıcı birden fazla siparişe sahip olabilir)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silindiğinde ona ait siparişler de silinsin

        // Order - OrderItem ilişkisi (Bir sipariş birden fazla sipariş kalemine sahip olabilir)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Sipariş silindiğinde ona ait kalemler de silinsin

        // Admin - User ilişkisi (Bir admin birden fazla kullanıcıyı yönetebilir)

       // User - VerificationCode ilişkisi (Bir kullanıcı birden fazla doğrulama koduna sahip olabilir)
        modelBuilder.Entity<VerificationCode>()
            .HasOne(vc => vc.User)
            .WithMany(u => u.VerificationCodes)
            .HasForeignKey(vc => vc.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silindiğinde doğrulama kodları da silinsin
    }
}
