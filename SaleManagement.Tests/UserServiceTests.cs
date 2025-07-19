using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;
using SaleManagement.Services;
using Xunit;

namespace SaleManagement.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetUserProfileAsync_UserExists_ReturnsUserProfile()
    {
        // Arrange
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

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetUserProfileAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Fullname, result.FullName);
    }

    [Fact]
    public async Task GetUserProfileAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetUserProfileAsync();
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_UserExists_ReturnsUpdateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testUser" , Password = "12334"};
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateProfileRequest("Updated Full Name", "0355812360", new DateTime(1990, 1, 2), "Male");
        
        // Act
        var result = await userService.UpdateUserProfileAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateUserProfileResult.Success, result);
        var updatedUserInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUserInDb);
        Assert.Equal(updateRequest.FullName, updatedUserInDb.Fullname);
        Assert.Equal(updateRequest.PhoneNumber, updatedUserInDb.PhoneNumber);
        Assert.Equal(updateRequest.Birthday, updatedUserInDb.Birthday);
        Assert.Equal(updateRequest.Gender, updatedUserInDb.Gender);
    }
    
    [Fact]
    public async Task UpdateUserProfileAsync_UserDoesNotExist_ReturnsUserNotFound()
    {
        // Arrange
        var nonExistUserId = Guid.NewGuid().ToString();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, nonExistUserId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);

        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateProfileRequest("Any name", null, null, null);
        
        // Act
        var result = await userService.UpdateUserProfileAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateUserProfileResult.UserNotFound, result);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_TokenInvalid_returnTokenInvalid()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>(); // No NameIdentifier claim
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateProfileRequest("Any name", null, null, null);
        
        // Act
        var result = await userService.UpdateUserProfileAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateUserProfileResult.TokenInvalid, result);
    }

    [Fact]
    public async Task GetAddressAsync_UserWithAddresses_ReturnAddressList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var address = new UserAddress { Id = Guid.NewGuid(), Name = "Test Address", UserId = userId };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserAddresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetAddressesAsync();
        
        // Assert
        var resultList = result.ToList();
        var firstAddress = Assert.Single(resultList);
        Assert.Equal(address.Id, firstAddress.Id);
        Assert.Equal(address.Name, firstAddress.Name);
    }

    [Fact]
    public async Task GetAddressAsync_UserWithoutAddress_ReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetAddressesAsync();
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetAddressAsync_TokenInvalid_ReturnEmptyList()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetAddressesAsync();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAddressAsync_UserNotFound_ReturnEmptyList()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.GetAddressesAsync();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAddressAsync_ValidRequest_ReturnNewAddressDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var addressRequest = new CreateAddressRequest("Test Address", 10.1, 20.2, false);
        
        // Act
        var result = await userService.CreateAddressAsync(addressRequest);
        
        // Assert
        Assert.NotNull(result);
        var addressInDb = await dbContext.UserAddresses.FirstOrDefaultAsync(a => a.UserId == userId);
        Assert.NotNull(addressInDb);
        Assert.Equal(addressRequest.Name, addressInDb.Name);
        Assert.Equal(addressRequest.Latitude, addressInDb.Latitude);
        Assert.Equal(addressRequest.Longitude, addressInDb.Longitude);
        Assert.False(addressInDb.IsDefault);
    }
    
    [Fact]
    public async Task CreateAddressAsync_UserNotFound_ReturnNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var addressRequest = new CreateAddressRequest("Test Address", 0, 0, false);
        
        // Act
        var result = await userService.CreateAddressAsync(addressRequest);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task CreateAddressAsync_TokenInvalid_ReturnNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var addressRequest = new CreateAddressRequest("Test Address", 0, 0, false);
        
        // Act
        var result = await userService.CreateAddressAsync(addressRequest);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAddressAsync_IsDefaultIsTrue_UpdateOldDefaultAddress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var oldDefaultAddress = new UserAddress { Id = Guid.NewGuid(),Name = "test nha", UserId = userId, IsDefault = true };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserAddresses.AddAsync(oldDefaultAddress);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var addressRequest = new CreateAddressRequest("New Default Address", 0, 0, true);
        
        // Act
        var result = await userService.CreateAddressAsync(addressRequest);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsDefault);
        
        var oldAddressInDb = await dbContext.UserAddresses.FindAsync(oldDefaultAddress.Id);
        Assert.NotNull(oldAddressInDb);
        Assert.False(oldAddressInDb.IsDefault);
    }
    
    [Fact]
    public async Task UpdateAddressAsync_AddressExists_ReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var addressId = Guid.NewGuid();
        var address = new UserAddress() { Id = addressId, Name = "Old Name", UserId = userId };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserAddresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateAddressRequest(addressId, "New Name", 1, 1, false);
        
        // Act
        var result = await userService.UpdateAddressAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateAddressResult.Success, result);
        var updatedAddressInDb = await dbContext.UserAddresses.FindAsync(addressId);
        Assert.NotNull(updatedAddressInDb);
        Assert.Equal(updateRequest.Name, updatedAddressInDb.Name);
    }
    
    [Fact]
    public async Task UpdateAddressAsync_AddressNotFound_returnsAddressNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateAddressRequest(Guid.NewGuid(), "Any Name", 0, 0, false);
        
        // Act
        var result = await userService.UpdateAddressAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateAddressResult.AdressNotFound, result);
    }
    
    [Fact]
    public async Task UpdateAddressAsync_TokenInvalid_returnTokenInvalid()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateAddressRequest(Guid.NewGuid(), "Any Name", 0, 0, false);
        
        // Act
        var result = await userService.UpdateAddressAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateAddressResult.TokenInvalid, result);
    }

    [Fact]
    public async Task UpdateAddressAsync_SetAsDefault_ReturnsSuccessAndUpdateOldDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var oldDefaultAddress = new UserAddress { Id = Guid.NewGuid(),Name = "test nha", UserId = userId, IsDefault = true };
        var newDefaultAddress = new UserAddress { Id = Guid.NewGuid(),Name = "test nha233", UserId = userId, IsDefault = false };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserAddresses.AddRangeAsync(oldDefaultAddress, newDefaultAddress);
        await dbContext.SaveChangesAsync();
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var updateRequest = new UpdateAddressRequest(newDefaultAddress.Id, "New Default", 0, 0, true);
        
        // Act
        var result = await userService.UpdateAddressAsync(updateRequest);
        
        // Assert
        Assert.Equal(UpdateAddressResult.Success, result);
        var updatedNewAddress = await dbContext.UserAddresses.FindAsync(newDefaultAddress.Id);
        Assert.NotNull(updatedNewAddress);
        Assert.True(updatedNewAddress.IsDefault);
        
        var updatedOldAddress = await dbContext.UserAddresses.FindAsync(oldDefaultAddress.Id);
        Assert.NotNull(updatedOldAddress);
        Assert.False(updatedOldAddress.IsDefault);
    }

    [Fact]
    public async Task DeleteAddressAsync_AddressExists_ReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var addressId = Guid.NewGuid();
        var address = new UserAddress() { Id = addressId,Name = "test nha", UserId = userId };
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);
        
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.UserAddresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var deleteRequest = new DeleteAddressRequest(addressId);
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.DeleteAddressAsync(deleteRequest);
        
        // Assert
        Assert.Equal(DeleteAddressResult.Success, result);
        var addressInDb = await dbContext.UserAddresses.FindAsync(addressId);
        Assert.Null(addressInDb);
    }

    [Fact]
    public async Task DeleteAddressAsync_AddressNotFound_ReturnsAddressNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "12hh3", Password = "12234"};
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var deleteRequest = new DeleteAddressRequest(Guid.NewGuid());
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        
        // Act
        var result = await userService.DeleteAddressAsync(deleteRequest);
        
        // Assert
        Assert.Equal(DeleteAddressResult.AddressNotFound, result);
    }
    
    [Fact]
    public async Task DeleteAddressAsync_UserNotFound_ReturnsUserNotFound()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var deleteRequest = new DeleteAddressRequest(Guid.NewGuid());
        
        // Act
        var result = await userService.DeleteAddressAsync(deleteRequest);
        
        // Assert
        Assert.Equal(DeleteAddressResult.UserNotFound, result);
    }

    [Fact]
    public async Task DeleteAddressAsync_TokenInvalid_ReturnsTokenInvalid()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(claimsPrincipal);

        var options = new DbContextOptionsBuilder<ApiDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        await using var dbContext = new ApiDbContext(options);
        
        var userService = new UserService(dbContext, mockHttpContextAccessor.Object);
        var deleteRequest = new DeleteAddressRequest(Guid.NewGuid());
        
        // Act
        var result = await userService.DeleteAddressAsync(deleteRequest);
        
        // Assert
        Assert.Equal(DeleteAddressResult.TokenInvalid, result);
    }
}