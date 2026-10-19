using Microsoft.EntityFrameworkCore;
using RaceDay.Infrastructure;
using RaceDay.API.Auth;

var builder = WebApplication.CreateBuilder(args);

// API to use Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<SessionAuthFilter>();
builder.Services.AddControllers(options =>
    options.Filters.AddService<SessionAuthFilter>());

// Add Swagger (the menu of endpoints)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connect the Database
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Turn on Swagger (only when running locally)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


public partial class Program { }
