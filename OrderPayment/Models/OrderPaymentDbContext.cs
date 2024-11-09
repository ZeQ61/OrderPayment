using Microsoft.EntityFrameworkCore;
using OrderPayment.Models;

public class OrderPaymentDbContext : DbContext
{
    public DbSet<User> Users { get; set; }


    public OrderPaymentDbContext(DbContextOptions<OrderPaymentDbContext> options) : base(options)
    { }
}
