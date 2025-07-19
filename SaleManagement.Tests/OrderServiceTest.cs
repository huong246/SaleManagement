using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Entities.Enums;
using SaleManagement.Hubs;
using SaleManagement.Schemas;
using SaleManagement.Services;
using System.Security.Claims;
using Xunit;

namespace SaleManagement.Tests.Services;

public class OrderServiceTests
{

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest = new CreateOrderRequest(new List<Guid> { item1.Id }, null, null, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var orderInDb = await dbContext.Orders.FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(orderInDb);
        Assert.Equal(50000, orderInDb.TotalAmount);
        Assert.Equal(shippingFee, orderInDb.ShippingFee);
        var itemInDb = await dbContext.Items.FindAsync(item1.Id);
        Assert.NotNull(itemInDb);
        Assert.Equal(8, itemInDb.Stock);
        connection.Close();
    }

    [Fact]
    public async Task CreateOrder_WithProductVoucher_AppliesDiscountCorrectly()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, null, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var orderInDb = await dbContext.Orders.FirstAsync(o => o.UserId == userId);
        Assert.Equal(10000, orderInDb.DiscountProductAmount);
        Assert.Equal(40000, orderInDb.TotalAmount); // 2-1+3
        var voucherInDb = await dbContext.Vouchers.FindAsync(voucherProduct.Id);
        Assert.NotNull(voucherInDb);
        Assert.Equal(4, voucherInDb.Quantity);

    }

    [Fact]
    public async Task CreateOrder_WithShippingVoucher_AppliesDiscountCorrectly()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, null, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var orderInDb = await dbContext.Orders.FirstAsync(o => o.UserId == userId);
        Assert.Equal(10000, orderInDb.DiscountProductAmount);
        Assert.Equal(40000, orderInDb.TotalAmount); // 2-1+3
        var voucherInDb = await dbContext.Vouchers.FindAsync(voucherShipping.Id);
        Assert.NotNull(voucherInDb);
        Assert.Equal(4, voucherInDb.Quantity);

    }


    [Fact]

    public async Task CreateOrder_WithBothVouchers_AppliesDiscountCorrectly()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var orderInDb = await dbContext.Orders.FirstAsync(o => o.UserId == userId);
        Assert.Equal(10000, orderInDb.DiscountProductAmount);
        Assert.Equal(30000, orderInDb.TotalAmount); // 2-1+3-1
        var voucherInDb = await dbContext.Vouchers.FindAsync(voucherShipping.Id);
        Assert.NotNull(voucherInDb);
        Assert.Equal(4, voucherInDb.Quantity);
        var voucherInDb2 = await dbContext.Vouchers.FindAsync(voucherProduct.Id);
        Assert.NotNull(voucherInDb2);
        Assert.Equal(4, voucherInDb2.Quantity);
    }


    [Fact]

    public async Task CreateOrder_StockNotEnough_ReturnsStockNotEnough()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 1, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.StockNotEnough, result);

        var itemInDb = await dbContext.Items.FindAsync(item1.Id);

        Assert.NotNull(itemInDb);

        Assert.Equal(1, itemInDb.Stock);

    }



    [Fact]
    public async Task CreateOrder_VoucherExpired_ReturnsVoucherExpired()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = false, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 10000, IsActive = false, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);
        Assert.Equal(CreateOrderResult.VoucherExpired, result);

    }


    [Fact]
    public async Task CreateOrder_MinSpendNotMet_ReturnsMinSpendNotMet()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 100000,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 10000, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 100000,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);
        Assert.Equal(CreateOrderResult.MinspendNotMet, result);

    }



    [Fact]
    public async Task CreateOrder_WithPercentageVoucher_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 20, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.Percentage, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 20, IsActive = true, MaxDiscountAmount = 10000,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.Percentage, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(order);
        Assert.Equal(40000, order.TotalAmount);

    }


    [Fact]

    public async Task CreateOrder_WhenDiscountIsGreaterThanSubTotal_DiscountEqualsSubTotal()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 200000, IsActive = true,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 50000, IsActive = true,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .Returns(Task.FromResult(shippingFee));
        mockShippingService.Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync("TEST_TRACKING_CODE");


        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        var result = await orderService.CreateOrder(createOrderRequest);

        Assert.Equal(CreateOrderResult.Success, result);
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(order);
        Assert.Equal(0, order.TotalAmount);

    }



    [Fact]
    public async Task CreateOrder_ShippingServiceThrowsException_ReturnsDatabaseErrorAndRollsBack()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = Guid.NewGuid(), Username = "seller", Password = "123" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", UserId = seller.Id };
        var item1 = new Item
            { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 10, ShopId = shop.Id, RowVersion = [] };
        var cartItem1 = new CartItem
            { Id = Guid.NewGuid(), UserId = userId, ItemId = item1.Id, Quantity = 2, Item = item1 };
        var voucherShipping = new Voucher
        {
            Id = Guid.NewGuid(), Code = "12345", DiscountValue = 200000, IsActive = true,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Shipping, RowVersion = []
        };
        var voucherProduct = new Voucher
        {
            Id = Guid.NewGuid(), Code = "123456789", DiscountValue = 50000, IsActive = true,
            ItemId = item1.Id, Quantity = 5, MethodType = DiscountMethod.FixedAmount, MinSpend = 0,
            TargetType = VoucherTarger.Product, RowVersion = []
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        decimal shippingFee = 30000;
        mockShippingService.Setup(s => s.CalculateFeeAsync(It.IsAny<Shop>(), It.IsAny<User>()))
            .ThrowsAsync(new Exception("Lỗi giả lập từ dịch vụ vận chuyển"));

        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Items.AddAsync(item1);
        await dbContext.CartItems.AddAsync(cartItem1);
        await dbContext.Vouchers.AddAsync(voucherShipping);
        await dbContext.Vouchers.AddAsync(voucherProduct);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );
        var createOrderRequest =
            new CreateOrderRequest(new List<Guid> { item1.Id }, voucherProduct.Id, voucherShipping.Id, 10.0, 106.0);

        await Assert.ThrowsAsync<Exception>(() => orderService.CreateOrder(createOrderRequest));
        var orderCount = await dbContext.Orders.CountAsync();
        Assert.Equal(0, orderCount);
        var itemInDb = await dbContext.Items.FindAsync(item1.Id);
        Assert.NotNull(itemInDb);
        Assert.Equal(10, itemInDb.Stock);
    }

    [Fact]
    public async Task UpdateOrderStatus_ByAdmin_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Admin,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.pending,
            UserId = userId,
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new UpdateOrderStatusRequest(orderId, OrderStatus.processing, "update status");
        var result = await orderService.UpdateOrderStatus(request);
        Assert.Equal(UpdateOrderStatusResult.Success, result);
        var orderInDb = await dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal(OrderStatus.processing, orderInDb.Status);
        var historyExists =
            await dbContext.OrderHistories.AnyAsync(h => h.OrderId == order.Id && h.Status == OrderStatus.processing);
        Assert.True(historyExists);

    }



    [Fact]
    public async Task UpdateOrderStatus_ByCorrectSellerWithValidTransition_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 10,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderId = Guid.NewGuid();
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };

        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.pending,
            UserId = userId,
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, sellerId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Seller.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);

        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new UpdateOrderStatusRequest(orderId, OrderStatus.processing, "update status");
        var result = await orderService.UpdateOrderStatus(request);
        // Assert
        Assert.Equal(UpdateOrderStatusResult.Success, result);
        var orderInDb = await dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal(OrderStatus.processing, orderInDb.Status);

    }


    [Fact]
    public async Task UpdateOrderStatus_BySellerWithInvalidTransition_ReturnsInvalidStatusTransition()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.pending,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 10,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, sellerId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Seller.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new UpdateOrderStatusRequest(orderId, OrderStatus.in_transit, "update status");
        var result = await orderService.UpdateOrderStatus(request);
        // Assert
        Assert.Equal(UpdateOrderStatusResult.InvalidStatusTransition, result);

        var orderInDb = await dbContext.Orders.FindAsync(order.Id);

        Assert.NotNull(orderInDb);

        Assert.Equal(OrderStatus.pending, orderInDb.Status);

    }

    [Fact]
    public async Task UpdateOrderStatus_ByWrongSeller_ReturnsAuthorizeFailed()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.pending,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 10,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var wrongSellerId = Guid.NewGuid();
        var wrongSeller = new User()
        {
            Id = wrongSellerId,
            Fullname = "12455",
            Username = "1245",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
            Password = "1234",
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, wrongSellerId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Seller.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Users.AddAsync(wrongSeller);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new UpdateOrderStatusRequest(orderId, OrderStatus.in_transit, "update status");
        var result = await orderService.UpdateOrderStatus(request);
        // Assert
        Assert.Equal(UpdateOrderStatusResult.AuthorizeFailed, result);

    }


    [Fact]
    public async Task CancelOrder_ByUserWhenStatusIsPending_ReturnsSuccessAndRestoresStock()
    {
        // Arrange

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.pending,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 8,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new CancelOrderRequest(orderId);
        var result = await orderService.CancelOrder(request);
        Assert.Equal(CancelOrderResult.Success, result);
        var orderInDb = await dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(orderInDb);
        Assert.Equal(OrderStatus.cancelled, orderInDb.Status);
        var itemInDb = await dbContext.Items.FindAsync(item.Id);
        Assert.NotNull(itemInDb);
        Assert.Equal(10, itemInDb.Stock);
        var historyExists =
            await dbContext.OrderHistories.AnyAsync(h => h.OrderId == order.Id && h.Status == OrderStatus.cancelled);
        Assert.True(historyExists);

    }


    [Fact]
    public async Task CancelOrder_WhenStatusIsNotPending_ReturnsNotAllowed()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.in_transit,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 8,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new CancelOrderRequest(orderId);
        var result = await orderService.CancelOrder(request);
        Assert.Equal(CancelOrderResult.NotAllowed, result);
    }


    [Fact]
    public async Task RequestReturn_WhenOrderStatusIsDelivered_ReturnsSuccess()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.delivered,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 8,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new RequestReturnRequest(orderId, "test reason");
        var result = await orderService.RequestReturn(request);
        Assert.Equal(RequestReturnResult.Success, result);
        ;
        ;
        var returnRequestInDb = await dbContext.ReturnRequests.FirstOrDefaultAsync();
        Assert.NotNull(returnRequestInDb);
        Assert.Equal(order.Id, returnRequestInDb.OrderId);
        Assert.Equal(RequestStatus.Pending, returnRequestInDb.Status);

    }


    [Fact]
    public async Task RequestReturn_WhenOrderStatusIsNotDelivered_ReturnsNotAllowed()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockShippingService = new Mock<IShippingService>();
        var mockNotificationHub = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockNotificationHub.Setup(hub => hub.Clients).Returns(mockClients.Object);
        mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var sellerId = Guid.NewGuid();
        var seller = new User()
        {
            Id = sellerId,
            Username = "testUser1",
            Password = "123323",
            Fullname = "Test User13",
            PhoneNumber = "12345678933",
            UserRoles = UserRole.Seller,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            UserId = sellerId,
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.in_transit,
            UserId = userId,
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "Item1",
            Price = 10000,
            Stock = 8,
            ShopId = shop.Id,
            RowVersion = [],
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            Quantity = 2,
            Item = item,
            ShopId = shop.Id,
        };
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddAsync(item);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();

        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockNotificationHub.Object,
            mockShippingService.Object
        );

        var request = new RequestReturnRequest(orderId, "test reason");
        var result = await orderService.RequestReturn(request);
        Assert.Equal(RequestReturnResult.NotAllowed, result);
        var returnRequestCount = await dbContext.ReturnRequests.CountAsync();
        Assert.Equal(0, returnRequestCount);
    }


    [Fact]
    public async Task GetOrderHistoryAsync_OrderExists_ReturnsCorrectAndOrderedHistory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "1245",
            Password = "!234",
            Fullname = "1234rf",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var orderId = Guid.NewGuid();
        var order = new Order()
        {
            Id = orderId,
            Status = OrderStatus.delivered,
            UserId = userId,
        };
        var history1 = new OrderHistory
            { OrderId = orderId, Status = OrderStatus.pending, CreatedDate = DateTime.UtcNow.AddMinutes(1) };
        var history2 = new OrderHistory
            { OrderId = orderId, Status = OrderStatus.processing, CreatedDate = DateTime.UtcNow.AddMinutes(5) };
        var history3 = new OrderHistory
            { OrderId = orderId, Status = OrderStatus.cancelled, CreatedDate = DateTime.UtcNow.AddMinutes(3) };
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderHistories.AddRangeAsync(history1, history2, history3);
        await dbContext.SaveChangesAsync();
        var orderService = new OrderService(null, dbContext, null, null);
        var result = await orderService.GetOrderHistoryAsync(orderId);
        var historyList = result.ToList();
        Assert.NotNull(result);
        Assert.Equal(3, historyList.Count);
        Assert.Equal(history1.Status, historyList[0].Status);
        Assert.Equal(history3.Status, historyList[1].Status);
        Assert.Equal(history2.Status, historyList[2].Status);

    }


    [Fact]
    public async Task GetOrderHistoryAsync_OrderWithoutHistory_ReturnsEmptyList()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var nonExistentOrderId = Guid.NewGuid(); 

        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
    
        var orderService = new OrderService(null, dbContext, null, null);
 
        var result = await orderService.GetOrderHistoryAsync(nonExistentOrderId);
 
        Assert.NotNull(result);
 
        Assert.Empty(result); 

        connection.Close();
    }

    [Fact]
    public async Task ProcessPayoutForSuccessfulOrder_WhenOrderIsCompleted_ReturnsTrueAndUpdateSellerBalance()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var userId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var initialBalance = 100000;
        var user = new User()
        {
            Id = userId,
            Username = "testUser",
            Password = "1233",
            Fullname = "Test User",
            PhoneNumber = "123456789",
            UserRoles = UserRole.Customer,
            Birthday = new DateTime(1990, 1, 1),
            Gender = "Female",
        };
        var seller = new User { Id = sellerId, Username = "seller", Balance = initialBalance, Password = "1244", Fullname = "123233"};
        var shop = new Shop { Id = shopId, UserId = sellerId, Name = "Seller's Shop" };
        var item1 = new Item { Id = Guid.NewGuid(), Name = "Item1", Price = 10000, Stock = 8, ShopId = shopId , RowVersion = []};
        var item2 = new Item { Id = Guid.NewGuid(), Name = "Item2", Price = 20000, Stock = 8, ShopId = shopId , RowVersion = []};
       
        var order = new Order { Id = orderId, Status = OrderStatus.completed, UserId = userId};
       
        var orderItem1 = new OrderItem { OrderId = orderId, ShopId = shopId, ItemId = item1.Id, Price = 10000, Quantity = 1 };
        var orderItem2 = new OrderItem { OrderId = orderId, ShopId = shopId, ItemId = item2.Id, Price = 20000, Quantity = 2 };

       
        await using var dbContext = new ApiDbContext(options);
        dbContext.Database.EnsureCreated();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(seller);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.Items.AddRangeAsync(item1, item2);
        await dbContext.OrderItems.AddRangeAsync(orderItem1, orderItem2);
        await dbContext.SaveChangesAsync();
        var orderService = new OrderService(null, dbContext, null, null);
        var request = new ProcessPayoutForSuccessfulOrderRequest(orderId);
     
        var result = await orderService.ProcessPayoutForSuccessfulOrder(request);
        
        Assert.True(result); 
        
        var sellerInDb = await dbContext.Users.FindAsync(sellerId);
        Assert.NotNull(sellerInDb);
        var expectedPayout = (10000 * 1) + (20000 * 2); 
        Assert.Equal(initialBalance + expectedPayout, sellerInDb.Balance);
        var transactionExists = await dbContext.Transactions
            .AnyAsync(t => t.UserId == sellerId && t.RelatedOrderId == orderId && t.Amount == expectedPayout);
        Assert.True(transactionExists);

        connection.Close();

}



[Fact]

public async Task ProcessPayoutForSuccessfulOrder_WhenOrderIsNotCompleted_ReturnsFalse()
{
     
    var userId = Guid.NewGuid();
    var user = new User()
    {
        Id = userId,
        Username = "testUser",
        Password = "1233",
        Fullname = "Test User",
        PhoneNumber = "123456789",
        UserRoles = UserRole.Customer,
        Birthday = new DateTime(1990, 1, 1),
        Gender = "Female",
    };
    var orderId = Guid.NewGuid();
    var order = new Order { Id = orderId, Status = OrderStatus.pending, UserId = userId};

    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
    await using var dbContext = new ApiDbContext(options);
    dbContext.Database.EnsureCreated();
    await dbContext.Users.AddAsync(user);
    await dbContext.Orders.AddAsync(order);
    await dbContext.SaveChangesAsync();

    var orderService = new OrderService(null, dbContext, null, null);
    var request = new ProcessPayoutForSuccessfulOrderRequest(orderId);

    // --- Act ---
    var result = await orderService.ProcessPayoutForSuccessfulOrder(request);

    // --- Assert ---
    Assert.False(result);

}

}

