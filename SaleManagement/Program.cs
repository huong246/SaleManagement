using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaleManagement.Data;
using SaleManagement.Services;

var builder = WebApplication.CreateBuilder(args); //tao ra mot trinh xay dung ung dung web

 
var configuration = builder.Configuration; // lay ra doi tuong cau hinh, cho phep truy cap vao file
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "SaleManagement.db"); // tao duong noi den co so du lieu
var connectionString = $"Data Source={dbPath}";
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionString)); // dang ky ApiDbContext
builder.Services.AddControllers();
 
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); // giup swagger kham pha va hien thi cac API endpoint
//them bao mat vao swagger. tao nut authorize tren giao dien swagger cho phep nguoi dung nhap vao 1 jwt de xac thuc
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
       
    });
}); 
//phan xac thuc nguoi dung bang bearer token
builder.Services.AddAuthentication(options =>
{
    
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = configuration["Jwt:Audience"],
        ValidIssuer = configuration["Jwt:Issuer"],

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))

    };
});

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICartItemService, CartItemService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISellerRequestService, SellerRequestService>();
builder.Services.AddScoped<ICategoryService, CategoryService > ();
builder.Services.AddScoped<ISuggestionService, SuggestionService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<IMomoPaymentService, MomoService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IItemImageService, ItemImageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHostedService<VoucherUpdateStatusService>();
builder.Services.AddMemoryCache();

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor(); //dang ky cho phep truy cap vao HttpContext

var app = builder.Build();
//tu dong cap nhat co so du lieu database migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}
 
if (app.Environment.IsDevelopment())
{   app.UseSwagger();
    app.UseSwaggerUI();
   
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.UseMiddleware<JwtBlacklistMiddleware>(); 
app.MapControllers();
app.MapHub<SaleManagement.Hubs.NotificationHub>("/notificationHub");
app.MapHub<SaleManagement.Hubs.ChatHub>("/chatHub");

app.Run();