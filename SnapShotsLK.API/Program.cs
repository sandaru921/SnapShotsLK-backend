using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Add Services (Services එකතු කිරීම) ---

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

app.UseHttpsRedirection();

app.UseAuthorization();

// අපේ Controllers (AuthController වගේ ඒවා) වැඩ කරවන්න
app.MapControllers();

app.Run();