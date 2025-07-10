using Microsoft.EntityFrameworkCore;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;

namespace SaleManagement.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ReviewAndRating> ReviewAndRatings { get; set; }
    public DbSet<OrderHistory> OrderHistories { get; set; }

    public DbSet<SellerUpgradeRequest> SellerUpgradeRequests { get; set; }
    public DbSet<CategorySuggestion> CategorySuggestions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Shop>()
            .HasOne(shop => shop.User)
            .WithOne()
            .HasForeignKey<Shop>(shop => shop.UserId);

        modelBuilder.Entity<Item>()
            .HasOne(item => item.Shop)
            .WithMany(shop => shop.Items)
            .HasForeignKey(item => item.ShopId);

        modelBuilder.Entity<Item>()
            .HasOne(item => item.Category)
            .WithMany(category => category.Items)
            .HasForeignKey(item => item.CategoryId)
            .IsRequired(false);
    }
}