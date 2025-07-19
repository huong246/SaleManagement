using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public DbSet<RevokedToken> RevokedTokens { get; set; }
    public DbSet<ReturnRequest> ReturnRequests { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserViewHistory> UserViewHistories { get; set; }
    public DbSet<ItemImage> ItemImages { get; set; }
    public DbSet<UserAddress>  UserAddresses { get; set; }

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
            .HasForeignKey(item => item.ShopId)
            .OnDelete(DeleteBehavior.ClientSetNull);
        modelBuilder.Entity<Item>()
            .Property(i => i.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<Item>()
            .HasOne(item => item.Category)
            .WithMany(category => category.Items)
            .HasForeignKey(item => item.CategoryId)
            .IsRequired(false);
        modelBuilder.HasDbFunction(typeof(ApiDbContext)
                .GetMethod(nameof(FtsMatch), new[] { typeof(string), typeof(string) })!) // Thêm dấu ! ở đây
            .HasName("MATCH");
        modelBuilder.Entity<ItemImage>()
            .HasOne(itemImage => itemImage.Item)
            .WithMany(item => item.ItemImages)
            .HasForeignKey(itemImage => itemImage.ItemId)
            .IsRequired(false);
        modelBuilder.Entity<Voucher>()
            .Property(v => v.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<User>()
            .HasMany(u => u.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            
            if (entityType.FindProperty("RowVersion") is { } property)
            {
                // Thiết lập giá trị mặc định và trigger để tự động cập nhật RowVersion
             
                property.SetDefaultValueSql("X'00'"); 
                property.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                property.IsConcurrencyToken = true;
            }
        }
    }
    public static bool FtsMatch(string query, string pattern)
    {
        throw new NotImplementedException();
    }
}