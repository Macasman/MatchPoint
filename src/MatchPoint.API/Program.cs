using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Infra & App
using MatchPoint.Infrastructure.Persistence;           // seu SqlDbContext
using MatchPoint.Infrastructure.Messaging;
using MatchPoint.Application.Interfaces;
using MatchPoint.Infrastructure.Repositories;
using MatchPoint.Infrastructure.Logging;
using MatchPoint.Application.Logging;
using MatchPoint.API.Filters;
using MatchPoint.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ==== Mongo Audit Options ====
var auditSection = builder.Configuration.GetSection("MongoAuditLog");
var auditOptions = auditSection.Get<MongoAuditLogOptions>() ?? new MongoAuditLogOptions();
builder.Services.AddSingleton(auditOptions);

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
// (uma única chamada já com o filtro de auditoria)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==== Messaging ====
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// ==== Audit Log (Mongo) ====
builder.Services.AddSingleton<IAuditLogService, MongoAuditLogService>();

// ==== MediatR ====
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));

// ==== DB Provider ====
builder.Services.AddSingleton<SqlDbContext>(); // conforme seu padrão atual

// ==== Repositories ====
builder.Services.AddSingleton<IResourceRepository, ResourceRepository>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<IPaymentIntentRepository, PaymentIntentRepository>();
builder.Services.AddSingleton<IKycVerificationRepository, KycVerificationRepository>();
builder.Services.AddSingleton<IAuditRepository, AuditRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWebhookQueueWriter, WebhookQueueSqlWriter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationAndBufferingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
