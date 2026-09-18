using AccessRequestHub.Data;
using AccessRequestHub.Middleware;
using AccessRequestHub.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Configure Swagger with Authentication Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AccessRequestHub.Api",
        Version = "v1"
    });

    // Add custom header for simulated auth
    c.AddSecurityDefinition("X-Current-User-Id", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Current-User-Id",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "apiKey",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Simulated User ID. Use these demo users:\n- Alice (Requester): 11111111-1111-1111-1111-111111111111\n- Bob (Manager): 22222222-2222-2222-2222-222222222222\n- Carol (System Owner CRM): 33333333-3333-3333-333333333333\n- Dana (System Owner Finance): 44444444-4444-444444444444\n- Erin (Admin): 55555555-5555-5555-555555555555"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "X-Current-User-Id"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7121", "https://localhost:7200", "https://localhost:7100")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();