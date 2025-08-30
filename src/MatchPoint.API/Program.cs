using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

// Infra & App
using MatchPoint.Infrastructure.Persistence;           // SqlDbContext (mesmo usado por Reservations)
using MatchPoint.Infrastructure.Messaging;
using MatchPoint.Application.Interfaces;
using MatchPoint.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ==== Auth (JWT) ====
var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

// ==== MVC / Swagger ====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==== Messaging ====
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// ==== MediatR (registre TODOS os handlers do projeto Application de uma vez) ====
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));

// ==== DB Provider (MESMO usado pelos Reservations) ====
builder.Services.AddSingleton<SqlDbContext>(); // mantém seu padrão atual

// ==== Repositories ====
builder.Services.AddSingleton<IResourceRepository, ResourceRepository>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<IPaymentIntentRepository, PaymentIntentRepository>();
builder.Services.AddSingleton<IKycVerificationRepository, KycVerificationRepository>();
builder.Services.AddSingleton<IAuditRepository, AuditRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();

// 🔴 IMPORTANTE: NÃO registrar ISqlConnectionFactory aqui
// builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();  // REMOVIDO

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
