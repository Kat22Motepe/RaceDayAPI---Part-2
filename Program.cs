using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.API.Auth;
using RaceDay.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers and the Bouncer (Auth Filter)
builder.Services.AddScoped<SessionAuthFilter>();
builder.Services.AddControllers(options =>
    options.Filters.AddService<SessionAuthFilter>());

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "The API for the RaceDay event management platform."
    });

    // creates Authorize button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your session token here."
    });

   
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// 3. Connect the Database
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// 4. Turn on Swagger (only when running locally)
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
