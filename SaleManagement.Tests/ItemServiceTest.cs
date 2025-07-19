using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Schemas;
using SaleManagement.Services;
using Xunit;

namespace SaleManagement.Tests;

public class ItemServiceTest
{
    
    [Fact]
    public async Task CreateItemAsync_WhenRequestIsValid_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
        var request = new CreateItemRequest("New item", "beautiful item", 100, 10, null);

        // Act
        var result = await itemService.CreateItem(request);
        var itemCount = await dbContext.Items.CountAsync();
        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Name == "New item");

        // Assert
        Assert.Equal(CreateItemResult.Success, result);
        Assert.Equal(1, itemCount);
        Assert.NotNull(item);
    }

    [Fact]
    public async Task CreateItemAsync_WhenUserIsNotLoggedIn_ReturnTokenInvalid()
    {
         var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        { 
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
        var request = new CreateItemRequest("New item", "beautiful item", 100, 10, null);


        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.TokenInvalid, result);
    }

    [Fact]
    public async Task CreateItemAsync_WhenUserNotExists_ReturnUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
        var request = new CreateItemRequest("New item", "beautiful item", 100, 10, null);

        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.UserNotFound, result);
    }

    [Fact]
    public async Task CreateItemAsync_WhenUserDoesNotHaveShop_ReturnShopNotFound()
    { var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
        var request = new CreateItemRequest("New item", "beautiful item", 100, 10, null);
        
        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.ShopNotFound, result);
    }

    [Theory] // Sửa từ [NUnit.Framework.Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CreateItemAsync_WhenStockIsInvalid_ReturnStockInvalid(int invalidStock)
    {
        // Arrange
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);

        var request = new CreateItemRequest("New item", "beautiful item", 100, invalidStock, Guid.NewGuid());

        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.StockInvalid, result);
    }

    [Fact]
    public async Task CreateItemAsync_WhenPriceIsInvalid_ReturnPriceInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
        var request = new CreateItemRequest("New item", "beautiful item", -19, 10, null);

        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.PriceInvalid, result);
    }

    [Fact]
    public async Task CreateItemAsync_WhenCategoryIsInvalid_ReturnCategoryNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);

        var request = new CreateItemRequest("New item", "beautiful item", 100, 10, Guid.NewGuid());

        // Act
        var result = await itemService.CreateItem(request);

        // Assert
        Assert.Equal(CreateItemResult.CategoryNotFound, result);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenRequestIsValid_ReturnSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
                                                   
        var originalRowVersion = item.RowVersion;

        var request = new UpdateItemRequest(item.Id, "New item", "beautiful item", 500, 500, null, originalRowVersion);
        var result = await itemService.UpdateItem(request);
        // Assert
        Assert.Equal(UpdateItemResult.Success, result);
        var updatedItem = await dbContext.Items.FindAsync(item.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal("New item", updatedItem.Name);
        Assert.Equal(500, updatedItem.Price);
        Assert.Equal(500, updatedItem.Stock);
        Assert.NotEqual(originalRowVersion, updatedItem.RowVersion);
        connection.Close();
    }


    [Fact]
    public async Task UpdateItemAsync_WhenItemNotFound_ReturnItemNotFound()
    {
       var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
                                                   
        var originalRowVersion = item.RowVersion;

        var request = new UpdateItemRequest(item.Id, "New item", "beautiful item", 500, 500, null, originalRowVersion);
        var result = await itemService.UpdateItem(request);
        // Assert
        Assert.Equal(UpdateItemResult.ItemNotFound, result);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenRowVersionIsOutdated_ReturnConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        
                                                   
        var originalRowVersion = Array.Empty<byte>();
        await using (var setupContext = new ApiDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(user);
            setupContext.Shops.Add(shop);
            setupContext.Items.Add(item);
            await setupContext.SaveChangesAsync();
            originalRowVersion = item.RowVersion; 
        }
        
        await using (var conflictingContext = new ApiDbContext(options))
        {
            // SỬA Ở ĐÂY: Dùng FirstOrDefaultAsync thay vì FindAsync
            var itemToConflict = await conflictingContext.Items
                .FirstOrDefaultAsync(i => i.Id == item.Id);
            
            Assert.NotNull(itemToConflict); // Đảm bảo đã tìm thấy item

            itemToConflict.Name = "Updated by another user";
            await conflictingContext.SaveChangesAsync();
        }
    
        // --- BƯỚC 3: Thực hiện hành động chính và kiểm tra xung đột ---
        await using (var actionContext = new ApiDbContext(options))
        {
            var itemService = new ItemService(actionContext, mockHttpContextAccessor.Object);
            var request = new UpdateItemRequest(item.Id, "My update attempt", "beautiful item", 500, 500, null, originalRowVersion);

            var result = await itemService.UpdateItem(request);
        
            // Assert: Kết quả phải là xung đột
            Assert.Equal(UpdateItemResult.ConcurrencyConflict, result);
        }
        
        connection.Close();
    }

    [Fact]
    public async Task DeleteItemAsync_WhenRequestIsValid_ReturnSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);
                                                   
        var originalRowVersion = item.RowVersion;

        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItem(request);
        var itemCount = await dbContext.Items.CountAsync();

        // Assert
        Assert.Equal(DeleteItemResult.Success, result);
        Assert.Equal(0, itemCount);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenDeleteByAdmin_ReturnSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "testUser1",
            Password = "122322",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Admin,
            Gender = "Female",
        };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, UserRole.Admin.ToString()),
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);

        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItem(request);
        var itemCount = await dbContext.Items.CountAsync();
        // Assert
        Assert.Equal(DeleteItemResult.Success, result);
        Assert.Equal(0, itemCount);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenOtherUserTriesDelete_ReturnUserNotPermission()
    {
       var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "testUser1",
            Password = "122322",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Customer,
            Gender = "Female",
        };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString()),
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);

        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItem(request);
        var itemCount = await dbContext.Items.CountAsync();

        // Assert
        Assert.Equal(DeleteItemResult.UserNotPermission, result);
        Assert.Equal(1, itemCount);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenItemNotFound_ReturnItemNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "testUser1",
            Password = "122322",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            UserRoles = UserRole.Seller,
            Gender = "Female",
        };

        var shop = new Shop { UserId = user.Id, Name = "1234", Id = Guid.NewGuid() };

        var item = new Item
        {
            Id = Guid.NewGuid(), Name = "Test item", Description = "Product test", Price = 100, Stock = 10,
            ShopId = shop.Id, RowVersion = [9]
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, UserRole.Seller.ToString()),
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();        
        var itemService = new ItemService(dbContext, mockHttpContextAccessor.Object);

        var request = new DeleteItemRequest(item.Id);
        var result = await itemService.DeleteItem(request);

        // Assert
        Assert.Equal(DeleteItemResult.ItemNotFound, result);
    }
}
