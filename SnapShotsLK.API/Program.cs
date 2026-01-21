using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Add Services (Services එකතු කිරීම) ---

// 1. Add CORS Policy (Frontend එකට අවසර දීම)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()  // ඕනෑම තැනක ඉඳන් එන්න දෙන්න
            .AllowAnyMethod()  // ඕනෑම දෙයක් කරන්න දෙන්න (GET, POST)
            .AllowAnyHeader()); // ඕනෑම විස්තරයක් එවන්න දෙන්න
});

// Controllers පාවිච්චි කරන්න (අපි හදන API වලට)
builder.Services.AddControllers();

// Swagger (API Testing Interface) එක සඳහා
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Connection එක
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- 2. Configure Pipeline (Pipeline එක සැකසීම) ---

// Development කාලෙදි Swagger පෙන්නන්න
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// 2. Use CORS (මේක අනිවාර්යයෙන් දාන්න)
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

// අපේ Controllers (AuthController වගේ ඒවා) වැඩ කරවන්න
app.MapControllers();

app.Run();