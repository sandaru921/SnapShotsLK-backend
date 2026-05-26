using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()  
            .AllowAnyMethod()  
            .AllowAnyHeader()); 
});

// Controllers 
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Swagger (API Testing Interface) 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Connection 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("JwtSettings:Key").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

var app = builder.Build();

// ─────────────────────────────────────────────────────────
//  Seed SuperAdmin on startup (only once, if not exists)
// ─────────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // Apply any pending migrations

    const string superAdminEmail = "superadmin@snapshotslk.com";
    if (!db.Users.Any(u => u.Role == "superadmin"))
    {
        db.Users.Add(new User
        {
            Email = superAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123"),
            Role = "superadmin",
            Name = "Super Admin",
            IsApproved = true
        });
        db.SaveChanges();
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║        SUPERADMIN ACCOUNT CREATED        ║");
        Console.WriteLine($"║  Email:    {superAdminEmail,-30}║");
        Console.WriteLine("║  Password: SuperAdmin@123                ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARN] Migration/Seed skipped: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();