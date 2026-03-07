using InstruaMe.Domain.Contracts.Services;
using InstruaMe.Infrastructure.ORM;
using InstruaMe.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "InstruaMe API",
        Version = "v1"
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");

if (string.IsNullOrWhiteSpace(jwtSettings["SecretKey"]))
    throw new InvalidOperationException("JWT SecretKey n�o configurada");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)
            ),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<InstruaMeDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(InstruaMeDbContext).Assembly.FullName);
        }
    );
});

builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<InstruaMe.Services.WebSocketManager>();
builder.Services.AddSingleton<InstruaMe.Services.ChatWebSocketHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Map("/ws/chat/{conversationId:guid}", async (HttpContext context, Guid conversationId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var token = context.Request.Query["token"].ToString();
    if (string.IsNullOrWhiteSpace(token))
    {
        context.Response.StatusCode = 401;
        return;
    }

    var jwtSettings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
    var tokenHandler = new JwtSecurityTokenHandler();
    ClaimsPrincipal principal;
    try
    {
        principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        }, out _);
    }
    catch
    {
        context.Response.StatusCode = 401;
        return;
    }

    var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var role = principal.FindFirstValue(ClaimTypes.Role)!;

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var handler = context.RequestServices.GetRequiredService<InstruaMe.Services.ChatWebSocketHandler>();
    await handler.HandleAsync(conversationId, userId, role, socket, context.RequestAborted);
});

app.MapControllers();

app.Run();
