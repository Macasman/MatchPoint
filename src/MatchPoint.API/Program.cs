using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatchPoint.Infrastructure.Persistence;
using MatchPoint.Application.Interfaces;
using MatchPoint.Infrastructure.Repositories;
using System.Reflection;
using MatchPoint.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MatchPoint.Infrastructure.Abstractions;
using System.Data.Entity.Infrastructure;


var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration["Jwt:Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!))
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// DI - Infrastructure
builder.Services.AddSingleton<SqlDbContext>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));
builder.Services.AddSingleton<IPaymentIntentRepository, PaymentIntentRepository>();
builder.Services.AddSingleton<IKycVerificationRepository, KycVerificationRepository>();
builder.Services.AddSingleton<IAuditRepository, AuditRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MatchPoint.Application.Resources.Commands.CreateResourceCommand).Assembly));
builder.Services.AddSingleton<IResourceRepository, ResourceRepository>();
builder.Services.AddSingleton<ISqlConnectionFactory, MatchPoint.Infrastructure.Abstractions.SqlConnectionFactory>(); // sua implementação concreta

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
